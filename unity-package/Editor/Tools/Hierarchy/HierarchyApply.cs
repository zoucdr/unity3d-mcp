﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles GameObject prefab applying and connection operations.
    /// 对应方法名: hierarchy_apply
    /// </summary>
    [ToolName("hierarchy_apply", "层级管理")]
    public class HierarchyApply : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: apply", false),
                new MethodKey("target_object", "Target GameObject identifier (used for apply operation)", false),
                new MethodKey("prefab_path", "Prefab path", true),
                new MethodKey("apply_type", "Link type: connect_to_prefab, apply_prefab_changes, break_prefab_connection", true),
                new MethodKey("force_apply", "Whether to force create link (overwrite existing connection)", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Branch("apply")
                        .OptionalKey("apply_type")
                            .Leaf("connect_to_prefab", HandleConnectToPrefab)
                            .Leaf("apply_prefab_changes", HandleApplyPrefabChanges)
                            .Leaf("break_prefab_connection", HandleBreakPrefabConnection)
                            .DefaultLeaf(HandleConnectToPrefab)
                        .Up()
                        .DefaultLeaf(HandleapplyAction)
                    .Up()
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理链接预制体的操作
        /// </summary>
        private object HandleapplyAction(JsonClass args)
        {
            string applyType = args["apply_type"]?.Value?.ToLower();
            if (string.IsNullOrEmpty(applyType))
            {
                applyType = "connect_to_prefab"; // 默认连接到预制体
            }

            LogInfo($"[Hierarchyapply] Executing apply action with type: '{applyType}'");

            switch (applyType)
            {
                case "connect_to_prefab":
                    return HandleConnectToPrefab(args);
                case "apply_prefab_changes":
                    return HandleApplyPrefabChanges(args);
                case "break_prefab_connection":
                    return HandleBreakPrefabConnection(args);
                default:
                    return Response.Error($"Unknown link type: '{applyType}'");
            }
        }

        /// <summary>
        /// 连接GameObject到预制体
        /// </summary>
        private object HandleConnectToPrefab(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Connecting GameObject to prefab");
            return ConnectGameObjectToPrefab(args);
        }

        /// <summary>
        /// 应用预制体更改
        /// </summary>
        private object HandleApplyPrefabChanges(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Applying prefab changes");
            return ApplyPrefabChanges(args);
        }

        /// <summary>
        /// 断开预制体连接
        /// </summary>
        private object HandleBreakPrefabConnection(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Breaking prefab connection");
            return BreakPrefabConnection(args);
        }

        // --- Prefab apply Methods ---

        /// <summary>
        /// 连接GameObject到指定的预制体
        /// </summary>
        private object ConnectGameObjectToPrefab(JsonClass args)
        {
            try
            {
                // 获取目标GameObject
                JsonNode targetToken = args["target_object"];
                if (targetToken == null)
                {
                    return Response.Error("'target_object' parameter is required for apply operation.");
                }

                GameObject targetGo = GameObjectUtils.FindObjectByIdOrPath(targetToken);
                if (targetGo == null)
                {
                    return Response.Error($"Target GameObject '{targetToken}' not found.");
                }

                // 获取预制体路径
                string prefabPath = args["prefab_path"]?.Value;
                if (string.IsNullOrEmpty(prefabPath))
                {
                    return Response.Error("'prefab_path' parameter is required for connecting to prefab.");
                }

                // 解析预制体路径
                string resolvedPath = ResolvePrefabPath(prefabPath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    return Response.Error($"Prefab not found at path: '{prefabPath}'");
                }

                // 加载预制体资源
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                if (prefabAsset == null)
                {
                    return Response.Error($"Failed to load prefab asset at: '{resolvedPath}'");
                }

                // 检查是否强制链接
                bool forceapply = args["force_apply"].AsBoolDefault(false);

                // 检查现有连接
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) != PrefabInstanceStatus.NotAPrefab && !forceapply)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is already connected to a prefab. Use 'force_apply': true to override.");
                }

                // 记录撤销操作
                Undo.RecordObject(targetGo, $"Connect '{targetGo.name}' to prefab '{prefabAsset.name}'");

                // 连接到预制体 - 使用现代API
                // 先检查GameObject是否与预制体兼容
                bool canConnect = CanGameObjectConnectToPrefab(targetGo, prefabAsset);
                if (!canConnect && !forceapply)
                {
                    return Response.Error($"GameObject '{targetGo.name}' structure doesn't match prefab '{prefabAsset.name}'. Use 'force_apply': true to force connection.");
                }

                GameObject connectedInstance;
                if (forceapply)
                {
                    // 强制连接：先创建一个新的预制体实例，然后替换原对象
                    GameObject newInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                    if (newInstance != null)
                    {
                        // 复制变换信息
                        newInstance.transform.SetParent(targetGo.transform.parent);
                        newInstance.transform.localPosition = targetGo.transform.localPosition;
                        newInstance.transform.localRotation = targetGo.transform.localRotation;
                        newInstance.transform.localScale = targetGo.transform.localScale;
                        newInstance.name = targetGo.name;

                        // 删除原对象
                        Undo.DestroyObjectImmediate(targetGo);
                        connectedInstance = newInstance;
                    }
                    else
                    {
                        return Response.Error($"Failed to instantiate prefab '{resolvedPath}'");
                    }
                }
                else
                {
                    // 尝试使用替换组件的方式
                    connectedInstance = ReplaceWithPrefabInstance(targetGo, prefabAsset);
                    if (connectedInstance == null)
                    {
                        return Response.Error($"Failed to connect GameObject '{targetGo.name}' to prefab '{resolvedPath}'");
                    }
                }

                LogInfo($"[Hierarchyapply] Successfully connected GameObject '{targetGo.name}' to prefab '{resolvedPath}'");

                // 选择连接后的对象
                Selection.activeGameObject = connectedInstance;

                return Response.Success(
                    $"GameObject '{targetGo.name}' successfully connected to prefab '{prefabAsset.name}'.",
                    GameObjectUtils.GetGameObjectData(connectedInstance)
                );
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Error connecting GameObject to prefab: {e.Message}");
                return Response.Error($"Error connecting GameObject to prefab: {e.Message}");
            }
        }

        /// <summary>
        /// 应用预制体实例的更改到预制体资源
        /// </summary>
        private object ApplyPrefabChanges(JsonClass args)
        {
            try
            {
                // 获取目标GameObject
                JsonNode targetToken = args["target_object"];
                if (targetToken == null)
                {
                    return Response.Error("'target_object' parameter is required for apply operation.");
                }

                GameObject targetGo = GameObjectUtils.FindObjectByIdOrPath(targetToken);
                if (targetGo == null)
                {
                    return Response.Error($"Target GameObject '{targetToken}' not found.");
                }

                // 检查是否为预制体实例
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) == PrefabInstanceStatus.NotAPrefab)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is not a prefab instance.");
                }

                // 获取预制体资源
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(targetGo);
                if (prefabAsset == null)
                {
                    return Response.Error($"Cannot find corresponding prefab asset for '{targetGo.name}'");
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

                // 应用预制体更改
                PrefabUtility.ApplyPrefabInstance(targetGo, InteractionMode.UserAction);

                LogInfo($"[Hierarchyapply] Applied changes from instance '{targetGo.name}' to prefab '{prefabPath}'");

                return Response.Success(
                    $"Successfully applied changes from instance '{targetGo.name}' to prefab '{prefabAsset.name}'.",
                    GameObjectUtils.GetGameObjectData(targetGo)
                );
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Error applying prefab changes: {e.Message}");
                return Response.Error($"Error applying prefab changes: {e.Message}");
            }
        }

        /// <summary>
        /// 断开GameObject与预制体的连接
        /// </summary>
        private object BreakPrefabConnection(JsonClass args)
        {
            try
            {
                // 获取目标GameObject
                JsonNode targetToken = args["target_object"];
                if (targetToken == null)
                {
                    return Response.Error("'target_object' parameter is required for break connection operation.");
                }

                GameObject targetGo = GameObjectUtils.FindObjectByIdOrPath(targetToken);
                if (targetGo == null)
                {
                    return Response.Error($"Target GameObject '{targetToken}' not found.");
                }

                // 检查是否为预制体实例
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) == PrefabInstanceStatus.NotAPrefab)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is not connected to a prefab.");
                }

                // 获取预制体信息（在断开前）
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(targetGo);
                string prefabName = prefabAsset != null ? prefabAsset.name : "Unknown";

                // 记录撤销操作
                Undo.RecordObject(targetGo, $"Break prefab connection for '{targetGo.name}'");

                // 断开预制体连接
                // 注意：UnpackPrefabInstance 方法在某些Unity版本中可能不可用
                // PrefabUtility.UnpackPrefabInstance(targetGo, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                LogWarning($"[Hierarchyapply] UnpackPrefabInstance method not supported in current Unity version");

                LogInfo($"[Hierarchyapply] Successfully broke prefab connection for GameObject '{targetGo.name}'");

                // 选择断开连接后的对象
                Selection.activeGameObject = targetGo;

                return Response.Success(
                    $"Successfully broke prefab connection for GameObject '{targetGo.name}' (was connected to '{prefabName}').",
                    GameObjectUtils.GetGameObjectData(targetGo)
                );
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Error breaking prefab connection: {e.Message}");
                return Response.Error($"Error breaking prefab connection: {e.Message}");
            }
        }

        /// <summary>
        /// 检查GameObject是否可以连接到指定的预制体
        /// </summary>
        private bool CanGameObjectConnectToPrefab(GameObject gameObject, GameObject prefabAsset)
        {
            try
            {
                // 基本检查：比较组件类型
                var gameObjectComponents = gameObject.GetComponents<Component>().Select(c => c.GetType()).ToArray();
                var prefabComponents = prefabAsset.GetComponents<Component>().Select(c => c.GetType()).ToArray();

                // 预制体的所有组件在GameObject中都应该存在
                foreach (var prefabComponentType in prefabComponents)
                {
                    if (!gameObjectComponents.Contains(prefabComponentType))
                    {
                        LogInfo($"[Hierarchyapply] GameObject missing component: {prefabComponentType.Name}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Error checking prefab compatibility: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 用预制体实例替换GameObject
        /// </summary>
        private GameObject ReplaceWithPrefabInstance(GameObject originalGo, GameObject prefabAsset)
        {
            try
            {
                // 记录原对象的变换信息
                Transform originalTransform = originalGo.transform;
                Transform parentTransform = originalTransform.parent;
                Vector3 localPosition = originalTransform.localPosition;
                Quaternion localRotation = originalTransform.localRotation;
                Vector3 localScale = originalTransform.localScale;
                string originalName = originalGo.name;
                int siblingIndex = originalTransform.GetSiblingIndex();

                // 创建预制体实例
                GameObject newInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (newInstance == null)
                {
                    return null;
                }

                // 设置变换信息
                newInstance.transform.SetParent(parentTransform);
                newInstance.transform.localPosition = localPosition;
                newInstance.transform.localRotation = localRotation;
                newInstance.transform.localScale = localScale;
                newInstance.name = originalName;
                newInstance.transform.SetSiblingIndex(siblingIndex);

                // 尝试复制兼容的组件属性
                CopyCompatibleComponentProperties(originalGo, newInstance);

                // 删除原对象
                Undo.DestroyObjectImmediate(originalGo);

                LogInfo($"[Hierarchyapply] Replaced GameObject with prefab instance: '{originalName}'");
                return newInstance;
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Error replacing with prefab instance: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 复制兼容的组件属性
        /// </summary>
        private void CopyCompatibleComponentProperties(GameObject source, GameObject target)
        {
            try
            {
                var sourceComponents = source.GetComponents<Component>();
                var targetComponents = target.GetComponents<Component>();

                foreach (var sourceComp in sourceComponents)
                {
                    if (sourceComp == null) continue;

                    var targetComp = targetComponents.FirstOrDefault(tc => tc != null && tc.GetType() == sourceComp.GetType());
                    if (targetComp != null)
                    {
                        // 复制序列化字段
                        var serializedObject = new SerializedObject(sourceComp);
                        var targetSerializedObject = new SerializedObject(targetComp);

                        SerializedProperty iterator = serializedObject.GetIterator();
                        while (iterator.NextVisible(true))
                        {
                            if (iterator.name == "m_Script") continue; // 跳过脚本引用

                            var targetProperty = targetSerializedObject.FindProperty(iterator.propertyPath);
                            if (targetProperty != null && targetProperty.propertyType == iterator.propertyType)
                            {
                                targetSerializedObject.CopyFromSerializedProperty(iterator);
                            }
                        }

                        targetSerializedObject.ApplyModifiedProperties();
                    }
                }
            }
            catch (Exception e)
            {
                LogInfo($"[Hierarchyapply] Warning: Could not copy all component properties: {e.Message}");
            }
        }

        // --- Shared Utility Methods ---

        /// <summary>
        /// 解析预制体路径
        /// </summary>
        private string ResolvePrefabPath(string prefabPath)
        {
            // 如果没有路径分隔符且没有.prefab扩展名，搜索预制体
            if (!prefabPath.Contains("/") && !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                string prefabNameOnly = prefabPath;
                LogInfo($"[Hierarchyapply] Searching for prefab named: '{prefabNameOnly}'");

                string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefabNameOnly}");
                if (guids.Length == 0)
                {
                    return null; // 未找到
                }
                else if (guids.Length > 1)
                {
                    string foundPaths = string.Join(", ", guids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                    LogInfo($"[Hierarchyapply] Multiple prefabs found matching name '{prefabNameOnly}': {foundPaths}. Using first one.");
                }

                string resolvedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                LogInfo($"[Hierarchyapply] Found prefab at path: '{resolvedPath}'");
                return resolvedPath;
            }
            else if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                // 自动添加.prefab扩展名
                LogInfo($"[Hierarchyapply] Adding .prefab extension to path: '{prefabPath}'");
                return prefabPath + ".prefab";
            }

            return prefabPath;
        }

        /// <summary>
        /// 查找GameObject，用于链接操作
        /// </summary>
        private GameObject FindObjectByIdOrNameOrPath(JsonNode targetToken)
        {
            string searchTerm = targetToken?.Value;
            if (string.IsNullOrEmpty(searchTerm))
                return null;

            // 尝试按ID查找
            if (int.TryParse(searchTerm, out int id))
            {
                var allObjects = GameObjectUtils.GetAllSceneObjects(true);
                GameObject objById = allObjects.FirstOrDefault(go => go.GetInstanceID() == id);
                if (objById != null)
                    return objById;
            }

            // 尝试按路径查找
            GameObject objByPath = GameObject.Find(searchTerm);
            if (objByPath != null)
                return objByPath;

            // 尝试按名称查找
            var allObjectsName = GameObjectUtils.GetAllSceneObjects(true);
            return allObjectsName.FirstOrDefault(go => go.name == searchTerm);
        }




    }
}
