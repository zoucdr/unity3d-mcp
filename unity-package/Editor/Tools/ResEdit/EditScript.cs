using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles C# script management operations in Unity.
    /// 对应方法名: manage_script
    /// </summary>
    [ToolName("edit_script", "资源管理")]
    public class EditScript : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型
                new MethodStr("action", "操作类型", false)
                    .SetEnumValues("create", "read", "modify", "delete", "search", "import")
                    .AddExamples("create", "read"),
                
                // 脚本名称
                new MethodStr("name", "脚本名称", false)
                    .AddExamples("PlayerController", "GameManager"),
                
                // 文件夹路径
                new MethodStr("folder", "脚本所在文件夹", false)
                    .AddExamples("Assets/Scripts/", "Assets/Scripts/Player/"),
                
                // 代码内容
                new MethodArr("lines", "C#代码内容"),
                
                // 脚本类型
                new MethodStr("script_type", "脚本类型")
                    .SetEnumValues("MonoBehaviour", "ScriptableObject", "Class", "Interface", "Enum")
                    .AddExample("MonoBehaviour")
                    .SetDefault("MonoBehaviour"),
                
                // 命名空间
                new MethodStr("namespace", "命名空间")
                    .AddExamples("MyGame", "MyGame.Player")
                    .SetDefault(""),
                
                // 查询字符串
                new MethodStr("query", "查询字符串")
                    .AddExamples("Player", "Controller"),
                
                // 源文件路径
                new MethodStr("source_path", "源文件路径")
                    .AddExamples("D:/Scripts/Player.cs", "C:/Code/GameManager.cs")
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("create", HandleCreateAction)
                    .Leaf("read", HandleReadAction)
                    .Leaf("modify", HandleModifyAction)
                    .Leaf("delete", HandleDeleteAction)
                    .Leaf("search", HandleSearchAction)
                    .Leaf("import", HandleImportAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理创建脚本的操作
        /// </summary>
        private object HandleCreateAction(JsonClass args)
        {
            try
            {
                var scriptInfo = ParseScriptArguments(args);
                if (scriptInfo.ErrorResponse != null)
                    return scriptInfo.ErrorResponse;

                // Ensure the target directory exists
                var dirResult = EnsureDirectoryExists(scriptInfo.FullPathDir);
                if (dirResult != null)
                    return dirResult;

                McpLogger.Log($"[ManageScript] Creating script '{scriptInfo.Name}.cs' at '{scriptInfo.RelativePath}'");
                return CreateScript(scriptInfo.FullPath, scriptInfo.RelativePath, scriptInfo.Name, scriptInfo.Contents, scriptInfo.NamespaceName);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Create action failed: {e}");
                return Response.Error($"Internal error processing create action: {e.Message}");
            }
        }

        /// <summary>
        /// 处理读取脚本的操作
        /// </summary>
        private object HandleReadAction(JsonClass args)
        {
            try
            {
                var scriptInfo = ParseScriptArguments(args);
                if (scriptInfo.ErrorResponse != null)
                    return scriptInfo.ErrorResponse;

                McpLogger.Log($"[ManageScript] Reading script at '{scriptInfo.RelativePath}'");
                return ReadScript(scriptInfo.FullPath, scriptInfo.RelativePath);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Read action failed: {e}");
                return Response.Error($"Internal error processing read action: {e.Message}");
            }
        }

        /// <summary>
        /// 处理修改脚本的操作
        /// </summary>
        private object HandleModifyAction(JsonClass args)
        {
            try
            {
                var scriptInfo = ParseScriptArguments(args);
                if (scriptInfo.ErrorResponse != null)
                    return scriptInfo.ErrorResponse;

                McpLogger.Log($"[ManageScript] Modifying script '{scriptInfo.Name}.cs' at '{scriptInfo.RelativePath}'");
                return ModifyScript(scriptInfo.FullPath, scriptInfo.RelativePath, scriptInfo.Name, scriptInfo.Contents);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Modify action failed: {e}");
                return Response.Error($"Internal error processing modify action: {e.Message}");
            }
        }

        /// <summary>
        /// 处理删除脚本的操作
        /// </summary>
        private object HandleDeleteAction(JsonClass args)
        {
            try
            {
                var scriptInfo = ParseScriptArguments(args);
                if (scriptInfo.ErrorResponse != null)
                    return scriptInfo.ErrorResponse;

                McpLogger.Log($"[ManageScript] Deleting script at '{scriptInfo.RelativePath}'");
                return DeleteScript(scriptInfo.FullPath, scriptInfo.RelativePath);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Delete action failed: {e}");
                return Response.Error($"Internal error processing delete action: {e.Message}");
            }
        }

        /// <summary>
        /// 处理类型搜索操作
        /// </summary>
        private object HandleSearchAction(JsonClass args)
        {
            try
            {
                string query = args["query"]?.Value;
                if (string.IsNullOrEmpty(query))
                {
                    return Response.Error("Query parameter is required for search action.");
                }

                McpLogger.Log($"[ManageScript] Searching for types matching '{query}'");
                var searchResults = SearchTypes(query);

                return Response.Success(
                    $"Found {searchResults.Count} types matching '{query}'.",
                    new { types = searchResults }
                );
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Search action failed: {e}");
                return Response.Error($"Internal error processing search action: {e.Message}");
            }
        }

        // --- Internal Helper Methods ---

        /// <summary>
        /// 脚本信息结构
        /// </summary>
        private struct ScriptInfo
        {
            public string Name;
            public string Contents;
            public string NamespaceName;
            public string FullPath;
            public string RelativePath;
            public string FullPathDir;
            public string SourcePath;
            public object ErrorResponse;
        }

        /// <summary>
        /// 解析脚本相关参数
        /// </summary>
        private ScriptInfo ParseScriptArguments(JsonClass args)
        {
            var info = new ScriptInfo();

            // Extract basic args
            string name = args["name"]?.Value;
            string folder = args["folder"]?.Value; // Relative to Assets/
            string contents = null;
            string sourcePath = args["source_path"]?.Value;

            // Handle lines parameter (array of strings)
            if (args["lines"] != null)
            {
                try
                {
                    var linesArray = args["lines"] as JsonArray;
                    if (linesArray != null)
                    {
                        contents = string.Join("\n", linesArray.ToStringList().Select(line => line.ToString()));
                    }
                    else
                    {
                        info.ErrorResponse = Response.Error("Lines parameter must be an array of strings.");
                        return info;
                    }
                }
                catch (Exception e)
                {
                    info.ErrorResponse = Response.Error($"Failed to process lines parameter: {e.Message}");
                    return info;
                }
            }

            string namespaceName = args["namespace"]?.Value;

            // Validate required args for non-search actions
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(name) && action != "search")
            {
                info.ErrorResponse = Response.Error("Name parameter is required for this action.");
                return info;
            }

            // Basic name validation for non-search actions
            if (!string.IsNullOrEmpty(name) && action != "search" && !Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                info.ErrorResponse = Response.Error(
                    $"Invalid script name: '{name}'. Use only letters, numbers, underscores, and don't start with a number."
                );
                return info;
            }

            // Process folder
            string relativeDir = folder ?? "Scripts";
            if (!string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = relativeDir.Replace('\\', '/').Trim('/');
                if (relativeDir.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeDir = relativeDir.Substring("Assets/".Length).TrimStart('/');
                }
            }
            if (string.IsNullOrEmpty(relativeDir))
            {
                relativeDir = "Scripts";
            }

            // Construct paths
            string scriptFileName = $"{name}.cs";
            string fullPathDir = Path.Combine(Application.dataPath, relativeDir);
            string fullPath = Path.Combine(fullPathDir, scriptFileName);
            string relativePath = Path.Combine("Assets", relativeDir, scriptFileName).Replace('\\', '/');

            // Populate info
            info.Name = name;
            info.Contents = contents;
            info.NamespaceName = namespaceName;
            info.FullPath = fullPath;
            info.RelativePath = relativePath;
            info.FullPathDir = fullPathDir;
            info.SourcePath = sourcePath;

            return info;
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private object EnsureDirectoryExists(string fullPathDir)
        {
            try
            {
                Directory.CreateDirectory(fullPathDir);
                return null; // Success
            }
            catch (Exception e)
            {
                return Response.Error($"Could not create directory '{fullPathDir}': {e.Message}");
            }
        }

        // --- Action Implementations ---

        /// <summary>
        /// 处理导入脚本的操作
        /// </summary>
        private object HandleImportAction(JsonClass args)
        {
            try
            {
                var scriptInfo = ParseScriptArguments(args);
                if (scriptInfo.ErrorResponse != null)
                    return scriptInfo.ErrorResponse;

                if (string.IsNullOrEmpty(scriptInfo.SourcePath))
                    return Response.Error("source_path is required for import action.");

                // 确保目标目录存在
                var dirResult = EnsureDirectoryExists(scriptInfo.FullPathDir);
                if (dirResult != null)
                    return dirResult;

                McpLogger.Log($"[ManageScript] Importing script from '{scriptInfo.SourcePath}' to '{scriptInfo.RelativePath}'");
                return ImportScript(scriptInfo.SourcePath, scriptInfo.FullPath, scriptInfo.RelativePath, scriptInfo.Name);
            }
            catch (Exception e)
            {
                McpLogger.LogError($"[ManageScript] Import action failed: {e}");
                return Response.Error($"Internal error processing import action: {e.Message}");
            }
        }

        private object CreateScript(
            string fullPath,
            string relativePath,
            string name,
            string contents,
            string namespaceName
        )
        {
            // Check if script already exists
            if (File.Exists(fullPath))
            {
                return Response.Error(
                    $"Script already exists at '{relativePath}'. Use 'update' action to modify."
                );
            }

            // Generate default content if none provided
            if (string.IsNullOrEmpty(contents))
            {
                contents = GenerateDefaultScriptContent(name, namespaceName);
            }

            // Validate syntax (basic check)
            if (!ValidateScriptSyntax(contents))
            {
                // Optionally return a specific error or warning about syntax
                // return Response.Error("Provided script content has potential syntax errors.");
                McpLogger.LogWarning($"Potential syntax error in script being created: {name}");
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh(); // Ensure Unity recognizes the new script
                return Response.Success(
                    $"Script '{name}.cs' created successfully at '{relativePath}'.",
                    new { path = relativePath }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create script '{relativePath}': {e.Message}");
            }
        }

        private object ReadScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'.");
            }

            try
            {
                string contents = File.ReadAllText(fullPath);

                // Return both normal content and lines array
                var responseData = new
                {
                    path = relativePath,
                    contents = contents,
                    lines = contents.Split('\n')
                };

                return Response.Success(
                    $"Script '{Path.GetFileName(relativePath)}' read successfully.",
                    responseData
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to read script '{relativePath}': {e.Message}");
            }
        }

        private object ModifyScript(
            string fullPath,
            string relativePath,
            string name,
            string contents
        )
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error(
                    $"Script not found at '{relativePath}'. Use 'create' action to add a new script or 'import' to import from external source."
                );
            }
            if (string.IsNullOrEmpty(contents))
            {
                return Response.Error("Content is required for the 'modify' action.");
            }

            // Validate syntax (basic check)
            if (!ValidateScriptSyntax(contents))
            {
                McpLogger.LogWarning($"Potential syntax error in script being modified: {name}");
                // Consider if this should be a hard error or just a warning
            }

            try
            {
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath); // Re-import to reflect changes
                AssetDatabase.Refresh();
                return Response.Success(
                    $"Script '{name}.cs' modified successfully at '{relativePath}'.",
                    new { path = relativePath }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to update script '{relativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// 导入脚本从外部源
        /// </summary>
        private object ImportScript(
            string sourcePath,
            string fullPath,
            string relativePath,
            string name
        )
        {
            // 检查源文件是否存在
            if (!File.Exists(sourcePath))
            {
                return Response.Error($"Source file not found at '{sourcePath}'.");
            }

            // 检查目标脚本是否已存在
            if (File.Exists(fullPath))
            {
                return Response.Error(
                    $"Script already exists at '{relativePath}'. Use 'modify' action to update."
                );
            }

            try
            {
                // 读取源文件并复制到目标位置
                string contents = File.ReadAllText(sourcePath);
                File.WriteAllText(fullPath, contents);
                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh(); // 确保Unity识别新脚本

                return Response.Success(
                    $"Script '{name}.cs' imported successfully from '{sourcePath}' to '{relativePath}'.",
                    new { path = relativePath }
                );
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to import script from '{sourcePath}' to '{relativePath}': {e.Message}");
            }
        }

        private object DeleteScript(string fullPath, string relativePath)
        {
            if (!File.Exists(fullPath))
            {
                return Response.Error($"Script not found at '{relativePath}'. Cannot delete.");
            }

            try
            {
                // Use AssetDatabase.MoveAssetToTrash for safer deletion (allows undo)
                bool deleted = AssetDatabase.MoveAssetToTrash(relativePath);
                if (deleted)
                {
                    AssetDatabase.Refresh();
                    return Response.Success(
                        $"Script '{Path.GetFileName(relativePath)}' moved to trash successfully."
                    );
                }
                else
                {
                    // Fallback or error if MoveAssetToTrash fails
                    return Response.Error(
                        $"Failed to move script '{relativePath}' to trash. It might be locked or in use."
                    );
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error deleting script '{relativePath}': {e.Message}");
            }
        }

        /// <summary>
        /// Generates basic C# script content based on name.
        /// </summary>
        private static string GenerateDefaultScriptContent(
            string name,
            string namespaceName
        )
        {
            string usingStatements = "using UnityEngine;\nusing System.Collections;\n";
            string classDeclaration;
            string body =
                "\n    // Use this for initialization\n    void Start() {\n\n    }\n\n    // Update is called once per frame\n    void Update() {\n\n    }\n";

            // 默认使用MonoBehaviour作为基类
            string baseClass = " : MonoBehaviour";

            classDeclaration = $"public class {name}{baseClass}";

            string fullContent = $"{usingStatements}\n";
            bool useNamespace = !string.IsNullOrEmpty(namespaceName);

            if (useNamespace)
            {
                fullContent += $"namespace {namespaceName}\n{{\n";
                // Indent class and body if using namespace
                classDeclaration = "    " + classDeclaration;
                body = string.Join("\n", body.Split('\n').Select(line => "    " + line));
            }

            fullContent += $"{classDeclaration}\n{{\n{body}\n}}";

            if (useNamespace)
            {
                fullContent += "\n}"; // Close namespace
            }

            return fullContent.Trim() + "\n"; // Ensure a trailing newline
        }

        /// <summary>
        /// Performs a very basic syntax validation (checks for balanced braces).
        /// TODO: Implement more robust syntax checking if possible.
        /// </summary>
        private static bool ValidateScriptSyntax(string contents)
        {
            if (string.IsNullOrEmpty(contents))
                return true; // Empty is technically valid?

            int braceBalance = 0;
            foreach (char c in contents)
            {
                if (c == '{')
                    braceBalance++;
                else if (c == '}')
                    braceBalance--;
            }

            return braceBalance == 0;
            // This is extremely basic. A real C# parser/compiler check would be ideal
            // but is complex to implement directly here.
        }

        /// <summary>
        /// 在所有程序集中搜索匹配查询的类型
        /// </summary>
        /// <param name="query">查询字符串</param>
        /// <returns>匹配的类型列表</returns>
        private List<object> SearchTypes(string query)
        {
            var results = new List<object>();
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // 转换查询为小写以进行不区分大小写的搜索
            string lowerQuery = query.ToLowerInvariant();
            bool isExactMatch = !lowerQuery.Contains("*");

            foreach (var assembly in loadedAssemblies)
            {
                try
                {
                    // 跳过动态程序集，它们可能会导致问题
                    if (assembly.IsDynamic)
                        continue;

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 处理无法加载的类型
                        types = ex.Types.Where(t => t != null).ToArray();
                    }

                    foreach (var type in types)
                    {
                        if (type == null)
                            continue;

                        bool isMatch = false;
                        string typeName = type.Name;
                        string fullName = type.FullName ?? "";

                        // 检查是否匹配
                        if (isExactMatch)
                        {
                            isMatch = typeName.ToLowerInvariant().Contains(lowerQuery) ||
                                     fullName.ToLowerInvariant().Contains(lowerQuery);
                        }
                        else
                        {
                            // 支持通配符搜索
                            string pattern = "^" + Regex.Escape(lowerQuery).Replace("\\*", ".*") + "$";
                            isMatch = Regex.IsMatch(typeName.ToLowerInvariant(), pattern, RegexOptions.IgnoreCase) ||
                                     Regex.IsMatch(fullName.ToLowerInvariant(), pattern, RegexOptions.IgnoreCase);
                        }

                        if (isMatch)
                        {
                            results.Add(new
                            {
                                name = type.Name,
                                fullName = type.FullName,
                                assemblyName = assembly.GetName().Name,
                                baseType = type.BaseType?.FullName,
                                nameSpace = type.Namespace
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他程序集
                    McpLogger.LogWarning($"Error searching in assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return results;
        }
    }
}

