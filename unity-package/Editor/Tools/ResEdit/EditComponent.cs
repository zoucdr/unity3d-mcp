using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles Component-specific operations using dual state tree architecture.
    /// First tree: Target location (using GameObjectSelector)
    /// Second tree: Component operation execution
    /// 对应方法名: edit_component
    /// </summary>
    [ToolName("edit_component", "资源管理")]
    public class EditComponent : DualStateMethodBase
    {
        /// <summary>
        /// 目标查找
        /// </summary>
        private IObjectSelector objectSelector;
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                // 目标查找参数
                new MethodKey("instance_id", "Object InstanceID", false),
                new MethodKey("path", "Object Hierarchy path", false),
                new MethodKey("action", "Operation type: get_component_propertys, set_component_propertys", true),
                new MethodKey("component_type", "Component type name (type name inheriting from Component)", true),
                new MethodKey("properties", "Properties dictionary (set_component_propertys)", false),
            };
        }

        /// <summary>
        /// 创建目标定位状态树（使用GameObjectSelector）
        /// </summary>
        protected override StateTree CreateTargetTree()
        {
            objectSelector = objectSelector ?? new HierarchySelector<GameObject>();
            return objectSelector.BuildStateTree();
        }

        /// <summary>
        /// 创建组件操作执行状态树
        /// </summary>
        protected override StateTree CreateActionTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("get_component_propertys", (Func<StateTreeContext, object>)HandleGetComponentPropertysAction)
                    .Leaf("set_component_propertys", (Func<StateTreeContext, object>)HandleSetComponentPropertysAction)
                    .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultAction)
                .Build();
        }

        /// <summary>
        /// 默认操作处理（当没有指定具体action时）
        /// </summary>
        private object HandleDefaultAction(StateTreeContext args)
        {
            if (args.ContainsKey("properties"))
            {
                return HandleSetComponentPropertysAction(args);
            }
            return Response.Error("Action is required for edit_component. Valid actions are: get_component_propertys, set_component_propertys.");
        }

        /// <summary>
        /// 从执行上下文中提取目标GameObject（单个）
        /// </summary>
        private GameObject ExtractTargetFromContext(StateTreeContext context)
        {
            // 先尝试从ObjectReferences获取
            if (context.TryGetObjectReference("_resolved_targets", out object targetsObj))
            {
                if (targetsObj is GameObject singleGameObject)
                {
                    return singleGameObject;
                }
                else if (targetsObj is GameObject[] gameObjectArray && gameObjectArray.Length > 0)
                {
                    return gameObjectArray[0]; // 只取第一个
                }
                else if (targetsObj is System.Collections.IList list && list.Count > 0)
                {
                    if (list[0] is GameObject go)
                        return go;
                }
            }

            // 不要尝试从JSONNode转换，因为JSONNode不能转为GameObject
            return null;
        }



        #region 组件操作Action Handlers

        /// <summary>
        /// 处理获取组件属性的操作（批量）
        /// </summary>
        private object HandleGetComponentPropertysAction(StateTreeContext args)
        {
            GameObject target = ExtractTargetFromContext(args);
            if (target == null)
            {
                return Response.Error("No target GameObject found in execution context.");
            }

            return GetComponentPropertysFromTarget(args, target);
        }

        /// <summary>
        /// 处理设置组件属性的操作（批量）
        /// </summary>
        private object HandleSetComponentPropertysAction(StateTreeContext args)
        {
            GameObject target = ExtractTargetFromContext(args);
            if (target == null)
            {
                return Response.Error("No target GameObject found in execution context.");
            }

            return SetComponentPropertysOnTarget(args, target);
        }

        #endregion

        #region 组件操作核心方法

        /// <summary>
        /// 获取组件属性的具体实现（批量）
        /// 只收集Inspector面板上可见的字段：public字段和带[SerializeField]特性的私有字段
        /// </summary>
        private object GetComponentPropertysFromTarget(StateTreeContext cmd, GameObject targetGo)
        {
            try
            {
                if (!cmd.TryGetValue("component_type", out object compNameObj) || compNameObj == null)
                {
                    return Response.Error("'component_type' parameter is required.");
                }

                string compName = compNameObj.ToString();

                // 查找组件
                Component targetComponent = FindComponentOnGameObject(targetGo, compName);
                if (targetComponent == null)
                {
                    return Response.Error($"Component '{compName}' not found on '{targetGo.name}'.");
                }

                // 获取组件的所有字段和属性
                Type componentType = targetComponent.GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

                var propertiesDict = new Dictionary<string, object>();

                // 定义要跳过的属性名称（Unity组件快捷访问器和不常用属性）
                var skipProperties = new HashSet<string>
                {
                    "hideFlags", "rigidbody", "rigidbody2D", "camera", "light", "animation",
                    "constantForce", "renderer", "audio", "networkView", "collider", "collider2D",
                    "hingeJoint", "particleSystem", "gameObject", "transform", "tag", "name",
                    "worldToLocalMatrix", "localToWorldMatrix", "isPartOfStaticBatch",
                    // 跳过会导致实例化的属性（避免副作用）
                    "material", "materials", "mesh"  // 使用 sharedMaterial, sharedMaterials, sharedMesh 代替
                };

                // 1. 获取所有公共属性 (Properties)
                PropertyInfo[] properties = componentType.GetProperties(flags);
                if (properties != null)
                {
                    foreach (PropertyInfo prop in properties)
                    {
                        // 跳过黑名单中的属性
                        if (skipProperties.Contains(prop.Name)) continue;

                        // 只获取可读的、可写的属性（排除只读属性和索引器）
                        if (prop.CanRead && prop.CanWrite && !prop.GetIndexParameters().Any())
                        {
                            try
                            {
                                object value = prop.GetValue(targetComponent, null);
                                propertiesDict[prop.Name] = ConvertToSerializableValue(value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[GetComponentPropertysFromTarget] Failed to get property '{prop.Name}': {ex.Message}");
                                // 不添加错误信息到结果中
                            }
                        }
                    }
                }

                // 2. 获取所有字段 (Fields) - 包括 SerializeField
                FieldInfo[] fields = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fields != null)
                {
                    foreach (FieldInfo field in fields)
                    {
                        // 检查字段是否可序列化
                        // 条件1: public字段
                        // 条件2: 带有SerializeField特性的非public字段
                        bool isSerializable = field.IsPublic ||
                                             field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;

                        if (isSerializable && !propertiesDict.ContainsKey(field.Name)) // 避免重复
                        {
                            try
                            {
                                object value = field.GetValue(targetComponent);
                                propertiesDict[field.Name] = ConvertToSerializableValue(value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[GetComponentPropertysFromTarget] Failed to get field '{field.Name}': {ex.Message}");
                                // 不添加错误信息到结果中
                            }
                        }
                    }
                }

                // 如果没有任何可访问的属性或字段
                if (propertiesDict.Count == 0)
                {
                    return Response.Success(
                        $"Component '{compName}' has no accessible properties or fields.",
                        new Dictionary<string, object>
                        {
                            { "component_type", compName },
                            { "gameobject_name", targetGo.name },
                            { "properties", "" }  // 空YAML
                        }
                    );
                }

                // 转换为YAML格式
                string propertiesYaml = ConvertDictionaryToYaml(propertiesDict);
                return Response.Success(
                    $"Retrieved {propertiesDict.Count} serializable fields from component '{compName}' on '{targetGo.name}'.",
                    new Dictionary<string, object>
                    {
                        { "component_type", compName },
                        { "gameobject_name", targetGo.name },
                        { "properties", propertiesYaml }  // 直接用YAML格式
                    }
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetComponentPropertysFromTarget] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return Response.Error($"Failed to get component properties: {ex.Message}");
            }
        }


        /// <summary>
        /// 设置组件属性的具体实现（批量）
        /// </summary>
        private object SetComponentPropertysOnTarget(StateTreeContext cmd, GameObject targetGo)
        {
            if (!cmd.TryGetValue("component_type", out object compNameObj) || compNameObj == null)
            {
                return Response.Error("'component_type' parameter is required.");
            }

            string compName = compNameObj.ToString();

            // 查找组件
            Component targetComponent = FindComponentOnGameObject(targetGo, compName);
            if (targetComponent == null)
            {
                return Response.Error($"Component '{compName}' not found on '{targetGo.name}'.");
            }

            // 获取要设置的属性字典
            if (!cmd.TryGetValue("properties", out object propertiesObj) || propertiesObj == null)
            {
                return Response.Error("'properties' parameter is required for setting component properties.");
            }

            JsonClass propertiesToSet = null;
            if (propertiesObj is JsonClass jObj)
            {
                propertiesToSet = jObj;
            }
            else
            {
                // 尝试从其他格式转换
                try
                {
                    propertiesToSet = Json.FromObject(propertiesObj) as JsonClass;
                }
                catch (Exception ex)
                {
                    return Response.Error($"Failed to parse 'properties' parameter: {ex.Message}");
                }
            }

            if (propertiesToSet == null || propertiesToSet.Count == 0)
            {
                return Response.Error("'properties' dictionary cannot be empty.");
            }

            Undo.RecordObject(targetComponent, "Set Component Properties");

            var results = new Dictionary<string, object>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (var prop in propertiesToSet.AsEnumerable())
            {
                string propName = prop.Key;
                JsonNode propValue = prop.Value;

                try
                {
                    if (SetComponentProperty(targetComponent, propName, propValue, out string error))
                    {
                        results[propName] = "Success";
                        successCount++;
                    }
                    else
                    {
                        results[propName] = "Failed";
                        // Error already logged in SetComponentProperty
                        errors.Add($"Property '{propName}': {error}");
                    }
                }
                catch (Exception e)
                {
                    results[propName] = $"Error: {e.Message}";
                    errors.Add($"Property '{propName}': {e.Message}");
                    Debug.LogError($"[SetComponentPropertysOnTarget] Unexpected exception for property '{propName}': {e.Message}");
                }
            }

            EditorUtility.SetDirty(targetComponent);

            string message = $"Set {successCount} of {propertiesToSet.Count} properties on component '{compName}' on '{targetGo.name}'.";
            var responseData = new Dictionary<string, object>
            {
                { "component_type", compName },
                { "total_properties", propertiesToSet.Count },
                { "successful_properties", successCount },
                { "failed_properties", propertiesToSet.Count - successCount },
                { "results", Json.FromObject(results) }
            };

            if (errors.Count > 0)
            {
                // 转换为数组以确保序列化正确
                responseData["errors"] = errors.ToArray();
            }

            if (successCount > 0)
            {
                return Response.Success(message, Json.FromObject(responseData));
            }
            else
            {
                return Response.Error($"Failed to set any properties on component '{compName}'.", Json.FromObject(responseData));
            }
        }



        #endregion

        #region 组件辅助方法

        /// <summary>
        /// 在GameObject上查找组件
        /// </summary>
        private Component FindComponentOnGameObject(GameObject gameObject, string componentName)
        {
            Type componentType = FindComponentType(componentName);
            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
            {
                return gameObject.GetComponent(componentType);
            }
            return null;
        }

        /// <summary>
        /// 将值转换为可序列化的格式
        /// </summary>
        private object ConvertToSerializableValue(object value)
        {
            if (value == null) return null;

            Type valueType = value.GetType();

            // 基本类型直接返回
            if (valueType.IsPrimitive || value is string || value is decimal)
            {
                return value;
            }

            // Unity基本类型转换
            if (value is Vector2 v2)
                return new { x = v2.x, y = v2.y };
            if (value is Vector3 v3)
                return new { x = v3.x, y = v3.y, z = v3.z };
            if (value is Vector4 v4)
                return new { x = v4.x, y = v4.y, z = v4.z, w = v4.w };
            if (value is Quaternion q)
                return new { x = q.x, y = q.y, z = q.z, w = q.w };
            if (value is Color c)
                return new { r = c.r, g = c.g, b = c.b, a = c.a };
            if (value is Rect rect)
                return new { x = rect.x, y = rect.y, width = rect.width, height = rect.height };

            // Unity Object引用
            if (value is UnityEngine.Object unityObj)
            {
                // Unity的fake null检查：被销毁的对象不是真正的null
                if (unityObj == null || !unityObj)
                    return null;

                try
                {
                    return new
                    {
                        name = unityObj.name ?? "<null>",
                        type = unityObj.GetType()?.Name ?? "<unknown>",
                        instanceID = unityObj.GetInstanceID()
                    };
                }
                catch
                {
                    return "<destroyed Unity Object>";
                }
            }

            // 枚举类型
            if (valueType.IsEnum)
            {
                return value.ToString();
            }

            // 数组或列表
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var list = new List<object>();
                try
                {
                    foreach (var item in enumerable)
                    {
                        // 安全地转换每个元素，防止空引用
                        try
                        {
                            list.Add(ConvertToSerializableValue(item));
                        }
                        catch (Exception ex)
                        {
                            list.Add($"<Error: {ex.Message}>");
                        }

                        if (list.Count > 10) // 限制数组长度避免过大
                        {
                            list.Add("...(truncated)");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"<Enumeration Error: {ex.Message}>";
                }
                return list.ToArray();
            }

            // 复杂对象，尝试序列化为字符串
            try
            {
                return value.ToString();
            }
            catch
            {
                return $"<{valueType.Name}>";
            }
        }

        /// <summary>
        /// 将Dictionary转换为YAML格式字符串
        /// </summary>
        private string ConvertDictionaryToYaml(Dictionary<string, object> dict, int indent = 0)
        {
            if (dict == null || dict.Count == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            string indentStr = new string(' ', indent);

            foreach (var kvp in dict)
            {
                sb.Append(indentStr);
                sb.Append(kvp.Key);
                sb.Append(": ");

                if (kvp.Value == null)
                {
                    sb.AppendLine("null");
                }
                else if (kvp.Value is string str)
                {
                    // 多行字符串
                    if (str.Contains("\n"))
                    {
                        sb.AppendLine("|");
                        foreach (var line in str.Split('\n'))
                        {
                            sb.Append(indentStr);
                            sb.Append("  ");
                            sb.AppendLine(line.TrimEnd());
                        }
                    }
                    else
                    {
                        sb.AppendLine(str);
                    }
                }
                else if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    // 嵌套字典
                    sb.AppendLine();
                    sb.Append(ConvertDictionaryToYaml(nestedDict, indent + 2));
                }
                else if (kvp.Value is System.Collections.IList list)
                {
                    // 数组
                    if (list.Count == 0)
                    {
                        sb.AppendLine("[]");
                    }
                    else
                    {
                        sb.AppendLine();
                        foreach (var item in list)
                        {
                            sb.Append(indentStr);
                            sb.Append("  - ");
                            if (item is Dictionary<string, object> itemDict)
                            {
                                sb.AppendLine();
                                sb.Append(ConvertDictionaryToYaml(itemDict, indent + 4));
                            }
                            else
                            {
                                sb.AppendLine(item?.ToString() ?? "null");
                            }
                        }
                    }
                }
                else
                {
                    // 其他类型转字符串
                    sb.AppendLine(kvp.Value.ToString());
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取组件属性值
        /// </summary>
        private object GetComponentProperty(Component component, string propertyName)
        {
            Type type = component.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            try
            {
                // 处理嵌套属性（点符号分隔）
                if (propertyName.Contains('.') || propertyName.Contains('['))
                {
                    return GetNestedProperty(component, propertyName);
                }

                PropertyInfo propInfo = type.GetProperty(propertyName, flags);
                if (propInfo != null && propInfo.CanRead)
                {
                    return propInfo.GetValue(component);
                }
                else
                {
                    FieldInfo fieldInfo = type.GetField(propertyName, flags);
                    if (fieldInfo != null)
                    {
                        return fieldInfo.GetValue(component);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetComponentProperty] Failed to get '{propertyName}' from {type.Name}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 获取嵌套属性值
        /// </summary>
        private object GetNestedProperty(object target, string path)
        {
            try
            {
                string[] pathParts = SplitPropertyPath(path);
                if (pathParts.Length == 0) return null;

                object currentObject = target;
                Type currentType = currentObject.GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

                for (int i = 0; i < pathParts.Length; i++)
                {
                    string part = pathParts[i];
                    bool isArray = false;
                    int arrayIndex = -1;

                    if (part.Contains("["))
                    {
                        int startBracket = part.IndexOf('[');
                        int endBracket = part.IndexOf(']');
                        if (startBracket > 0 && endBracket > startBracket)
                        {
                            string indexStr = part.Substring(startBracket + 1, endBracket - startBracket - 1);
                            if (int.TryParse(indexStr, out arrayIndex))
                            {
                                isArray = true;
                                part = part.Substring(0, startBracket);
                            }
                        }
                    }

                    PropertyInfo propInfo = currentType.GetProperty(part, flags);
                    FieldInfo fieldInfo = null;
                    if (propInfo == null)
                    {
                        fieldInfo = currentType.GetField(part, flags);
                        if (fieldInfo == null) return null;
                    }

                    currentObject = propInfo != null ? propInfo.GetValue(currentObject) : fieldInfo.GetValue(currentObject);
                    if (currentObject == null) return null;

                    if (isArray)
                    {
                        if (currentObject is System.Collections.IList list)
                        {
                            if (arrayIndex < 0 || arrayIndex >= list.Count) return null;
                            currentObject = list[arrayIndex];
                        }
                        else return null;
                    }

                    currentType = currentObject.GetType();
                }

                return currentObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetNestedProperty] Error getting nested property '{path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Helper to set a property or field via reflection, handling basic types.
        /// </summary>
        private bool SetComponentProperty(object target, string memberName, JsonNode value, out string error)
        {
            Type type = target.GetType();
            BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            string targetName = GetSafeObjectName(target);

            try
            {
                // Handle special case for materials with dot notation (material.property)
                // Examples: material.color, sharedMaterial.color, materials[0].color
                if (memberName.Contains('.') || memberName.Contains('['))
                {
                    return SetNestedProperty(target, memberName, value, out error);
                }

                // Try to find and set a field first
                FieldInfo fieldInfo = type.GetField(memberName, flags);
                if (fieldInfo != null)
                {
                    object convertedValue = ConvertJTokenToType(value, fieldInfo.FieldType);
                    if (convertedValue != null || fieldInfo.FieldType.IsClass)
                    {
                        fieldInfo.SetValue(target, convertedValue);
                        error = null;
                        return true;
                    }
                    else
                    {
                        error = $"Failed to convert value for '{memberName}' on {targetName} ({type.Name})";
                        return false;
                    }
                }

                // If field not found, try to find and set a property
                PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
                if (propertyInfo != null)
                {
                    if (!propertyInfo.CanWrite)
                    {
                        error = $"Property '{memberName}' on {targetName} ({type.Name}) is read-only";
                        return false;
                    }

                    object convertedValue = ConvertJTokenToType(value, propertyInfo.PropertyType);
                    if (convertedValue != null || propertyInfo.PropertyType.IsClass)
                    {
                        propertyInfo.SetValue(target, convertedValue);
                        error = null;
                        return true;
                    }
                    else
                    {
                        error = $"Failed to convert value for '{memberName}' on {targetName} ({type.Name})";
                        return false;
                    }
                }

                error = $"Field or Property '{memberName}' not found on {targetName} ({type.Name})";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Failed to set '{memberName}' on {targetName} ({type.Name}): {ex.Message}";
                Debug.LogError($"[SetComponentProperty] {error}");
            }
            return false;
        }

        /// <summary>
        /// Sets a nested property using dot notation (e.g., "material.color") or array access (e.g., "materials[0]")
        /// </summary>
        private bool SetNestedProperty(object target, string path, JsonNode value, out string error)
        {
            string targetName = GetSafeObjectName(target);

            try
            {
                // Split the path into parts (handling both dot notation and array indexing)
                string[] pathParts = SplitPropertyPath(path);
                if (pathParts.Length == 0)
                {
                    error = "Path parts length is 0";
                    return false;
                }

                object currentObject = target;
                Type currentType = currentObject.GetType();
                BindingFlags flags =
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

                // Traverse the path until we reach the final property
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    string part = pathParts[i];
                    bool isArray = false;
                    int arrayIndex = -1;

                    // Check if this part contains array indexing
                    if (part.Contains("["))
                    {
                        int startBracket = part.IndexOf('[');
                        int endBracket = part.IndexOf(']');
                        if (startBracket > 0 && endBracket > startBracket)
                        {
                            string indexStr = part.Substring(
                                startBracket + 1,
                                endBracket - startBracket - 1
                            );
                            if (int.TryParse(indexStr, out arrayIndex))
                            {
                                isArray = true;
                                part = part.Substring(0, startBracket);
                            }
                        }
                    }

                    // Get the property/field
                    if (!TryGetMemberValue(currentObject, part, flags, out currentObject, out Type memberType, out error))
                    {
                        return false;
                    }

                    // If the current property is null, we need to stop
                    if (currentObject == null)
                    {
                        error = $"Member '{part}' is null on {targetName}, cannot access nested properties";
                        return false;
                    }

                    // If this is an array/list access, get the element at the index
                    if (isArray)
                    {
                        if (currentObject is Material[])
                        {
                            var materials = currentObject as Material[];
                            if (arrayIndex < 0 || arrayIndex >= materials.Length)
                            {
                                error = $"Material index {arrayIndex} out of range (0-{materials.Length - 1}) on {targetName}";
                                return false;
                            }
                            currentObject = materials[arrayIndex];
                        }
                        else if (currentObject is System.Collections.IList)
                        {
                            var list = currentObject as System.Collections.IList;
                            if (arrayIndex < 0 || arrayIndex >= list.Count)
                            {
                                error = $"Index {arrayIndex} out of range (0-{list.Count - 1}) on {targetName}";
                                return false;
                            }
                            currentObject = list[arrayIndex];
                        }
                        else
                        {
                            error = $"Field '{part}' is not an array or list on {targetName}, cannot access by index";
                            return false;
                        }
                    }

                    // Update type for next iteration
                    currentType = currentObject.GetType();
                }

                // Validate Unity Object asset path
                if (typeof(UnityEngine.Object).IsAssignableFrom(currentType))
                {
                    var unityObj = currentObject as UnityEngine.Object;
                    if (unityObj != null)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(unityObj);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Only validate if it's an asset (not a scene object)
                            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
                            {
                                Debug.LogWarning($"[SetNestedProperty] Asset path '{assetPath}' is not in Assets or Packages folder");
                            }
                        }
                    }
                }

                // Set the final property
                string finalPart = pathParts[pathParts.Length - 1];

                // Special handling for Material properties (shader properties)
                if (currentObject is Material material && finalPart.StartsWith("_"))
                {
                    return SetMaterialShaderProperty(material, finalPart, value, out error);
                }

                // Use helper method to set field or property
                return TrySetMemberValue(currentObject, finalPart, value, flags, out error);
            }
            catch (Exception ex)
            {
                error = $"Error setting nested property '{path}' on {targetName}: {ex.Message}";
                Debug.LogError($"[SetNestedProperty] {error}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Sets material shader properties
        /// </summary>
        private bool SetMaterialShaderProperty(Material material, string propertyName, JsonNode value, out string error)
        {
            string materialName = material != null ? material.name : "<null>";

            try
            {
                // Handle various material property types
                if (value is JsonArray JsonArray)
                {
                    if (JsonArray.Count == 4) // Color with alpha or Vector4
                    {
                        if (propertyName.ToLower().Contains("color"))
                        {
                            Color color = new Color(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat,
                                JsonArray[2].AsFloat,
                                JsonArray[3].AsFloat
                            );
                            material.SetColor(propertyName, color);
                            error = null;
                            return true;
                        }
                        else
                        {
                            Vector4 vec = new Vector4(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat,
                                JsonArray[2].AsFloat,
                                JsonArray[3].AsFloat
                            );
                            material.SetVector(propertyName, vec);
                            error = null;
                            return true;
                        }
                    }
                    else if (JsonArray.Count == 3) // Color without alpha
                    {
                        Color color = new Color(
                            JsonArray[0].AsFloat,
                            JsonArray[1].AsFloat,
                            JsonArray[2].AsFloat,
                            1.0f
                        );
                        material.SetColor(propertyName, color);
                        error = null;
                        return true;
                    }
                    else if (JsonArray.Count == 2) // Vector2
                    {
                        Vector2 vec = new Vector2(
                            JsonArray[0].AsFloat,
                            JsonArray[1].AsFloat
                        );
                        material.SetVector(propertyName, vec);
                        error = null;
                        return true;
                    }
                }
                else if (value.type == JsonNodeType.Float || value.type == JsonNodeType.Integer)
                {
                    material.SetFloat(propertyName, value.AsFloat);
                    error = null;
                    return true;
                }
                else if (value.type == JsonNodeType.Boolean)
                {
                    material.SetFloat(propertyName, value.AsBool ? 1f : 0f);
                    error = null;
                    return true;
                }
                else if (value.type == JsonNodeType.String)
                {
                    // Might be a texture path
                    string texturePath = value.Value;
                    if (texturePath.EndsWith(".png") || texturePath.EndsWith(".jpg") || texturePath.EndsWith(".tga"))
                    {
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        if (texture != null)
                        {
                            material.SetTexture(propertyName, texture);
                            error = null;
                            return true;
                        }
                        else
                        {
                            error = $"Texture not found at path '{texturePath}' for material '{materialName}'";
                            return false;
                        }
                    }
                }

                error = $"Unsupported material property value type: {value.type} for property '{propertyName}' on material '{materialName}'";
                return false;
            }
            catch (Exception ex)
            {
                error = $"Error setting material property '{propertyName}' on '{materialName}': {ex.Message}";
                Debug.LogError($"[SetMaterialShaderProperty] {error}");
                return false;
            }
        }

        /// <summary>
        /// Safely gets an object's name for error messages
        /// </summary>
        private string GetSafeObjectName(object obj)
        {
            if (obj == null) return "<null>";

            try
            {
                // Check if it's a Unity Object
                if (obj is UnityEngine.Object unityObj)
                {
                    // Unity's fake null check
                    if (unityObj == null || !unityObj)
                        return "<destroyed Unity Object>";

                    return unityObj.name ?? obj.GetType().Name;
                }

                // Check if object has a name property
                var nameProperty = obj.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                if (nameProperty != null && nameProperty.CanRead && nameProperty.PropertyType == typeof(string))
                {
                    try
                    {
                        var nameValue = nameProperty.GetValue(obj) as string;
                        if (!string.IsNullOrEmpty(nameValue))
                            return nameValue;
                    }
                    catch { }
                }

                // Fall back to type name
                return obj.GetType().Name;
            }
            catch
            {
                return "<error getting name>";
            }
        }

        /// <summary>
        /// Helper to get a field or property value
        /// </summary>
        private bool TryGetMemberValue(object obj, string memberName, BindingFlags flags, out object value, out Type memberType, out string error)
        {
            Type type = obj.GetType();
            string targetName = GetSafeObjectName(obj);

            // Try field first
            FieldInfo fieldInfo = type.GetField(memberName, flags);
            if (fieldInfo != null)
            {
                value = fieldInfo.GetValue(obj);
                memberType = fieldInfo.FieldType;
                error = null;
                return true;
            }

            // Try property
            PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
            if (propertyInfo != null && propertyInfo.CanRead)
            {
                value = propertyInfo.GetValue(obj);
                memberType = propertyInfo.PropertyType;
                error = null;
                return true;
            }

            value = null;
            memberType = null;
            error = $"Could not find field or property '{memberName}' on type '{type.Name}' (from {targetName})";
            return false;
        }

        /// <summary>
        /// Helper to set a field or property value
        /// </summary>
        private bool TrySetMemberValue(object obj, string memberName, JsonNode jsonValue, BindingFlags flags, out string error)
        {
            Type type = obj.GetType();
            string targetName = GetSafeObjectName(obj);

            // Try field first
            FieldInfo fieldInfo = type.GetField(memberName, flags);
            if (fieldInfo != null)
            {
                object convertedValue = ConvertJTokenToType(jsonValue, fieldInfo.FieldType);
                if (convertedValue != null || fieldInfo.FieldType.IsClass)
                {
                    fieldInfo.SetValue(obj, convertedValue);
                    error = null;
                    return true;
                }
                else
                {
                    error = $"Failed to convert value for field '{memberName}' on {targetName} ({type.Name})";
                    return false;
                }
            }

            // Try property
            PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
            if (propertyInfo != null)
            {
                if (!propertyInfo.CanWrite)
                {
                    error = $"Property '{memberName}' on {targetName} ({type.Name}) is read-only";
                    return false;
                }

                object convertedValue = ConvertJTokenToType(jsonValue, propertyInfo.PropertyType);
                if (convertedValue != null || propertyInfo.PropertyType.IsClass)
                {
                    propertyInfo.SetValue(obj, convertedValue);
                    error = null;
                    return true;
                }
                else
                {
                    error = $"Failed to convert value for property '{memberName}' on {targetName} ({type.Name})";
                    return false;
                }
            }

            error = $"Could not find field or property '{memberName}' on type '{type.Name}' (from {targetName})";
            return false;
        }

        /// <summary>
        /// Split a property path into parts, handling both dot notation and array indexers
        /// </summary>
        private string[] SplitPropertyPath(string path)
        {
            // Handle complex paths with both dots and array indexers
            List<string> parts = new List<string>();
            int startIndex = 0;
            bool inBrackets = false;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];

                if (c == '[')
                {
                    inBrackets = true;
                }
                else if (c == ']')
                {
                    inBrackets = false;
                }
                else if (c == '.' && !inBrackets)
                {
                    // Found a dot separator outside of brackets
                    parts.Add(path.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }

            // Add the final part
            if (startIndex < path.Length)
            {
                parts.Add(path.Substring(startIndex));
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Simple JsonNode to Type conversion for common Unity types.
        /// </summary>
        private object ConvertJTokenToType(JsonNode token, Type targetType)
        {
            try
            {
                // Basic types first
                if (targetType == typeof(string))
                    return token.Value;
                if (targetType == typeof(int))
                    return token.AsInt;
                if (targetType == typeof(float))
                    return token.AsFloat;
                if (targetType == typeof(bool))
                    return token.AsBool;

                // Vector/Quaternion/Color types
                // 处理 Vector2
                if (targetType == typeof(Vector2))
                {
                    if (token is JsonArray arrV2 && arrV2.Count == 2)
                        return new Vector2(arrV2[0].AsFloat, arrV2[1].AsFloat);
                    if (token.type == JsonNodeType.String)
                    {
                        float[] values = ParseNumberArrayFromString(token.Value, 2);
                        if (values != null)
                            return new Vector2(values[0], values[1]);
                    }
                }

                // 处理 Vector3
                if (targetType == typeof(Vector3))
                {
                    if (token is JsonArray arrV3 && arrV3.Count == 3)
                        return new Vector3(arrV3[0].AsFloat, arrV3[1].AsFloat, arrV3[2].AsFloat);
                    if (token.type == JsonNodeType.String)
                    {
                        float[] values = ParseNumberArrayFromString(token.Value, 3);
                        if (values != null)
                            return new Vector3(values[0], values[1], values[2]);
                    }
                }

                // 处理 Vector4
                if (targetType == typeof(Vector4))
                {
                    if (token is JsonArray arrV4 && arrV4.Count == 4)
                        return new Vector4(arrV4[0].AsFloat, arrV4[1].AsFloat, arrV4[2].AsFloat, arrV4[3].AsFloat);
                    if (token.type == JsonNodeType.String)
                    {
                        float[] values = ParseNumberArrayFromString(token.Value, 4);
                        if (values != null)
                            return new Vector4(values[0], values[1], values[2], values[3]);
                    }
                }

                // 处理 Quaternion
                if (targetType == typeof(Quaternion))
                {
                    if (token is JsonArray arrQ && arrQ.Count == 4)
                        return new Quaternion(arrQ[0].AsFloat, arrQ[1].AsFloat, arrQ[2].AsFloat, arrQ[3].AsFloat);
                    if (token.type == JsonNodeType.String)
                    {
                        float[] values = ParseNumberArrayFromString(token.Value, 4);
                        if (values != null)
                            return new Quaternion(values[0], values[1], values[2], values[3]);
                    }
                }

                // 处理 Color
                if (targetType == typeof(Color))
                {
                    if (token is JsonArray arrC && arrC.Count >= 3)
                        return new Color(arrC[0].AsFloat, arrC[1].AsFloat, arrC[2].AsFloat, arrC.Count > 3 ? arrC[3].AsFloat : 1.0f);
                    if (token.type == JsonNodeType.String)
                    {
                        float[] values = ParseNumberArrayFromString(token.Value, -1); // -1 表示接受 3 或 4 个值
                        if (values != null && values.Length >= 3)
                            return new Color(values[0], values[1], values[2], values.Length > 3 ? values[3] : 1.0f);
                    }
                }

                // Enum types
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, token.Value, true);

                // Handle Array types
                if (targetType.IsArray && token is JsonArray JsonArray)
                {
                    Type elementType = targetType.GetElementType();
                    Array array = Array.CreateInstance(elementType, JsonArray.Count);

                    for (int i = 0; i < JsonArray.Count; i++)
                    {
                        try
                        {
                            object convertedElement = ConvertJTokenToType(JsonArray[i], elementType);
                            if (convertedElement != null || elementType.IsClass)
                            {
                                array.SetValue(convertedElement, i);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ConvertJTokenToType] Failed to convert array element {i}: {ex.Message}");
                        }
                    }

                    return array;
                }

                // Handle List<> types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>) && token is JsonArray jList)
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    var list = (System.Collections.IList)Activator.CreateInstance(targetType);

                    for (int i = 0; i < jList.Count; i++)
                    {
                        try
                        {
                            object convertedElement = ConvertJTokenToType(jList[i], elementType);
                            if (convertedElement != null || elementType.IsClass)
                            {
                                list.Add(convertedElement);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ConvertJTokenToType] Failed to convert list element {i}: {ex.Message}");
                        }
                    }

                    return list;
                }

                // Handle Unity Objects (Assets)
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    if (token.type == JsonNodeType.String)
                    {
                        string assetPath = token.Value;
                        if (!string.IsNullOrEmpty(assetPath) && System.IO.File.Exists(assetPath))
                        {
                            UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                            if (loadedAsset != null)
                            {
                                return loadedAsset;
                            }
                            else
                            {
                                Debug.LogWarning($"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from path: '{assetPath}'");
                            }
                        }
                        else
                        {
                            var sceneObj = GameObjectUtils.FindByHierarchyPath(assetPath, targetType);
                            if (sceneObj != null)
                            {
                                return sceneObj;
                            }
                            else
                            {
                                Debug.LogWarning($"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from path: '{assetPath}'");
                            }
                        }
                    }
                    else if (token.type == JsonNodeType.Integer)
                    {
                        var instanceId = token.AsInt;
                        var objectItem = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                        if (objectItem != null)
                        {
                            if (objectItem.GetType() == targetType)
                            {
                                return objectItem;
                            }
                            else if (objectItem is GameObject go && typeof(Component).IsAssignableFrom(targetType))
                            {
                                return go.GetComponent(targetType);
                            }
                            else
                            {
                                Debug.LogWarning($"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from instance id: '{instanceId}'");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from instance id: '{instanceId}'");
                        }
                    }

                    return null;
                }

                // SimpleJson 不支持 ToObject(targetType)
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConvertJTokenToType] Could not convert JsonNode '{token}' to type '{targetType.Name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 查找组件类型，遍历所有已加载的程序集
        /// </summary>
        private Type FindComponentType(string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return null;

            // 遍历所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    // 某些动态程序集可能抛异常，忽略
                    continue;
                }

                foreach (var type in types)
                {
                    if (!typeof(Component).IsAssignableFrom(type))
                        continue;

                    // 名字或全名匹配即可
                    if (type.Name == componentName || type.FullName == componentName)
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取GameObject的数据表示 - 使用优化的YAML格式
        /// </summary>
        private object GetGameObjectData(GameObject go)
        {
            if (go == null) return null;

            // 使用统一的YAML格式，大幅减少token使用量
            var yamlData = GameObjectUtils.GetGameObjectDataYaml(go);
            return new { yaml = yamlData };
        }

        /// <summary>
        /// 从字符串解析数字数组，支持多种格式：
        /// "[0.1, 0.2, 0.3]", "(0.1, 0.2, 0.3)", "0.1, 0.2, 0.3"
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <param name="expectedCount">期望的数字数量，-1 表示不限制</param>
        /// <returns>解析后的 float 数组，失败返回 null</returns>
        private float[] ParseNumberArrayFromString(string str, int expectedCount)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;

            try
            {
                // 去除首尾空格
                str = str.Trim();

                // 移除外层括号（支持方括号和圆括号）
                if ((str.StartsWith("[") && str.EndsWith("]")) ||
                    (str.StartsWith("(") && str.EndsWith(")")))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                // 按逗号分割
                string[] parts = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // 检查数量
                if (expectedCount > 0 && parts.Length != expectedCount)
                {
                    Debug.LogWarning($"[ParseNumberArrayFromString] Expected {expectedCount} values, but got {parts.Length} in string: '{str}'");
                    return null;
                }

                if (expectedCount == -1 && (parts.Length < 3 || parts.Length > 4))
                {
                    Debug.LogWarning($"[ParseNumberArrayFromString] Expected 3-4 values, but got {parts.Length} in string: '{str}'");
                    return null;
                }

                // 解析每个数字
                float[] result = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result[i]))
                    {
                        Debug.LogWarning($"[ParseNumberArrayFromString] Failed to parse '{parts[i].Trim()}' as float in string: '{str}'");
                        return null;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParseNumberArrayFromString] Failed to parse string '{str}': {ex.Message}");
                return null;
            }
        }



        #endregion
    }
}


