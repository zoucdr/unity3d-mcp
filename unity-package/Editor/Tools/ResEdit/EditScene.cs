﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mcp.Models; // For Response class
using Unity.Mcp;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles Unity scene management operations.
    /// 对应方法名: manage_scene
    /// </summary>
    [ToolName("edit_scene", "资源管理")]
    public class EditScene : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "操作类型：load, save, create, get_hierarchy", false),
                new MethodKey("name", "场景名称", true),
                new MethodKey("path", "场景资产路径", true),
                new MethodKey("build_index", "构建索引", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create", HandleCreateAction)
                    .Branch("load")
                        .OptionalKey("buildIndex")
                            .DefaultLeaf(HandleLoadByBuildIndex)
                        .Up()
                        .DefaultLeaf(HandleLoadByPath)
                    .Up()
                    .Leaf("save", HandleSaveAction)
                    .Leaf("get_hierarchy", HandleGetHierarchyAction)
                    .Leaf("get_active", HandleGetActiveAction)
                    .Leaf("get_build_settings", HandleGetBuildSettingsAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理创建场景的操作
        /// </summary>
        private object HandleCreateAction(JsonClass args)
        {
            string name = args["name"]?.Value;
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("'name' parameter is required for 'create' action.");
            }

            // 准备路径参数
            var pathInfo = PrepareScenePaths(name, path, "create");
            if (pathInfo.error != null)
                return pathInfo.error;

            McpLogger.Log($"[ManageScene] Creating scene: '{name}' at path: '{pathInfo.relativePath}'");
            return CreateScene(pathInfo.fullPath, pathInfo.relativePath);
        }

        /// <summary>
        /// 处理通过路径加载场景的操作
        /// </summary>
        private object HandleLoadByPath(JsonClass args)
        {
            string name = args["name"]?.Value;
            string path = args["path"]?.Value;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(path))
            {
                return Response.Error("Either 'name' or 'path' parameter is required for 'load' action.");
            }

            // 准备路径参数
            var pathInfo = PrepareScenePaths(name, path, "load");
            if (pathInfo.error != null)
                return pathInfo.error;

            McpLogger.Log($"[ManageScene] Loading scene by path: '{pathInfo.relativePath}'");
            return LoadScene(pathInfo.relativePath);
        }

        /// <summary>
        /// 处理通过构建索引加载场景的操作
        /// </summary>
        private object HandleLoadByBuildIndex(JsonClass args)
        {
            int? buildIndex = args["buildIndex"] != null && !args["buildIndex"].IsNull()
                ? (int?)args["buildIndex"].AsInt
                : null;

            if (!buildIndex.HasValue)
            {
                return Response.Error("'buildIndex' parameter is required when loading by build index.");
            }

            McpLogger.Log($"[ManageScene] Loading scene by build index: {buildIndex.Value}");
            return LoadScene(buildIndex.Value);
        }

        /// <summary>
        /// 处理保存场景的操作
        /// </summary>
        private object HandleSaveAction(JsonClass args)
        {
            string name = args["name"]?.Value;
            string path = args["path"]?.Value;

            // 对于保存操作，路径是可选的
            var pathInfo = PrepareScenePaths(name, path, "save");
            if (pathInfo.error != null)
                return pathInfo.error;

            McpLogger.Log($"[ManageScene] Saving scene to path: '{pathInfo.relativePath}'");
            return SaveScene(pathInfo.fullPath, pathInfo.relativePath);
        }

        /// <summary>
        /// 处理获取场景层次结构的操作
        /// </summary>
        private object HandleGetHierarchyAction(JsonClass args)
        {
            McpLogger.Log("[ManageScene] Getting scene hierarchy");
            return GetSceneHierarchy();
        }

        /// <summary>
        /// 处理获取当前激活场景信息的操作
        /// </summary>
        private object HandleGetActiveAction(JsonClass args)
        {
            McpLogger.Log("[ManageScene] Getting active scene info");
            return GetActiveSceneInfo();
        }

        /// <summary>
        /// 处理获取构建设置中场景列表的操作
        /// </summary>
        private object HandleGetBuildSettingsAction(JsonClass args)
        {
            McpLogger.Log("[ManageScene] Getting build settings scenes");
            return GetBuildSettingsScenes();
        }

        /// <summary>
        /// 准备场景路径信息的辅助方法
        /// </summary>
        private (string fullPath, string relativePath, string fullPathDir, object error) PrepareScenePaths(string name, string path, string action)
        {
            // 检查path是否已经包含文件名（.unity扩展名）
            bool pathContainsFileName = !string.IsNullOrEmpty(path) &&
                                      (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains(".unity/") ||
                                       path.Contains(".unity\\"));

            string fileName = null;
            string relativeDir = path ?? string.Empty;

            // 如果path包含文件名，则需要分离目录和文件名
            if (pathContainsFileName)
            {
                // 提取文件名和目录
                fileName = Path.GetFileName(relativeDir);
                relativeDir = Path.GetDirectoryName(relativeDir) ?? string.Empty;

                // 如果提供了name参数，警告可能存在冲突
                if (!string.IsNullOrEmpty(name) && !fileName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    LogWarning($"[ManageScene] Both 'name' ({name}) and 'path' with filename ({fileName}) provided. Using filename from path.");
                }
            }

            // Ensure path is relative to Assets/, removing any leading "Assets/"
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }

            // Apply default *after* sanitizing, using the original path variable for the check
            if (string.IsNullOrEmpty(path) && action == "create")
            {
                relativeDir = "Scenes"; // Default relative directory
            }

            // 如果path中没有文件名，则使用name参数创建文件名
            string sceneFileName;
            if (pathContainsFileName)
            {
                sceneFileName = fileName; // 使用从path中提取的文件名
            }
            else
            {
                sceneFileName = string.IsNullOrEmpty(name) ? null : $"{name}.unity";
            }

            string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
            string fullPath = string.IsNullOrEmpty(sceneFileName)
                ? null
                : Path.Combine(fullPathDir, sceneFileName);
            string relativePath = string.IsNullOrEmpty(sceneFileName)
                ? null
                : Path.Combine("Assets", relativeDir, sceneFileName).Replace('\\', '/');

            // Ensure directory exists for 'create'
            if (action == "create" && !string.IsNullOrEmpty(fullPathDir))
            {
                try
                {
                    Directory.CreateDirectory(fullPathDir);
                }
                catch (Exception e)
                {
                    return (null, null, null, Response.Error($"Could not create directory '{fullPathDir}': {e.Message}"));
                }
            }

            return (fullPath, relativePath, fullPathDir, null);
        }

        private object CreateScene(string fullPath, string relativePath)
        {
            if (File.Exists(fullPath))
            {
                return Response.Error($"Scene already exists at '{relativePath}'.");
            }

            try
            {
                // Create a new empty scene
                Scene newScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single
                );
                // Save it to the specified path
                bool saved = EditorSceneManager.SaveScene(newScene, relativePath);

                if (saved)
                {
                    AssetDatabase.Refresh(); // Ensure Unity sees the new scene file
                    return Response.Success(
                        $"Scene '{Path.GetFileName(relativePath)}' created successfully at '{relativePath}'.",
                        new { path = relativePath }
                    );
                }
                else
                {
                    // If SaveScene fails, it might leave an untitled scene open.
                    // Optionally try to close it, but be cautious.
                    return Response.Error($"Failed to save new scene to '{relativePath}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error creating scene '{relativePath}': {e.Message}");
            }
        }

        private object LoadScene(string relativePath)
        {
            if (
                !File.Exists(
                    Path.Combine(
                        Application.dataPath.Substring(
                            0,
                            Application.dataPath.Length - "Assets".Length
                        ),
                        relativePath
                    )
                )
            )
            {
                return Response.Error($"Scene file not found at '{relativePath}'.");
            }

            // Check for unsaved changes in the current scene
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                // Optionally prompt the user or save automatically before loading
                return Response.Error(
                    "Current scene has unsaved changes. Please save or discard changes before loading a new scene."
                );
                // Example: bool saveOK = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                // if (!saveOK) return Response.Error("Load cancelled by user.");
            }

            try
            {
                EditorSceneManager.OpenScene(relativePath, OpenSceneMode.Single);
                return Response.Success(
                    $"Scene '{relativePath}' loaded successfully.",
                    new
                    {
                        path = relativePath,
                        name = Path.GetFileNameWithoutExtension(relativePath),
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error loading scene '{relativePath}': {e.Message}");
            }
        }

        private object LoadScene(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                return Response.Error(
                    $"Invalid build index: {buildIndex}. Must be between 0 and {SceneManager.sceneCountInBuildSettings - 1}."
                );
            }

            // Check for unsaved changes
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                return Response.Error(
                    "Current scene has unsaved changes. Please save or discard changes before loading a new scene."
                );
            }

            try
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                return Response.Success(
                    $"Scene at build index {buildIndex} ('{scenePath}') loaded successfully.",
                    new
                    {
                        path = scenePath,
                        name = Path.GetFileNameWithoutExtension(scenePath),
                        buildIndex = buildIndex,
                    }
                );
            }
            catch (Exception e)
            {
                return Response.Error(
                    $"Error loading scene with build index {buildIndex}: {e.Message}"
                );
            }
        }

        private object SaveScene(string fullPath, string relativePath)
        {
            try
            {
                Scene currentScene = EditorSceneManager.GetActiveScene();
                if (!currentScene.IsValid())
                {
                    return Response.Error("No valid scene is currently active to save.");
                }

                bool saved;
                string finalPath = currentScene.path; // Path where it was last saved or will be saved

                if (!string.IsNullOrEmpty(relativePath) && currentScene.path != relativePath)
                {
                    // Save As...
                    // Ensure directory exists
                    string dir = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    saved = EditorSceneManager.SaveScene(currentScene, relativePath);
                    finalPath = relativePath;
                }
                else
                {
                    // Save (overwrite existing or save untitled)
                    if (string.IsNullOrEmpty(currentScene.path))
                    {
                        // Scene is untitled, needs a path
                        return Response.Error(
                            "Cannot save an untitled scene without providing a 'name' and 'path'. Use Save As functionality."
                        );
                    }
                    saved = EditorSceneManager.SaveScene(currentScene);
                }

                if (saved)
                {
                    AssetDatabase.Refresh();
                    return Response.Success(
                        $"Scene '{currentScene.name}' saved successfully to '{finalPath}'.",
                        new { path = finalPath, name = currentScene.name }
                    );
                }
                else
                {
                    return Response.Error($"Failed to save scene '{currentScene.name}'.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error saving scene: {e.Message}");
            }
        }

        private object GetActiveSceneInfo()
        {
            try
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                if (!activeScene.IsValid())
                {
                    return Response.Error("No active scene found.");
                }

                var sceneInfo = new
                {
                    name = activeScene.name,
                    path = activeScene.path,
                    buildIndex = activeScene.buildIndex, // -1 if not in build settings
                    isDirty = activeScene.isDirty,
                    isLoaded = activeScene.isLoaded,
                    rootCount = activeScene.rootCount,
                };

                return Response.Success("Retrieved active scene information.", sceneInfo);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting active scene info: {e.Message}");
            }
        }

        private object GetBuildSettingsScenes()
        {
            try
            {
                var scenes = new List<object>();
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    var scene = EditorBuildSettings.scenes[i];
                    scenes.Add(
                        new
                        {
                            path = scene.path,
                            guid = scene.guid.ToString(),
                            enabled = scene.enabled,
                            buildIndex = i, // Actual build index considering only enabled scenes might differ
                        }
                    );
                }
                return Response.Success("Retrieved scenes from Build Settings.", scenes);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting scenes from Build Settings: {e.Message}");
            }
        }

        private object GetSceneHierarchy()
        {
            try
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                if (!activeScene.IsValid() || !activeScene.isLoaded)
                {
                    return Response.Error(
                        "No valid and loaded scene is active to get hierarchy from."
                    );
                }

                GameObject[] rootObjects = activeScene.GetRootGameObjects();

                // 创建简化的场景摘要，大幅减少token使用量
                var totalObjects = 0;
                var rootSummary = new List<string>();

                foreach (var rootObj in rootObjects)
                {
                    var childCount = CountAllChildren(rootObj.transform);
                    totalObjects += childCount + 1; // +1 for the root object itself
                    rootSummary.Add($"{rootObj.name}({childCount})");
                }

                var sceneYaml = $@"scene: {activeScene.name}
path: {activeScene.path}
rootObjects: {rootObjects.Length}
totalObjects: {totalObjects}
isDirty: {activeScene.isDirty.ToString().ToLower()}
isLoaded: {activeScene.isLoaded.ToString().ToLower()}
hierarchy: |
{string.Join("\n", rootSummary.Select(s => $"  - {s}"))}";

                return Response.Success(
                    $"Retrieved hierarchy for scene '{activeScene.name}'.",
                    new { yaml = sceneYaml }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting scene hierarchy: {e.Message}");
            }
        }

        /// <summary>
        /// Recursively builds a data representation of a GameObject and its children.
        /// </summary>
        /// <summary>
        /// 获取GameObject的简化层级数据 - 使用YAML格式，避免深度递归
        /// </summary>
        private object GetGameObjectDataRecursive(GameObject go)
        {
            if (go == null)
                return null;

            // 使用YAML格式减少token使用量
            var yamlData = GameObjectUtils.GetGameObjectDataYaml(go);

            // 对于深层级结构，只返回直接子对象的名称，不递归获取完整数据
            var childrenNames = new List<string>();
            foreach (Transform child in go.transform)
            {
                childrenNames.Add($"{child.gameObject.name}({child.childCount})");
            }

            return new
            {
                yaml = yamlData,
                childrenSummary = childrenNames.Count > 0 ? string.Join(", ", childrenNames) : null
            };
        }

        /// <summary>
        /// 递归计算所有子对象的数量
        /// </summary>
        private int CountAllChildren(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                count += 1 + CountAllChildren(child);
            }
            return count;
        }


    }
}

