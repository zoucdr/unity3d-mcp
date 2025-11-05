using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniMcp.Models;

namespace UniMcp
{
    /// <summary>
    /// GameObject操作的通用工具类
    /// </summary>
    public static class GameObjectUtils
    {
        /// <summary>
        /// 根据Token和搜索方法查找单个GameObject
        /// </summary>
        public static GameObject FindObjectInternal(
            JsonNode targetToken,
            string searchMethod,
            JsonClass findParams = null
        )
        {
            // If find_all is not explicitly false, we still want only one for most single-target operations.
            bool findAll = findParams != null && findParams["find_all"] != null ? findParams["find_all"].AsBoolDefault(false) : false;
            // If a specific target ID is given, always find just that one.
            if (
                targetToken?.type == JsonNodeType.Integer
                || (searchMethod == "by_id" && int.TryParse(targetToken?.Value, out _))
            )
            {
                findAll = false;
            }
            List<GameObject> results = FindObjectsInternal(
                targetToken,
                searchMethod,
                findAll,
                findParams
            );
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// 根据Token和搜索方法查找多个GameObject
        /// </summary>
        public static List<GameObject> FindObjectsInternal(
            JsonNode targetToken,
            string searchMethod,
            bool findAll,
            JsonClass findParams = null
        )
        {
            List<GameObject> results = new List<GameObject>();
            string searchTerm = findParams?["search_term"]?.Value ?? targetToken?.Value;
            bool searchInChildren = findParams != null && findParams["search_in_children"] != null ? findParams["search_in_children"].AsBoolDefault(false) : false;
            bool searchInactive = findParams != null && findParams["search_in_inactive"] != null ? findParams["search_in_inactive"].AsBoolDefault(false) : false;

            // Default search method if not specified
            if (string.IsNullOrEmpty(searchMethod))
            {
                if (targetToken?.type == JsonNodeType.Integer)
                    searchMethod = "by_id";
                else if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Contains('/'))
                    searchMethod = "by_path";
                else
                    searchMethod = "by_name"; // Default fallback
            }

            GameObject rootSearchObject = null;
            // If searching in children, find the initial target first
            if (searchInChildren && targetToken != null)
            {
                rootSearchObject = FindObjectInternal(targetToken, "by_id_or_name_or_path");
                if (rootSearchObject == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[GameObjectUtils.Find] Root object '{targetToken}' for child search not found."
                    );
                    return results;
                }
            }

