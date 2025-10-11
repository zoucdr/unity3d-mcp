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
    /// 对应方法名: project_search
    /// </summary>
    [ToolName("project_search", "项目管理")]
    public class ProjectSearch : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("search_target", "搜索类型：asset, folder, script, texture, material, prefab, scene等", false),
                new MethodKey("query", "搜索关键词", false),
                new MethodKey("directory", "搜索路径（相对于Assets）", true),
                new MethodKey("file_extension", "文件扩展名过滤", true),
                new MethodKey("recursive", "是否递归搜索子文件夹", true),
                new MethodKey("case_sensitive", "是否区分大小写", true),
                new MethodKey("max_results", "最大返回结果数", true),
                new MethodKey("include_meta", "是否包含.meta文件", true)
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
        /// 搜索所有类型的资产
        /// </summary>
        private object HandleAssetSearch(JsonClass args)
        {
            return PerformSearch(args, null);
        }

        /// <summary>
        /// 搜索文件夹
        /// </summary>
        private object HandleFolderSearch(JsonClass args)
        {
            return PerformSearch(args, "folder");
        }

        /// <summary>
        /// 搜索脚本文件
        /// </summary>
        private object HandleScriptSearch(JsonClass args)
        {
            return PerformSearch(args, "script", new[] { ".cs", ".js", ".boo" });
        }

        /// <summary>
        /// 搜索纹理文件
        /// </summary>
        private object HandleTextureSearch(JsonClass args)
        {
            return PerformSearch(args, "texture", new[] { ".png", ".jpg", ".jpeg", ".tga", ".tiff", ".bmp", ".psd", ".exr" });
        }

        /// <summary>
        /// 搜索材质文件
        /// </summary>
        private object HandleMaterialSearch(JsonClass args)
        {
            return PerformSearch(args, "material", new[] { ".mat" });
        }

        /// <summary>
        /// 搜索预制体文件
        /// </summary>
        private object HandlePrefabSearch(JsonClass args)
        {
            return PerformSearch(args, "prefab", new[] { ".prefab" });
        }

        /// <summary>
        /// 搜索场景文件
        /// </summary>
        private object HandleSceneSearch(JsonClass args)
        {
            return PerformSearch(args, "scene", new[] { ".unity" });
        }

        /// <summary>
        /// 搜索音频文件
        /// </summary>
        private object HandleAudioSearch(JsonClass args)
        {
            return PerformSearch(args, "audio", new[] { ".mp3", ".wav", ".ogg", ".aiff", ".aif" });
        }

        /// <summary>
        /// 搜索3D模型文件
        /// </summary>
        private object HandleModelSearch(JsonClass args)
        {
            return PerformSearch(args, "model", new[] { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".skp", ".blend", ".max", ".c4d", ".ma", ".mb" });
        }

        /// <summary>
        /// 搜索Shader文件
        /// </summary>
        private object HandleShaderSearch(JsonClass args)
        {
            return PerformSearch(args, "shader", new[] { ".shader", ".cginc", ".hlsl" });
        }

        /// <summary>
        /// 搜索动画文件
        /// </summary>
        private object HandleAnimationSearch(JsonClass args)
        {
            return PerformSearch(args, "animation", new[] { ".anim", ".controller", ".playable" });
        }

        /// <summary>
        /// 通用搜索
        /// </summary>
        private object HandleGeneralSearch(JsonClass args)
        {
            return PerformSearch(args, null);
        }

        /// <summary>
        /// 执行搜索的主要实现
        /// </summary>
        private object PerformSearch(JsonClass args, string searchType, string[] extensions = null)
        {
            string searchTerm = args["query"]?.Value;
            string searchPath = args["directory"]?.Value ?? "Assets";
            bool recursive = args["recursive"].AsBoolDefault(true);
            bool caseSensitive = args["case_sensitive"].AsBoolDefault(false);
            int maxResults = args["max_results"].AsIntDefault(100);
            bool includeMeta = args["include_meta"].AsBoolDefault(false);

            // 验证搜索路径
            if (!searchPath.StartsWith("Assets/") && searchPath != "Assets")
            {
                searchPath = "Assets/" + searchPath.TrimStart('/');
            }

            try
            {
                // 用JArray来序列化结果，确保兼容JSON序列化
                List<JsonClass> results = new List<JsonClass>();

                // 获取所有资产GUID
                string[] guids = AssetDatabase.FindAssets(searchTerm, new[] { searchPath });

                foreach (string guid in guids)
                {
                    if (results.Count >= maxResults)
                        break;

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // 检查是否在指定路径范围内
                    if (!IsInSearchPath(assetPath, searchPath, recursive))
                        continue;

                    // 检查文件扩展名
                    if (extensions != null && !extensions.Any(ext => assetPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // 跳过.meta文件（除非明确要求包含）
                    if (!includeMeta && assetPath.EndsWith(".meta"))
                        continue;

                    // 获取资产对象
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        var assetInfo = GetAssetInfo(asset, assetPath, guid);
                        // assetInfo本身是JObject，直接加入JArray
                        results.Add(assetInfo);
                    }
                }

                string message = $"Found {results.Count} asset(s) matching '{searchTerm}' in '{searchPath}'";
                if (searchType != null)
                {
                    message += $" (type: {searchType})";
                }

                // 用JObject包装返回，保证序列化友好
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
        /// 检查资产路径是否在搜索路径范围内
        /// </summary>
        private bool IsInSearchPath(string assetPath, string searchPath, bool recursive)
        {
            if (assetPath.StartsWith(searchPath))
            {
                if (recursive)
                    return true;

                // 如果不递归，检查是否在直接子目录中
                string relativePath = assetPath.Substring(searchPath.Length).TrimStart('/');
                return !relativePath.Contains('/');
            }
            return false;
        }

        /// <summary>
        /// 获取资产的详细信息
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

            // 根据资产类型添加特定信息
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

            // 添加文件信息
            try
            {
                var fileInfo = new System.IO.FileInfo(assetPath);
                info["fileSize"] = fileInfo.Length;
                info["lastModified"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                // 忽略文件信息获取错误
            }

            return info;
        }

        /// <summary>
        /// 搜索特定类型的资产
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
        /// 查找指定资源的所有依赖项
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

            // 规范化资产路径
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
            {
                assetPath = "Assets/" + assetPath.TrimStart('/');
            }

            try
            {
                // 检查资产是否存在
                if (!System.IO.File.Exists(assetPath) && !System.IO.Directory.Exists(assetPath))
                {
                    return Response.Error($"Asset not found: {assetPath}");
                }

                // 获取依赖项
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive);

                List<JsonClass> results = new List<JsonClass>();

                foreach (string depPath in dependencies)
                {
                    if (results.Count >= maxResults)
                        break;

                    // 跳过自身
                    if (depPath == assetPath)
                        continue;

                    // 加载资产
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
        /// 查找引用指定资源的所有资源
        /// </summary>
        private object HandleReferencesSearch(JsonClass args)
        {
            string assetPath = args["query"]?.Value;
            string searchPath = args["directory"]?.Value ?? "Assets";
            int maxResults = args["max_results"].AsIntDefault(1000);

            if (string.IsNullOrEmpty(assetPath))
            {
                return Response.Error("'query' parameter is required and must be a valid asset path");
            }

            // 规范化资产路径
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
            {
                assetPath = "Assets/" + assetPath.TrimStart('/');
            }

            try
            {
                // 检查资产是否存在
                if (!System.IO.File.Exists(assetPath) && !System.IO.Directory.Exists(assetPath))
                {
                    return Response.Error($"Asset not found: {assetPath}");
                }

                // 获取目标资产的 GUID
                string targetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                List<JsonClass> results = new List<JsonClass>();

                // 搜索所有资产
                string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { searchPath });

                foreach (string guid in allAssetGuids)
                {
                    if (results.Count >= maxResults)
                        break;

                    string checkedPath = AssetDatabase.GUIDToAssetPath(guid);

                    // 跳过自身
                    if (checkedPath == assetPath)
                        continue;

                    // 获取该资产的依赖项
                    string[] dependencies = AssetDatabase.GetDependencies(checkedPath, false);

                    // 检查是否依赖目标资产
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