using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp;

namespace Unity.Mcp.Tools.Storage
{
    /// <summary>
    /// Handles asset and folder location operations, including opening folders and revealing files.
    /// 对应方法名: source_location
    /// </summary>
    [ToolName("source_location", "资源定位")]
    public class SourceLocation : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型
                new MethodStr("action", "操作类型")
                    .SetEnumValues("reveal_in_finder", "open_folder", "ping_asset", "select_asset", "get_asset_path")
                    .AddExamples("reveal_in_finder", "ping_asset"),
                
                // 资源路径
                new MethodStr("asset_path", "资源路径", true)
                    .AddExamples("Assets/Scripts/Player.cs", "Assets/Textures/icon.png"),
                
                // 文件夹路径
                new MethodStr("folder_path", "文件夹路径", true)
                    .AddExamples("Assets/Scripts/", "D:/Projects/MyGame/"),
                
                // 实例ID
                new MethodInt("instance_id", "实例ID", true)
                    .AddExample("12345"),
                
                // 资源GUID
                new MethodStr("guid", "资源GUID", true)
                    .AddExample("abc123def456ghi789"),
                
                // 对象名称
                new MethodStr("object_name", "对象名称", true)
                    .AddExamples("Player", "MainCamera")
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("reveal_in_finder", HandleRevealInFinderAction)
                    .Leaf("open_folder", HandleOpenFolderAction)
                    .Leaf("ping_asset", HandlePingAssetAction)
                    .Leaf("select_asset", HandleSelectAssetAction)
                    .Leaf("get_asset_path", HandleGetAssetPathAction)
                .Build();
        }

        // --- Action Handlers ---

        /// <summary>
        /// 在文件浏览器中显示文件（Windows资源管理器/Mac Finder）
        /// </summary>
        private object HandleRevealInFinderAction(JsonClass args)
        {
            string assetPath = args["assetPath"]?.Value;
            string guid = args["guid"]?.Value;
            string instanceIdStr = args["instanceId"]?.Value;

            try
            {
                // 尝试从不同参数获取资源路径
                if (string.IsNullOrEmpty(assetPath))
                {
                    if (!string.IsNullOrEmpty(guid))
                    {
                        assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    }
                    else if (!string.IsNullOrEmpty(instanceIdStr) && int.TryParse(instanceIdStr, out int instanceId))
                    {
                        var obj = EditorUtility.InstanceIDToObject(instanceId);
                        if (obj != null)
                        {
                            assetPath = AssetDatabase.GetAssetPath(obj);
                        }
                    }
                }

                if (string.IsNullOrEmpty(assetPath))
                {
                    return Response.Error("Asset path, GUID, or instance ID is required.");
                }

                // 转换为绝对路径
                string fullPath = Path.GetFullPath(assetPath);

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return Response.Error($"Path does not exist: {fullPath}");
                }

                // 使用Unity内置方法在系统文件浏览器中显示
                EditorUtility.RevealInFinder(fullPath);

                McpLogger.Log($"[SourceLocation] Revealed in finder: {fullPath}");
                return Response.Success($"Revealed in finder: {assetPath}", new { assetPath, fullPath });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[SourceLocation] Error revealing in finder: {e.Message}");
                return Response.Error($"Error revealing in finder: {e.Message}");
            }
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        private object HandleOpenFolderAction(JsonClass args)
        {
            string folderPath = args["folderPath"]?.Value;
            string assetPath = args["assetPath"]?.Value;

            try
            {
                // 如果提供了资源路径，获取其所在文件夹
                if (string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(assetPath))
                {
                    string fullAssetPath = Path.GetFullPath(assetPath);
                    if (File.Exists(fullAssetPath))
                    {
                        folderPath = Path.GetDirectoryName(fullAssetPath);
                    }
                    else if (Directory.Exists(fullAssetPath))
                    {
                        folderPath = fullAssetPath;
                    }
                }

                if (string.IsNullOrEmpty(folderPath))
                {
                    return Response.Error("Folder path or asset path is required.");
                }

                // 转换为绝对路径
                string fullPath = Path.GetFullPath(folderPath);

                if (!Directory.Exists(fullPath))
                {
                    return Response.Error($"Folder does not exist: {fullPath}");
                }

                // 打开文件夹
                Process.Start(new ProcessStartInfo()
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                McpLogger.Log($"[SourceLocation] Opened folder: {fullPath}");
                return Response.Success($"Opened folder: {folderPath}", new { folderPath, fullPath });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[SourceLocation] Error opening folder: {e.Message}");
                return Response.Error($"Error opening folder: {e.Message}");
            }
        }

        /// <summary>
        /// 在Project窗口中高亮显示资源（Ping效果）
        /// </summary>
        private object HandlePingAssetAction(JsonClass args)
        {
            string assetPath = args["assetPath"]?.Value;
            string guid = args["guid"]?.Value;
            string instanceIdStr = args["instanceId"]?.Value;
            string objectName = args["objectName"]?.Value;

            try
            {
                UnityEngine.Object targetObject = null;

                // 尝试从不同参数获取对象
                if (!string.IsNullOrEmpty(assetPath))
                {
                    targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
                else if (!string.IsNullOrEmpty(guid))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
                else if (!string.IsNullOrEmpty(instanceIdStr) && int.TryParse(instanceIdStr, out int instanceId))
                {
                    targetObject = EditorUtility.InstanceIDToObject(instanceId);
                }
                else if (!string.IsNullOrEmpty(objectName))
                {
                    // 搜索场景中的GameObject
                    GameObject go = GameObject.Find(objectName);
                    if (go != null)
                    {
                        targetObject = go;
                    }
                }

                if (targetObject == null)
                {
                    return Response.Error("Could not find asset or object to ping.");
                }

                // 在Project或Hierarchy窗口中高亮显示
                EditorGUIUtility.PingObject(targetObject);

                McpLogger.Log($"[SourceLocation] Pinged object: {targetObject.name}");
                return Response.Success($"Pinged object: {targetObject.name}", new
                {
                    name = targetObject.name,
                    type = targetObject.GetType().Name,
                    instanceID = targetObject.GetInstanceID()
                });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[SourceLocation] Error ping asset: {e.Message}");
                return Response.Error($"Error ping asset: {e.Message}");
            }
        }

        /// <summary>
        /// 选择资源或GameObject
        /// </summary>
        private object HandleSelectAssetAction(JsonClass args)
        {
            string assetPath = args["assetPath"]?.Value;
            string guid = args["guid"]?.Value;
            string instanceIdStr = args["instanceId"]?.Value;
            string objectName = args["objectName"]?.Value;

            try
            {
                UnityEngine.Object targetObject = null;

                // 尝试从不同参数获取对象
                if (!string.IsNullOrEmpty(assetPath))
                {
                    targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
                else if (!string.IsNullOrEmpty(guid))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
                else if (!string.IsNullOrEmpty(instanceIdStr) && int.TryParse(instanceIdStr, out int instanceId))
                {
                    targetObject = EditorUtility.InstanceIDToObject(instanceId);
                }
                else if (!string.IsNullOrEmpty(objectName))
                {
                    // 搜索场景中的GameObject
                    GameObject go = GameObject.Find(objectName);
                    if (go != null)
                    {
                        targetObject = go;
                    }
                }

                if (targetObject == null)
                {
                    return Response.Error("Could not find asset or object to select.");
                }

                // 选择对象
                Selection.activeObject = targetObject;
                EditorGUIUtility.PingObject(targetObject);

                McpLogger.Log($"[SourceLocation] Selected object: {targetObject.name}");
                return Response.Success($"Selected object: {targetObject.name}", new
                {
                    name = targetObject.name,
                    type = targetObject.GetType().Name,
                    instanceID = targetObject.GetInstanceID(),
                    assetPath = AssetDatabase.GetAssetPath(targetObject)
                });
            }
            catch (Exception e)
            {
                McpLogger.Log($"[SourceLocation] Error selecting asset: {e.Message}");
                return Response.Error($"Error selecting asset: {e.Message}");
            }
        }

        /// <summary>
        /// 获取资源路径信息
        /// </summary>
        private object HandleGetAssetPathAction(JsonClass args)
        {
            string instanceIdStr = args["instanceId"]?.Value;
            string objectName = args["objectName"]?.Value;
            string guid = args["guid"]?.Value;

            try
            {
                UnityEngine.Object targetObject = null;
                string assetPath = null;

                // 尝试从不同参数获取对象
                if (!string.IsNullOrEmpty(guid))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }
                else if (!string.IsNullOrEmpty(instanceIdStr) && int.TryParse(instanceIdStr, out int instanceId))
                {
                    targetObject = EditorUtility.InstanceIDToObject(instanceId);
                    if (targetObject != null)
                    {
                        assetPath = AssetDatabase.GetAssetPath(targetObject);
                    }
                }
                else if (!string.IsNullOrEmpty(objectName))
                {
                    // 搜索场景中的GameObject
                    GameObject go = GameObject.Find(objectName);
                    if (go != null)
                    {
                        targetObject = go;
                        assetPath = AssetDatabase.GetAssetPath(go);
                    }
                }

                if (targetObject == null)
                {
                    return Response.Error("Could not find asset or object.");
                }

                string fullPath = string.IsNullOrEmpty(assetPath) ? null : Path.GetFullPath(assetPath);
                string assetGuid = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath);

                var pathInfo = new
                {
                    name = targetObject.name,
                    type = targetObject.GetType().Name,
                    instanceID = targetObject.GetInstanceID(),
                    assetPath = assetPath,
                    fullPath = fullPath,
                    guid = assetGuid,
                    isSceneObject = string.IsNullOrEmpty(assetPath)
                };

                McpLogger.Log($"[SourceLocation] Got asset path: {targetObject.name}");
                return Response.Success($"Retrieved path for: {targetObject.name}", pathInfo);
            }
            catch (Exception e)
            {
                McpLogger.Log($"[SourceLocation] Error getting asset path: {e.Message}");
                return Response.Error($"Error getting asset path: {e.Message}");
            }
        }
    }
}