            switch (searchMethod)
            {
                case "by_id":
                    if (int.TryParse(searchTerm, out int instanceId))
                    {
                        var allObjects = GetAllSceneObjects(searchInactive);
                        GameObject obj = allObjects.FirstOrDefault(go =>
                            go.GetInstanceID() == instanceId
                        );
                        if (obj != null)
                            results.Add(obj);
                    }
                    break;
                case "by_name":
                    var searchPoolName = rootSearchObject
                        ? rootSearchObject
                            .GetComponentsInChildren<Transform>(searchInactive)
                            .Select(t => t.gameObject)
                        : GetAllSceneObjects(searchInactive);
                    results.AddRange(searchPoolName.Where(go => go.name == searchTerm));
                    break;
                case "by_path":
                    Transform foundTransform = rootSearchObject
                        ? rootSearchObject.transform.Find(searchTerm)
                        : GameObject.Find(searchTerm)?.transform;
                    if (foundTransform != null)
                        results.Add(foundTransform.gameObject);
                    break;
                case "by_tag":
                    var searchPoolTag = rootSearchObject
                        ? rootSearchObject
                            .GetComponentsInChildren<Transform>(searchInactive)
                            .Select(t => t.gameObject)
                        : GetAllSceneObjects(searchInactive);
                    results.AddRange(searchPoolTag.Where(go => go.CompareTag(searchTerm)));
                    break;
                case "by_layer":
                    var searchPoolLayer = rootSearchObject
                        ? rootSearchObject
                            .GetComponentsInChildren<Transform>(searchInactive)
                            .Select(t => t.gameObject)
                        : GetAllSceneObjects(searchInactive);
                    if (int.TryParse(searchTerm, out int layerIndex))
                    {
                        results.AddRange(searchPoolLayer.Where(go => go.layer == layerIndex));
                    }
                    else
                    {
                        int namedLayer = LayerMask.NameToLayer(searchTerm);
                        if (namedLayer != -1)
                            results.AddRange(searchPoolLayer.Where(go => go.layer == namedLayer));
                    }
                    break;
                case "by_component":
                    Type componentType = FindType(searchTerm);
                    if (componentType != null)
                    {
                        FindObjectsInactive findInactive = searchInactive
                            ? FindObjectsInactive.Include
                            : FindObjectsInactive.Exclude;
                        var searchPoolComp = rootSearchObject
                            ? rootSearchObject
                                .GetComponentsInChildren(componentType, searchInactive)
                                .Select(c => (c as Component).gameObject)
                            : UnityEngine
                                .Object.FindObjectsByType(
                                    componentType,
                                    findInactive,
                                    FindObjectsSortMode.None
                                )
                                .Select(c => (c as Component).gameObject);
                        results.AddRange(searchPoolComp.Where(go => go != null));
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[GameObjectUtils.Find] Component type not found: {searchTerm}"
                        );
                    }
                    break;
                case "by_id_or_name_or_path":
                    if (int.TryParse(searchTerm, out int id))
                    {
                        var allObjectsId = GetAllSceneObjects(true);
                        GameObject objById = allObjectsId.FirstOrDefault(go =>
                            go.GetInstanceID() == id
                        );
                        if (objById != null)
                        {
                            results.Add(objById);
                            break;
                        }
                    }
                    GameObject objByPath = GameObject.Find(searchTerm);
                    if (objByPath != null)
                    {
                        results.Add(objByPath);
                        break;
                    }

                    var allObjectsName = GetAllSceneObjects(true);
                    results.AddRange(allObjectsName.Where(go => go.name == searchTerm));
                    break;
                default:
                    UnityEngine.Debug.LogWarning(
                        $"[GameObjectUtils.Find] Unknown search method: {searchMethod}"
                    );
                    break;
            }

            if (!findAll && results.Count > 1)
            {
                return new List<GameObject> { results[0] };
            }

