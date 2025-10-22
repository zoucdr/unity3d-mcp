using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models; // For Response class

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles asset creation operations in the Project window.
    /// 对应方法名: project_create
    /// 支持: menu, empty, template, copy
    /// </summary>
    [ToolName("project_create", "项目管理")]
    public class ProjectCreate : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("name", "资源文件名称", false),
                new MethodKey("source", "操作类型：menu, empty, template, copy", false),
                new MethodKey("folder_path", "目标文件夹路径（相对于Assets）", true),
                new MethodKey("menu_path", "菜单路径", true),
                new MethodKey("template_path", "模板文件路径", true),
                new MethodKey("copy_source", "要复制的资源路径", true),
                new MethodKey("extension", "文件扩展名（不含.）", true),
                new MethodKey("content", "文件内容（用于empty类型）", true),
                new MethodKey("force", "是否强制覆盖已存在的文件", true),
                new MethodKey("open_after_create", "创建后是否打开文件", true),
                new MethodKey("select_after_create", "创建后是否选中文件", true),
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("source")
                    .Leaf("menu", HandleCreateFromMenu)
                    .Leaf("empty", HandleCreateEmpty)
                    .Leaf("template", HandleCreateFromTemplate)
                    .Leaf("copy", HandleCreateFromCopy)
                .Build();
        }

        /// <summary>
        /// 异步执行从菜单创建资源
        /// </summary>
        private IEnumerator HandleCreateFromMenuAsync(StateTreeContext ctx)
        {
            string menuPath = ctx["menu_path"]?.ToString();
            if (string.IsNullOrEmpty(menuPath))
            {
                yield return Response.Error("'menu_path' parameter is required for menu creation.");
                yield break;
            }

            LogInfo($"[ProjectCreate] Creating asset from menu: '{menuPath}'");

            if (!menuPath.StartsWith("Assets"))
            {
                yield return Response.Error("'menu_path' parameter must start with 'Assets'");
                yield break;
            }

            // 获取目标文件夹路径
            string folderPath = ctx["folder_path"]?.ToString() ?? "Assets";
            if (!folderPath.StartsWith("Assets"))
            {
                folderPath = "Assets/" + folderPath;
            }

            // 确保目标文件夹存在
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                LogInfo($"[ProjectCreate] Creating folder path: {folderPath}");
                CreateFolderRecursive(folderPath);
            }

            // 记录创建前的选中对象
            UnityEngine.Object[] previousSelection = Selection.objects;

            // 选中目标文件夹
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            if (folder != null)
            {
                Selection.activeObject = folder;
                LogInfo($"[ProjectCreate] Selected folder: {folderPath}");
            }
            else
            {
                LogInfo($"[ProjectCreate] Failed to select folder: {folderPath}");
            }

            // 执行菜单项
            JsonClass menuResult = MenuUtils.TryExecuteMenuItem(menuPath);

            // 检查菜单执行结果
            if (!menuResult["success"].AsBoolDefault(false))
            {
                LogInfo($"[ProjectCreate] Menu execution failed: {menuResult}");
                yield return menuResult;
                yield break;
            }

            // 多次尝试检测新创建的对象，因为菜单创建可能需要时间
            UnityEngine.Object newAsset = null;
            int maxRetries = 10;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                // 获取当前选中的对象
                UnityEngine.Object[] currentSelection = Selection.objects;

                // 查找新创建的对象（与之前选中的对象不同）
                foreach (var obj in currentSelection)
                {
                    if (obj != null && !previousSelection.Contains(obj))
                    {
                        newAsset = obj;
                        LogInfo($"[ProjectCreate] Found newly created asset: '{AssetDatabase.GetAssetPath(newAsset)}'");
                        break;
                    }
                }

                // 检查是否找到了新对象
                if (newAsset != null)
                {
                    break;
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    // 优化携程调用：每次重试之间yield return null，等待一帧
                    yield return null;
                }
            }

            // 如果找到了新对象，进行设置
            if (newAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(newAsset);
                string fileName = ctx["name"]?.ToString();

                // 如果指定了新文件名，则重命名资源
                if (!string.IsNullOrEmpty(fileName))
                {
                    string extension = Path.GetExtension(assetPath);
                    string newPath = Path.Combine(Path.GetDirectoryName(assetPath), fileName + extension);
                    newPath = newPath.Replace('\\', '/');

                    LogInfo($"[ProjectCreate] Renaming asset from '{assetPath}' to '{newPath}'");
                    AssetDatabase.MoveAsset(assetPath, newPath);
                    assetPath = newPath;
                    newAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                }

                bool selectAfterCreate = ctx.JsonData["select_after_create"].AsBoolDefault(true);
                if (selectAfterCreate)
                {
                    Selection.activeObject = newAsset;
                }

                bool openAfterCreate = ctx.JsonData["open_after_create"].AsBoolDefault(false);
                if (openAfterCreate)
                {
                    AssetDatabase.OpenAsset(newAsset);
                }

                yield return Response.Success($"Asset created successfully at '{assetPath}'", new JsonClass
                {
                    ["path"] = assetPath,
                    ["type"] = newAsset.GetType().Name,
                    ["name"] = Path.GetFileName(assetPath)
                });
            }
            else
            {
                yield return Response.Error($"Menu item '{menuPath}' executed, but no new asset was detected after {maxRetries} retries.");
            }
        }

        /// <summary>
        /// 处理从菜单创建资源的操作
        /// </summary>
        private object HandleCreateFromMenu(StateTreeContext ctx)
        {
            return ctx.AsyncReturn(HandleCreateFromMenuAsync(ctx));
        }

        /// <summary>
        /// 处理创建空文件的操作
        /// </summary>
        private object HandleCreateEmpty(JsonClass args)
        {
            string fileName = args["name"]?.Value;
            if (string.IsNullOrEmpty(fileName))
            {
                return Response.Error("'name' parameter is required for empty file creation.");
            }

            string folderPath = args["folder_path"]?.Value ?? "Assets";
            if (!folderPath.StartsWith("Assets"))
            {
                folderPath = "Assets/" + folderPath;
            }

            string extension = args["extension"]?.Value;
            if (string.IsNullOrEmpty(extension))
            {
                extension = "txt";
            }
            else if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }

            string content = args["content"]?.Value ?? string.Empty;
            bool force = args["force"].AsBoolDefault(false);

            // 确保目标文件夹存在
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                LogInfo($"[ProjectCreate] Creating folder path: {folderPath}");
                CreateFolderRecursive(folderPath);
            }

            // 构建完整文件路径
            string filePath = $"{folderPath}/{fileName}.{extension}";

            // 检查文件是否已存在
            if (File.Exists(filePath) && !force)
            {
                return Response.Error($"File '{filePath}' already exists. Use 'force' parameter to overwrite.");
            }

            try
            {
                // 创建文件
                File.WriteAllText(filePath, content);
                AssetDatabase.Refresh();

                LogInfo($"[ProjectCreate] Created empty file: '{filePath}'");

                // 获取创建的资源
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);

                bool selectAfterCreate = args["select_after_create"].AsBoolDefault(true);
                if (selectAfterCreate && asset != null)
                {
                    Selection.activeObject = asset;
                }

                bool openAfterCreate = args["open_after_create"].AsBoolDefault(false);
                if (openAfterCreate && asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }

                return Response.Success($"Empty file created successfully at '{filePath}'", new JsonClass
                {
                    ["path"] = filePath,
                    ["type"] = asset?.GetType().Name ?? "Unknown",
                    ["name"] = Path.GetFileName(filePath)
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create empty file '{filePath}': {e.Message}");
            }
        }

        /// <summary>
        /// 处理从模板创建资源的操作
        /// </summary>
        private object HandleCreateFromTemplate(JsonClass args)
        {
            string fileName = args["name"]?.Value;
            if (string.IsNullOrEmpty(fileName))
            {
                return Response.Error("'name' parameter is required for template creation.");
            }

            string templatePath = args["template_path"]?.Value;
            if (string.IsNullOrEmpty(templatePath))
            {
                return Response.Error("'template_path' parameter is required for template creation.");
            }

            string folderPath = args["folder_path"]?.Value ?? "Assets";
            if (!folderPath.StartsWith("Assets"))
            {
                folderPath = "Assets/" + folderPath;
            }

            bool force = args["force"].AsBoolDefault(false);

            // 确保模板文件存在
            if (!File.Exists(templatePath) && !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(templatePath))
            {
                return Response.Error($"Template file '{templatePath}' does not exist.");
            }

            // 确保目标文件夹存在
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                LogInfo($"[ProjectCreate] Creating folder path: {folderPath}");
                CreateFolderRecursive(folderPath);
            }

            // 获取模板文件的扩展名
            string extension = Path.GetExtension(templatePath);

            // 构建完整文件路径
            string filePath = $"{folderPath}/{fileName}{extension}";

            // 检查文件是否已存在
            if (File.Exists(filePath) && !force)
            {
                return Response.Error($"File '{filePath}' already exists. Use 'force' parameter to overwrite.");
            }

            try
            {
                // 复制模板文件
                if (File.Exists(templatePath))
                {
                    // 从文件系统复制
                    File.Copy(templatePath, filePath, force);
                }
                else
                {
                    // 从Asset数据库复制
                    string result = AssetDatabase.CopyAsset(templatePath, filePath) ? "success" : "failed";
                    if (result != "success")
                    {
                        return Response.Error($"Failed to copy asset from '{templatePath}' to '{filePath}'");
                    }
                }

                AssetDatabase.Refresh();

                LogInfo($"[ProjectCreate] Created file from template: '{filePath}'");

                // 获取创建的资源
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);

                bool selectAfterCreate = args["select_after_create"].AsBoolDefault(true);
                if (selectAfterCreate && asset != null)
                {
                    Selection.activeObject = asset;
                }

                bool openAfterCreate = args["open_after_create"].AsBoolDefault(false);
                if (openAfterCreate && asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }

                return Response.Success($"File created from template successfully at '{filePath}'", new JsonClass
                {
                    ["path"] = filePath,
                    ["type"] = asset?.GetType().Name ?? "Unknown",
                    ["name"] = Path.GetFileName(filePath)
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create file from template '{templatePath}': {e.Message}");
            }
        }

        /// <summary>
        /// 处理从现有资源复制的操作
        /// </summary>
        private object HandleCreateFromCopy(JsonClass args)
        {
            string fileName = args["name"]?.Value;

            string copySource = args["copy_source"]?.Value;
            if (string.IsNullOrEmpty(copySource))
            {
                return Response.Error("'copy_source' parameter is required for copy creation.");
            }

            string folderPath = args["folder_path"]?.Value ?? "Assets";
            if (!folderPath.StartsWith("Assets"))
            {
                folderPath = "Assets/" + folderPath;
            }

            bool force = args["force"].AsBoolDefault(false);

            // 确保源文件存在
            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(copySource))
            {
                return Response.Error($"Source asset '{copySource}' does not exist.");
            }

            // 确保目标文件夹存在
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                LogInfo($"[ProjectCreate] Creating folder path: {folderPath}");
                CreateFolderRecursive(folderPath);
            }

            // 如果没有指定文件名，使用源文件名
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(copySource);
            }

            // 获取源文件的扩展名
            string extension = Path.GetExtension(copySource);

            // 构建完整文件路径
            string filePath = $"{folderPath}/{fileName}{extension}";

            // 检查文件是否已存在
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath) != null && !force)
            {
                return Response.Error($"Asset '{filePath}' already exists. Use 'force' parameter to overwrite.");
            }

            try
            {
                // 复制资源
                string result = AssetDatabase.CopyAsset(copySource, filePath) ? "success" : "failed";
                if (result != "success")
                {
                    return Response.Error($"Failed to copy asset from '{copySource}' to '{filePath}'");
                }

                AssetDatabase.Refresh();

                LogInfo($"[ProjectCreate] Created file from copy: '{filePath}'");

                // 获取创建的资源
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);

                bool selectAfterCreate = args["select_after_create"].AsBoolDefault(true);
                if (selectAfterCreate && asset != null)
                {
                    Selection.activeObject = asset;
                }

                bool openAfterCreate = args["open_after_create"].AsBoolDefault(false);
                if (openAfterCreate && asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }

                return Response.Success($"File copied successfully to '{filePath}'", new JsonClass
                {
                    ["path"] = filePath,
                    ["type"] = asset?.GetType().Name ?? "Unknown",
                    ["name"] = Path.GetFileName(filePath)
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to copy asset '{copySource}': {e.Message}");
            }
        }

        /// <summary>
        /// 递归创建文件夹
        /// </summary>
        private void CreateFolderRecursive(string folderPath)
        {
            // 如果文件夹已存在，直接返回
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // 分割路径
            string[] pathParts = folderPath.Split('/');
            string currentPath = pathParts[0]; // 应该是"Assets"

            // 从第二部分开始，逐级创建文件夹
            for (int i = 1; i < pathParts.Length; i++)
            {
                string folderName = pathParts[i];
                string parentPath = currentPath;
                currentPath = $"{currentPath}/{folderName}";

                // 如果当前级别的文件夹不存在，创建它
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    string guid = AssetDatabase.CreateFolder(parentPath, folderName);
                    if (string.IsNullOrEmpty(guid))
                    {
                        LogInfo($"[ProjectCreate] Failed to create folder: {currentPath}");
                    }
                    else
                    {
                        LogInfo($"[ProjectCreate] Created folder: {currentPath}");
                    }
                }
            }

            // 刷新资源数据库以确保文件夹创建成功
            AssetDatabase.Refresh();
        }
    }
}
