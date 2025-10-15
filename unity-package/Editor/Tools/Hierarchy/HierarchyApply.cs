using System;
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
    /// Corresponding method name: hierarchy_apply
    /// </summary>
    [ToolName("hierarchy_apply", "Hierarchy management")]
    public class HierarchyApply : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
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
        /// Handle connecting prefab operation
        /// </summary>
        private object HandleapplyAction(JsonClass args)
        {
            string applyType = args["apply_type"]?.Value?.ToLower();
            if (string.IsNullOrEmpty(applyType))
            {
                applyType = "connect_to_prefab"; // Default connect to prefab
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
        /// ConnectGameObjectTo prefab
        /// </summary>
        private object HandleConnectToPrefab(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Connecting GameObject to prefab");
            return ConnectGameObjectToPrefab(args);
        }

        /// <summary>
        /// Apply prefab changes
        /// </summary>
        private object HandleApplyPrefabChanges(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Applying prefab changes");
            return ApplyPrefabChanges(args);
        }

        /// <summary>
        /// Disconnect prefab connection
        /// </summary>
        private object HandleBreakPrefabConnection(JsonClass args)
        {
            LogInfo("[Hierarchyapply] Breaking prefab connection");
            return BreakPrefabConnection(args);
        }

        // --- Prefab apply Methods ---

        /// <summary>
        /// ConnectGameObjectTo specified prefab
        /// </summary>
        private object ConnectGameObjectToPrefab(JsonClass args)
        {
            try
            {
                // Get targetGameObject
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

                // Get prefab path
                string prefabPath = args["prefab_path"]?.Value;
                if (string.IsNullOrEmpty(prefabPath))
                {
                    return Response.Error("'prefab_path' parameter is required for connecting to prefab.");
                }

                // Parse prefab path
                string resolvedPath = ResolvePrefabPath(prefabPath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    return Response.Error($"Prefab not found at path: '{prefabPath}'");
                }

                // Load prefab asset
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                if (prefabAsset == null)
                {
                    return Response.Error($"Failed to load prefab asset at: '{resolvedPath}'");
                }

                // Check if force-link
                bool forceapply = args["force_apply"].AsBoolDefault(false);

                // Check existing connection
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) != PrefabInstanceStatus.NotAPrefab && !forceapply)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is already connected to a prefab. Use 'force_apply': true to override.");
                }

                // Record undo operation
                Undo.RecordObject(targetGo, $"Connect '{targetGo.name}' to prefab '{prefabAsset.name}'");

                // Connect to prefab - Use modernAPI
                // Check firstGameObjectWhether compatible with prefab
                bool canConnect = CanGameObjectConnectToPrefab(targetGo, prefabAsset);
                if (!canConnect && !forceapply)
                {
                    return Response.Error($"GameObject '{targetGo.name}' structure doesn't match prefab '{prefabAsset.name}'. Use 'force_apply': true to force connection.");
                }

                GameObject connectedInstance;
                if (forceapply)
                {
                    // Force connect：Create a new prefab instance first，Then replace original object
                    GameObject newInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                    if (newInstance != null)
                    {
                        // Copy transform info
                        newInstance.transform.SetParent(targetGo.transform.parent);
                        newInstance.transform.localPosition = targetGo.transform.localPosition;
                        newInstance.transform.localRotation = targetGo.transform.localRotation;
                        newInstance.transform.localScale = targetGo.transform.localScale;
                        newInstance.name = targetGo.name;

                        // Delete original object
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
                    // Attempt replacement of component method
                    connectedInstance = ReplaceWithPrefabInstance(targetGo, prefabAsset);
                    if (connectedInstance == null)
                    {
                        return Response.Error($"Failed to connect GameObject '{targetGo.name}' to prefab '{resolvedPath}'");
                    }
                }

                LogInfo($"[Hierarchyapply] Successfully connected GameObject '{targetGo.name}' to prefab '{resolvedPath}'");

                // Select object after connect
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
        /// Apply prefab instance changes to prefab asset
        /// </summary>
        private object ApplyPrefabChanges(JsonClass args)
        {
            try
            {
                // Get targetGameObject
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

                // Check if it is a prefab instance
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) == PrefabInstanceStatus.NotAPrefab)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is not a prefab instance.");
                }

                // Get prefab asset
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(targetGo);
                if (prefabAsset == null)
                {
                    return Response.Error($"Cannot find corresponding prefab asset for '{targetGo.name}'");
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);

                // Apply prefab changes
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
        /// DisconnectGameObjectConnection with prefab
        /// </summary>
        private object BreakPrefabConnection(JsonClass args)
        {
            try
            {
                // Get targetGameObject
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

                // Check if it is a prefab instance
                if (PrefabUtility.GetPrefabInstanceStatus(targetGo) == PrefabInstanceStatus.NotAPrefab)
                {
                    return Response.Error($"GameObject '{targetGo.name}' is not connected to a prefab.");
                }

                // Get prefab info（Before disconnecting）
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(targetGo);
                string prefabName = prefabAsset != null ? prefabAsset.name : "Unknown";

                // Record undo operation
                Undo.RecordObject(targetGo, $"Break prefab connection for '{targetGo.name}'");

                // Disconnect prefab connection
                // Note：UnpackPrefabInstance Method in someUnityMay be unavailable in version
                // PrefabUtility.UnpackPrefabInstance(targetGo, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                LogWarning($"[Hierarchyapply] UnpackPrefabInstance method not supported in current Unity version");

                LogInfo($"[Hierarchyapply] Successfully broke prefab connection for GameObject '{targetGo.name}'");

                // Select objects after disconnecting
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
        /// CheckGameObjectWhether can connect to specified prefab
        /// </summary>
        private bool CanGameObjectConnectToPrefab(GameObject gameObject, GameObject prefabAsset)
        {
            try
            {
                // Basic check：Compare component types
                var gameObjectComponents = gameObject.GetComponents<Component>().Select(c => c.GetType()).ToArray();
                var prefabComponents = prefabAsset.GetComponents<Component>().Select(c => c.GetType()).ToArray();

                // All components of prefab inGameObjectShould all exist in
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
        /// Replace with prefab instanceGameObject
        /// </summary>
        private GameObject ReplaceWithPrefabInstance(GameObject originalGo, GameObject prefabAsset)
        {
            try
            {
                // Record transforms of original object
                Transform originalTransform = originalGo.transform;
                Transform parentTransform = originalTransform.parent;
                Vector3 localPosition = originalTransform.localPosition;
                Quaternion localRotation = originalTransform.localRotation;
                Vector3 localScale = originalTransform.localScale;
                string originalName = originalGo.name;
                int siblingIndex = originalTransform.GetSiblingIndex();

                // Create prefab instance
                GameObject newInstance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (newInstance == null)
                {
                    return null;
                }

                // Set transform info
                newInstance.transform.SetParent(parentTransform);
                newInstance.transform.localPosition = localPosition;
                newInstance.transform.localRotation = localRotation;
                newInstance.transform.localScale = localScale;
                newInstance.name = originalName;
                newInstance.transform.SetSiblingIndex(siblingIndex);

                // Try copying compatible component properties
                CopyCompatibleComponentProperties(originalGo, newInstance);

                // Delete original object
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
        /// Copy compatible component properties
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
                        // Copy serialized fields
                        var serializedObject = new SerializedObject(sourceComp);
                        var targetSerializedObject = new SerializedObject(targetComp);

                        SerializedProperty iterator = serializedObject.GetIterator();
                        while (iterator.NextVisible(true))
                        {
                            if (iterator.name == "m_Script") continue; // Skip script references

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
        /// Parse prefab path
        /// </summary>
        private string ResolvePrefabPath(string prefabPath)
        {
            // If no path separator and no.prefabExtension，Search prefab
            if (!prefabPath.Contains("/") && !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                string prefabNameOnly = prefabPath;
                LogInfo($"[Hierarchyapply] Searching for prefab named: '{prefabNameOnly}'");

                string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefabNameOnly}");
                if (guids.Length == 0)
                {
                    return null; // Not found
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
                // Auto add.prefabExtension
                LogInfo($"[Hierarchyapply] Adding .prefab extension to path: '{prefabPath}'");
                return prefabPath + ".prefab";
            }

            return prefabPath;
        }

        /// <summary>
        /// LookupGameObject，For linking operation
        /// </summary>
        private GameObject FindObjectByIdOrNameOrPath(JsonNode targetToken)
        {
            string searchTerm = targetToken?.Value;
            if (string.IsNullOrEmpty(searchTerm))
                return null;

            // Try byIDLookup
            if (int.TryParse(searchTerm, out int id))
            {
                var allObjects = GameObjectUtils.GetAllSceneObjects(true);
                GameObject objById = allObjects.FirstOrDefault(go => go.GetInstanceID() == id);
                if (objById != null)
                    return objById;
            }

            // Try searching by path
            GameObject objByPath = GameObject.Find(searchTerm);
            if (objByPath != null)
                return objByPath;

            // Try to search by name
            var allObjectsName = GameObjectUtils.GetAllSceneObjects(true);
            return allObjectsName.FirstOrDefault(go => go.name == searchTerm);
        }




    }
}