            return results.Distinct().ToList();
        }

        /// <summary>
        /// 简单查找GameObject，用于父对象查找等场景
        /// </summary>
        public static GameObject FindObjectByIdOrPath(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return null;

            // 尝试按ID查找
            if (int.TryParse(searchTerm, out int id))
            {
                var allObjects = GetAllSceneObjects(true);
                GameObject objById = allObjects.FirstOrDefault(go => go.GetInstanceID() == id);
                if (objById != null)
                    return objById;
            }
            var go = FindByHierarchyPath(searchTerm, typeof(GameObject));
            if (go != null)
                return go as GameObject;
            return null;
        }


        /// <summary>
        /// 通过Hierarchy路径在当前场景中查找对象
        /// </summary>
        /// <param name="path">Hierarchy path, like "Parent/Child/Target" or "Parent/Child/Target:ComponentType"</param>
        /// <param name="type">查找类型</param>
        /// <returns>找到的对象，未找到则返回null</returns>
        public static object FindByHierarchyPath(string path, Type type)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 检查是否包含组件类型指定符 ":"
            string gameObjectPath = path;
            string componentTypeName = null;

            if (path.Contains(':'))
            {
                var parts = path.Split(':');
                gameObjectPath = parts[0];
                componentTypeName = parts.Length > 1 ? parts[1] : null;
            }

            // 获取当前活动场景中的所有根对象
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                return null;
            }

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            // 分割路径
            string[] pathSegments = gameObjectPath.Split('/');
            if (pathSegments.Length == 0)
            {
                return null;
            }

            // 递归查找所有可能的路径
            List<GameObject> currentLevel = new List<GameObject>();
            // 首先找到所有名字匹配的根对象
            foreach (var rootObject in rootObjects)
            {
                if (rootObject.name == pathSegments[0])
                {
                    currentLevel.Add(rootObject);
                }
            }

            if (currentLevel.Count == 0)
            {
                return null;
            }

            // 逐层查找
            for (int i = 1; i < pathSegments.Length; i++)
            {
                string segment = pathSegments[i];
                List<GameObject> nextLevel = new List<GameObject>();
                foreach (var parent in currentLevel)
                {
                    // 这里不能用Find，因为Find只会返回第一个匹配的
                    for (int j = 0; j < parent.transform.childCount; j++)
                    {
                        var child = parent.transform.GetChild(j);
                        if (child.name == segment)
                        {
                            nextLevel.Add(child.gameObject);
                        }
                    }
                }
                if (nextLevel.Count == 0)
                {
                    return null;
                }
                currentLevel = nextLevel;
            }

            // 最终所有匹配的对象都在currentLevel里，返回最后一个类型匹配的（新创建的对象通常在后面）
            object lastMatch = null;
            foreach (var obj in currentLevel)
            {
                // 如果指定了组件类型名，优先使用指定的组件类型
                if (!string.IsNullOrEmpty(componentTypeName))
                {
                    Type specifiedComponentType = FindType(componentTypeName);
                    if (specifiedComponentType != null && typeof(UnityEngine.Component).IsAssignableFrom(specifiedComponentType))
                    {
                        var comp = obj.GetComponent(specifiedComponentType);
                        if (comp != null)
                            lastMatch = comp; // 记录最后匹配的组件
                    }
                    continue;
                }

                // 如果type是GameObject类型，直接返回
                if (type == typeof(GameObject))
                {
                    lastMatch = obj; // 记录最后匹配的GameObject
                }
                // 如果T是Component类型，尝试获取组件
                else if (typeof(UnityEngine.Component).IsAssignableFrom(type))
                {
                    var comp = obj.GetComponent(type);
                    if (comp != null)
                        lastMatch = comp; // 记录最后匹配的组件
                }
            }

            // 返回最后匹配的对象
            if (lastMatch != null)
                return lastMatch;

            return null;
        }
        /// <summary>
        /// 通过Hierarchy路径在当前场景中查找对象（泛型版本）
        /// 支持场景中有多个重名对象的路径查找，每一层都可能有多个同名对象，需逐层递归查找
        /// </summary>
        /// <param name="path">Hierarchy路径，支持两种格式：
        /// 1. "Parent/Child/Target" - 只指定GameObject路径
        /// 2. "Parent/Child/Target:ComponentType" - 指定GameObject路径和组件类型
        /// </param>
        /// <returns>找到的对象，未找到则返回default(T)</returns>
        public static T FindByHierarchyPath<T>(string path)
        {
            var obj = FindByHierarchyPath(path, typeof(T));
            if (obj is T o)
                return o;
            return default(T);
        }

        /// <summary>
        /// 获取场景中的所有GameObject
        /// </summary>
        public static IEnumerable<GameObject> GetAllSceneObjects(bool includeInactive)
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var allObjects = new List<GameObject>();
            foreach (var root in rootObjects)
            {
                allObjects.AddRange(
                    root.GetComponentsInChildren<Transform>(includeInactive)
                        .Select(t => t.gameObject)
                );
            }
            return allObjects;
        }

        /// <summary>
        /// 根据类型名称查找Type，搜索相关程序集
        /// </summary>
        public static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var type =
                Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule")
                ?? Type.GetType($"UnityEngine.{typeName}, UnityEngine.PhysicsModule")
                ?? Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI")
                ?? Type.GetType($"UnityEditor.{typeName}, UnityEditor.CoreModule")
                ?? Type.GetType(typeName);

            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEngine." + typeName);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEditor." + typeName);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEngine.UI." + typeName);
                if (type != null)
                    return type;
                foreach (var typeNext in assembly.GetTypes())
                {
                    if (typeNext.Name == typeName)
                        return typeNext;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析JArray为Vector3
        /// </summary>
        public static Vector3? ParseVector3(JsonArray array)
        {
            if (array != null && array.Count == 3)
            {
                try
                {
                    return new Vector3(
                        array[0].AsFloat,
                        array[1].AsFloat,
                        array[2].AsFloat
                    );
                }
                catch
                {
                }
            }
            return null;
        }

        /// <summary>
        /// 创建GameObject的可序列化表示，返回JSONClass
        /// </summary>
        public static JsonClass GetGameObjectData(GameObject go)
        {
            if (go == null)
                return null;

            // 使用YAML格式的紧凑表示
            var yamlData = GetGameObjectDataYaml(go);

            JsonClass result = new JsonClass();
            result["yaml"] = yamlData;
            return result;
        }

        /// <summary>
        /// 创建GameObject的YAML格式数据表示（节省token）
        /// </summary>
        public static string GetGameObjectDataYaml(GameObject go)
        {
            if (go == null)
                return "null";

            var components = go.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();

            var yaml = $@"name: {go.name}
id: {go.GetInstanceID()}
tag: {go.tag}
layer: {go.layer}
pos: [{go.transform.position.x:F1}, {go.transform.position.y:F1}, {go.transform.position.z:F1}]
localPos: [{go.transform.localPosition.x:F1}, {go.transform.localPosition.y:F1}, {go.transform.localPosition.z:F1}]
rot: [{go.transform.eulerAngles.x:F1}, {go.transform.eulerAngles.y:F1}, {go.transform.eulerAngles.z:F1}]
scale: [{go.transform.localScale.x:F1}, {go.transform.localScale.y:F1}, {go.transform.localScale.z:F1}]
active: {go.activeSelf.ToString().ToLower()}
activeInHierarchy: {go.activeInHierarchy.ToString().ToLower()}
static: {go.isStatic.ToString().ToLower()}
components: [{string.Join(", ", components)}]
path: {GetHierarchyPath(go)}
scene: {go.scene.name}";

            // 添加父对象信息
            if (go.transform.parent != null)
            {
                yaml += $"\nparent: {go.transform.parent.gameObject.name}";
                yaml += $"\nparentId: {go.transform.parent.gameObject.GetInstanceID()}";
            }

            // 如果有子对象，添加子对象信息
            if (go.transform.childCount > 0)
            {
                var children = new List<string>();
                var childIds = new List<int>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        children.Add(child.gameObject.name);
                        childIds.Add(child.gameObject.GetInstanceID());
                    }
                }
                if (children.Count > 0)
                {
                    yaml += $"\nchildren: [{string.Join(", ", children)}]";
                    yaml += $"\nchildrenIds: [{string.Join(", ", childIds)}]";
                }
            }

            return yaml;
        }

        /// <summary>
        /// 创建子对象列表，递归记录所有层级的子物体信息
        /// </summary>
        private static List<object> CreateChildIdMap(GameObject go)
        {
            var childList = new List<object>();

            if (go == null || go.transform == null)
                return childList;

            // 递归遍历所有子对象
            CollectChildrenRecursively(go.transform, childList);
            return childList;
        }

        /// <summary>
        /// 递归收集子对象信息
        /// </summary>
        private static void CollectChildrenRecursively(Transform parent, List<object> childList)
        {
            if (parent == null)
                return;

            // 遍历所有直接子对象
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    var childInfo = new
                    {
                        name = child.gameObject.name,
                        instance_id = child.gameObject.GetInstanceID(),
                        hierarchy_path = GetHierarchyPath(child.gameObject)
                    };

                    childList.Add(childInfo);

                    // 递归处理子对象的子对象
                    if (child.childCount > 0)
                    {
                        CollectChildrenRecursively(child, childList);
                    }
                }
            }
        }

        /// <summary>
        /// 获取GameObject的完整层级路径
        /// </summary>
        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null)
                return string.Empty;

            string path = go.name;
            Transform current = go.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        // --- GameObject Configuration Methods ---

        /// <summary>
        /// 预先选中父对象（如果指定了父对象参数）
        /// 用于创建对象前选中父对象，确保Unity创建操作能正确识别父子关系
        /// </summary>
        public static void PreselectParentIfSpecified(JsonClass args, Action<string> logAction = null)
        {
            // 检查是否指定了父对象
            string parentPath = args["parent"]?.Value;
            string parentIdStr = args["parent_id"]?.Value;
            string parentPathParam = args["parent_path"]?.Value;

            if (string.IsNullOrEmpty(parentPath) &&
                string.IsNullOrEmpty(parentIdStr) &&
                string.IsNullOrEmpty(parentPathParam))
            {
                // 没有指定父对象，不需要预选中
                return;
            }

            GameObject parentObject = null;

            // 优先使用 parent_id
            if (!string.IsNullOrEmpty(parentIdStr) && int.TryParse(parentIdStr, out int parentId))
            {
                parentObject = EditorUtility.InstanceIDToObject(parentId) as GameObject;
                if (parentObject != null)
                {
                    logAction?.Invoke($"[GameObjectUtils] Found parent by ID: '{parentObject.name}' (ID: {parentId})");
                }
            }

            // 如果通过 ID 未找到，尝试使用 parent 或 parent_path
            if (parentObject == null)
            {
                string searchPath = !string.IsNullOrEmpty(parentPath) ? parentPath : parentPathParam;
                if (!string.IsNullOrEmpty(searchPath))
                {
                    parentObject = FindObjectByIdOrPath(searchPath);
                    if (parentObject != null)
                    {
                        logAction?.Invoke($"[GameObjectUtils] Found parent by path: '{parentObject.name}' at '{searchPath}'");
                    }
                }
            }

            // 如果找到父对象，选中它
            if (parentObject != null)
            {
                Selection.activeGameObject = parentObject;
                logAction?.Invoke($"[GameObjectUtils] Pre-selected parent GameObject: '{parentObject.name}' (ID: {parentObject.GetInstanceID()})");
            }
            else
            {
                logAction?.Invoke($"[GameObjectUtils] Parent object specified but not found. Parent will be set during finalization.");
            }
        }

        /// <summary>
        /// 应用通用GameObject设置
        /// </summary>
        public static void ApplyCommonGameObjectSettings(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {

            // 设置名称
            ApplyNameSetting(args, newGo, logAction);

            // 设置父对象
            ApplyParentSetting(args, newGo, logAction);

            // 设置变换
            ApplyTransformSettings(args, newGo);

            // 设置标签
            ApplyTagSetting(args, newGo, logAction);

            // 设置层
            ApplyLayerSetting(args, newGo, logAction);

            // 添加组件
            ApplyComponentsToAdd(args, newGo, logAction);

            //设置组件属性
            ApplyComponentProperties(args, newGo, logAction);

            // 设置激活状态
            bool? setActive = args["active"] != null && !args["active"].IsNull()
                ? (bool?)args["active"].AsBool
                : null;
            if (setActive.HasValue)
            {
                newGo.SetActive(setActive.Value);
            }
        }
        /// <summary>
        /// 应用名称设置
        /// </summary>
        public static void ApplyNameSetting(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            string name = args["name"]?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                newGo.name = name;
            }
        }
        /// <summary>
        /// 应用父对象设置
        /// </summary>
        public static void ApplyParentSetting(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            string parentToken = args["parent_id"].Value;
            if (string.IsNullOrEmpty(parentToken))
                parentToken = args["parent_path"].Value;
            if (string.IsNullOrEmpty(parentToken))
                parentToken = args["parent"].Value;
            if (!string.IsNullOrEmpty(parentToken))
            {
                GameObject parentGo = FindObjectByIdOrPath(parentToken);
                if (parentGo == null)
                {
                    logAction?.Invoke($"Parent specified ('{parentToken}') but not found.");
                    return;
                }
                newGo.transform.SetParent(parentGo.transform, true);
            }
        }

        /// <summary>
        /// 应用变换设置
        /// </summary>
        public static void ApplyTransformSettings(JsonClass args, GameObject newGo)
        {
            Vector3? position = ParseVector3(args["position"] as JsonArray);
            Vector3? rotation = ParseVector3(args["rotation"] as JsonArray);
            Vector3? scale = ParseVector3(args["scale"] as JsonArray);

            if (position.HasValue)
                newGo.transform.localPosition = position.Value;
            if (rotation.HasValue)
                newGo.transform.localEulerAngles = rotation.Value;
            if (scale.HasValue)
                newGo.transform.localScale = scale.Value;
        }

        /// <summary>
        /// 应用标签设置
        /// </summary>
        public static void ApplyTagSetting(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            string tag = args["tag"]?.Value;
            if (!string.IsNullOrEmpty(tag))
            {
                string tagToSet = string.IsNullOrEmpty(tag) ? "Untagged" : tag;
                try
                {
                    newGo.tag = tagToSet;
                }
                catch (UnityException ex)
                {
                    if (ex.Message.Contains("is not defined"))
                    {
                        logAction?.Invoke($"Tag '{tagToSet}' not found. Attempting to create it.");
                        try
                        {
                            InternalEditorUtility.AddTag(tagToSet);
                            newGo.tag = tagToSet;
                            logAction?.Invoke($"Tag '{tagToSet}' created and assigned successfully.");
                        }
                        catch (Exception innerEx)
                        {
                            logAction?.Invoke($"Failed to create or assign tag '{tagToSet}': {innerEx.Message}");
                        }
                    }
                    else
                    {
                        logAction?.Invoke($"Failed to set tag to '{tagToSet}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 应用层设置
        /// </summary>
        public static void ApplyLayerSetting(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            string layerName = args["layer"]?.Value;
            if (!string.IsNullOrEmpty(layerName))
            {
                int layerId = LayerMask.NameToLayer(layerName);
                if (layerId != -1)
                {
                    newGo.layer = layerId;
                }
                else
                {
                    logAction?.Invoke($"Layer '{layerName}' not found. Using default layer.");
                }
            }
        }

        /// <summary>
        /// 应用组件添加
        /// </summary>
        public static void ApplyComponentsToAdd(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            if (args["components"] is JsonArray componentsToAddArray)
            {
                foreach (JsonNode compToken in componentsToAddArray)
                {
                    string typeName = null;
                    JsonClass properties = null;

                    if (compToken.type == JsonNodeType.String)
                    {
                        typeName = compToken.Value;
                    }
                    else if (compToken is JsonClass compObj)
                    {
                        typeName = compObj["type_name"]?.Value;
                        properties = compObj["properties"] as JsonClass;
                    }

                    if (!string.IsNullOrEmpty(typeName))
                    {
                        var addResult = AddComponentInternal(newGo, typeName, properties);
                        if (addResult != null)
                        {
                            logAction?.Invoke($"Failed to add component '{typeName}': {addResult}");
                        }
                    }
                    else
                    {
                        logAction?.Invoke($"Invalid component format in components: {compToken}");
                    }
                }
            }
        }

        /// <summary>
        /// 应用组件属性设置
        /// </summary>
        public static void ApplyComponentProperties(JsonClass args, GameObject newGo, Action<string> logAction = null)
        {
            // 处理component_properties
            if (args["component_properties"] is JsonClass componentPropsObj)
            {
                foreach (KeyValuePair<string, JsonNode> componentProp in componentPropsObj.Properties())
                {
                    string componentName = componentProp.Key;
                    if (componentProp.Value is JsonClass properties)
                    {
                        SetComponentPropertiesInternal(newGo, componentName, properties, logAction);
                    }
                }
            }

            // 处理单个component_type和component_properties的情况
            string singleComponentName = args["component_type"]?.Value;
            if (!string.IsNullOrEmpty(singleComponentName) && args["component_properties"] is JsonClass singleProps)
            {
                // 检查是否是嵌套结构
                if (singleProps[singleComponentName] is JsonClass nestedProps)
                {
                    SetComponentPropertiesInternal(newGo, singleComponentName, nestedProps, logAction);
                }
                else
                {
                    // 直接使用属性对象
                    SetComponentPropertiesInternal(newGo, singleComponentName, singleProps, logAction);
                }
            }
        }

        /// <summary>
        /// 设置组件属性的内部方法
        /// </summary>
        private static void SetComponentPropertiesInternal(
            GameObject targetGo,
            string componentName,
            JsonClass properties,
            Action<string> logAction = null
        )
        {
            if (properties == null || properties.Count == 0)
                return;

            // 查找组件类型
            Type componentType = FindType(componentName);
            Component targetComponent = null;

            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
            {
                targetComponent = targetGo.GetComponent(componentType);
            }
            else
            {
                // 尝试常见的Unity组件命名空间
                string[] commonNamespaces = { "UnityEngine", "UnityEngine.UI" };
                foreach (string ns in commonNamespaces)
                {
                    string fullTypeName = $"{ns}.{componentName}";
                    componentType = FindType(fullTypeName);
                    if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                    {
                        targetComponent = targetGo.GetComponent(componentType);
                        break;
                    }
                }
            }

            if (targetComponent == null)
            {
                logAction?.Invoke($"Component '{componentName}' not found on '{targetGo.name}' to set properties.");
                return;
            }

            Undo.RecordObject(targetComponent, "Set Component Properties");

            foreach (KeyValuePair<string, JsonNode> prop in properties.Properties())
            {
                string propName = prop.Key;
                JsonNode propValue = prop.Value;

                try
                {
                    if (!SetObjectPropertyDeepth(targetComponent, propName, propValue, logAction))
                    {
                        logAction?.Invoke($"Could not set property '{propName}' on component '{componentName}'. Property might not exist, be read-only, or type mismatch.");
                    }
                }
                catch (Exception e)
                {
                    logAction?.Invoke($"Error setting property '{propName}' on '{componentName}': {e.Message}");
                }
            }

            EditorUtility.SetDirty(targetComponent);
        }

        /// <summary>
        /// 设置组件属性
        /// </summary>
        public static bool SetObjectPropertyDeepth(object target, string memberName, JsonNode value, Action<string> logAction = null)
        {
            Type type = target.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            try
            {
                // 处理材质属性的特殊情况
                if (memberName.Equals("material", StringComparison.OrdinalIgnoreCase) && value.type == JsonNodeType.String)
                {
                    string materialPath = value.Value;
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material != null)
                        {
                            PropertyInfo materialProp = type.GetProperty("material", flags);
                            if (materialProp != null && materialProp.CanWrite)
                            {
                                materialProp.SetValue(target, material);
                                logAction?.Invoke($"Set material to '{materialPath}' on {type.Name}");
                                return true;
                            }
                        }
                        else
                        {
                            logAction?.Invoke($"Could not load material at path: '{materialPath}'");
                            return false;
                        }
                    }
                }

                // 处理嵌套属性 (如 material.color)
                if (memberName.Contains('.'))
                {
                    return SetNestedProperty(target, memberName, value, logAction);
                }

                // 处理普通属性
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
                logAction?.Invoke($"Failed to set '{memberName}' on {type.Name}: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 设置嵌套属性
        /// </summary>
        private static bool SetNestedProperty(object target, string path, JsonNode value, Action<string> logAction = null)
        {
            string[] pathParts = path.Split('.');
            if (pathParts.Length < 2)
                return false;

            object currentObject = target;
            Type currentType = currentObject.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            // 遍历到最后一个属性之前
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                string part = pathParts[i];
                PropertyInfo propInfo = currentType.GetProperty(part, flags);
                if (propInfo != null)
                {
                    currentObject = propInfo.GetValue(currentObject);
                    if (currentObject == null)
                    {
                        logAction?.Invoke($"Property '{part}' is null, cannot access nested properties.");
                        return false;
                    }
                    currentType = currentObject.GetType();
                }
                else
                {
                    logAction?.Invoke($"Could not find property '{part}' on type '{currentType.Name}'");
                    return false;
                }
            }

            // 设置最终属性
            string finalPart = pathParts[pathParts.Length - 1];
            return SetObjectPropertyDeepth(currentObject, finalPart, value, logAction);
        }

        /// <summary>
        /// 转换JToken为指定类型
        /// </summary>
        private static object ConvertJTokenToType(JsonNode token, Type targetType)
        {
            try
            {
                if (targetType == typeof(string))
                    return token.Value;
                if (targetType == typeof(int))
                    return token.AsInt;
                if (targetType == typeof(float))
                    return token.AsFloat;
                if (targetType == typeof(bool))
                    return token.AsBool;

                // Vector类型
                if (targetType == typeof(Vector2) && token is JsonArray arrV2 && arrV2.Count == 2)
                    return new Vector2(arrV2[0].AsFloat, arrV2[1].AsFloat);
                if (targetType == typeof(Vector3) && token is JsonArray arrV3 && arrV3.Count == 3)
                    return new Vector3(arrV3[0].AsFloat, arrV3[1].AsFloat, arrV3[2].AsFloat);
                if (targetType == typeof(Vector4) && token is JsonArray arrV4 && arrV4.Count == 4)
                    return new Vector4(arrV4[0].AsFloat, arrV4[1].AsFloat, arrV4[2].AsFloat, arrV4[3].AsFloat);

                // Color类型
                if (targetType == typeof(Color) && token is JsonArray arrC && arrC.Count >= 3)
                    return new Color(arrC[0].AsFloat, arrC[1].AsFloat, arrC[2].AsFloat, arrC.Count > 3 ? arrC[3].AsFloat : 1.0f);

                // 枚举类型
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, token.Value, true);

                // Unity Object类型（Material, Texture等）
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType) && token.type == JsonNodeType.String)
                {
                    string assetPath = token.Value;
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        return AssetDatabase.LoadAssetAtPath(assetPath, targetType);
                    }
                }

                // 尝试直接转换
                return token.ToObject(targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 添加组件的内部方法（包含物理组件冲突检查）
        /// Returns null on success, or an error response object on failure.
        /// </summary>
        public static object AddComponentInternal(
            GameObject targetGo,
            string typeName,
            JsonClass properties = null
        )
        {
            Type componentType = FindType(typeName);
            if (componentType == null)
            {
                return Response.Error(
                    $"Component type '{typeName}' not found or is not a valid Component."
                );
            }
            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                return Response.Error($"Type '{typeName}' is not a Component.");
            }

            // Prevent adding Transform again
            if (componentType == typeof(Transform))
            {
                return Response.Error("Cannot add another Transform component.");
            }

            // Check for 2D/3D physics component conflicts
            bool isAdding2DPhysics =
                typeof(Rigidbody2D).IsAssignableFrom(componentType)
                || typeof(Collider2D).IsAssignableFrom(componentType);
            bool isAdding3DPhysics =
                typeof(Rigidbody).IsAssignableFrom(componentType)
                || typeof(Collider).IsAssignableFrom(componentType);

            if (isAdding2DPhysics)
            {
                // Check if the GameObject already has any 3D Rigidbody or Collider
                if (
                    targetGo.GetComponent<Rigidbody>() != null
                    || targetGo.GetComponent<Collider>() != null
                )
                {
                    return Response.Error(
                        $"Cannot add 2D physics component '{typeName}' because the GameObject '{targetGo.name}' already has a 3D Rigidbody or Collider."
                    );
                }
            }
            else if (isAdding3DPhysics)
            {
                // Check if the GameObject already has any 2D Rigidbody or Collider
                if (
                    targetGo.GetComponent<Rigidbody2D>() != null
                    || targetGo.GetComponent<Collider2D>() != null
                )
                {
                    return Response.Error(
                        $"Cannot add 3D physics component '{typeName}' because the GameObject '{targetGo.name}' already has a 2D Rigidbody or Collider."
                    );
                }
            }

            try
            {
                // Use Undo.AddComponent for undo support
                Component newComponent = Undo.AddComponent(targetGo, componentType);
                if (newComponent == null)
                {
                    return Response.Error(
                        $"Failed to add component '{typeName}' to '{targetGo.name}'. It might be disallowed (e.g., adding script twice)."
                    );
                }

                // Set default values for specific component types
                if (newComponent is Light light)
                {
                    // Default newly added lights to directional
                    light.type = LightType.Directional;
                }

                // Note: Property setting is handled by the calling code if needed
                // This keeps the method simpler and more focused

                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error(
                    $"Error adding component '{typeName}' to '{targetGo.name}': {e.Message}"
                );
            }
        }
    }
}
