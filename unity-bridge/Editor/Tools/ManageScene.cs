using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMcpBridge.Editor.Helpers; // For Response class

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles scene management operations like loading, saving, creating, and querying hierarchy.
    /// </summary>
    public static class ManageScene
    {
        /// <summary>
        /// Main handler for scene management actions.
        /// </summary>
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString().ToLower();
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString(); // Relative to Assets/
            int? buildIndex = @params["buildIndex"]?.ToObject<int?>();
            // bool loadAdditive = @params["loadAdditive"]?.ToObject<bool>() ?? false; // Example for future extension

            // Ensure path is relative to Assets/, removing any leading "Assets/"
            string relativeDir = path ?? string.Empty;
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }

            // Apply default *after* sanitizing, using the original path variable for the check
            if (string.IsNullOrEmpty(path) && action == "create") // Check original path for emptiness
            {
                relativeDir = "Scenes"; // Default relative directory
            }

            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            string sceneFileName = string.IsNullOrEmpty(name) ? null : $"{name}.unity";
            // Construct full system path correctly: ProjectRoot/Assets/relativeDir/sceneFileName
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir); // Combine with Assets path (Application.dataPath ends in Assets)
            string fullPath = string.IsNullOrEmpty(sceneFileName)
                ? null
                : Path.Combine(fullPathDir, sceneFileName);
            // Ensure relativePath always starts with "Assets/" and uses forward slashes
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
                    return Response.Error(
                        $"Could not create directory '{fullPathDir}': {e.Message}"
                    );
                }
            }

            // Route action
            switch (action)
            {
                case "create":
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relativePath))
                        return Response.Error(
                            "'name' and 'path' parameters are required for 'create' action."
                        );
                    return CreateScene(fullPath, relativePath);
                case "load":
                    // Loading can be done by path/name or build index
                    if (!string.IsNullOrEmpty(relativePath))
                        return LoadScene(relativePath);
                    else if (buildIndex.HasValue)
                        return LoadScene(buildIndex.Value);
                    else
                        return Response.Error(
                            "Either 'name'/'path' or 'buildIndex' must be provided for 'load' action."
                        );
                case "save":
                    // Save current scene, optionally to a new path
                    return SaveScene(fullPath, relativePath);
                case "get_hierarchy":
                    return GetSceneHierarchy();
                case "get_active":
                    return GetActiveSceneInfo();
                case "get_build_settings":
                    return GetBuildSettingsScenes();
                // Add cases for modifying build settings, additive loading, unloading etc.
                default:
                    return Response.Error(
                        $"Unknown action: '{action}'. Valid actions: create, load, save, get_hierarchy, get_active, get_build_settings."
                    );
            }
        }

        private static object CreateScene(string fullPath, string relativePath)
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

        private static object LoadScene(string relativePath)
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

        private static object LoadScene(int buildIndex)
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

        private static object SaveScene(string fullPath, string relativePath)
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

        private static object GetActiveSceneInfo()
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

        private static object GetBuildSettingsScenes()
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

        private static object GetSceneHierarchy()
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
                var hierarchy = rootObjects.Select(go => GetGameObjectDataRecursive(go)).ToList();

                return Response.Success(
                    $"Retrieved hierarchy for scene '{activeScene.name}'.",
                    hierarchy
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
        private static object GetGameObjectDataRecursive(GameObject go)
        {
            if (go == null)
                return null;

            var childrenData = new List<object>();
            foreach (Transform child in go.transform)
            {
                childrenData.Add(GetGameObjectDataRecursive(child.gameObject));
            }

            var gameObjectData = new Dictionary<string, object>
            {
                { "name", go.name },
                { "activeSelf", go.activeSelf },
                { "activeInHierarchy", go.activeInHierarchy },
                { "tag", go.tag },
                { "layer", go.layer },
                { "isStatic", go.isStatic },
                { "instanceID", go.GetInstanceID() }, // Useful unique identifier
                {
                    "transform",
                    new
                    {
                        position = new
                        {
                            x = go.transform.localPosition.x,
                            y = go.transform.localPosition.y,
                            z = go.transform.localPosition.z,
                        },
                        rotation = new
                        {
                            x = go.transform.localRotation.eulerAngles.x,
                            y = go.transform.localRotation.eulerAngles.y,
                            z = go.transform.localRotation.eulerAngles.z,
                        }, // Euler for simplicity
                        scale = new
                        {
                            x = go.transform.localScale.x,
                            y = go.transform.localScale.y,
                            z = go.transform.localScale.z,
                        },
                    }
                },
                { "children", childrenData },
            };

            return gameObjectData;
        }
    }
}

