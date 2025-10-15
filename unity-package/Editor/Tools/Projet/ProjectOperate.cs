using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class
using UnityMcp.Tools; // Add this reference

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles Unity asset management operations including import, modify, move, duplicate, etc.
    /// Corresponding method name: manage_asset
    /// </summary>
    [ToolName("project_operate", "Project management")]
    public class ProjectOperate : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type：import, modify, move, duplicate, rename, get_info, create_folder, reload, select, ping, select_depends, select_usage, treeEtc", false),
                new MethodKey("path", "Asset path，UnityStandard format：Assets/Folder/File.extension，treeAs root path", false),
                new MethodKey("properties", "Asset property dictionary，Used to set various asset properties", true),
                new MethodKey("destination", "Target path（Move/Used during copy）", true),
                new MethodKey("force", "Whether operation is forced（Overwrite existing files etc.）", true),
                new MethodKey("refresh_type", "Refresh type：all(All), assets(Assets only), scripts(Scripts only)，Defaultall", true),
                new MethodKey("save_before_refresh", "Whether to save all assets before refresh，Defaulttrue", true),
                new MethodKey("include_indirect", "Whether includes indirect dependencies/Reference，Defaultfalse", true),
                new MethodKey("max_results", "Maximum result count，Default100", true)
            };
        }

        /// <summary>
        /// Create state tree
        /// </summary>
        /// <returns>State tree</returns>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("import", ReimportAsset)
                    .Leaf("refresh", RefreshProject)
                    .Leaf("modify", ModifyAsset)
                    .Leaf("duplicate", DuplicateAsset)
                    .Leaf("move", MoveOrRenameAsset)
                    .Leaf("rename", MoveOrRenameAsset)
                    .Leaf("get_info", GetAssetInfo)
                    .Leaf("create_folder", CreateFolder)
                    .Leaf("select", SelectAsset)
                    .Leaf("ping", PingAsset)
                    .Leaf("select_depends", SelectDependencies)
                    .Leaf("select_usage", SelectUsages)
                    .Leaf("tree", GetFolderStructure)
                .Build();
        }

        private object ReimportAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for reimport.");
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                // TODO: Apply importer properties before reimporting?
                // This is complex as it requires getting the AssetImporter, casting it,
                // applying properties via reflection or specific methods, saving, then reimporting.
                JsonClass properties = args["properties"] as JsonClass;
                if (properties != null && properties.Count > 0)
                {
                    Debug.LogWarning(
                        "[ManageAsset.Reimport] Modifying importer properties before reimport is not fully implemented yet."
                    );
                    // AssetImporter importer = AssetImporter.GetAtPath(fullPath);
                    // if (importer != null) { /* Apply properties */ AssetDatabase.WriteImportSettingsIfDirty(fullPath); }
                }

                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
                // AssetDatabase.Refresh(); // Usually ImportAsset handles refresh
                return Response.Success($"Asset '{fullPath}' reimported.", GetAssetData(fullPath));
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to reimport asset '{fullPath}': {e.Message}");
            }
        }



        private object CreateFolder(JsonClass args)
        {
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for create_folder.");
            string fullPath = SanitizeAssetPath(path);
            string parentDir = Path.GetDirectoryName(fullPath);
            string folderName = Path.GetFileName(fullPath);

            if (AssetExists(fullPath))
            {
                // Check if it's actually a folder already
                if (AssetDatabase.IsValidFolder(fullPath))
                {
                    return Response.Success(
                        $"Folder already exists at path: {fullPath}",
                        GetAssetData(fullPath)
                    );
                }
                else
                {
                    return Response.Error(
                        $"An asset (not a folder) already exists at path: {fullPath}"
                    );
                }
            }

            try
            {
                // Ensure parent exists
                if (!string.IsNullOrEmpty(parentDir) && !AssetDatabase.IsValidFolder(parentDir))
                {
                    // Recursively create parent folders if needed (AssetDatabase handles this internally)
                    // Or we can do it manually: Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), parentDir)); AssetDatabase.Refresh();
                }

                string guid = AssetDatabase.CreateFolder(parentDir, folderName);
                if (string.IsNullOrEmpty(guid))
                {
                    return Response.Error(
                        $"Failed to create folder '{fullPath}'. Check logs and permissions."
                    );
                }

                // AssetDatabase.Refresh(); // CreateFolder usually handles refresh
                return Response.Success(
                    $"Folder '{fullPath}' created successfully.",
                    GetAssetData(fullPath)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create folder '{fullPath}': {e.Message}");
            }
        }

        private object ModifyAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            JsonClass properties = args["properties"] as JsonClass;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for modify.");
            if (properties == null || properties.Count == 0)
                return Response.Error("'properties' are required for modify.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    fullPath
                );
                if (asset == null)
                    return Response.Error($"Failed to load asset at path: {fullPath}");

                // Record the asset state for Undo before making any modifications
                Undo.RecordObject(asset, $"Modify Asset '{Path.GetFileName(fullPath)}'");

                bool modified = false; // Flag to track if any changes were made

                // --- NEW: Handle GameObject / Prefab Component Modification ---
                if (asset is GameObject gameObject)
                {
                    // Iterate through the properties Json: keys are component names, values are properties objects for that component
                    foreach (KeyValuePair<string, JsonNode> prop in properties.AsEnumerable())
                    {
                        string component_type = prop.Key; // e.g., "Collectible"
                        // Check if the value associated with the component name is actually an object containing properties
                        if (
                            prop.Value is JsonClass component_properties
                            && component_properties.Count > 0
                        ) // e.g., {"bobSpeed": 2.0}
                        {
                            // Find the component on the GameObject using the name from the Json key
                            // Using GetComponent(string) is convenient but might require exact type name or be ambiguous.
                            // Consider using FindType helper if needed for more complex scenarios.
                            Component targetComponent = gameObject.GetComponent(component_type);

                            if (targetComponent != null)
                            {
                                // Record the component state for Undo before modification
                                Undo.RecordObject(targetComponent, $"Modify Component '{component_type}' on '{gameObject.name}'");

                                // Apply the nested properties (e.g., bobSpeed) to the found component instance
                                // Use |= to ensure 'modified' becomes true if any component is successfully modified
                                modified |= ApplyObjectProperties(
                                    targetComponent,
                                    component_properties
                                );
                            }
                            else
                            {
                                // Log a warning if a specified component couldn't be found
                                Debug.LogWarning(
                                    $"[ManageAsset.ModifyAsset] Component '{component_type}' not found on GameObject '{gameObject.name}' in asset '{fullPath}'. Skipping modification for this component."
                                );
                            }
                        }
                        else
                        {
                            // Log a warning if the structure isn't {"component_type": {"prop": value}}
                            // We could potentially try to apply this property directly to the GameObject here if needed,
                            // but the primary goal is component modification.
                            Debug.LogWarning(
                                $"[ManageAsset.ModifyAsset] Property '{prop.Key}' for GameObject modification should have a Json object value containing component properties. Value was: {prop.Value.type}. Skipping."
                            );
                        }
                    }
                    // Note: 'modified' is now true if ANY component property was successfully changed.
                }
                // --- End NEW ---

                // --- Existing logic for other asset types (now as else-if) ---
                // Example: Modifying a Material
                else if (asset is Material material)
                {
                    // Material already recorded by the main Undo.RecordObject call above
                    // Apply properties directly to the material. If this modifies, it sets modified=true.
                    // Use |= in case the asset was already marked modified by previous logic (though unlikely here)
                    modified |= ApplyMaterialProperties(material, properties);
                }
                // Example: Modifying a ScriptableObject
                else if (asset is ScriptableObject so)
                {
                    // ScriptableObject already recorded by the main Undo.RecordObject call above
                    // Apply properties directly to the ScriptableObject.
                    modified |= ApplyObjectProperties(so, properties); // General helper
                }
                // Example: Modifying TextureImporter settings
                else if (asset is Texture)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(fullPath);
                    if (importer is TextureImporter textureImporter)
                    {
                        // Record the importer state for Undo before modification
                        Undo.RecordObject(textureImporter, $"Modify Texture Import Settings '{Path.GetFileName(fullPath)}'");

                        bool importerModified = ApplyObjectProperties(textureImporter, properties);
                        if (importerModified)
                        {
                            // Importer settings need saving and reimporting
                            AssetDatabase.WriteImportSettingsIfDirty(fullPath);
                            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate); // Reimport to apply changes
                            modified = true; // Mark overall operation as modified
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Could not get TextureImporter for {fullPath}.");
                    }
                }
                // TODO: Add modification logic for other common asset types (Models, AudioClips importers, etc.)
                else // Fallback for other asset types OR direct properties on non-GameObject assets
                {
                    // This block handles non-GameObject/Material/ScriptableObject/Texture assets.
                    // Asset already recorded by the main Undo.RecordObject call above
                    // Attempts to apply properties directly to the asset itself.
                    Debug.LogWarning(
                        $"[ManageAsset.ModifyAsset] Asset type '{asset.GetType().Name}' at '{fullPath}' is not explicitly handled for component modification. Attempting generic property setting on the asset itself."
                    );
                    modified |= ApplyObjectProperties(asset, properties);
                }
                // --- End Existing Logic ---

                // Check if any modification happened (either component or direct asset modification)
                if (modified)
                {
                    // Mark the asset as dirty (important for prefabs/SOs) so Unity knows to save it.
                    EditorUtility.SetDirty(asset);
                    // Save all modified assets to disk.
                    AssetDatabase.SaveAssets();
                    // Refresh might be needed in some edge cases, but SaveAssets usually covers it.
                    // AssetDatabase.Refresh();
                    return Response.Success(
                        $"Asset '{fullPath}' modified successfully.",
                        GetAssetData(fullPath)
                    );
                }
                else
                {
                    // If no changes were made (e.g., component not found, property names incorrect, value unchanged), return a success message indicating nothing changed.
                    return Response.Success(
                        $"No applicable or modifiable properties found for asset '{fullPath}'. Check component names, property names, and values.",
                        GetAssetData(fullPath)
                    );
                    // Previous message: return Response.Success($"No applicable properties found to modify for asset '{fullPath}'.", GetAssetData(fullPath));
                }
            }
            catch (Exception e)
            {
                // Log the detailed error internally
                Debug.LogError($"[ManageAsset] Action 'modify' failed for path '{path}': {e}");
                // Return a user-friendly error message
                return Response.Error($"Failed to modify asset '{fullPath}': {e.Message}");
            }
        }



        private object DuplicateAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for duplicate.");

            string sourcePath = SanitizeAssetPath(path);
            if (!AssetExists(sourcePath))
                return Response.Error($"Source asset not found at path: {sourcePath}");

            string destPath;
            if (string.IsNullOrEmpty(destinationPath))
            {
                // Generate a unique path if destination is not provided
                destPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            }
            else
            {
                destPath = SanitizeAssetPath(destinationPath);
                if (AssetExists(destPath))
                    return Response.Error($"Asset already exists at destination path: {destPath}");
                // Ensure destination directory exists
                EnsureDirectoryExists(Path.GetDirectoryName(destPath));
            }

            try
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
                if (success)
                {
                    // AssetDatabase.Refresh();
                    return Response.Success(
                        $"Asset '{sourcePath}' duplicated to '{destPath}'.",
                        GetAssetData(destPath)
                    );
                }
                else
                {
                    return Response.Error(
                        $"Failed to duplicate asset from '{sourcePath}' to '{destPath}'."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error duplicating asset '{sourcePath}': {e.Message}");
            }
        }

        private object MoveOrRenameAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            string destinationPath = args["destination"]?.Value;

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for move/rename.");
            if (string.IsNullOrEmpty(destinationPath))
                return Response.Error("'destination' path is required for move/rename.");

            string sourcePath = SanitizeAssetPath(path);
            string destPath = SanitizeAssetPath(destinationPath);

            if (!AssetExists(sourcePath))
                return Response.Error($"Source asset not found at path: {sourcePath}");
            if (AssetExists(destPath))
                return Response.Error(
                    $"An asset already exists at the destination path: {destPath}"
                );

            // Ensure destination directory exists
            EnsureDirectoryExists(Path.GetDirectoryName(destPath));

            try
            {
                // Validate will return an error string if failed, null if successful
                string error = AssetDatabase.ValidateMoveAsset(sourcePath, destPath);
                if (!string.IsNullOrEmpty(error))
                {
                    return Response.Error(
                        $"Failed to move/rename asset from '{sourcePath}' to '{destPath}': {error}"
                    );
                }

                string guid = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (!string.IsNullOrEmpty(guid)) // MoveAsset returns the new GUID on success
                {
                    // AssetDatabase.Refresh(); // MoveAsset usually handles refresh
                    return Response.Success(
                        $"Asset moved/renamed from '{sourcePath}' to '{destPath}'.",
                        GetAssetData(destPath)
                    );
                }
                else
                {
                    // This case might not be reachable if ValidateMoveAsset passes, but good to have
                    return Response.Error(
                        $"MoveAsset call failed unexpectedly for '{sourcePath}' to '{destPath}'."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error moving/renaming asset '{sourcePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Select resource at specified path
        /// </summary>
        private object SelectAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for select.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                if (asset == null)
                    return Response.Error($"Failed to load asset at path: {fullPath}");

                Selection.activeObject = asset;
                return Response.Success(
                    $"Asset '{fullPath}' selected successfully.",
                    GetAssetData(fullPath)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error selecting asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Locate（ping）Resource at specified path，InProjectHighlight in window
        /// </summary>
        private object PingAsset(JsonClass args)
        {
            string path = args["path"]?.Value;
            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for ping.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                if (asset == null)
                    return Response.Error($"Failed to load asset at path: {fullPath}");

                EditorGUIUtility.PingObject(asset);
                return Response.Success(
                    $"Asset '{fullPath}' pinged successfully.",
                    GetAssetData(fullPath)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error pinging asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Select all dependencies of specified resource
        /// </summary>
        private object SelectDependencies(JsonClass args)
        {
            string path = args["path"]?.Value;
            bool includeIndirect = args["include_indirect"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for select_depends.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                string[] dependencies = AssetDatabase.GetDependencies(fullPath, includeIndirect);
                List<UnityEngine.Object> dependencyObjects = new List<UnityEngine.Object>();
                List<object> dependencyData = new List<object>();

                foreach (string depPath in dependencies)
                {
                    if (depPath == fullPath) continue; // Exclude self

                    UnityEngine.Object depAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(depPath);
                    if (depAsset != null)
                    {
                        dependencyObjects.Add(depAsset);
                        dependencyData.Add(GetAssetData(depPath));
                    }
                }

                // Select all dependencies
                Selection.objects = dependencyObjects.ToArray();

                return Response.Success(
                    $"Selected {dependencyObjects.Count} dependencies for asset '{fullPath}' (indirect: {includeIndirect}).",
                    new
                    {
                        sourceAsset = GetAssetData(fullPath),
                        dependencyCount = dependencyObjects.Count,
                        includeIndirect = includeIndirect,
                        dependencies = dependencyData
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error selecting dependencies for asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Query and select all resources that reference the specified resource（Optimized version）
        /// </summary>
        private object SelectUsages(JsonClass args)
        {
            string path = args["path"]?.Value;
            bool includeIndirect = args["include_indirect"].AsBoolDefault(false);
            int maxResults = args["max_results"].AsIntDefault(100); // Limit result count

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for select_usage.");

            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                var startTime = System.DateTime.Now;

                // Get resource'sGUID
                string targetGuid = AssetDatabase.AssetPathToGUID(fullPath);
                if (string.IsNullOrEmpty(targetGuid))
                    return Response.Error($"Could not get GUID for asset: {fullPath}");

                List<string> referencingPaths = new List<string>();

                // Method1: UseUnityBuiltin reference search（More efficient）
                if (TryFindReferencesUsingBuiltinAPI(fullPath, targetGuid, referencingPaths, maxResults))
                {
                    LogInfo($"[SelectUsages] Used builtin API to find references");
                }
                else
                {
                    // Method2: Optimized manual search（Find only common asset types）
                    FindReferencesOptimized(fullPath, targetGuid, referencingPaths, includeIndirect, maxResults);
                }

                List<UnityEngine.Object> referencingObjects = new List<UnityEngine.Object>();
                List<object> referencingData = new List<object>();

                foreach (string refPath in referencingPaths.Take(maxResults))
                {
                    UnityEngine.Object refAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(refPath);
                    if (refAsset != null)
                    {
                        referencingObjects.Add(refAsset);
                        referencingData.Add(GetAssetData(refPath));
                    }
                }

                // Select all resources referencing this one
                Selection.objects = referencingObjects.ToArray();

                var duration = System.DateTime.Now - startTime;
                string message = referencingPaths.Count >= maxResults
                    ? $"Found {referencingPaths.Count}+ references (showing first {referencingObjects.Count}) for '{fullPath}' in {duration.TotalMilliseconds:F0}ms"
                    : $"Selected {referencingObjects.Count} assets that reference '{fullPath}' in {duration.TotalMilliseconds:F0}ms";

                return Response.Success(message, new
                {
                    targetAsset = GetAssetData(fullPath),
                    referencingCount = referencingObjects.Count,
                    totalFound = referencingPaths.Count,
                    maxResults = maxResults,
                    includeIndirect = includeIndirect,
                    searchDurationMs = duration.TotalMilliseconds,
                    referencingAssets = referencingData
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Error finding usages for asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Try usingUnityBuiltinAPIFind references（If available）
        /// </summary>
        private bool TryFindReferencesUsingBuiltinAPI(string targetPath, string targetGuid, List<string> referencingPaths, int maxResults)
        {
            try
            {
                // Try usingUnity 2020+Builtin reference search forAPI
                var searchFilter = $"ref:{targetGuid}";
                string[] foundGuids = AssetDatabase.FindAssets(searchFilter);

                if (foundGuids != null && foundGuids.Length > 0)
                {
                    foreach (string guid in foundGuids.Take(maxResults))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(assetPath) && assetPath != targetPath)
                        {
                            referencingPaths.Add(assetPath);
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogInfo($"[SelectUsages] Builtin API not available or failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Optimized manual reference lookup（Find only common asset types）
        /// </summary>
        private void FindReferencesOptimized(string targetPath, string targetGuid, List<string> referencingPaths, bool includeIndirect, int maxResults)
        {
            // Find only common resource types that may contain references，Instead of all resources
            string[] searchFilters = {
                "t:Scene",           // Scene file
                "t:Prefab",          // Prefab
                "t:Material",        // Material
                "t:AnimationClip",   // Animation clip
                "t:AnimatorController", // Animator controller
                "t:ScriptableObject", // ScriptableObject
                "t:Shader"           // Shader
            };

            var processedGuids = new HashSet<string>();

            foreach (string filter in searchFilters)
            {
                if (referencingPaths.Count >= maxResults) break;

                string[] guids = AssetDatabase.FindAssets(filter);

                foreach (string guid in guids)
                {
                    if (referencingPaths.Count >= maxResults) break;
                    if (processedGuids.Contains(guid)) continue;

                    processedGuids.Add(guid);
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (string.IsNullOrEmpty(assetPath) || assetPath == targetPath) continue;

                    // Check dependency relations
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, includeIndirect);
                    if (dependencies.Contains(targetPath))
                    {
                        referencingPaths.Add(assetPath);
                    }
                }
            }
        }

        private object GetAssetInfo(JsonClass args)
        {
            string path = args["path"]?.Value;
            bool generatePreview = args["generate_preview"].AsBoolDefault(false);

            if (string.IsNullOrEmpty(path))
                return Response.Error("'path' is required for get_info.");
            string fullPath = SanitizeAssetPath(path);
            if (!AssetExists(fullPath))
                return Response.Error($"Asset not found at path: {fullPath}");

            try
            {
                return Response.Success(
                    "Asset info retrieved.",
                    GetAssetData(fullPath, generatePreview)
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting info for asset '{fullPath}': {e.Message}");
            }
        }

        /// <summary>
        /// Overload（Refresh）UnityProject asset database
        /// </summary>
        private object RefreshProject(JsonClass args)
        {
            try
            {
                string refreshType = args["refresh_type"]?.Value;
                if (string.IsNullOrEmpty(refreshType)) refreshType = "all";
                else refreshType = refreshType.ToLower();
                bool saveBeforeRefresh = args["save_before_refresh"].AsBoolDefault(true);
                string specificPath = args["path"]?.Value;

                LogInfo($"[ProjectOperate] Starting project reload with type: {refreshType}");

                // Record start time for performance monitoring
                var startTime = System.DateTime.Now;

                // Save all pending assets
                if (saveBeforeRefresh)
                {
                    LogInfo("[ProjectOperate] Saving all modified assets before refresh...");
                    AssetDatabase.SaveAssets();
                }

                // Perform different refresh ops according to type
                switch (refreshType)
                {
                    case "all":
                        LogInfo("[ProjectOperate] Performing full project refresh...");
                        // Full refresh：Including asset import、Script compilation etc.
                        if (!string.IsNullOrEmpty(specificPath))
                        {
                            string sanitizedPath = SanitizeAssetPath(specificPath);
                            if (AssetExists(sanitizedPath))
                            {
                                AssetDatabase.ImportAsset(sanitizedPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                                LogInfo($"[ProjectOperate] Refreshed specific path: {sanitizedPath}");
                            }
                            else
                            {
                                LogInfo($"[ProjectOperate] Specified path '{specificPath}' not found, performing full refresh");
                            }
                        }
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        break;

                    case "assets":
                        LogInfo("[ProjectOperate] Performing assets-only refresh...");
                        // Refresh only assets，Do not trigger script recompilation
                        if (!string.IsNullOrEmpty(specificPath))
                        {
                            string sanitizedPath = SanitizeAssetPath(specificPath);
                            if (AssetExists(sanitizedPath))
                            {
                                AssetDatabase.ImportAsset(sanitizedPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                            }
                        }
                        AssetDatabase.Refresh(ImportAssetOptions.Default);
                        break;

                    case "scripts":
                        LogInfo("[ProjectOperate] Performing scripts-only refresh...");
                        // Mainly for script recompilation
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        // Force request for script recompilation
                        EditorUtility.RequestScriptReload();
                        break;

                    default:
                        return Response.Error($"Unknown refresh_type: '{refreshType}'. Valid options are 'all', 'assets', or 'scripts'.");
                }

                // Calculate duration
                var duration = System.DateTime.Now - startTime;

                // Get project stats info
                var projectStats = GetProjectStatistics();

                LogInfo($"[ProjectOperate] Project reload completed in {duration.TotalSeconds:F2} seconds");

                return Response.Success(
                    $"Project reloaded successfully with type '{refreshType}' in {duration.TotalSeconds:F2} seconds.",
                    new
                    {
                        refreshType = refreshType,
                        durationSeconds = duration.TotalSeconds,
                        savedBeforeRefresh = saveBeforeRefresh,
                        specificPath = specificPath,
                        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        projectStatistics = projectStats
                    }
                );
            }
            catch (Exception e)
            {
                LogError($"[ProjectOperate] Failed to reload project: {e.Message}");
                return Response.Error($"Failed to reload project: {e.Message}");
            }
        }



        /// <summary>
        /// Get project stats info
        /// </summary>
        private object GetProjectStatistics()
        {
            try
            {
                // Count different asset types
                string[] allAssetGUIDs = AssetDatabase.FindAssets("");

                int totalAssets = allAssetGUIDs.Length;
                int scriptCount = AssetDatabase.FindAssets("t:MonoScript").Length;
                int prefabCount = AssetDatabase.FindAssets("t:Prefab").Length;
                int materialCount = AssetDatabase.FindAssets("t:Material").Length;
                int textureCount = AssetDatabase.FindAssets("t:Texture2D").Length;
                int sceneCount = AssetDatabase.FindAssets("t:Scene").Length;
                int audioCount = AssetDatabase.FindAssets("t:AudioClip").Length;
                int modelCount = AssetDatabase.FindAssets("t:Model").Length;

                // Count folders
                int folderCount = 0;
                foreach (string guid in allAssetGUIDs)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        folderCount++;
                    }
                }

                return new
                {
                    totalAssets = totalAssets,
                    breakdown = new
                    {
                        scripts = scriptCount,
                        prefabs = prefabCount,
                        materials = materialCount,
                        textures = textureCount,
                        scenes = sceneCount,
                        audioClips = audioCount,
                        models = modelCount,
                        folders = folderCount,
                        others = totalAssets - scriptCount - prefabCount - materialCount - textureCount - sceneCount - audioCount - modelCount - folderCount
                    },
                    lastRefreshTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectOperate] Failed to get project statistics: {e.Message}");
                return new
                {
                    error = "Failed to gather project statistics",
                    message = e.Message
                };
            }
        }

        /// <summary>
        /// Get folder structure（YAMLFormat）
        /// </summary>
        private object GetFolderStructure(JsonClass args)
        {
            try
            {
                string rootPath = args["path"]?.Value;
                if (string.IsNullOrEmpty(rootPath))
                    rootPath = "Assets";

                const int maxDepth = 10; // Fixed maximum depth as10

                rootPath = SanitizeAssetPath(rootPath);

                if (!AssetDatabase.IsValidFolder(rootPath))
                {
                    return Response.Error($"Path '{rootPath}' is not a valid folder.");
                }

                LogInfo($"[ProjectOperate] Getting folder structure for: {rootPath}");

                var startTime = System.DateTime.Now;

                // Build folder structure
                var structure = BuildFolderStructure(rootPath, 0, maxDepth);

                // GenerateYAMLFormat string
                var yamlBuilder = new System.Text.StringBuilder();
                GenerateYamlStructure(structure, yamlBuilder, 0);

                var duration = System.DateTime.Now - startTime;

                return Response.Success(
                    $"Folder structure retrieved successfully in {duration.TotalMilliseconds:F0}ms.",
                    new
                    {
                        rootPath = rootPath,
                        yaml = yamlBuilder.ToString(),
                        durationMs = duration.TotalMilliseconds,
                        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                );
            }
            catch (Exception e)
            {
                LogError($"[ProjectOperate] Failed to get folder structure: {e.Message}");
                return Response.Error($"Failed to get folder structure: {e.Message}");
            }
        }

        /// <summary>
        /// Recursively build folder structure
        /// </summary>
        private FolderNode BuildFolderStructure(string folderPath, int currentDepth, int maxDepth)
        {
            var node = new FolderNode
            {
                Name = Path.GetFileName(folderPath),
                Path = folderPath,
                FileCount = 0,
                SubFolders = new List<FolderNode>()
            };

            // If max depth reached，No longer recurse
            if (currentDepth >= maxDepth)
            {
                return node;
            }

            try
            {
                // Get all assets under current folder
                string[] allGuids = AssetDatabase.FindAssets("", new[] { folderPath });

                foreach (string guid in allGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Check if direct child of current folder
                    string parentDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (parentDir != folderPath)
                    {
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        // Recursively process subfolders
                        var subFolder = BuildFolderStructure(assetPath, currentDepth + 1, maxDepth);
                        node.SubFolders.Add(subFolder);
                    }
                    else
                    {
                        // Count files（Exclude.metaFile）
                        if (!assetPath.EndsWith(".meta"))
                        {
                            node.FileCount++;
                        }
                    }
                }

                // Sort subfolders by name
                node.SubFolders = node.SubFolders.OrderBy(f => f.Name).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildFolderStructure] Error processing folder '{folderPath}': {ex.Message}");
            }

            return node;
        }

        /// <summary>
        /// GenerateYAMLFolder structure string in format of
        /// </summary>
        private void GenerateYamlStructure(FolderNode node, System.Text.StringBuilder builder, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);

            // Output folder and file counts
            if (indentLevel == 0)
            {
                // Root node
                builder.AppendLine($"{node.Name}:");
            }
            else
            {
                if (node.SubFolders.Count > 0)
                {
                    // Has subfolder，Single line
                    builder.AppendLine($"{indent}{node.Name}:");
                    if (node.FileCount > 0)
                    {
                        builder.AppendLine($"{indent}  files: {node.FileCount}");
                    }
                }
                else
                {
                    // No subfolder，Directly output file counts
                    builder.AppendLine($"{indent}{node.Name}: {node.FileCount}");
                    return; // No subfolder，End
                }
            }

            // Recursively output subfolders
            foreach (var subFolder in node.SubFolders)
            {
                GenerateYamlStructure(subFolder, builder, indentLevel + 1);
            }
        }

        /// <summary>
        /// Folder node class，For folder structure representation
        /// </summary>
        private class FolderNode
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public int FileCount { get; set; }
            public List<FolderNode> SubFolders { get; set; }
        }

        // --- Internal Helpers ---

        /// <summary>
        /// Ensures the asset path starts with "Assets/".
        /// </summary>
        private string SanitizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            path = path.Replace('\\', '/'); // Normalize separators
            if (!path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets/" + path.TrimStart('/');
            }
            return path;
        }

        /// <summary>
        /// Checks if an asset exists at the given path (file or folder).
        /// </summary>
        private bool AssetExists(string sanitizedPath)
        {
            // AssetDatabase APIs are generally preferred over raw File/Directory checks for assets.
            // Check if it's a known asset GUID.
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath)))
            {
                return true;
            }
            // AssetPathToGUID might not work for newly created folders not yet refreshed.
            // Check directory explicitly for folders.
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                // Check if it's considered a *valid* folder by Unity
                return AssetDatabase.IsValidFolder(sanitizedPath);
            }
            // Check file existence for non-folder assets.
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), sanitizedPath)))
            {
                return true; // Assume if file exists, it's an asset or will be imported
            }

            return false;
            // Alternative: return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(sanitizedPath));
        }

        /// <summary>
        /// Ensures the directory for a given asset path exists, creating it if necessary.
        /// </summary>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;
            string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directoryPath);
            if (!Directory.Exists(fullDirPath))
            {
                Directory.CreateDirectory(fullDirPath);
                AssetDatabase.Refresh(); // Let Unity know about the new folder
            }
        }

        /// <summary>
        /// Applies properties from JsonClass to a Material.
        /// </summary>
        private bool ApplyMaterialProperties(Material mat, JsonClass properties)
        {
            if (mat == null || properties == null)
                return false;
            bool modified = false;

            // Example: Set shader
            if (properties["shader"]?.type == JsonNodeType.String)
            {
                Shader newShader = Shader.Find(properties["shader"].Value);
                if (newShader != null && mat.shader != newShader)
                {
                    mat.shader = newShader;
                    modified = true;
                }
            }
            // Example: Set color property
            if (properties["color"] is JsonClass colorProps)
            {
                string propName = colorProps["name"]?.Value;
                if (string.IsNullOrEmpty(propName)) propName = "_Color"; // Default main color
                if (colorProps["value"] is JsonArray colArr && colArr.Count >= 3)
                {
                    try
                    {
                        Color newColor = new Color(
                            colArr[0].AsFloat,
                            colArr[1].AsFloat,
                            colArr[2].AsFloat,
                            colArr.Count > 3 ? colArr[3].AsFloat : 1.0f
                        );
                        if (mat.HasProperty(propName) && mat.GetColor(propName) != newColor)
                        {
                            mat.SetColor(propName, newColor);
                            modified = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"Error parsing color property '{propName}': {ex.Message}"
                        );
                    }
                }
            }
            // Example: Set float property
            if (properties["float"] is JsonClass floatProps)
            {
                string propName = floatProps["name"]?.Value;
                if (
                    !string.IsNullOrEmpty(propName) && floatProps["value"]?.type == JsonNodeType.Float
                    || floatProps["value"]?.type == JsonNodeType.Integer
                )
                {
                    try
                    {
                        float newVal = floatProps["value"].AsFloat;
                        if (mat.HasProperty(propName) && mat.GetFloat(propName) != newVal)
                        {
                            mat.SetFloat(propName, newVal);
                            modified = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"Error parsing float property '{propName}': {ex.Message}"
                        );
                    }
                }
            }
            // Example: Set texture property
            if (properties["texture"] is JsonClass texProps)
            {
                string propName = texProps["name"]?.Value;
                if (string.IsNullOrEmpty(propName)) propName = "_MainTex"; // Default main texture
                string texPath = texProps["path"]?.Value;
                if (!string.IsNullOrEmpty(texPath))
                {
                    Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(
                        SanitizeAssetPath(texPath)
                    );
                    if (
                        newTex != null
                        && mat.HasProperty(propName)
                        && mat.GetTexture(propName) != newTex
                    )
                    {
                        mat.SetTexture(propName, newTex);
                        modified = true;
                    }
                    else if (newTex == null)
                    {
                        Debug.LogWarning($"Texture not found at path: {texPath}");
                    }
                }
            }

            // Handle direct material properties (e.g., "_Color", "_MainTex", etc.)
            foreach (KeyValuePair<string, JsonNode> prop in properties.AsEnumerable())
            {
                string propName = prop.Key;
                JsonNode propValue = prop.Value;

                // Skip properties already handled above
                if (propName == "shader" || propName == "color" || propName == "float" || propName == "texture")
                    continue;

                try
                {
                    if (mat.HasProperty(propName))
                    {
                        // Handle Color properties (like "_Color")
                        if (propValue is JsonClass colorObj &&
                            colorObj["r"] != null && colorObj["g"] != null && colorObj["b"] != null)
                        {
                            Color newColor = new Color(
                                colorObj["r"].AsFloat,
                                colorObj["g"].AsFloat,
                                colorObj["b"].AsFloat,
                                colorObj["a"].AsFloatDefault(1.0f)
                            );
                            if (mat.GetColor(propName) != newColor)
                            {
                                mat.SetColor(propName, newColor);
                                modified = true;
                                Debug.Log($"[ApplyMaterialProperties] Set {propName} to {newColor}");
                            }
                        }
                        // Handle Vector4 properties
                        else if (propValue is JsonArray vecArray && vecArray.Count >= 4)
                        {
                            Vector4 newVector = new Vector4(
                                vecArray[0].AsFloat,
                                vecArray[1].AsFloat,
                                vecArray[2].AsFloat,
                                vecArray[3].AsFloat
                            );
                            if (mat.GetVector(propName) != newVector)
                            {
                                mat.SetVector(propName, newVector);
                                modified = true;
                            }
                        }
                        // Handle Float properties
                        else if (propValue.type == JsonNodeType.Float || propValue.type == JsonNodeType.Integer)
                        {
                            float newVal = propValue.AsFloat;
                            if (Math.Abs(mat.GetFloat(propName) - newVal) > 0.001f)
                            {
                                mat.SetFloat(propName, newVal);
                                modified = true;
                            }
                        }
                        // Handle Texture properties (string paths)
                        else if (propValue.type == JsonNodeType.String)
                        {
                            string texPath = propValue.Value;
                            if (!string.IsNullOrEmpty(texPath))
                            {
                                Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(SanitizeAssetPath(texPath));
                                if (newTex != null && mat.GetTexture(propName) != newTex)
                                {
                                    mat.SetTexture(propName, newTex);
                                    modified = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ApplyMaterialProperties] Error setting property '{propName}': {ex.Message}");
                }
            }

            // TODO: Add handlers for other property types (Keywords, RenderQueue, etc.)
            return modified;
        }

        /// <summary>
        /// Generic helper to set properties on any UnityEngine.Object using reflection.
        /// </summary>
        private bool ApplyObjectProperties(UnityEngine.Object target, JsonClass properties)
        {
            if (target == null || properties == null)
                return false;
            bool modified = false;
            Type type = target.GetType();

            foreach (KeyValuePair<string, JsonNode> prop in properties.AsEnumerable())
            {
                string propName = prop.Key;
                JsonNode propValue = prop.Value;
                if (SetPropertyOrField(target, propName, propValue, type))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Helper to set a property or field via reflection, handling basic types and Unity objects.
        /// </summary>
        private bool SetPropertyOrField(
            object target,
            string memberName,
            JsonNode value,
            Type type = null
        )
        {
            type = type ?? target.GetType();
            System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.IgnoreCase;

            try
            {
                System.Reflection.PropertyInfo propInfo = type.GetProperty(memberName, flags);
                if (propInfo != null && propInfo.CanWrite)
                {
                    object convertedValue = ConvertJTokenToType(value, propInfo.PropertyType);
                    if (
                        convertedValue != null
                        && !object.Equals(propInfo.GetValue(target), convertedValue)
                    )
                    {
                        propInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                else
                {
                    System.Reflection.FieldInfo fieldInfo = type.GetField(memberName, flags);
                    if (fieldInfo != null)
                    {
                        object convertedValue = ConvertJTokenToType(value, fieldInfo.FieldType);
                        if (
                            convertedValue != null
                            && !object.Equals(fieldInfo.GetValue(target), convertedValue)
                        )
                        {
                            fieldInfo.SetValue(target, convertedValue);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SetPropertyOrField] Failed to set '{memberName}' on {type.Name}: {ex.Message}"
                );
            }
            return false;
        }

        /// <summary>
        /// Simple JsonNode to Type conversion for common Unity types and primitives.
        /// </summary>
        private object ConvertJTokenToType(JsonNode token, Type targetType)
        {
            try
            {
                if (token == null || token.type == JsonNodeType.Null)
                    return null;

                if (targetType == typeof(string))
                    return token.Value;
                if (targetType == typeof(int))
                    return token.AsInt;
                if (targetType == typeof(float))
                    return token.AsFloat;
                if (targetType == typeof(bool))
                    return token.AsBool;
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
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, token.Value, true); // Case-insensitive enum parsing

                // Handle loading Unity Objects (Materials, Textures, etc.) by path
                if (
                    typeof(UnityEngine.Object).IsAssignableFrom(targetType)
                    && token.type == JsonNodeType.String
                )
                {
                    string assetPath = SanitizeAssetPath(token.Value);
                    UnityEngine.Object loadedAsset = AssetDatabase.LoadAssetAtPath(
                        assetPath,
                        targetType
                    );
                    if (loadedAsset == null)
                    {
                        Debug.LogWarning(
                            $"[ConvertJTokenToType] Could not load asset of type {targetType.Name} from path: {assetPath}"
                        );
                    }
                    return loadedAsset;
                }

                // SimpleJson Not supported ToObject(targetType)
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[ConvertJTokenToType] Could not convert JsonNode '{token}' (type {token.type}) to type '{targetType.Name}': {ex.Message}"
                );
                return null;
            }
        }

        /// <summary>
        /// Helper to find a Type by name, searching relevant assemblies.
        /// Needed for creating ScriptableObjects or finding component types by name.
        /// </summary>
        private Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Try direct lookup first (common Unity types often don't need assembly qualified name)
            var type =
                Type.GetType(typeName)
                ?? Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule")
                ?? Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI")
                ?? Type.GetType($"UnityEditor.{typeName}, UnityEditor.CoreModule");

            if (type != null)
                return type;

            // If not found, search loaded assemblies (slower but more robust for user scripts)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Look for non-namespaced first
                type = assembly.GetType(typeName, false, true); // throwOnError=false, ignoreCase=true
                if (type != null)
                    return type;

                // Check common namespaces if simple name given
                type = assembly.GetType("UnityEngine." + typeName, false, true);
                if (type != null)
                    return type;
                type = assembly.GetType("UnityEditor." + typeName, false, true);
                if (type != null)
                    return type;
                // Add other likely namespaces if needed (e.g., specific plugins)
            }

            Debug.LogWarning($"[FindType] Type '{typeName}' not found in any loaded assembly.");
            return null; // Not found
        }

        // --- Data Serialization ---

        /// <summary>
        /// Creates a serializable representation of an asset.
        /// </summary>
        private object GetAssetData(string path, bool generatePreview = false)
        {
            if (string.IsNullOrEmpty(path) || !AssetExists(path))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(path);
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            string previewBase64 = null;
            int previewWidth = 0;
            int previewHeight = 0;

            if (generatePreview && asset != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(asset);

                if (preview != null)
                {
                    try
                    {
                        // Ensure texture is readable for EncodeToPNG
                        // Creating a temporary readable copy is safer
                        RenderTexture rt = RenderTexture.GetTemporary(
                            preview.width,
                            preview.height
                        );
                        Graphics.Blit(preview, rt);
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = rt;
                        Texture2D readablePreview = new Texture2D(preview.width, preview.height);
                        readablePreview.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        readablePreview.Apply();
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(rt);

                        byte[] pngData = readablePreview.EncodeToPNG();
                        previewBase64 = Convert.ToBase64String(pngData);
                        previewWidth = readablePreview.width;
                        previewHeight = readablePreview.height;
                        UnityEngine.Object.DestroyImmediate(readablePreview); // Clean up temp texture
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"Failed to generate readable preview for '{path}': {ex.Message}. Preview might not be readable."
                        );
                        // Fallback: Try getting static preview if available?
                        // Texture2D staticPreview = AssetPreview.GetMiniThumbnail(asset);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"Could not get asset preview for {path} (Type: {assetType?.Name}). Is it supported?"
                    );
                }
            }

            return new
            {
                path = path,
                guid = guid,
                assetType = assetType?.FullName ?? "Unknown",
                name = Path.GetFileNameWithoutExtension(path),
                fileName = Path.GetFileName(path),
                isFolder = AssetDatabase.IsValidFolder(path),
                instanceID = asset?.GetInstanceID() ?? 0,
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(
                        Path.Combine(Directory.GetCurrentDirectory(), path)
                    )
                    .ToString("o"), // ISO 8601
                // --- Preview Data ---
                previewBase64 = previewBase64, // PNG data as Base64 string
                previewWidth = previewWidth,
                previewHeight = previewHeight,
                // TODO: Add more metadata? Importer settings? Dependencies?
            };
        }

    }
}

