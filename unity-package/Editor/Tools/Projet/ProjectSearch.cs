using System;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles Project window search operations for finding assets and objects.
    /// Corresponding method name: project_search
    /// </summary>
    [ToolName("project_search", "Project management")]
    public class ProjectSearch : StateMethodBase
    {
        /// <summary>
        /// Create parameter key list supported by current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("search_target", "Search type：asset, folder, script, texture, material, prefab, sceneEtc", false),
                new MethodKey("query", "Search keywords", false),
                new MethodKey("directory", "Search path（Relative toAssets）", true),
                new MethodKey("file_extension", "File extension filter", true),
                new MethodKey("recursive", "Whether to recursively search subfolders", true),
                new MethodKey("case_sensitive", "Case sensitive or not", true),
                new MethodKey("max_results", "Maximum return count", true),
                new MethodKey("include_meta", "Whether contains.metaFile", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("search_target")
                     .Leaf("asset", HandleAssetSearch)
                     .Leaf("folder", HandleFolderSearch)
                     .Leaf("script", HandleScriptSearch)
                     .Leaf("texture", HandleTextureSearch)
                     .Leaf("material", HandleMaterialSearch)
                     .Leaf("prefab", HandlePrefabSearch)
                     .Leaf("scene", HandleSceneSearch)
                     .Leaf("audio", HandleAudioSearch)
                     .Leaf("model", HandleModelSearch)
                     .Leaf("shader", HandleShaderSearch)
                     .Leaf("animation", HandleAnimationSearch)
                     .Leaf("general", HandleGeneralSearch)
                     .Leaf("dependencies", HandleDependenciesSearch)
                     .Leaf("references", HandleReferencesSearch)
                .Build();
        }

        /// <summary>
        /// Search all types of assets
        /// </summary>
        private object HandleAssetSearch(JsonClass args)
        {
            return PerformSearch(args, null);
        }

        /// <summary>
        /// Search folders
        /// </summary>
        private object HandleFolderSearch(JsonClass args)
        {
            return PerformSearch(args, "folder");
        }

        /// <summary>
        /// Search script files
        /// </summary>
        private object HandleScriptSearch(JsonClass args)
        {
            return PerformSearch(args, "script", new[] { ".cs", ".js", ".boo" });
        }

        /// <summary>
        /// Search texture files
        /// </summary>
        private object HandleTextureSearch(JsonClass args)
        {
            return PerformSearch(args, "texture", new[] { ".png", ".jpg", ".jpeg", ".tga", ".tiff", ".bmp", ".psd", ".exr" });
        }

        /// <summary>
        /// Search material files
        /// </summary>
        private object HandleMaterialSearch(JsonClass args)
        {
            return PerformSearch(args, "material", new[] { ".mat" });
        }

        /// <summary>
        /// Search prefab files
        /// </summary>
        private object HandlePrefabSearch(JsonClass args)
        {
            return PerformSearch(args, "prefab", new[] { ".prefab" });
        }

        /// <summary>
        /// Search scene files
        /// </summary>
        private object HandleSceneSearch(JsonClass args)
        {
            return PerformSearch(args, "scene", new[] { ".unity" });
        }

        /// <summary>
        /// Search audio files
        /// </summary>
        private object HandleAudioSearch(JsonClass args)
        {
            return PerformSearch(args, "audio", new[] { ".mp3", ".wav", ".ogg", ".aiff", ".aif" });
        }

        /// <summary>
        /// Search3DModel file
        /// </summary>
        private object HandleModelSearch(JsonClass args)
        {
            return PerformSearch(args, "model", new[] { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".skp", ".blend", ".max", ".c4d", ".ma", ".mb" });
        }

        /// <summary>
        /// SearchShaderFile
        /// </summary>
        private object HandleShaderSearch(JsonClass args)
        {
            return PerformSearch(args, "shader", new[] { ".shader", ".cginc", ".hlsl" });
        }

        /// <summary>
        /// Search animation files
        /// </summary>
        private object HandleAnimationSearch(JsonClass args)
        {
            return PerformSearch(args, "animation", new[] { ".anim", ".controller", ".playable" });
        }

        /// <summary>
        /// Generic search
        /// </summary>
        private object HandleGeneralSearch(JsonClass args)
        {
            return PerformSearch(args, null);
        }

        /// <summary>
        /// Main implementation of search
        /// </summary>
        private object PerformSearch(JsonClass args, string searchType, string[] extensions = null)
        {
            string searchTerm = args["query"]?.Value;
            string searchPath = args["directory"]?.Value;
            if (string.IsNullOrEmpty(searchPath)) searchPath = "Assets";
            bool recursive = args["recursive"].AsBoolDefault(true);
            bool caseSensitive = args["case_sensitive"].AsBoolDefault(false);
            int maxResults = args["max_results"].AsIntDefault(100);
            bool includeMeta = args["include_meta"].AsBoolDefault(false);

            // Validate search path
            if (!searchPath.StartsWith("Assets/") && searchPath != "Assets")
            {
                searchPath = "Assets/" + searchPath.TrimStart('/');
            }

            try
            {
                // UseJArrayFor serializing result，Ensure compatibilityJSONSerialize
                List<JsonClass> results = new List<JsonClass>();

                // Get all assetsGUID
                string[] guids = AssetDatabase.FindAssets(searchTerm, new[] { searchPath });

                foreach (string guid in guids)
                {
                    if (results.Count >= maxResults)
                        break;

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Check if path is in given scope
                    if (!IsInSearchPath(assetPath, searchPath, recursive))
                        continue;

                    // Check file extension
                    if (extensions != null && !extensions.Any(ext => assetPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Skip.metaFile（Unless explicitly required to include）
                    if (!includeMeta && assetPath.EndsWith(".meta"))
                        continue;

                    // Get asset object
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        var assetInfo = GetAssetInfo(asset, assetPath, guid);
                        // assetInfoItself isJObject，Join directlyJArray
                        results.Add(assetInfo);
                    }
                }

                string message = $"Found {results.Count} asset(s) matching '{searchTerm}' in '{searchPath}'";
                if (searchType != null)
                {
                    message += $" (type: {searchType})";
                }

                // UseJObjectWrap result，Ensure serialization-friendly
                var resultObj = new JsonClass
                {
                    ["query"] = searchTerm,
                    ["directory"] = searchPath,
                    ["search_target"] = searchType,
                    ["total_results"] = results.Count,
                    ["max_results"] = maxResults,
                    ["results"] = Json.FromObject(results),
                };
                return Response.Success(message, resultObj);
            }
            catch (Exception ex)
            {
                return Response.Error($"Search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if asset path is within search range
        /// </summary>
        private bool IsInSearchPath(string assetPath, string searchPath, bool recursive)
        {
            if (assetPath.StartsWith(searchPath))
            {
                if (recursive)
                    return true;

                // If not recursive，Check if in subdirectory
                string relativePath = assetPath.Substring(searchPath.Length).TrimStart('/');
                return !relativePath.Contains('/');
            }
            return false;
        }

        /// <summary>
        /// Get detailed info of asset
        /// </summary>
        private JsonClass GetAssetInfo(UnityEngine.Object asset, string assetPath, string guid)
        {
            var info = new JsonClass
            {
                ["name"] = asset.name,
                ["path"] = assetPath,
                ["guid"] = guid,
                ["type"] = asset.GetType().Name,
                ["instanceID"] = asset.GetInstanceID()
            };

            // Add specific info according to asset type
            if (asset is Texture2D texture)
            {
                info["width"] = texture.width;
                info["height"] = texture.height;
                info["format"] = texture.format.ToString();
            }
            else if (asset is Material material)
            {
                info["shader"] = material.shader?.name ?? "None";
            }
            else if (asset is GameObject prefab)
            {
                info["prefabType"] = PrefabUtility.GetPrefabAssetType(prefab).ToString();
            }
            else if (asset is SceneAsset scene)
            {
                info["sceneName"] = scene.name;
            }
            else if (asset is AudioClip audio)
            {
                info["length"] = audio.length;
                info["frequency"] = audio.frequency;
                info["channels"] = audio.channels;
            }
            else if (asset is Mesh mesh)
            {
                info["vertexCount"] = mesh.vertexCount;
                info["triangleCount"] = mesh.triangles.Length / 3;
            }
            else if (asset is ScriptableObject scriptableObject)
            {
                info["scriptableObjectType"] = scriptableObject.GetType().FullName;
            }

            // Add file info
            try
            {
                var fileInfo = new System.IO.FileInfo(assetPath);
                info["fileSize"] = fileInfo.Length;
                info["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                // Ignore file info fetching errors
            }

            return info;
        }

        /// <summary>
        /// Search specific asset types
        /// </summary>
        private object SearchByType<T>(string searchTerm, string searchPath, bool recursive, int maxResults) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name} {searchTerm}", new[] { searchPath });
            List<object> results = new List<object>();

            foreach (string guid in guids)
            {
                if (results.Count >= maxResults)
                    break;

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsInSearchPath(assetPath, searchPath, recursive))
                    continue;

                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    var assetInfo = GetAssetInfo(asset, assetPath, guid);
                    results.Add(assetInfo);
                }
            }

            return results;
        }

        /// <summary>
        /// Find all dependencies for specified resource
        /// </summary>
        private object HandleDependenciesSearch(JsonClass args)
        {
            string assetPath = args["query"]?.Value;
            bool recursive = args["recursive"].AsBoolDefault(true);
            int maxResults = args["max_results"].AsIntDefault(1000);

            if (string.IsNullOrEmpty(assetPath))
            {
                return Response.Error("'query' parameter is required and must be a valid asset path");
            }

            // Normalize asset path
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
            {
                assetPath = "Assets/" + assetPath.TrimStart('/');
            }

            try
            {
                // Check if asset exists
                if (!System.IO.File.Exists(assetPath) && !System.IO.Directory.Exists(assetPath))
                {
                    return Response.Error($"Asset not found: {assetPath}");
                }

                // Get dependencies
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive);

                List<JsonClass> results = new List<JsonClass>();

                foreach (string depPath in dependencies)
                {
                    if (results.Count >= maxResults)
                        break;

                    // Skip self
                    if (depPath == assetPath)
                        continue;

                    // Load asset
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(depPath);
                    if (asset != null)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(depPath);
                        var depInfo = GetAssetInfo(asset, depPath, guid);
                        results.Add(depInfo);
                    }
                }

                string message = recursive
                    ? $"Found {results.Count} dependencies (recursive) for '{assetPath}'"
                    : $"Found {results.Count} direct dependencies for '{assetPath}'";

                var resultObj = new JsonClass
                {
                    ["asset_path"] = assetPath,
                    ["recursive"] = recursive,
                    ["total_dependencies"] = results.Count,
                    ["dependencies"] = Json.FromObject(results)
                };

                return Response.Success(message, resultObj);
            }
            catch (Exception ex)
            {
                return Response.Error($"Failed to get dependencies: {ex.Message}");
            }
        }

        /// <summary>
        /// Find all resources referring to specified resource
        /// </summary>
        private object HandleReferencesSearch(JsonClass args)
        {
            string assetPath = args["query"]?.Value;
            string searchPath = args["directory"]?.Value;
            if (string.IsNullOrEmpty(searchPath)) searchPath = "Assets";
            int maxResults = args["max_results"].AsIntDefault(1000);

            if (string.IsNullOrEmpty(assetPath))
            {
                return Response.Error("'query' parameter is required and must be a valid asset path");
            }

            // Normalize asset path
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
            {
                assetPath = "Assets/" + assetPath.TrimStart('/');
            }

            try
            {
                // Check if asset exists
                if (!System.IO.File.Exists(assetPath) && !System.IO.Directory.Exists(assetPath))
                {
                    return Response.Error($"Asset not found: {assetPath}");
                }

                // Get target asset's GUID
                string targetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                List<JsonClass> results = new List<JsonClass>();

                // Search all assets
                string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { searchPath });

                foreach (string guid in allAssetGuids)
                {
                    if (results.Count >= maxResults)
                        break;

                    string checkedPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip self
                    if (checkedPath == assetPath)
                        continue;

                    // Get dependencies of this asset
                    string[] dependencies = AssetDatabase.GetDependencies(checkedPath, false);

                    // Check if depends on target asset
                    if (dependencies.Contains(assetPath))
                    {
                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(checkedPath);
                        if (asset != null)
                        {
                            var refInfo = GetAssetInfo(asset, checkedPath, guid);
                            results.Add(refInfo);
                        }
                    }
                }

                string message = $"Found {results.Count} assets referencing '{assetPath}' in '{searchPath}'";

                var resultObj = new JsonClass
                {
                    ["asset_path"] = assetPath,
                    ["search_directory"] = searchPath,
                    ["total_references"] = results.Count,
                    ["references"] = Json.FromObject(results)
                };

                return Response.Success(message, resultObj);
            }
            catch (Exception ex)
            {
                return Response.Error($"Failed to find references: {ex.Message}");
            }
        }


    }
}