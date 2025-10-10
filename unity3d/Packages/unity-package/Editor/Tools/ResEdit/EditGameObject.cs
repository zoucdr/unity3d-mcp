﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles GameObject modification operations using dual state tree architecture.
    /// First tree: Target location (using GameObjectSelector)
    /// Second tree: Property modification operations
    /// 对应方法名: gameobject_modify
    /// </summary>
    [ToolName("edit_gameobject", "资源管理")]
    public class EditGameObject : DualStateMethodBase
    {
        private HierarchyCreate hierarchyCreate;
        private IObjectSelector objectSelector;

        public EditGameObject()
        {
            hierarchyCreate = new HierarchyCreate();
            objectSelector = objectSelector ?? new ObjectSelector<GameObject>();
        }

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                   // 目标查找参数
                new MethodKey("path", "Object Hierarchy path", false),
                new MethodKey("instance_id", "Object InstanceID", true),
                // 操作参数
                new MethodKey("action", "Operation type: create, modify, get_components, add_component, remove_component, set_parent", false),
                // 基本修改参数
                new MethodKey("name", "GameObject name", true),
                new MethodKey("tag", "GameObject tag", true),
                new MethodKey("layer", "GameObject layer", true),
                new MethodKey("parent_id", "Parent object InstanceID", true),
                new MethodKey("parent_path", "Parent object scene path", true),
                new MethodKey("position", "Position coordinates [x, y, z]", true),
                new MethodKey("rotation", "Rotation angles [x, y, z]", true),
                new MethodKey("scale", "Scale ratios [x, y, z]", true),
                new MethodKey("active", "Set active state", true),
                // 组件操作参数
                new MethodKey("component_type", "Component name", true),
                new MethodKey("component_properties", "Component properties dictionary", true),
            };
        }

        /// <summary>
        /// 创建目标定位状态树（使用GameObjectDynamicSelector）
        /// </summary>
        protected override StateTree CreateTargetTree()
        {
            return objectSelector.BuildStateTree();
        }

        /// <summary>
        /// 创建操作执行状态树
        /// </summary>
        protected override StateTree CreateActionTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create", (Func<StateTreeContext, object>)HandleCreateAction)
                    .Leaf("modify", (Func<StateTreeContext, object>)HandleModifyAction)
                    .Leaf("get_components", (Func<StateTreeContext, object>)HandleGetComponentsAction)
                    .Leaf("add_component", (Func<StateTreeContext, object>)HandleAddComponentAction)
                    .Leaf("remove_component", (Func<StateTreeContext, object>)HandleRemoveComponentAction)
                    .Leaf("set_parent", (Func<StateTreeContext, object>)HandleSetParentAction)
                    .DefaultLeaf((Func<StateTreeContext, object>)HandleDefaultAction)
                .Build();
        }

        /// <summary>
        /// 处理创建操作
        /// </summary>
        private object HandleCreateAction(StateTreeContext args)
        {
            hierarchyCreate.ExecuteMethod(args);
            return args;
        }

        /// <summary>
        /// 处理修改操作
        /// </summary>
        private object HandleModifyAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                // 单个对象修改
                return ApplyModifications(targets[0], args);
            }
            else
            {
                // 批量修改
                return ApplyModificationsToMultiple(targets, args);
            }
        }

        /// <summary>
        /// 默认操作处理（兼容性，不指定action时使用modify）
        /// </summary>
        private object HandleDefaultAction(StateTreeContext args)
        {
            LogInfo("[GameObjectModify] No action specified, using default modify action");
            if (args.TryGetValue("action", out object actionObj))
            {
                return Response.Error("Invalid action specified: " + actionObj);
            }
            return HandleModifyAction(args);
        }

        /// <summary>
        /// 从执行上下文中提取目标GameObject数组
        /// </summary>
        private GameObject[] ExtractTargetsFromContext(StateTreeContext context)
        {
            // 先尝试从ObjectReferences获取（避免序列化问题）
            if (context.TryGetObjectReference("_resolved_targets", out object targetsObj))
            {
                if (targetsObj is GameObject[] gameObjectArray)
                {
                    return gameObjectArray;
                }
                else if (targetsObj is GameObject singleGameObject)
                {
                    return new GameObject[] { singleGameObject };
                }
                else if (targetsObj is System.Collections.IList list)
                {
                    var gameObjects = new List<GameObject>();
                    foreach (var item in list)
                    {
                        if (item is GameObject go)
                            gameObjects.Add(go);
                    }
                    return gameObjects.ToArray();
                }
            }
            return new GameObject[0];
        }

        /// <summary>
        /// 从目标数组中提取第一个GameObject（用于需要单个目标的操作）
        /// </summary>
        private GameObject ExtractFirstTargetFromContext(StateTreeContext context)
        {
            GameObject[] targets = ExtractTargetsFromContext(context);
            return targets.Length > 0 ? targets[0] : null;
        }

        /// <summary>
        /// 检查是否应该进行批量操作
        /// </summary>
        private bool ShouldSelectMany(StateTreeContext context)
        {
            if (context.TryGetValue("select_many", out object selectManyObj))
            {
                if (selectManyObj is bool selectMany)
                    return selectMany;
                if (bool.TryParse(selectManyObj?.ToString(), out bool parsedSelectMany))
                    return parsedSelectMany;
            }
            return false; // 默认为false
        }

        /// <summary>
        /// 根据select_many参数获取目标对象（单个或多个）
        /// </summary>
        private GameObject[] GetTargetsBasedOnSelectMany(StateTreeContext context)
        {
            GameObject[] targets = ExtractTargetsFromContext(context);

            if (ShouldSelectMany(context))
            {
                return targets; // 返回所有匹配的对象
            }
            else
            {
                // 只返回第一个对象（如果存在）
                return targets.Length > 0 ? new GameObject[] { targets[0] } : new GameObject[0];
            }
        }

        /// <summary>
        /// 处理设置父级操作
        /// </summary>
        private object HandleSetParentAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                return SetParentOnSingleTarget(targets[0], args);
            }
            else
            {
                return SetParentOnMultipleTargets(targets, args);
            }
        }


        /// <summary>
        /// 应用修改到GameObject
        /// </summary>
        private object ApplyModifications(GameObject targetGo, StateTreeContext args)
        {
            // Record state for Undo *before* modifications
            Undo.RecordObject(targetGo.transform, "Modify GameObject Transform");
            Undo.RecordObject(targetGo, "Modify GameObject Properties");

            bool modified = false;

            // 应用名称修改
            modified |= ApplyNameModification(targetGo, args);

            // 应用父对象修改
            modified |= ApplyParentModification(targetGo, args);

            // 应用激活状态修改
            modified |= ApplyActiveStateModification(targetGo, args);

            // 应用标签修改
            object tagResult = ApplyTagModification(targetGo, args);
            if (tagResult != null)
                return tagResult;
            modified |= tagResult != null;

            // 应用层级修改
            modified |= ApplyLayerModification(targetGo, args);

            // 应用变换修改
            modified |= ApplyTransformModifications(targetGo, args);

            if (!modified)
            {
                return Response.Success(
                    $"No modifications applied to GameObject '{targetGo.name}'.",
                    Json.FromObject(GetGameObjectData(targetGo))
                );
            }

            EditorUtility.SetDirty(targetGo); // Mark scene as dirty
            return Response.Success(
                $"GameObject '{targetGo.name}' modified successfully.",
                Json.FromObject(GetGameObjectData(targetGo))
            );
        }

        /// <summary>
        /// 应用修改到多个GameObject
        /// </summary>
        private object ApplyModificationsToMultiple(GameObject[] targets, StateTreeContext args)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = ApplyModifications(targetGo, args);

                    if (IsSuccessResponse(result, out object data, out string responseMessage))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            // GetGameObjectData现在返回YAML格式，需要适配
                            var gameObjectData = GetGameObjectData(targetGo);
                            results.Add(new Dictionary<string, object>
                            {
                                { "target", targetGo.name },
                                { "instanceID", targetGo.GetInstanceID() },
                                { "data", gameObjectData }
                            });
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {responseMessage ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            // 构建响应消息
            string message;
            if (successCount == targets.Length)
            {
                message = $"Successfully modified {successCount} GameObject(s).";
            }
            else if (successCount > 0)
            {
                message = $"Modified {successCount} of {targets.Length} GameObject(s). {errors.Count} failed.";
            }
            else
            {
                message = $"Failed to modify any of the {targets.Length} GameObject(s).";
            }

            var responseData = new Dictionary<string, object>
            {
                { "modified_count", successCount },
                { "total_count", targets.Length },
                { "success_rate", (double)successCount / targets.Length },
                { "modified_objects", results }
            };

            if (errors.Count > 0)
            {
                responseData["errors"] = errors;
            }

            // 如果有成功的修改，返回成功响应
            if (successCount > 0)
            {
                return Response.Success(message, responseData);
            }
            else
            {
                return Response.Error(message, responseData);
            }
        }

        /// <summary>
        /// 应用名称修改
        /// </summary>
        private bool ApplyNameModification(GameObject targetGo, StateTreeContext args)
        {
            if (args.TryGetValue("name", out object nameObj) && nameObj != null)
            {
                string name = nameObj.ToString();
                if (!string.IsNullOrEmpty(name) && targetGo.name != name)
                {
                    targetGo.name = name;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 应用父对象修改
        /// </summary>
        private bool ApplyParentModification(GameObject targetGo, StateTreeContext args)
        {
            object parentIdObj = args["parent_id"] ?? args["parent_path"];
            if (parentIdObj == null)
            {
                return false;
            }

            GameObject newParentGo = GameObjectUtils.FindObjectByIdOrPath(Json.FromObject(parentIdObj));
            if (newParentGo != null)
            {
                if (newParentGo != null && newParentGo.transform.IsChildOf(targetGo.transform))
                {
                    return false;
                }

                if (targetGo.transform.parent != (newParentGo?.transform))
                {
                    targetGo.transform.SetParent(newParentGo?.transform, true); // worldPositionStays = true
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 应用激活状态修改
        /// </summary>
        private bool ApplyActiveStateModification(GameObject targetGo, StateTreeContext args)
        {
            if (args.TryGetValue("setActive", out object setActiveObj) ||
                args.TryGetValue("active", out setActiveObj))
            {
                if (setActiveObj is bool setActive)
                {
                    if (targetGo.activeSelf != setActive)
                    {
                        targetGo.SetActive(setActive);
                        return true;
                    }
                }
                else if (bool.TryParse(setActiveObj?.ToString(), out bool parsedActive))
                {
                    if (targetGo.activeSelf != parsedActive)
                    {
                        targetGo.SetActive(parsedActive);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 应用标签修改
        /// </summary>
        private object ApplyTagModification(GameObject targetGo, StateTreeContext args)
        {
            if (args.TryGetValue("tag", out object tagObj) && tagObj != null)
            {
                string tag = tagObj.ToString();
                // Only attempt to change tag if a non-null tag is provided and it's different from the current one.
                // Allow setting an empty string to remove the tag (Unity uses "Untagged").
                if (targetGo.tag != tag)
                {
                    // Ensure the tag is not empty, if empty, it means "Untagged" implicitly
                    string tagToSet = string.IsNullOrEmpty(tag) ? "Untagged" : tag;

                    try
                    {
                        // First attempt to set the tag
                        targetGo.tag = tagToSet;
                        return true;
                    }
                    catch (UnityException ex)
                    {
                        // Check if the error is specifically because the tag doesn't exist
                        if (ex.Message.Contains("is not defined"))
                        {
                            LogInfo($"[GameObjectModify] Tag '{tagToSet}' not found. Attempting to create it.");
                            try
                            {
                                // Attempt to create the tag using internal utility
                                InternalEditorUtility.AddTag(tagToSet);
                                // Wait a frame maybe? Not strictly necessary but sometimes helps editor updates.
                                // yield return null; // Cannot yield here, editor script limitation

                                // Retry setting the tag immediately after creation
                                targetGo.tag = tagToSet;
                                LogInfo($"[GameObjectModify] Tag '{tagToSet}' created and assigned successfully.");
                                return true;
                            }
                            catch (Exception innerEx)
                            {
                                // Handle failure during tag creation or the second assignment attempt
                                Debug.LogError(
                                    $"[GameObjectModify] Failed to create or assign tag '{tagToSet}' after attempting creation: {innerEx.Message}"
                                );
                                return Response.Error(
                                    $"Failed to create or assign tag '{tagToSet}': {innerEx.Message}. Check Tag Manager and permissions."
                                );
                            }
                        }
                        else
                        {
                            // If the exception was for a different reason, return the original error
                            return Response.Error($"Failed to set tag to '{tagToSet}': {ex.Message}.");
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 应用层级修改
        /// </summary>
        private bool ApplyLayerModification(GameObject targetGo, StateTreeContext args)
        {
            if (args.TryGetValue("layer", out object layerObj) && layerObj != null)
            {
                string layerName = layerObj.ToString();
                if (!string.IsNullOrEmpty(layerName))
                {
                    int layerId = LayerMask.NameToLayer(layerName);
                    if (layerId == -1 && layerName != "Default")
                    {
                        Debug.LogWarning($"Invalid layer specified: '{layerName}'. Use a valid layer name.");
                        return false;
                    }
                    if (layerId != -1 && targetGo.layer != layerId)
                    {
                        targetGo.layer = layerId;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 应用变换修改
        /// </summary>
        private bool ApplyTransformModifications(GameObject targetGo, StateTreeContext args)
        {
            bool modified = false;

            // 获取transform参数并转换为JSONArray（如果需要）
            JsonArray positionArray = null;
            JsonArray rotationArray = null;
            JsonArray scaleArray = null;

            if (args.TryGetValue("position", out object positionObj))
            {
                positionArray = positionObj as JsonArray ?? (positionObj != null ? Json.FromObject(positionObj) as JsonArray : null);
            }

            if (args.TryGetValue("rotation", out object rotationObj))
            {
                rotationArray = rotationObj as JsonArray ?? (rotationObj != null ? Json.FromObject(rotationObj) as JsonArray : null);
            }

            if (args.TryGetValue("scale", out object scaleObj))
            {
                scaleArray = scaleObj as JsonArray ?? (positionObj != null ? Json.FromObject(scaleObj) as JsonArray : null);
            }

            Vector3? position = GameObjectUtils.ParseVector3(positionArray);
            Vector3? rotation = GameObjectUtils.ParseVector3(rotationArray);
            Vector3? scale = GameObjectUtils.ParseVector3(scaleArray);

            if (position.HasValue && targetGo.transform.localPosition != position.Value)
            {
                targetGo.transform.localPosition = position.Value;
                modified = true;
            }
            if (rotation.HasValue && targetGo.transform.localEulerAngles != rotation.Value)
            {
                targetGo.transform.localEulerAngles = rotation.Value;
                modified = true;
            }
            if (scale.HasValue && targetGo.transform.localScale != scale.Value)
            {
                targetGo.transform.localScale = scale.Value;
                modified = true;
            }

            return modified;
        }



        #region 父级设置方法

        /// <summary>
        /// 在单个目标上设置父级
        /// </summary>
        private object SetParentOnSingleTarget(GameObject target, StateTreeContext args)
        {
            try
            {
                GameObject newParent = null;

                // 只处理parent_id参数
                if (args.TryGetValue("parent_id", out object parentIdObj) && parentIdObj != null)
                {
                    // 通过GameObjectUtils.FindObjectByIdOrPath查找父级
                    newParent = GameObjectUtils.FindObjectByIdOrPath(Json.FromObject(parentIdObj));
                    // 如果parent_id为0或找不到，newParent为null，表示设置为根对象
                }
                else if (args.TryGetValue("parent_path", out object parentPathObj) && parentPathObj != null)
                {
                    newParent = GameObjectUtils.FindByHierarchyPath(parentPathObj.ToString(), typeof(GameObject)) as GameObject;
                }
                else
                {
                    return Response.Error("parent_id or parent_path is required for set_parent action.");
                }

                // 检查循环引用
                if (newParent != null && newParent.transform.IsChildOf(target.transform))
                {
                    return Response.Error($"Cannot parent '{target.name}' to '{newParent.name}', as it would create a hierarchy loop.");
                }

                // 记录撤销操作
                Undo.RecordObject(target.transform, "Set Parent");

                if (newParent != null)
                {
                    target.transform.SetParent(newParent?.transform, true);
                }

                LogInfo($"[EditGameObject] Set parent of '{target.name}' to '{newParent?.name ?? "null"}'");

                return Response.Success(
                    $"Successfully set parent of '{target.name}' to '{newParent?.name ?? "null"}'.",
                    new Dictionary<string, object>
                    {
                        { "target", target.name },
                        { "target_id", target.GetInstanceID() },
                        { "new_parent", newParent?.name },
                        { "new_parent_id", newParent?.GetInstanceID() ?? 0 }
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set parent for '{target.name}': {e.Message}");
            }
        }

        /// <summary>
        /// 在多个目标上设置父级
        /// </summary>
        private object SetParentOnMultipleTargets(GameObject[] targets, StateTreeContext args)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                try
                {
                    var result = SetParentOnSingleTarget(target, args);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            results.Add(new Dictionary<string, object>
                            {
                                { "target", target.name },
                                { "target_id", target.GetInstanceID() }
                            });
                        }
                    }
                    else
                    {
                        errors.Add($"[{target.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{target.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("set parent", successCount, targets.Length, results, errors);
        }

        #endregion

        #region 组件操作方法

        /// <summary>
        /// 处理获取组件的操作
        /// </summary>
        private object HandleGetComponentsAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                return GetComponentsFromTarget(targets[0]);
            }
            else
            {
                return GetComponentsFromMultipleTargets(targets);
            }
        }

        /// <summary>
        /// 处理添加组件的操作
        /// </summary>
        private object HandleAddComponentAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                return AddComponentToTarget(args, targets[0]);
            }
            else
            {
                return AddComponentToMultipleTargets(args, targets);
            }
        }

        /// <summary>
        /// 处理移除组件的操作
        /// </summary>
        private object HandleRemoveComponentAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                return RemoveComponentFromTarget(args, targets[0]);
            }
            else
            {
                return RemoveComponentFromMultipleTargets(args, targets);
            }
        }

        /// <summary>
        /// 处理设置组件属性的操作
        /// </summary>
        private object HandleSetComponentPropertyAction(StateTreeContext args)
        {
            GameObject[] targets = GetTargetsBasedOnSelectMany(args);
            if (targets.Length == 0)
            {
                return Response.Error("No target GameObjects found in execution context.");
            }

            if (targets.Length == 1)
            {
                return SetComponentPropertyOnTarget(args, targets[0]);
            }
            else
            {
                return SetComponentPropertyOnMultipleTargets(args, targets);
            }
        }

        #endregion

        #region 组件操作核心方法

        private object GetComponentsFromTarget(GameObject targetGo)
        {
            try
            {
                Component[] components = targetGo.GetComponents<Component>();
                var componentData = components.Select(c => GetComponentData(c)).ToList();
                return Response.Success(
                    $"Retrieved {componentData.Count} components from '{targetGo.name}'.",
                    componentData
                );
            }
            catch (Exception e)
            {
                return Response.Error(
                    $"Error getting components from '{targetGo.name}': {e.Message}"
                );
            }
        }

        /// <summary>
        /// 从多个GameObject获取组件
        /// </summary>
        private object GetComponentsFromMultipleTargets(GameObject[] targets)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = GetComponentsFromTarget(targetGo);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        var targetData = new Dictionary<string, object>
                        {
                            { "target", targetGo.name },
                            { "instanceID", targetGo.GetInstanceID() },
                            { "components", data }
                        };
                        results.Add(targetData);
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("get components", successCount, targets.Length, results, errors);
        }

        private object AddComponentToTarget(StateTreeContext cmd, GameObject targetGo)
        {
            string typeName = null;
            JsonClass properties = null;

            // Allow adding component specified directly or via components array (take first)
            if (cmd.TryGetValue("component_type", out object componentNameObj))
            {
                typeName = componentNameObj?.ToString();

                // Check if props are nested under name
                if (cmd.TryGetValue("component_properties", out object componentPropsObj))
                {
                    if (componentPropsObj is JsonClass allProps && !string.IsNullOrEmpty(typeName))
                    {
                        properties = allProps[typeName] as JsonClass ?? allProps;
                    }
                    else if (componentPropsObj is JsonClass directProps)
                    {
                        properties = directProps;
                    }
                }
            }
            else if (cmd.TryGetValue("components", out object componentsObj) && componentsObj is JsonArray componentsToAddArray && componentsToAddArray.Count > 0)
            {
                var compToken = componentsToAddArray[0];
                if (compToken != null && compToken.type == JsonNodeType.String)
                    typeName = compToken.Value;
                else if (compToken is JsonClass compObj)
                {
                    typeName = compObj["typeName"]?.Value;
                    properties = compObj["properties"] as JsonClass;
                }
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return Response.Error(
                    "Component type name ('component_type' or first element in 'components') is required."
                );
            }

            var addResult = AddComponentInternal(targetGo, typeName, properties);
            if (addResult != null)
                return addResult; // Return error

            // Set properties if provided (after successful component addition)
            if (properties != null)
            {
                var setResult = SetComponentPropertiesInternal(
                    targetGo,
                    typeName,
                    properties
                );
                if (setResult != null)
                {
                    // If setting properties failed, consider removing the component or log warning
                    Debug.LogWarning($"[EditGameObject] Failed to set properties for component '{typeName}': {setResult}");
                }
            }

            EditorUtility.SetDirty(targetGo);
            return Response.Success(
                $"Component '{typeName}' added to '{targetGo.name}'.",
               Json.FromObject(GetGameObjectData(targetGo))
            ); // Return updated GO data
        }

        private object RemoveComponentFromTarget(StateTreeContext cmd, GameObject targetGo)
        {
            string typeName = null;
            // Allow removing component specified directly or via components_to_remove array (take first)
            if (cmd.TryGetValue("component_type", out object componentNameObj))
            {
                typeName = componentNameObj?.ToString();
            }
            else if (cmd.TryGetValue("components_to_remove", out object componentsToRemoveObj) &&
                     componentsToRemoveObj is JsonArray componentsToRemoveArray &&
                     componentsToRemoveArray.Count > 0)
            {
                typeName = componentsToRemoveArray[0]?.Value;
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return Response.Error(
                    "Component type name ('component_type' or first element in 'components_to_remove') is required."
                );
            }

            var removeResult = RemoveComponentInternal(targetGo, typeName);
            if (removeResult != null)
                return removeResult; // Return error

            EditorUtility.SetDirty(targetGo);
            return Response.Success(
                $"Component '{typeName}' removed from '{targetGo.name}'.",
                Json.FromObject(GetGameObjectData(targetGo))
            );
        }

        private object SetComponentPropertyOnTarget(StateTreeContext cmd, GameObject targetGo)
        {
            if (!cmd.TryGetValue("component_type", out object compNameObj) || compNameObj == null)
            {
                return Response.Error("'component_type' parameter is required.");
            }

            string compName = compNameObj.ToString();
            JsonClass propertiesToSet = null;

            // Properties might be directly under component_properties or nested under the component name
            if (cmd.TryGetValue("component_properties", out object compPropsObj) && compPropsObj is JsonClass compProps)
            {
                propertiesToSet = compProps[compName] as JsonClass ?? compProps; // Allow flat or nested structure
            }

            if (propertiesToSet == null || propertiesToSet.Count == 0)
            {
                return Response.Error(
                    "'component_properties' dictionary for the specified component is required and cannot be empty."
                );
            }

            var setResult = SetComponentPropertiesInternal(targetGo, compName, propertiesToSet);
            if (setResult != null)
                return setResult; // Return error

            EditorUtility.SetDirty(targetGo);
            return Response.Success(
                $"Properties set for component '{compName}' on '{targetGo.name}'.",
                Json.FromObject(GetGameObjectData(targetGo))
            );
        }

        /// <summary>
        /// 批量添加组件到多个GameObject
        /// </summary>
        private object AddComponentToMultipleTargets(StateTreeContext cmd, GameObject[] targets)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = AddComponentToTarget(cmd, targetGo);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            // GetGameObjectData现在返回YAML格式，需要适配
                            var gameObjectData = GetGameObjectData(targetGo);
                            results.Add(new Dictionary<string, object>
                            {
                                { "target", targetGo.name },
                                { "instanceID", targetGo.GetInstanceID() },
                                { "data", gameObjectData }
                            });
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("add component", successCount, targets.Length, results, errors);
        }

        /// <summary>
        /// 批量移除组件从多个GameObject
        /// </summary>
        private object RemoveComponentFromMultipleTargets(StateTreeContext cmd, GameObject[] targets)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = RemoveComponentFromTarget(cmd, targetGo);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            // GetGameObjectData现在返回YAML格式，需要适配
                            var gameObjectData = GetGameObjectData(targetGo);
                            results.Add(new Dictionary<string, object>
                            {
                                { "target", targetGo.name },
                                { "instanceID", targetGo.GetInstanceID() },
                                { "data", gameObjectData }
                            });
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("remove component", successCount, targets.Length, results, errors);
        }

        /// <summary>
        /// 批量设置组件属性在多个GameObject上
        /// </summary>
        private object SetComponentPropertyOnMultipleTargets(StateTreeContext cmd, GameObject[] targets)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            int successCount = 0;

            foreach (GameObject targetGo in targets)
            {
                if (targetGo == null) continue;

                try
                {
                    var result = SetComponentPropertyOnTarget(cmd, targetGo);

                    if (IsSuccessResponse(result, out object data, out string message))
                    {
                        successCount++;
                        if (data is Dictionary<string, object> dictData)
                        {
                            results.Add(dictData);
                        }
                        else
                        {
                            // GetGameObjectData现在返回YAML格式，需要适配
                            var gameObjectData = GetGameObjectData(targetGo);
                            results.Add(new Dictionary<string, object>
                            {
                                { "target", targetGo.name },
                                { "instanceID", targetGo.GetInstanceID() },
                                { "data", gameObjectData }
                            });
                        }
                    }
                    else
                    {
                        errors.Add($"[{targetGo.name}] {message ?? "Unknown error"}");
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"[{targetGo.name}] Error: {e.Message}");
                }
            }

            return CreateBatchOperationResponse("set component properties", successCount, targets.Length, results, errors);
        }

        /// <summary>
        /// 检查Response对象是否表示成功
        /// </summary>
        private bool IsSuccessResponse(object response, out object data, out string message)
        {
            data = null;
            message = null;

            var resultType = response.GetType();
            var successProperty = resultType.GetProperty("success");
            var dataProperty = resultType.GetProperty("data");
            var messageProperty = resultType.GetProperty("message");
            var errorProperty = resultType.GetProperty("error");

            bool isSuccess = successProperty != null && (bool)successProperty.GetValue(response);
            data = dataProperty?.GetValue(response);
            message = isSuccess ?
                messageProperty?.GetValue(response)?.ToString() :
                (errorProperty?.GetValue(response)?.ToString() ?? messageProperty?.GetValue(response)?.ToString());

            return isSuccess;
        }

        /// <summary>
        /// 创建批量操作响应
        /// </summary>
        private object CreateBatchOperationResponse(string operation, int successCount, int totalCount,
            List<Dictionary<string, object>> results, List<string> errors)
        {
            string message;
            if (successCount == totalCount)
            {
                message = $"Successfully completed {operation} on {successCount} GameObject(s).";
            }
            else if (successCount > 0)
            {
                message = $"Completed {operation} on {successCount} of {totalCount} GameObject(s). {errors.Count} failed.";
            }
            else
            {
                message = $"Failed to complete {operation} on any of the {totalCount} GameObject(s).";
            }

            var responseData = new Dictionary<string, object>
            {
                { "operation", operation },
                { "success_count", successCount },
                { "total_count", totalCount },
                { "success_rate", (double)successCount / totalCount },
                { "affected_objects", results }
            };

            if (errors.Count > 0)
            {
                responseData["errors"] = errors;
            }

            if (successCount > 0)
            {
                return Response.Success(message, responseData);
            }
            else
            {
                return Response.Error(message, responseData);
            }
        }

        #endregion

        #region 组件辅助方法

        /// <summary>
        /// Removes a component by type name.
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        private object RemoveComponentInternal(GameObject targetGo, string typeName)
        {
            Type componentType = FindComponentType(typeName);
            if (componentType == null)
            {
                return Response.Error($"Component type '{typeName}' not found for removal.");
            }

            // Prevent removing essential components
            if (componentType == typeof(Transform))
            {
                return Response.Error("Cannot remove the Transform component.");
            }

            Component componentToRemove = targetGo.GetComponent(componentType);
            if (componentToRemove == null)
            {
                return Response.Error(
                    $"Component '{typeName}' not found on '{targetGo.name}' to remove."
                );
            }

            try
            {
                // Use Undo.DestroyObjectImmediate for undo support
                Undo.DestroyObjectImmediate(componentToRemove);
                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error(
                    $"Error removing component '{typeName}' from '{targetGo.name}': {e.Message}"
                );
            }
        }

        /// <summary>
        /// Sets properties on a component.
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        private object SetComponentPropertiesInternal(
            GameObject targetGo,
            string compName,
            JsonClass propertiesToSet,
            Component targetComponentInstance = null
        )
        {
            Component targetComponent = targetComponentInstance;

            // If no specific component instance is provided, find it by type name
            if (targetComponent == null)
            {
                // Use FindType helper to locate the correct component type
                Type componentType = FindComponentType(compName);
                if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                {
                    targetComponent = targetGo.GetComponent(componentType);
                }
                else
                {
                    // Fallback: try common Unity component namespaces
                    string[] commonNamespaces = { "UnityEngine", "UnityEngine.UI" };
                    foreach (string ns in commonNamespaces)
                    {
                        string fullTypeName = $"{ns}.{compName}";
                        componentType = FindComponentType(fullTypeName);
                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            targetComponent = targetGo.GetComponent(componentType);
                            break;
                        }
                    }
                }
            }

            if (targetComponent == null)
            {
                return Response.Error(
                    $"Component '{compName}' not found on '{targetGo.name}' to set properties."
                );
            }

            Undo.RecordObject(targetComponent, "Set Component Properties");

            foreach (var propName in propertiesToSet.GetKeys())
            {
                JsonNode propValue = propertiesToSet[propName];

                try
                {
                    if (!SetComponentProperty(targetComponent, propName, propValue))
                    {
                        // Log warning if property could not be set
                        Debug.LogWarning(
                            $"[EditGameObject] Could not set property '{propName}' on component '{compName}' ('{targetComponent.GetType().Name}'). Property might not exist, be read-only, or type mismatch."
                        );
                        // Optionally return an error here instead of just logging
                        // return Response.Error($"Could not set property '{propName}' on component '{compName}'.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[EditGameObject] Error setting property '{propName}' on '{compName}': {e.Message}"
                    );
                    // Optionally return an error here
                    // return Response.Error($"Error setting property '{propName}' on '{compName}': {e.Message}");
                }
            }
            EditorUtility.SetDirty(targetComponent);
            return null; // Success (or partial success if warnings were logged)
        }

        /// <summary>
        /// Creates a serializable representation of a Component.
        /// </summary>
        private object GetComponentData(Component c)
        {
            if (c == null)
                return null;
            var data = new Dictionary<string, object>
            {
                { "typeName", c.GetType().FullName },
                { "instanceID", c.GetInstanceID() },
            };

            return data;
        }

        /// <summary>
        /// Helper to set a property or field via reflection, handling basic types.
        /// </summary>
        private bool SetComponentProperty(object target, string memberName, JsonNode value)
        {
            Type type = target.GetType();
            BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            try
            {
                // Handle special case for materials with dot notation (material.property)
                // Examples: material.color, sharedMaterial.color, materials[0].color
                if (memberName.Contains('.') || memberName.Contains('['))
                {
                    return SetNestedProperty(target, memberName, value);
                }

                PropertyInfo propInfo = type.GetProperty(memberName, flags);
                if (propInfo != null && propInfo.CanWrite)
                {
                    object convertedValue = ConvertJTokenToType(value, propInfo.PropertyType);
                    if (convertedValue != null)
                    {
                        propInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                else
                {
                    FieldInfo fieldInfo = type.GetField(memberName, flags);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ConvertJTokenToType(value, fieldInfo.FieldType);
                        if (convertedValue != null)
                        {
                            fieldInfo.SetValue(target, convertedValue);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SetComponentProperty] Failed to set '{memberName}' on {type.Name}: {ex.Message}"
                );
            }
            return false;
        }

        /// <summary>
        /// Sets a nested property using dot notation (e.g., "material.color") or array access (e.g., "materials[0]")
        /// </summary>
        private bool SetNestedProperty(object target, string path, JsonNode value)
        {
            try
            {
                // Split the path into parts (handling both dot notation and array indexing)
                string[] pathParts = SplitPropertyPath(path);
                if (pathParts.Length == 0)
                    return false;

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
                    PropertyInfo propInfo = currentType.GetProperty(part, flags);
                    FieldInfo fieldInfo = null;
                    if (propInfo == null)
                    {
                        fieldInfo = currentType.GetField(part, flags);
                        if (fieldInfo == null)
                        {
                            Debug.LogWarning(
                                $"[SetNestedProperty] Could not find property or field '{part}' on type '{currentType.Name}'"
                            );
                            return false;
                        }
                    }

                    // Get the value
                    currentObject =
                        propInfo != null
                            ? propInfo.GetValue(currentObject)
                            : fieldInfo.GetValue(currentObject);

                    // If the current property is null, we need to stop
                    if (currentObject == null)
                    {
                        Debug.LogWarning(
                            $"[SetNestedProperty] Property '{part}' is null, cannot access nested properties."
                        );
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
                                Debug.LogWarning(
                                    $"[SetNestedProperty] Material index {arrayIndex} out of range (0-{materials.Length - 1})"
                                );
                                return false;
                            }
                            currentObject = materials[arrayIndex];
                        }
                        else if (currentObject is System.Collections.IList)
                        {
                            var list = currentObject as System.Collections.IList;
                            if (arrayIndex < 0 || arrayIndex >= list.Count)
                            {
                                Debug.LogWarning(
                                    $"[SetNestedProperty] Index {arrayIndex} out of range (0-{list.Count - 1})"
                                );
                                return false;
                            }
                            currentObject = list[arrayIndex];
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[SetNestedProperty] Property '{part}' is not an array or list, cannot access by index."
                            );
                            return false;
                        }
                    }

                    // Update type for next iteration
                    currentType = currentObject.GetType();
                }

                // Set the final property
                string finalPart = pathParts[pathParts.Length - 1];

                // Special handling for Material properties (shader properties)
                if (currentObject is Material material && finalPart.StartsWith("_"))
                {
                    // Handle various material property types
                    if (value is JsonArray JsonArray)
                    {
                        if (JsonArray.Count == 4) // Color with alpha
                        {
                            Color color = new Color(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat,
                                JsonArray[2].AsFloat,
                                JsonArray[3].AsFloat
                            );
                            material.SetColor(finalPart, color);
                            return true;
                        }
                        else if (JsonArray.Count == 3) // Color without alpha
                        {
                            Color color = new Color(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat,
                                JsonArray[2].AsFloat,
                                1.0f
                            );
                            material.SetColor(finalPart, color);
                            return true;
                        }
                        else if (JsonArray.Count == 2) // Vector2
                        {
                            Vector2 vec = new Vector2(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat
                            );
                            material.SetVector(finalPart, vec);
                            return true;
                        }
                        else if (JsonArray.Count == 4) // Vector4
                        {
                            Vector4 vec = new Vector4(
                                JsonArray[0].AsFloat,
                                JsonArray[1].AsFloat,
                                JsonArray[2].AsFloat,
                                JsonArray[3].AsFloat
                            );
                            material.SetVector(finalPart, vec);
                            return true;
                        }
                    }
                    else if (value.type == JsonNodeType.Float || value.type == JsonNodeType.Integer)
                    {
                        material.SetFloat(finalPart, value.AsFloat);
                        return true;
                    }
                    else if (value.type == JsonNodeType.Boolean)
                    {
                        material.SetFloat(finalPart, value.AsBool ? 1f : 0f);
                        return true;
                    }
                    else if (value.type == JsonNodeType.String)
                    {
                        // Might be a texture path
                        string texturePath = value.Value;
                        if (
                            texturePath.EndsWith(".png")
                            || texturePath.EndsWith(".jpg")
                            || texturePath.EndsWith(".tga")
                        )
                        {
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                texturePath
                            );
                            if (texture != null)
                            {
                                material.SetTexture(finalPart, texture);
                                return true;
                            }
                        }
                        else
                        {
                            // Materials don't have SetString, use SetTextureOffset as workaround or skip
                            Debug.LogWarning(
                                $"[SetNestedProperty] String values not directly supported for material property {finalPart}"
                            );
                            return false;
                        }
                    }

                    Debug.LogWarning(
                        $"[SetNestedProperty] Unsupported material property value type: {value.type} for {finalPart}"
                    );
                    return false;
                }

                // For standard properties (not shader specific)
                PropertyInfo finalPropInfo = currentType.GetProperty(finalPart, flags);
                if (finalPropInfo != null && finalPropInfo.CanWrite)
                {
                    object convertedValue = ConvertJTokenToType(value, finalPropInfo.PropertyType);
                    if (convertedValue != null)
                    {
                        finalPropInfo.SetValue(currentObject, convertedValue);
                        return true;
                    }
                }
                else
                {
                    FieldInfo finalFieldInfo = currentType.GetField(finalPart, flags);
                    if (finalFieldInfo != null)
                    {
                        object convertedValue = ConvertJTokenToType(
                            value,
                            finalFieldInfo.FieldType
                        );
                        if (convertedValue != null)
                        {
                            finalFieldInfo.SetValue(currentObject, convertedValue);
                            return true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[SetNestedProperty] Could not find final property or field '{finalPart}' on type '{currentType.Name}'"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SetNestedProperty] Error setting nested property '{path}': {ex.Message}"
                );
            }

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
                // Unwrap nested material properties if we're assigning to a Material
                if (typeof(Material).IsAssignableFrom(targetType) && token is JsonClass materialProps)
                {
                    // Handle case where we're passing shader properties directly in a nested object
                    string materialPath = token["path"]?.Value;
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        // Load the material by path
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material != null)
                        {
                            // If there are additional properties, set them
                            foreach (var propName in materialProps.GetKeys())
                            {
                                if (propName != "path")
                                {
                                    SetComponentProperty(material, propName, materialProps[propName]);
                                }
                            }
                            return material;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Could not load material at path: '{materialPath}'"
                            );
                            return null;
                        }
                    }

                    // If no path is specified, could be a dynamic material or instance set by reference
                    return null;
                }

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
                if (targetType == typeof(Vector2) && token is JsonArray arrV2 && arrV2.Count == 2)
                    return new Vector2(arrV2[0].AsFloat, arrV2[1].AsFloat);
                if (targetType == typeof(Vector3) && token is JsonArray arrV3 && arrV3.Count == 3)
                    return new Vector3(
                        arrV3[0].AsFloat,
                        arrV3[1].AsFloat,
                        arrV3[2].AsFloat
                    );
                if (targetType == typeof(Vector4) && token is JsonArray arrV4 && arrV4.Count == 4)
                    return new Vector4(
                        arrV4[0].AsFloat,
                        arrV4[1].AsFloat,
                        arrV4[2].AsFloat,
                        arrV4[3].AsFloat
                    );
                if (targetType == typeof(Quaternion) && token is JsonArray arrQ && arrQ.Count == 4)
                    return new Quaternion(
                        arrQ[0].AsFloat,
                        arrQ[1].AsFloat,
                        arrQ[2].AsFloat,
                        arrQ[3].AsFloat
                    );
                if (targetType == typeof(Color) && token is JsonArray arrC && arrC.Count >= 3) // Allow RGB or RGBA
                    return new Color(
                        arrC[0].AsFloat,
                        arrC[1].AsFloat,
                        arrC[2].AsFloat,
                        arrC.Count > 3 ? arrC[3].AsFloat : 1.0f
                    );

                // Enum types
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, token.Value, true); // Case-insensitive enum parsing

                // Handle assigning Unity Objects (Assets, Scene Objects, Components)
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    // CASE 1: Reference is a Json Object specifying a scene object/component find criteria
                    if (token is JsonClass refObject)
                    {
                        JsonNode findToken = refObject["find"];
                        string findMethod =
                            refObject["method"]?.Value ?? "by_id_or_name_or_path"; // Default search
                        string componentTypeName = refObject["component"]?.Value;

                        if (findToken == null)
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Reference object missing 'find' property: {token}"
                            );
                            return null;
                        }

                        // Find the target GameObject
                        // Pass 'searchInactive: true' for internal lookups to be more robust
                        JsonClass findParams = new JsonClass();
                        findParams["searchInactive"] = new JsonData("true");
                        GameObject foundGo = GameObjectUtils.FindObjectInternal(findToken, findMethod, findParams);

                        if (foundGo == null)
                        {
                            Debug.LogWarning(
                                $"[ConvertJTokenToType] Could not find GameObject specified by reference object: {token}"
                            );
                            return null;
                        }

                        // If a component type is specified, try to get it
                        if (!string.IsNullOrEmpty(componentTypeName))
                        {
                            Type compType = FindComponentType(componentTypeName);
                            if (compType == null)
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Could not find component type '{componentTypeName}' specified in reference object: {token}"
                                );
                                return null;
                            }

                            // Ensure the targetType is assignable from the found component type
                            if (!targetType.IsAssignableFrom(compType))
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found component '{componentTypeName}' but it is not assignable to the target property type '{targetType.Name}'. Reference: {token}"
                                );
                                return null;
                            }

                            Component foundComp = foundGo.GetComponent(compType);
                            if (foundComp == null)
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found GameObject '{foundGo.name}' but could not find component '{componentTypeName}' on it. Reference: {token}"
                                );
                                return null;
                            }
                            return foundComp; // Return the found component
                        }
                        else
                        {
                            // Otherwise, return the GameObject itself, ensuring it's assignable
                            if (!targetType.IsAssignableFrom(typeof(GameObject)))
                            {
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Found GameObject '{foundGo.name}' but it is not assignable to the target property type '{targetType.Name}' (component name was not specified). Reference: {token}"
                                );
                                return null;
                            }
                            return foundGo; // Return the found GameObject
                        }
                    }
                    // CASE 2: Reference is a string, assume it's an asset path
                    else if (token.type == JsonNodeType.String)
                    {
                        string assetPath = token.Value;
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Attempt to load the asset from the provided path using the target type
                            UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath(
                                assetPath,
                                targetType
                            );
                            if (loadedAsset != null)
                            {
                                return loadedAsset; // Return the loaded asset if successful
                            }
                            else
                            {
                                // Log a warning if the asset could not be found at the path
                                Debug.LogWarning(
                                    $"[ConvertJTokenToType] Could not load asset of type '{targetType.Name}' from path: '{assetPath}'. Make sure the path is correct and the asset exists."
                                );
                                return null;
                            }
                        }
                        else
                        {
                            // Handle cases where an empty string might be intended to clear the reference
                            return null; // Assign null if the path is empty
                        }
                    }
                    // CASE 3: Reference is null or empty JsonNode, assign null
                    else if (
                        token.type == JsonNodeType.Null
                        || string.IsNullOrEmpty(token.Value)
                    )
                    {
                        return null;
                    }
                    // CASE 4: Invalid format for Unity Object reference
                    else
                    {
                        Debug.LogWarning(
                            $"[ConvertJTokenToType] Expected a string asset path or a reference object to assign Unity Object of type '{targetType.Name}', but received token type '{token.type}'. Value: {token}"
                        );
                        return null;
                    }
                }

                // Fallback: For other types, return null
                // Complex types should be handled by specific cases above
                Debug.LogWarning(
                    $"[ConvertJTokenToType] No conversion handler for type '{targetType.Name}'. Token: {token}"
                );
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[ConvertJTokenToType] Could not convert JsonNode '{token}' to type '{targetType.Name}': {ex.Message}"
                );
                return null;
            }
        }

        #endregion

        #region 辅助查找方法

        /// <summary>
        /// 查找父GameObject（用于设置父级关系）
        /// </summary>
        private GameObject FindParentGameObject(JsonNode parentToken)
        {
            if (parentToken == null || parentToken.type == JsonNodeType.Null)
                return null;

            string parentIdentifier = parentToken.Value;
            if (string.IsNullOrEmpty(parentIdentifier))
                return null;

            // 使用GameObjectDynamicSelector来查找父对象
            var selector = new ObjectSelector<GameObject>();
            JsonClass findArgs = new JsonClass();
            findArgs["id"] = parentIdentifier;
            findArgs["path"] = parentIdentifier;
            var stateTree = selector.BuildStateTree();
            object result = stateTree.Run(new StateTreeContext(findArgs));

            if (result is GameObject[] gameObjects && gameObjects.Length > 0)
            {
                return gameObjects[0]; // 返回找到的第一个对象
            }

            return null;
        }

        /// <summary>
        /// 查找组件类型
        /// </summary>
        /// <summary>
        /// 查找组件类型，遍历所有已加载的程序集，名字或全名匹配且继承自Component即可
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
        /// 添加组件到GameObject的内部实现
        /// </summary>
        private object AddComponentInternal(GameObject targetGo, string typeName, JsonClass properties)
        {
            Type componentType = FindComponentType(typeName);
            if (componentType == null)
            {
                return Response.Error($"Component type '{typeName}' not found.");
            }

            // 检查是否已经存在该组件（对于不允许重复的组件）
            if (componentType == typeof(Transform) || componentType == typeof(RectTransform))
            {
                return Response.Error($"Cannot add component '{typeName}' because it already exists or is not allowed to be duplicated.");
            }

            try
            {
                Component addedComponent = targetGo.GetComponent(componentType);
                if (addedComponent == null)
                    addedComponent = targetGo.AddComponent(componentType);
                if (addedComponent == null)
                {
                    return Response.Error($"Failed to add component '{typeName}' to '{targetGo.name}'.");
                }

                Undo.RegisterCreatedObjectUndo(addedComponent, $"Add Component {typeName}");
                LogInfo($"[EditGameObject] Successfully added component '{typeName}' to '{targetGo.name}'");

                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error($"Error adding component '{typeName}' to '{targetGo.name}': {e.Message}");
            }
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

        #endregion
    }
}