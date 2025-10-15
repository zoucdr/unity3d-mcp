using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    /// Handles GameObject creation operations in the scene hierarchy.
    /// Corresponding method name: hierarchy_create
    /// Support: menu, primitive, prefab, empty, copy
    /// </summary>
    [ToolName("hierarchy_create", "Hierarchy management")]
    public class HierarchyCreate : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("name", "GameObjectName", false),
                new MethodKey("source", "Operation type：menu, primitive, prefab, empty, copy", false),
                new MethodKey("tag", "GameObjectTag", true),
                new MethodKey("layer", "GameObjectLayer", true),
                new MethodKey("parent", "Parent object name or path", true),
                new MethodKey("parent_id", "Parent object uniqueid", true),
                new MethodKey("position", "Position coordinate [x, y, z]", true),
                new MethodKey("rotation", "Rotation angle [x, y, z]", true),
                new MethodKey("scale", "Scale factor [x, y, z]", true),
                new MethodKey("primitive_type", "Primitive type：Cube, Sphere, Cylinder, Capsule, Plane, Quad", true),
                new MethodKey("prefab_path", "Prefab path", true),
                new MethodKey("menu_path", "Menu path", true),
                new MethodKey("copy_source", "To be copiedGameObjectName", true),
                new MethodKey("save_as_prefab", "Whether to save as prefab", true),
                new MethodKey("set_active", "Set activation state", true),
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("source")
                    .Leaf("menu", HandleCreateFromMenu)
                    .Branch("primitive")
                        .OptionalKey("primitive_type")
                            .Leaf("Cube", HandleCreateCube)
                            .Leaf("Sphere", HandleCreateSphere)
                            .Leaf("Cylinder", HandleCreateCylinder)
                            .Leaf("Capsule", HandleCreateCapsule)
                            .Leaf("Plane", HandleCreatePlane)
                            .Leaf("Quad", HandleCreateQuad)
                            .DefaultLeaf(HandleCreateFromPrimitive)
                        .Up()
                        .DefaultLeaf(HandleCreateFromPrimitive)
                    .Up()
                    .Leaf("prefab", HandleCreateFromPrefab)
                    .Leaf("empty", HandleCreateEmpty)
                    .Leaf("copy", HandleCreateFromCopy)
                .Build();
        }

        /// <summary>
        /// Async download 
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private IEnumerator HandleCreateFromMenuAsync(StateTreeContext ctx)
        {
            string menuPath = ctx["menu_path"]?.ToString();
            if (string.IsNullOrEmpty(menuPath))
            {
                yield return Response.Error("'menu_path' parameter is required for menu creation.");
                yield break;
            }

            LogInfo($"[HierarchyCreate] Creating GameObject source menu: '{menuPath}'");

            if (!menuPath.StartsWith("GameObject"))
            {
                menuPath = "GameObject/" + menuPath;
                LogInfo($"[HierarchyCreate] Menu path adjusted: '{menuPath}'");
            }

            // Record selected object before creating
            GameObject previousSelection = Selection.activeGameObject;
            int previousSelectionID = previousSelection != null ? previousSelection.GetInstanceID() : 0;

            // Execute menu item
            JsonClass menuResult = MenuUtils.TryExecuteMenuItem(menuPath);

            // Check menu execution result
            if (!menuResult["success"].AsBoolDefault(false))
            {
                LogInfo($"[HierarchyCreate] Menu execution failed: {menuResult}");
                yield return menuResult;
                yield break;
            }

            // Try detecting newly created object multiple times，Menu creation may take time
            GameObject newObject = null;
            int maxRetries = 10;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                newObject = Selection.activeGameObject;

                // Check if a new object was found
                if (newObject != null &&
                    (previousSelection == null || newObject.GetInstanceID() != previousSelectionID))
                {
                    LogInfo($"[HierarchyCreate] Found newly created object: '{newObject.name}' (ID: {newObject.GetInstanceID()}) after {retryCount} retries");
                    break;
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    // Optimize coroutine call：Between each retryyield return null，Wait one frame
                    yield return null;
                }
            }

            // If found a new object，Perform settings
            if (newObject != null &&
                (previousSelection == null || newObject.GetInstanceID() != previousSelectionID))
            {
                // Wait another frame，Ensure object is fully initialized
                yield return null;

                LogInfo($"[HierarchyCreate] Finalizing newly created object: '{newObject.name}' (ID: {newObject.GetInstanceID()})");

                // Deselect first to exit rename mode
                Selection.activeGameObject = null;

                // Force quit edit mode
                EditorGUIUtility.editingTextField = false;
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.keyboardControl = 0;

                // SendESCKey event
                Event escapeEvent = new Event();
                escapeEvent.type = EventType.KeyDown;
                escapeEvent.keyCode = KeyCode.Escape;
                EditorWindow.focusedWindow?.SendEvent(escapeEvent);

                yield return null; // Wait one frame

                // Apply other settings（Name、Location etc.）
                var finalizeResult = FinalizeGameObjectCreation(ctx.JsonData, newObject, false);
                LogInfo($"[HierarchyCreate] Finalization result: {finalizeResult}");
                yield return finalizeResult;
                yield break;
            }
            else
            {
                // If no new object found，But menu executed successfully
                LogInfo($"[HierarchyCreate] Menu executed but no new object was detected after {maxRetries} retries. Previous: {previousSelection?.name}, Current: {newObject?.name}");
                yield return Response.Success($"Menu item '{menuPath}' executed successfully, but no new GameObject was detected.");
                yield break;
            }
        }

        /// <summary>
        /// Handle creation from menuGameObjectOperation of
        /// </summary>
        private object HandleCreateFromMenu(StateTreeContext ctx)
        {
            return ctx.AsyncReturn(HandleCreateFromMenuAsync(ctx));
        }

        /// <summary>
        /// Handle creation from prefabGameObjectOperation of
        /// </summary>
        private object HandleCreateFromPrefab(JsonClass args)
        {
            string prefabPath = args["prefab_path"]?.Value;
            if (string.IsNullOrEmpty(prefabPath))
            {
                return Response.Error("'prefab_path' parameter is required for prefab instantiation.");
            }

            LogInfo($"[HierarchyCreate] Creating GameObject source prefab: '{prefabPath}'");
            return CreateGameObjectFromPrefab(args, prefabPath);
        }

        /// <summary>
        /// Handle creation from primitive typeGameObjectOperation of
        /// </summary>
        private object HandleCreateFromPrimitive(JsonClass args)
        {
            string primitiveType = args["primitive_type"]?.Value;
            if (string.IsNullOrEmpty(primitiveType))
            {
                // Default useCubeAs primitive type
                primitiveType = "Cube";
                LogInfo("[HierarchyCreate] No primitive_type specified, using default: Cube");
            }

            LogInfo($"[HierarchyCreate] Creating GameObject source primitive: '{primitiveType}'");
            return CreateGameObjectFromPrimitive(args, primitiveType);
        }

        /// <summary>
        /// Handle creationCubeOperation of
        /// </summary>
        private object HandleCreateCube(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Cube primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Cube");
        }

        /// <summary>
        /// Handle creationSphereOperation of
        /// </summary>
        private object HandleCreateSphere(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Sphere primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Sphere");
        }

        /// <summary>
        /// Handle creationCylinderOperation of
        /// </summary>
        private object HandleCreateCylinder(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Cylinder primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Cylinder");
        }

        /// <summary>
        /// Handle creationCapsuleOperation of
        /// </summary>
        private object HandleCreateCapsule(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Capsule primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Capsule");
        }

        /// <summary>
        /// Handle creationPlaneOperation of
        /// </summary>
        private object HandleCreatePlane(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Plane primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Plane");
        }

        /// <summary>
        /// Handle creationQuadOperation of
        /// </summary>
        private object HandleCreateQuad(JsonClass args)
        {
            LogInfo("[HierarchyCreate] Creating Quad primitive using specialized handler");
            return CreateGameObjectFromPrimitive(args, "Quad");
        }

        /// <summary>
        /// Handle create emptyGameObjectOperation of（Create via code directly，Smart handlingUI/NonUIObject）
        /// </summary>
        private object HandleCreateEmpty(JsonClass args)
        {
            string name = args["name"]?.Value;
            if (string.IsNullOrEmpty(name))
            {
                name = "GameObject";
                LogInfo("[HierarchyCreate] No name specified for empty GameObject, using default: 'GameObject'");
            }

            LogInfo($"[HierarchyCreate] Creating empty GameObject: '{name}'");

            try
            {
                // Preselect parent object（If specified）
                GameObjectUtils.PreselectParentIfSpecified(args, LogInfo);

                // Get parent object
                GameObject parentObject = Selection.activeGameObject;

                // Check if parent object hasRectTransform（UIElement）
                bool parentIsUI = parentObject != null && parentObject.GetComponent<RectTransform>() != null;

                LogInfo($"[HierarchyCreate] Parent is UI: {parentIsUI}, creating appropriate empty object");

                GameObject newGo = null;

                if (parentIsUI)
                {
                    // InUICreate under parent object，Need to addRectTransform
                    newGo = new GameObject(name, typeof(RectTransform));

                    // Set parent object（Second parameterfalseIndicate to keep local coordinates）
                    if (parentObject != null)
                    {
                        newGo.transform.SetParent(parentObject.transform, false);
                    }

                    LogInfo($"[HierarchyCreate] Created UI GameObject with RectTransform: '{newGo.name}'");
                }
                else
                {
                    // Normal object，Create directly
                    newGo = new GameObject(name);

                    // Set parent object（If has）
                    if (parentObject != null)
                    {
                        newGo.transform.SetParent(parentObject.transform, true);
                    }

                    LogInfo($"[HierarchyCreate] Created standard GameObject: '{newGo.name}'");
                }

                // Register undo action
                Undo.RegisterCreatedObjectUndo(newGo, $"Create Empty GameObject '{newGo.name}'");

                LogInfo($"[HierarchyCreate] Finalizing empty object: '{newGo.name}' (ID: {newGo.GetInstanceID()})");

                // Apply other settings（Name、Location etc.）
                return FinalizeGameObjectCreation(args, newGo, true);
            }
            catch (Exception e)
            {
                LogInfo($"[HierarchyCreate] Failed to create empty GameObject '{name}': {e.Message}");
                return Response.Error($"Failed to create empty GameObject '{name}': {e.Message}");
            }
        }

        /// <summary>
        /// Handle operations copied from existing objects
        /// </summary>
        private object HandleCreateFromCopy(JsonClass args)
        {
            string copySource = args["copy_source"]?.Value;
            if (string.IsNullOrEmpty(copySource))
            {
                return Response.Error("'copy_source' parameter is required for copy creation.");
            }

            LogInfo($"[HierarchyCreate] Copying GameObject from source: '{copySource}'");
            return CreateGameObjectFromCopy(args, copySource);
        }

        // --- Core Creation Methods ---

        /// <summary>
        /// Create from prefabGameObject
        /// </summary>
        private object CreateGameObjectFromPrefab(JsonClass args, string prefabPath)
        {
            try
            {
                // Preselect parent object（If specified）
                GameObjectUtils.PreselectParentIfSpecified(args, LogInfo);

                // Handle prefab path lookup logic
                string resolvedPath = ResolvePrefabPath(prefabPath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    LogInfo($"[HierarchyCreate] Prefab not found at path: '{prefabPath}'");
                    return Response.Error($"Prefab not found at path: '{prefabPath}'");
                }

                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedPath);
                if (prefabAsset == null)
                {
                    LogInfo($"[HierarchyCreate] Failed to load prefab asset at: '{resolvedPath}'");
                    return Response.Error($"Failed to load prefab asset at: '{resolvedPath}'");
                }

                // Instantiate prefab
                GameObject newGo = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (newGo == null)
                {
                    LogInfo($"[HierarchyCreate] Failed to instantiate prefab: '{resolvedPath}'");
                    return Response.Error($"Failed to instantiate prefab: '{resolvedPath}'");
                }

                // WaitUnityComplete object initialization
                //Thread.Sleep(10);

                // Set name
                string name = args["name"]?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    newGo.name = name;
                }

                // Register undo action
                Undo.RegisterCreatedObjectUndo(newGo, $"Instantiate Prefab '{prefabAsset.name}' as '{newGo.name}'");
                LogInfo($"[HierarchyCreate] Instantiated prefab '{prefabAsset.name}' source path '{resolvedPath}' as '{newGo.name}'");

                return FinalizeGameObjectCreation(args, newGo, false);
            }
            catch (Exception e)
            {
                LogInfo($"[HierarchyCreate] Error instantiating prefab '{prefabPath}': {e.Message}");
                return Response.Error($"Error instantiating prefab '{prefabPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Create from primitiveGameObject
        /// </summary>
        private object CreateGameObjectFromPrimitive(JsonClass args, string primitiveType)
        {
            try
            {
                // Preselect parent object（If specified）
                GameObjectUtils.PreselectParentIfSpecified(args, LogInfo);

                PrimitiveType type = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), primitiveType, true);
                GameObject newGo = GameObject.CreatePrimitive(type);

                // WaitUnityComplete object initialization
                //Thread.Sleep(10);

                // Set name
                string name = args["name"]?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    newGo.name = name;
                }
                else
                {
                    LogInfo("[HierarchyCreate] 'name' parameter is recommended when creating a primitive.");
                }

                // Register undo action
                Undo.RegisterCreatedObjectUndo(newGo, $"Create GameObject '{newGo.name}'");
                return FinalizeGameObjectCreation(args, newGo, true);
            }
            catch (ArgumentException)
            {
                LogInfo($"[HierarchyCreate] Invalid primitive type: '{primitiveType}'. Valid types: {string.Join(", ", Enum.GetNames(typeof(PrimitiveType)))}");
                return Response.Error($"Invalid primitive type: '{primitiveType}'. Valid types: {string.Join(", ", Enum.GetNames(typeof(PrimitiveType)))}");
            }
            catch (Exception e)
            {
                LogInfo($"[HierarchyCreate] Failed to create primitive '{primitiveType}': {e.Message}");
                return Response.Error($"Failed to create primitive '{primitiveType}': {e.Message}");
            }
        }


        /// <summary>
        /// From existingGameObjectCopy and create new object
        /// </summary>
        private object CreateGameObjectFromCopy(JsonClass args, string copySource)
        {
            try
            {
                // Preselect parent object（If specified）
                GameObjectUtils.PreselectParentIfSpecified(args, LogInfo);

                // Find source object
                GameObject sourceObject = GameObject.Find(copySource);
                if (sourceObject == null)
                {
                    // If direct search failed，Try searching in scene
                    GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    sourceObject = allObjects.FirstOrDefault(go =>
                        go.name.Equals(copySource, StringComparison.OrdinalIgnoreCase));

                    if (sourceObject == null)
                    {
                        LogInfo($"[HierarchyCreate] Source GameObject '{copySource}' not found in scene");
                        return Response.Error($"Source GameObject '{copySource}' not found in scene");
                    }
                }

                LogInfo($"[HierarchyCreate] Found source object: '{sourceObject.name}' (ID: {sourceObject.GetInstanceID()})");

                // Copy object
                GameObject newGo = UnityEngine.Object.Instantiate(sourceObject);

                if (newGo == null)
                {
                    LogInfo($"[HierarchyCreate] Failed to instantiate copy of '{copySource}'");
                    return Response.Error($"Failed to instantiate copy of '{copySource}'");
                }

                // WaitUnityComplete object initialization
                //Thread.Sleep(10);

                // Set name
                string name = args["name"]?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    newGo.name = name;
                }
                else
                {
                    // Default to use source object's name（UnityWill auto add(Clone)Suffix）
                    LogInfo($"[HierarchyCreate] No name specified, copied object named: '{newGo.name}'");
                }

                // Register undo action
                Undo.RegisterCreatedObjectUndo(newGo, $"Copy GameObject '{sourceObject.name}' as '{newGo.name}'");

                LogInfo($"[HierarchyCreate] Successfully copied '{sourceObject.name}' to '{newGo.name}' (ID: {newGo.GetInstanceID()})");

                return FinalizeGameObjectCreation(args, newGo, true);
            }
            catch (Exception e)
            {
                LogInfo($"[HierarchyCreate] Error copying GameObject '{copySource}': {e.Message}");
                return Response.Error($"Error copying GameObject '{copySource}': {e.Message}");
            }
        }

        /// <summary>
        /// Parse prefab path
        /// </summary>
        private string ResolvePrefabPath(string prefabPath)
        {
            // If no path separator and no.prefabExtension name，Search prefab
            if (!prefabPath.Contains("/") && !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                string prefabNameOnly = prefabPath;
                LogInfo($"[HierarchyCreate] Searching for prefab named: '{prefabNameOnly}'");

                string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefabNameOnly}");
                if (guids.Length == 0)
                {
                    return null; // Not found
                }
                else if (guids.Length > 1)
                {
                    string foundPaths = string.Join(", ", guids.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                    LogInfo($"[HierarchyCreate] Multiple prefabs found matching name '{prefabNameOnly}': {foundPaths}. Using first one.");
                }

                string resolvedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                LogInfo($"[HierarchyCreate] Found prefab at path: '{resolvedPath}'");
                return resolvedPath;
            }
            else if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                // Auto add.prefabExtension name
                LogInfo($"[HierarchyCreate] Adding .prefab extension to path: '{prefabPath}'");
                return prefabPath + ".prefab";
            }

            return prefabPath;
        }

        /// <summary>
        /// CompleteGameObjectGeneric settings for creation
        /// </summary>
        private object FinalizeGameObjectCreation(JsonClass args, GameObject newGo, bool createdNewObject)
        {
            if (newGo == null)
            {
                return Response.Error("GameObject creation failed.");
            }

            try
            {
                LogInfo($"[HierarchyCreate] Starting finalization for '{newGo.name}' (ID: {newGo.GetInstanceID()})");

                // Record changes of transform and property
                Undo.RecordObject(newGo.transform, "Set GameObject Transform");
                Undo.RecordObject(newGo, "Set GameObject Properties");

                // Apply generic settings（Including name setting）
                GameObjectUtils.ApplyCommonGameObjectSettings(args, newGo, LogInfo);

                LogInfo($"[HierarchyCreate] Applied settings to '{newGo.name}' (ID: {newGo.GetInstanceID()})");

                // Handle prefab save
                GameObject finalInstance = newGo;
                bool saveAsPrefab = args["save_as_prefab"].AsBoolDefault(false);

                if (createdNewObject && saveAsPrefab)
                {
                    finalInstance = HandlePrefabSaving(args, newGo);
                    if (finalInstance == null)
                    {
                        return Response.Error("Failed to save GameObject as prefab.");
                    }
                }

                // Deselect and exit rename mode
                Selection.activeGameObject = null;

                // Delay call to ensure exit rename mode
                EditorApplication.delayCall += () =>
                {
                    // Attempt to exit edit status by multiple methods
                    Selection.activeGameObject = null;
                    EditorGUIUtility.editingTextField = false;

                    // SendESCKey event to exit editing
                    Event escapeEvent = new Event();
                    escapeEvent.type = EventType.KeyDown;
                    escapeEvent.keyCode = KeyCode.Escape;
                    EditorWindow.focusedWindow?.SendEvent(escapeEvent);

                    // Force end editing status
                    GUIUtility.keyboardControl = 0;
                    EditorGUIUtility.keyboardControl = 0;

                    // Refresh related windows
                    EditorApplication.RepaintHierarchyWindow();
                    if (EditorWindow.focusedWindow != null)
                    {
                        EditorWindow.focusedWindow.Repaint();
                    }
                };

                LogInfo($"[HierarchyCreate] Finalized '{finalInstance.name}' (ID: {finalInstance.GetInstanceID()})");

                // Generate success message
                string successMessage = GenerateCreationSuccessMessage(args, finalInstance, createdNewObject, saveAsPrefab);
                return Response.Success(successMessage, GameObjectUtils.GetGameObjectData(finalInstance));
            }
            catch (Exception e)
            {
                LogError($"[HierarchyCreate] Error finalizing GameObject creation: {e.Message}");
                // Clear failed objects
                if (newGo != null)
                {
                    UnityEngine.Object.DestroyImmediate(newGo);
                }
                return Response.Error($"Error finalizing GameObject creation: {e.Message}");
            }
        }



        /// <summary>
        /// Handle prefab save
        /// </summary>
        private GameObject HandlePrefabSaving(JsonClass args, GameObject newGo)
        {
            string prefabPath = args["prefab_path"]?.Value;
            if (string.IsNullOrEmpty(prefabPath))
            {
                LogInfo("[HierarchyCreate] 'prefab_path' is required when 'save_as_prefab' is true.");
                return null;
            }

            string finalPrefabPath = prefabPath;
            if (!finalPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                LogInfo($"[HierarchyCreate] Adding .prefab extension to save path: '{finalPrefabPath}'");
                finalPrefabPath += ".prefab";
            }

            try
            {
                // Ensure directory exists
                string directoryPath = System.IO.Path.GetDirectoryName(finalPrefabPath);
                if (!string.IsNullOrEmpty(directoryPath) && !System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    AssetDatabase.Refresh();
                    LogInfo($"[HierarchyCreate] Created directory for prefab: {directoryPath}");

                    // WaitUnityComplete directory creation
                    //Thread.Sleep(50);
                }

                // Save as prefab
                GameObject finalInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    newGo,
                    finalPrefabPath,
                    InteractionMode.UserAction
                );

                if (finalInstance == null)
                {
                    UnityEngine.Object.DestroyImmediate(newGo);
                    return null;
                }

                // Wait until prefab save completed
                //Thread.Sleep(10);

                LogInfo($"[HierarchyCreate] GameObject '{newGo.name}' saved as prefab to '{finalPrefabPath}' and instance connected.");
                return finalInstance;
            }
            catch (Exception e)
            {
                LogInfo($"[HierarchyCreate] Error saving prefab '{finalPrefabPath}': {e.Message}");
                UnityEngine.Object.DestroyImmediate(newGo);
                return null;
            }
        }

        /// <summary>
        /// Generate create success message
        /// </summary>
        private string GenerateCreationSuccessMessage(JsonClass args, GameObject finalInstance, bool createdNewObject, bool saveAsPrefab)
        {
            string messagePrefabPath = AssetDatabase.GetAssetPath(
                PrefabUtility.GetCorrespondingObjectFromSource(finalInstance) ?? (UnityEngine.Object)finalInstance
            );

            if (!createdNewObject && !string.IsNullOrEmpty(messagePrefabPath))
            {
                return $"Prefab '{messagePrefabPath}' instantiated successfully as '{finalInstance.name}'.";
            }
            else if (createdNewObject && saveAsPrefab && !string.IsNullOrEmpty(messagePrefabPath))
            {
                return $"GameObject '{finalInstance.name}' created and saved as prefab to '{messagePrefabPath}'.";
            }
            else
            {
                return $"GameObject '{finalInstance.name}' created successfully in scene.";
            }
        }
    }
}
