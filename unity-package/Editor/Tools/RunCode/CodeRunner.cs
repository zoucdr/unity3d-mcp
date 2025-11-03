using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using UnityEditor.Compilation;
using CompilationAssembly = UnityEditor.Compilation.Assembly;
using ReflectionAssembly = System.Reflection.Assembly;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles C# code execution including compilation and running arbitrary C# methods.
    /// 对应方法名: code_runner
    /// </summary>
    [ToolName("code_runner", "开发工具")]
    public class CodeRunner : StateMethodBase
    {
        // Code execution tracking
        private class CodeOperation
        {
            public TaskCompletionSource<object> CompletionSource { get; set; }
            public string Code { get; set; }
            public string MethodName { get; set; }
            public List<ExecutionResult> Results { get; set; } = new List<ExecutionResult>();
        }

        private class ExecutionResult
        {
            public string MethodName { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Output { get; set; }
            public string StackTrace { get; set; }
            public double Duration { get; set; }
            public object ReturnValue { get; set; }
        }

        // Queue of active code operations
        private readonly List<CodeOperation> _activeOperations = new List<CodeOperation>();

        // 移除未使用的字段

        private object validationResult;
        private object executionResult;

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型 - 枚举
                new MethodStr("action", "操作类型")
                    .SetEnumValues("execute", "validate")
                    .AddExamples("execute", "validate")
                    .SetDefault("execute"),
                
                // C#代码内容
                new MethodStr("code", "要执行的C#代码内容", true)
                    .AddExamples("Debug.Log(\"Hello World!\");", "var result = 1 + 2; Debug.Log(result);")
                    .SetDefault(""),
                
                // 代码功能描述
                new MethodStr("description", "代码功能描述", true)
                    .AddExamples("测试代码执行", "计算数学表达式")
                    .SetDefault(""),
                
                // 类名
                new MethodStr("class_name", "类名，默认是CodeClass", true)
                    .AddExamples("CodeClass", "TestRunner")
                    .SetDefault("CodeClass"),
                
                // 入口方法名
                new MethodStr("entry_method", "入口方法名，默认是Execute", true)
                    .AddExamples("Execute", "Run")
                    .SetDefault("Execute"),
                
                // 命名空间
                new MethodStr("namespace", "命名空间，默认是CodeNamespace", true)
                    .AddExamples("CodeNamespace", "TestNamespace")
                    .SetDefault("CodeNamespace"),
                
                // 引用语句列表
                new MethodArr("includes", "引用using语句列表，JSON数组格式", true)
                    .SetItemType("string")
                    .AddExample("[\"System\", \"UnityEngine\"]")
                    .AddExample("[\"System.Collections.Generic\", \"UnityEditor\"]"),
                
                // 方法参数
                new MethodArr("parameters", "方法参数，JSON数组格式", true)
                    .SetItemType("object")
                    .AddExample("[{\"name\": \"value\", \"type\": \"int\", \"value\": 42}]")
                    .AddExample("[{\"name\": \"message\", \"type\": \"string\", \"value\": \"test\"}]"),
                
                // 执行超时
                new MethodInt("timeout", "执行超时（秒），默认30秒", true)
                    .SetRange(1, 300)
                    .AddExample("30")
                    .SetDefault(30),
                
                // 清理临时文件
                new MethodBool("cleanup", "执行后是否清理临时文件，默认true", true)
                    .SetDefault(true),
                
                // 返回输出
                new MethodBool("return_output", "是否捕获并返回控制台输出，默认true", true)
                    .SetDefault(true)
            };
        }

        /// <summary>
        /// 创建状态树
        /// </summary>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("execute", HandleExecuteCode)
                    .Leaf("validate", HandleValidateCode)
                    .DefaultLeaf(HandleExecuteCode)
                .Build();
        }

        // --- 代码执行操作处理方法 ---

        /// <summary>
        /// 处理执行代码操作
        /// </summary>
        private object HandleExecuteCode(StateTreeContext ctx)
        {
            McpLogger.Log("[CodeRunner] Executing C# code");
            // 为C#代码执行设置超时时间（90秒）
            return ctx.AsyncReturn(ExecuteCodeCoroutine(ctx.JsonData), 90f);
        }

        /// <summary>
        /// 处理验证代码操作
        /// </summary>
        private object HandleValidateCode(StateTreeContext ctx)
        {
            McpLogger.Log("[CodeRunner] Validating C# code");
            // 为C#代码验证设置超时时间（30秒）
            return ctx.AsyncReturn(ValidateCodeCoroutine(ctx.JsonData), 30f);
        }

        // --- 异步执行方法 ---

        /// <summary>
        /// 异步执行代码协程
        /// </summary>
        private IEnumerator ExecuteCodeCoroutine(JsonClass args)
        {
            string tempFilePath = null;
            string tempAssemblyPath = null;

            try
            {
                string code = args["code"]?.Value;
                if (string.IsNullOrEmpty(code))
                {
                    yield return Response.Error("code parameter is required");
                    yield break;
                }

                // 获取参数，确保有默认值
                var classNameNode = args["class_name"];
                var methodNameNode = args["entry_method"];
                var namespaceNode = args["namespace"];

                string className = (!string.IsNullOrEmpty(classNameNode?.Value)) ? classNameNode.Value : "CodeClass";
                string methodName = (!string.IsNullOrEmpty(methodNameNode?.Value)) ? methodNameNode.Value : "Run";
                string namespaceName = (!string.IsNullOrEmpty(namespaceNode?.Value)) ? namespaceNode.Value : "CodeNamespace";

                var includes = (args["includes"] as JsonArray)?.ToStringList()?.ToArray() ?? new string[0];
                var parameters = new object[0]; // 暂时简化处理
                int timeout = args["timeout"].AsIntDefault(30);
                bool cleanup = args["cleanup"].AsBoolDefault(true);
                bool returnOutput = args["return_output"].AsBoolDefault(true);

                McpLogger.Log($"[CodeRunner] Executing method: {namespaceName}.{className}.{methodName}");

                // 使用协程执行代码
                yield return ExecuteCodeCoroutineInternal(code, className, methodName, namespaceName, includes, parameters, timeout, cleanup, returnOutput,
                    (tFilePath, tAssemblyPath) => { tempFilePath = tFilePath; tempAssemblyPath = tAssemblyPath; });
                yield return executionResult;
            }
            finally
            {
                // 清理临时目录
                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    var tempDir = Path.GetDirectoryName(tempFilePath);
                    EditorApplication.delayCall += () => CleanupTempDirectory(tempDir);
                }
            }
        }

        /// <summary>
        /// 验证代码协程
        /// </summary>
        private IEnumerator ValidateCodeCoroutine(JsonClass args)
        {
            string tempFilePath = null;
            string tempAssemblyPath = null;

            try
            {
                string code = args["code"]?.Value;
                if (string.IsNullOrEmpty(code))
                {
                    yield return Response.Error("code parameter is required");
                    yield break;
                }

                // 获取参数，确保有默认值
                var classNameNode = args["class_name"];
                var methodNameNode = args["entry_method"];
                var namespaceNode = args["namespace"];

                string className = (!string.IsNullOrEmpty(classNameNode?.Value)) ? classNameNode.Value : "CodeClass";
                string methodName = (!string.IsNullOrEmpty(methodNameNode?.Value)) ? methodNameNode.Value : "Run";
                string namespaceName = (!string.IsNullOrEmpty(namespaceNode?.Value)) ? namespaceNode.Value : "CodeNamespace";

                var includes = (args["includes"] as JsonArray)?.ToStringList()?.ToArray() ?? new string[0];

                McpLogger.Log($"[CodeRunner] Validating code class: {namespaceName}.{className}");

                // 在协程外部处理异常
                validationResult = null;
                string fullCode = code;

                bool failed = false;
                try
                {
                    fullCode = GenerateFullCode(code, className, methodName, namespaceName, includes);
                    McpLogger.Log($"[CodeRunner] Generated code for validation");
                }
                catch (Exception e)
                {
                    LogError($"[CodeRunner] Failed to generate validation code: {e.Message}");
                    validationResult = Response.Error($"Failed to generate code: {e.Message}", new
                    {
                        source_code = code
                    });
                    failed = true;
                }
                if (failed)
                {
                    yield return validationResult;
                    yield break;
                }

                // 使用协程编译验证
                yield return CompileCodeCoroutine(fullCode,
                    (tFilePath, tAssemblyPath) => { tempFilePath = tFilePath; tempAssemblyPath = tAssemblyPath; },
                    (success, assembly, errors, compilerMessages) =>
                    {
                        if (success)
                        {
                            validationResult = Response.Success(
                                "Code syntax is valid", new
                                {
                                    operation = "validate",
                                    class_name = className,
                                    entry_method = methodName,
                                    namespace_name = namespaceName,
                                    generated_code = fullCode,
                                    compilation_messages = FormatCompilerMessages(compilerMessages)
                                });
                        }
                        else
                        {
                            validationResult = Response.Error("Code syntax validation failed", new
                            {
                                operation = "validate",
                                errors = string.Join("\n", errors ?? new string[] { "Unknown validation error" }),
                                compilation_messages = FormatCompilerMessages(compilerMessages),
                                final_code = fullCode
                            });
                        }
                    });

                yield return validationResult;
            }
            finally
            {
                // 清理临时目录
                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    var tempDir = Path.GetDirectoryName(tempFilePath);
                    EditorApplication.delayCall += () => CleanupTempDirectory(tempDir);
                }
            }
        }


        /// <summary>
        /// 执行代码的内部协程
        /// </summary>
        private IEnumerator ExecuteCodeCoroutineInternal(string code, string className, string methodName, string namespaceName, string[] includes, object[] parameters, int timeout, bool cleanup, bool returnOutput, System.Action<string, string> onTempFilesCreated = null)
        {
            // 在协程外部处理异常，避免在try-catch中使用yield return
            executionResult = null;

            // 生成完整的代码
            string fullCode = code;
            bool failed = false;
            try
            {
                fullCode = GenerateFullCode(code, className, methodName, namespaceName, includes);
                McpLogger.Log($"[CodeRunner] Generated complete code");
            }
            catch (Exception e)
            {
                LogError($"[CodeRunner] Failed to generate code: {e.Message}");
                executionResult = Response.Error($"Failed to generate code: {e.Message}", new
                {
                    source_code = code
                });
                failed = true;
            }
            if (failed)
            {
                yield return executionResult;
                yield break;
            }

            // 使用协程编译代码
            yield return CompileCodeCoroutine(fullCode,
                onTempFilesCreated,
                (success, assembly, errors, compilerMessages) =>
                {
                    if (success)
                    {
                        try
                        {
                            // 检查是否是完整代码，如果是则需要动态查找执行方法
                            bool isCompleteCode = fullCode.Contains("using ") || fullCode.Contains("namespace ") ||
                                                 (fullCode.Contains("public class") || fullCode.Contains("public static class"));

                            ExecutionResult result;
                            if (isCompleteCode)
                            {
                                // 对于完整代码，尝试查找并执行第一个成功的静态方法
                                result = ExecuteCompleteCode(assembly, parameters, returnOutput);
                            }
                            else
                            {
                                // 对于代码片段，按原有方式执行（返回单个结果）
                                result = ExecuteCompiledCode(assembly, namespaceName, className, methodName, parameters, returnOutput);
                            }

                            executionResult = Response.Success(
                                result.Success ? "Code execution completed successfully" : "Code execution completed with errors",
                                new
                                {
                                    operation = "execute",
                                    class_name = className,
                                    entry_method = methodName,
                                    namespace_name = namespaceName,
                                    success = result.Success,
                                    message = result.Message,
                                    output = result.Output,
                                    return_value = result.ReturnValue?.ToString() ?? "null",
                                    duration = result.Duration,
                                    stack_trace = result.StackTrace,
                                    compilation_messages = FormatCompilerMessages(compilerMessages)
                                }
                            );
                        }
                        catch (Exception e)
                        {
                            LogError($"[CodeRunner] Code execution failed: {e.Message}");
                            executionResult = Response.Error($"Failed to execute compiled code: {e.Message}", new
                            {
                                final_code = fullCode
                            });
                        }
                    }
                    else
                    {
                        executionResult = Response.Error("Code compilation failed", new
                        {
                            operation = "execute",
                            errors = string.Join("\n", errors ?? new string[] { "Unknown compilation error" }),
                            compilation_messages = FormatCompilerMessages(compilerMessages),
                            final_code = fullCode
                        });
                    }
                });
            yield return executionResult;
        }


        /// <summary>
        /// 生成完整的代码
        /// </summary>
        private string GenerateFullCode(string code, string className, string methodName, string namespaceName, string[] includes)
        {
            // 检查代码是否已经是完整的（必须同时包含class定义和using/namespace）
            bool hasClassDefinition = code.Contains("public class") || code.Contains("public static class") ||
                                     code.Contains("class ") && (code.Contains("{") && code.Contains("}"));
            bool hasNamespaceOrUsing = code.Contains("namespace ") || code.Contains("using ");

            bool isCompleteCode = hasClassDefinition && hasNamespaceOrUsing;

            if (isCompleteCode)
            {
                // 如果是完整代码，直接返回，不添加任何包装
                McpLogger.Log("[CodeRunner] 检测到完整代码，直接使用");
                return code;
            }

            // 合并提取的using和默认的includes
            var allIncludes = new List<string>(includes);
            // 提取代码中的using语句
            var codeLines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            // 添加默认引用的命名空间
            var extractedUsings = new List<string>()
            {
                // 自动收录Unity常用的无需额外引包即可直接使用的命名空间
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.IO",
                "System.Linq",
                "System.Text",
                "System.Threading",
                "System.Threading.Tasks",
                "UnityEngine",
                "UnityEngine.SceneManagement",
                "UnityEngine.EventSystems",
                "UnityEngine.UI",
                "UnityEditor",
                "UnityEditorInternal",
                "UnityEngine.Rendering",
            };

            foreach (var extracted in extractedUsings)
            {
                if (!allIncludes.Contains(extracted))
                {
                    allIncludes.Add(extracted);
                    McpLogger.Log($"[CodeRunner] 自动添加命名空间: {extracted}");
                }
            }

            var codeWithoutUsings = new List<string>();
            // 用于存储代码中的类型别名声明
            var existingTypeAliases = new List<string>();

            foreach (var line in codeLines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("using ") && trimmedLine.EndsWith(";"))
                {
                    // 检查是否是类型别名声明（如 using Object = UnityEngine.Object;）
                    if (trimmedLine.Contains("="))
                    {
                        existingTypeAliases.Add(trimmedLine);
                        McpLogger.Log($"[CodeRunner] 保留类型别名声明: {trimmedLine}");
                    }
                    else
                    {
                        // 提取普通using语句（去掉"using "和";"）
                        var usingNamespace = trimmedLine.Substring(6, trimmedLine.Length - 7).Trim();
                        if (!allIncludes.Contains(usingNamespace))
                        {
                            allIncludes.Add(usingNamespace);
                            McpLogger.Log($"[CodeRunner] 提取using语句: {usingNamespace}");
                        }
                    }
                }
                else
                {
                    codeWithoutUsings.Add(line);
                }
            }

            // 使用清理后的代码（不包含using语句）
            // 清理多余的空行（但保留代码中的空行结构）
            var codeWithoutUsingsStr = string.Join("\n", codeWithoutUsings);

            // 分析代码（移除using后的），自动添加需要的命名空间和类型别名
            var analyzedIncludes = AnalyzeCodeIncludes(codeWithoutUsingsStr);
            foreach (var analyzed in analyzedIncludes)
            {
                if (!allIncludes.Contains(analyzed))
                {
                    allIncludes.Add(analyzed);
                    McpLogger.Log($"[CodeRunner] 自动添加命名空间: {analyzed}");
                }
            }

            // 继续使用清理后的代码
            code = codeWithoutUsingsStr;
            // 去掉开头和结尾的空行
            while (code.StartsWith("\n") || code.StartsWith("\r"))
            {
                code = code.TrimStart('\n', '\r');
            }
            code = code.TrimEnd();

            var sb = new StringBuilder();

            // 添加using语句（使用合并后的includes）
            foreach (var include in allIncludes)
            {
                sb.AppendLine($"using {include};");
            }

            // 检查代码中是否使用了需要特殊处理的类型（如Object, Random等）
            var typeAliases = GetTypeAliasesForCode(code);

            // 添加现有的类型别名声明
            foreach (var alias in existingTypeAliases)
            {
                sb.AppendLine(alias);
            }

            // 添加自动检测到的类型别名声明
            foreach (var alias in typeAliases)
            {
                // 检查是否已存在相同的别名声明
                bool alreadyExists = existingTypeAliases.Any(existing =>
                    existing.Replace(" ", "").Equals(alias.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));

                if (!alreadyExists)
                {
                    sb.AppendLine(alias);
                }
            }

            sb.AppendLine();

            // 添加命名空间和类
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            // 改进的方法定义检测：检查是否包含完整的方法签名
            bool hasMethodDefinition =
                System.Text.RegularExpressions.Regex.IsMatch(code, @"\b(public|private|protected|internal)\s+(static\s+)?\w+\s+\w+\s*\([^\)]*\)\s*\{") ||
                System.Text.RegularExpressions.Regex.IsMatch(code, @"\b(public|private|protected|internal)\s+class\s+\w+") ||
                System.Text.RegularExpressions.Regex.IsMatch(code, @"\b(public|private|protected|internal)\s+struct\s+\w+");

            if (hasMethodDefinition)
            {
                // 如果用户代码已包含方法定义，直接缩进并添加
                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    sb.AppendLine($"        {line}");
                }
            }
            else
            {
                // 顶层语句代码 - 包装在方法中
                McpLogger.Log("[CodeRunner] 检测到顶层语句，包装为方法");
                var noReturn = !code.ToLower().Contains("return") || code.ToLower().Contains("return;");
                var returnType = noReturn ? "void" : "object";
                sb.AppendLine($"        public static {returnType} {methodName}()");
                sb.AppendLine("        {");

                // 不需要 try-catch，让异常自然传播
                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    // 保留空行和缩进
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"            {line}");
                    }
                }

                // 如果代码不包含return语句，添加默认返回值
                if (!noReturn)
                {
                    sb.AppendLine("            return \"Execution completed\";");
                }

                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var generatedCode = sb.ToString();

            // 输出生成的完整代码用于调试
            McpLogger.Log($"[CodeRunner] Generated code ({generatedCode.Length} chars):");
            McpLogger.Log($"[CodeRunner] ===== Generated Code Start =====");
            McpLogger.Log(generatedCode);
            McpLogger.Log($"[CodeRunner] ===== Generated Code End =====");

            return generatedCode;
        }

        /// <summary>
        /// 获取代码中需要的类型别名声明
        /// </summary>
        private List<string> GetTypeAliasesForCode(string code)
        {
            var typeAliases = new List<string>();

            // 定义需要特殊处理的类型（在System和UnityEngine中都存在的类型）
            var ambiguousTypes = new Dictionary<string, (string, string)>
            {
                { "Object", ("System", "UnityEngine") },
                { "Random", ("System", "UnityEngine") },
                { "Math", ("System", "UnityEngine") },
                { "Debug", ("System.Diagnostics", "UnityEngine") },
                { "Exception", ("System", "UnityEngine") },
                { "Mathf", ("System", "UnityEngine") }
            };

            foreach (var kvp in ambiguousTypes)
            {
                var typeName = kvp.Key;
                var namespaces = kvp.Value;

                // 检查类型是否在代码中使用（使用正则表达式匹配单词边界）
                var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(typeName)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(code, pattern))
                {
                    // 默认使用UnityEngine命名空间
                    string aliasStatement = $"using {typeName} = {namespaces.Item2}.{typeName};";
                    typeAliases.Add(aliasStatement);
                    McpLogger.Log($"[CodeRunner] 添加类型别名: {aliasStatement}");
                }
            }

            return typeAliases;
        }

        /// <summary>
        /// 协程版本的代码编译
        /// </summary>
        private IEnumerator CompileCodeCoroutine(string code, System.Action<string, string> onTempFilesCreated, System.Action<bool, ReflectionAssembly, string[], CompilerMessage[]> callback)
        {
            // 打印最终参与编译的完整代码
            McpLogger.Log($"[CodeRunner] ========================================");
            McpLogger.Log($"[CodeRunner] 最终编译代码 ({code.Length} 字符):");
            McpLogger.Log($"[CodeRunner] ========================================");
            McpLogger.Log(code);
            McpLogger.Log($"[CodeRunner] ========================================");
            McpLogger.Log($"[CodeRunner] 代码打印完成");
            McpLogger.Log($"[CodeRunner] ========================================");

            // 创建基于代码内容的临时目录
            var baseDir = Path.Combine(Application.temporaryCachePath, "CodeRunner");
            var codeHash = GetCodeHash(code);
            var tempDir = Path.Combine(baseDir, codeHash);
            string tempFilePath = null;
            string tempAssemblyPath = null;
            AssemblyBuilder assemblyBuilder = null;

            // 在协程外部处理初始化异常
            try
            {
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                var tempFileName = "TestClass.cs";
                tempFilePath = Path.Combine(tempDir, tempFileName);
                tempAssemblyPath = Path.Combine(tempDir, "TestClass.dll");

                // 通知上层函数临时文件路径
                onTempFilesCreated?.Invoke(tempFilePath, tempAssemblyPath);

                // 检查是否已经存在编译好的程序集
                if (File.Exists(tempAssemblyPath))
                {
                    McpLogger.Log($"[CodeRunner] 发现已编译的程序集，直接加载: {tempAssemblyPath}");
                    try
                    {
                        var assemblyBytes = File.ReadAllBytes(tempAssemblyPath);
                        if (assemblyBytes.Length > 0)
                        {
                            var loadedAssembly = ReflectionAssembly.Load(assemblyBytes);
                            McpLogger.Log($"[CodeRunner] 程序集重用成功: {assemblyBytes.Length} bytes");
                            callback(true, loadedAssembly, null, null);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"[CodeRunner] 加载已存在程序集失败，将重新编译: {ex.Message}");
                        // 删除损坏的程序集文件
                        try { File.Delete(tempAssemblyPath); } catch { }
                    }
                }

                // 写入代码到临时文件
                File.WriteAllText(tempFilePath, code);
                McpLogger.Log($"[CodeRunner] 临时文件路径: {tempFilePath}");
                McpLogger.Log($"[CodeRunner] 目标程序集路径: {tempAssemblyPath}");

            }
            catch (Exception e)
            {
                LogError($"[CodeRunner] 初始化失败: {e.Message}");
                callback(false, null, new[] { $"Initialization failed: {e.Message}" }, null);
                yield break;
            }

            // 设置编译器
            try
            {
                assemblyBuilder = new AssemblyBuilder(tempAssemblyPath, new[] { tempFilePath });

                // 收集程序集引用
                var references = new List<string>();
                McpLogger.Log("[CodeRunner] 开始收集程序集引用...");

                foreach (var assembly in CompilationPipeline.GetAssemblies())
                {
                    references.AddRange(assembly.compiledAssemblyReferences);
                    if (!string.IsNullOrEmpty(assembly.outputPath) && File.Exists(assembly.outputPath))
                    {
                        references.Add(assembly.outputPath);
                    }
                }

                foreach (var loadedAssembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(loadedAssembly.Location) && File.Exists(loadedAssembly.Location))
                        {
                            references.Add(loadedAssembly.Location);
                        }
                    }
                    catch { }
                }

                // 添加基础引用
                references.Add(typeof(object).Assembly.Location);
                references.Add(typeof(System.Linq.Enumerable).Assembly.Location);
                references.Add(typeof(UnityEngine.Debug).Assembly.Location);
                references.Add(typeof(UnityEditor.EditorApplication).Assembly.Location);

                var uniqueReferences = references.Distinct().Where(r => !string.IsNullOrEmpty(r) && File.Exists(r)).ToArray();
                McpLogger.Log($"[CodeRunner] 收集到 {uniqueReferences.Length} 个有效引用");

                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
                assemblyBuilder.additionalReferences = uniqueReferences;

                // 记录详细的编译参数
                McpLogger.Log($"[CodeRunner] 编译参数:");
                McpLogger.Log($"  - 源文件: {tempFilePath}");
                McpLogger.Log($"  - 目标程序集: {tempAssemblyPath}");
                McpLogger.Log($"  - 引用选项: {assemblyBuilder.referencesOptions}");
                McpLogger.Log($"  - 额外引用数量: {uniqueReferences.Length}");
                McpLogger.Log($"  - 前20个引用: {string.Join(", ", uniqueReferences.Take(477).Select(Path.GetFileName))}");
            }
            catch (Exception e)
            {
                LogError($"[CodeRunner] 编译器设置失败: {e.Message}");
                callback(false, null, new[] { $"Compiler setup failed: {e.Message}" }, null);
                yield break;
            }

            // 启动编译
            McpLogger.Log("[CodeRunner] 尝试启动编译...");
            bool started = false;

            // 用于存储编译消息的变量
            CompilerMessage[] compilationMessages = null;
            bool compilationFinished = false;

            // 添加编译事件监听
            System.Action<object> buildStarted = (context) =>
            {
                EditorApplication.delayCall += () => McpLogger.Log($"[CodeRunner] 编译开始");
            };

            System.Action<string, CompilerMessage[]> buildFinished = (assemblyPath, messages) =>
            {
                compilationMessages = messages;
                compilationFinished = true;
                EditorApplication.delayCall += () =>
                {
                    McpLogger.Log($"[CodeRunner] 编译完成: {assemblyPath}");
                    if (messages != null && messages.Length > 0)
                    {
                        McpLogger.Log($"[CodeRunner] 收到 {messages.Length} 条编译消息");
                        foreach (var msg in messages)
                        {
                            var logLevel = msg.type == CompilerMessageType.Error ? "ERROR" :
                                         msg.type == CompilerMessageType.Warning ? "WARNING" : "INFO";
                            McpLogger.Log($"[CodeRunner] {logLevel}: {msg.message} (Line: {msg.line}, Column: {msg.column})");
                        }
                    }
                    else
                    {
                        McpLogger.Log("[CodeRunner] 没有收到编译消息");
                    }
                };
            };

            CompilationPipeline.compilationStarted += buildStarted;
            CompilationPipeline.assemblyCompilationFinished += buildFinished;

            // 读取源文件内容，确保在编译失败时可以返回
            string sourceContent = "";
            try
            {
                // 在启动编译前，先验证源文件内容
                McpLogger.Log($"[CodeRunner] 验证源文件: {tempFilePath}");
                if (File.Exists(tempFilePath))
                {
                    sourceContent = File.ReadAllText(tempFilePath);
                    McpLogger.Log($"[CodeRunner] 源文件大小: {sourceContent.Length} 字符");
                    McpLogger.Log($"[CodeRunner] 编译的代码文件: {sourceContent}");
                }

                started = assemblyBuilder.Build();
                McpLogger.Log($"[CodeRunner] 编译启动结果: {started}");

                // 记录AssemblyBuilder的初始状态
                McpLogger.Log($"[CodeRunner] 初始编译状态: {assemblyBuilder.status}");
            }
            catch (Exception e)
            {
                LogError($"[CodeRunner] 编译启动异常: {e.Message}");
                LogError($"[CodeRunner] 异常堆栈: {e.StackTrace}");
                callback(false, null, new[] {
                    $"Failed to start compilation: {e.Message}",
                    $"Stack trace: {e.StackTrace}",
                    $"Source code: {sourceContent}"
                }, null);
                yield break;
            }
            finally
            {
                // 移除事件监听
                CompilationPipeline.compilationStarted -= buildStarted;
                CompilationPipeline.assemblyCompilationFinished -= buildFinished;
            }

            if (!started)
            {
                LogError("[CodeRunner] 无法启动编译");
                callback(false, null, new[] {
                    "Failed to start compilation",
                    $"Source code: {sourceContent}"
                }, null);
                yield break;
            }

            McpLogger.Log("[CodeRunner] 编译已启动，等待完成...");

            // 使用协程等待编译完成
            float timeout = 30f;
            float elapsedTime = 0f;
            var lastStatus = assemblyBuilder.status;

            while (assemblyBuilder.status == AssemblyBuilderStatus.IsCompiling && elapsedTime < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;

                // 监控状态变化
                if (assemblyBuilder.status != lastStatus)
                {
                    McpLogger.Log($"[CodeRunner] 编译状态变化: {lastStatus} -> {assemblyBuilder.status}");
                    lastStatus = assemblyBuilder.status;
                }

                if (Mathf.FloorToInt(elapsedTime) != Mathf.FloorToInt(elapsedTime - 0.1f))
                {
                    McpLogger.Log($"[CodeRunner] 编译中... 状态: {assemblyBuilder.status}, 已等待: {elapsedTime:F1}s");
                }
            }

            // 额外等待编译消息（有时消息会稍晚到达）
            float messageWaitTime = 0f;
            const float maxMessageWaitTime = 2f;
            while (!compilationFinished && messageWaitTime < maxMessageWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                messageWaitTime += 0.1f;
                McpLogger.Log($"[CodeRunner] 等待编译消息... {messageWaitTime:F1}s");
            }

            McpLogger.Log($"[CodeRunner] 编译完成, 最终状态: {assemblyBuilder.status}, 消息已接收: {compilationFinished}");

            // 处理编译结果
            if (assemblyBuilder.status == AssemblyBuilderStatus.Finished)
            {
                McpLogger.Log($"[CodeRunner] 编译状态为Finished，开始验证结果...");

                // 立即检查文件是否存在
                var assemblyPath = assemblyBuilder.assemblyPath;
                McpLogger.Log($"[CodeRunner] 预期程序集路径: {assemblyPath}");
                McpLogger.Log($"[CodeRunner] 文件是否存在: {File.Exists(assemblyPath)}");

                if (File.Exists(assemblyPath))
                {
                    var fileInfo = new FileInfo(assemblyPath);
                    McpLogger.Log($"[CodeRunner] 文件大小: {fileInfo.Length} bytes, 修改时间: {fileInfo.LastWriteTime}");
                }

                yield return HandleCompilationSuccess(assemblyBuilder, compilationMessages, callback);
            }
            else if (elapsedTime >= timeout)
            {
                LogError("[CodeRunner] 编译超时");
                callback(false, null, new[] {
                    "Compilation timeout",
                    $"Source code: {sourceContent}"
                }, compilationMessages);
            }
            else
            {
                LogError($"[CodeRunner] 编译失败, 状态: {assemblyBuilder.status}");

                // 收集详细的错误信息
                var errorMessages = new List<string>();

                // 首先检查是否有编译消息
                if (compilationMessages != null && compilationMessages.Length > 0)
                {
                    McpLogger.Log($"[CodeRunner] 处理 {compilationMessages.Length} 条编译消息");
                    var errorMsgs = new List<string>();
                    var warningMsgs = new List<string>();

                    foreach (var msg in compilationMessages)
                    {
                        var msgText = $"Line {msg.line}, Column {msg.column}: {msg.message}";
                        if (msg.type == CompilerMessageType.Error)
                        {
                            errorMsgs.Add(msgText);
                            LogError($"[CodeRunner] 编译错误: {msgText}");
                        }
                        else if (msg.type == CompilerMessageType.Warning)
                        {
                            warningMsgs.Add(msgText);
                            LogWarning($"[CodeRunner] 编译警告: {msgText}");
                        }
                    }

                    if (errorMsgs.Count > 0)
                    {
                        errorMessages.Add($"=== C# 编译失败 ({errorMsgs.Count} 个错误) ===");
                        errorMessages.Add("");
                        foreach (var err in errorMsgs)
                        {
                            errorMessages.Add($"❌ {err}");
                        }
                        errorMessages.Add("");
                    }
                    else
                    {
                        // 没有具体错误但编译失败
                        errorMessages.Add($"编译失败: {assemblyBuilder.status}");
                        errorMessages.Add("未收到具体的错误消息，可能是编译器内部错误");
                    }

                    if (warningMsgs.Count > 0)
                    {
                        errorMessages.Add($"⚠️  编译警告 ({warningMsgs.Count} 个):");
                        foreach (var warn in warningMsgs)
                        {
                            errorMessages.Add($"  • {warn}");
                        }
                    }
                }
                else
                {
                    LogWarning("[CodeRunner] 没有收到编译消息，尝试其他方法获取错误信息");

                    errorMessages.Add("=== C# 编译失败 ===");
                    errorMessages.Add("");
                    errorMessages.Add($"编译状态: {assemblyBuilder.status}");
                    errorMessages.Add("⚠️  编译器未返回具体的错误消息");
                    errorMessages.Add("");

                    // 尝试获取编译错误信息（保留原有逻辑作为后备）
                    try
                    {
                        var tempDirPath = Path.GetDirectoryName(tempFilePath);
                        var logFiles = new string[]
                        {
                            Path.Combine(tempDirPath, "CompilerOutput.log"),
                            Path.ChangeExtension(tempAssemblyPath, ".log"),
                            tempAssemblyPath + ".log"
                        };

                        bool foundLog = false;
                        foreach (var logFile in logFiles)
                        {
                            if (File.Exists(logFile))
                            {
                                var logContent = File.ReadAllText(logFile);
                                if (!string.IsNullOrEmpty(logContent))
                                {
                                    errorMessages.Add($"📄 编译日志 ({Path.GetFileName(logFile)}):");
                                    errorMessages.Add(logContent);
                                    errorMessages.Add("");
                                    LogError($"[CodeRunner] 编译日志: {logContent}");
                                    foundLog = true;
                                }
                            }
                        }

                        // 检查临时目录中的所有文件
                        if (Directory.Exists(tempDirPath))
                        {
                            var allFiles = Directory.GetFiles(tempDirPath);
                            McpLogger.Log($"[CodeRunner] 临时目录文件: {string.Join(", ", allFiles.Select(Path.GetFileName))}");

                            if (!foundLog)
                            {
                                errorMessages.Add("📁 临时目录文件:");
                                errorMessages.Add($"  {string.Join(", ", allFiles.Select(Path.GetFileName))}");

                                // 如果没有找到 .dll 文件，说明编译根本没有生成输出
                                var dllFiles = allFiles.Where(f => f.EndsWith(".dll")).ToArray();
                                if (dllFiles.Length == 0)
                                {
                                    errorMessages.Add("");
                                    errorMessages.Add("⚠️  未生成程序集文件，可能是编译器启动失败或代码存在语法错误");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[CodeRunner] 获取编译错误信息失败: {ex.Message}");
                        errorMessages.Add($"❌ 获取详细错误信息失败: {ex.Message}");
                    }
                }

                // 添加生成的代码到错误信息中，便于调试
                errorMessages.Add("");
                errorMessages.Add("=== 生成的完整代码 ===");
                errorMessages.Add("");

                // 添加行号以便于定位错误
                var codeLines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                for (int i = 0; i < codeLines.Length; i++)
                {
                    errorMessages.Add($"{i + 1,4} | {codeLines[i]}");
                }

                // 添加源代码内容到错误信息
                errorMessages.Add("");
                errorMessages.Add("=== 源代码内容 ===");
                errorMessages.Add(sourceContent);

                callback(false, null, errorMessages.ToArray(), compilationMessages);
            }

            // 清理由上层函数负责
        }

        /// <summary>
        /// 处理编译成功后的程序集加载
        /// </summary>
        private IEnumerator HandleCompilationSuccess(AssemblyBuilder assemblyBuilder, CompilerMessage[] compilationMessages, System.Action<bool, ReflectionAssembly, string[], CompilerMessage[]> callback)
        {
            var assemblyPath = assemblyBuilder.assemblyPath;
            var tempDir = Path.GetDirectoryName(assemblyPath);
            float waitTime = 0f;
            const float maxWaitTime = 2f;

            McpLogger.Log($"[CodeRunner] 等待程序集文件生成: {assemblyPath}");

            // 等待文件存在
            while (!File.Exists(assemblyPath) && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;

                // 每0.5秒检查一次临时目录状态
                if (waitTime % 0.5f < 0.1f)
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            var files = Directory.GetFiles(tempDir);
                            McpLogger.Log($"[CodeRunner] 临时目录文件 ({waitTime:F1}s): {string.Join(", ", files.Select(Path.GetFileName))}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[CodeRunner] 检查临时目录失败: {ex.Message}");
                    }
                }
            }

            if (File.Exists(assemblyPath))
            {
                // 等待文件写入完成
                yield return new WaitForSeconds(0.1f);

                try
                {
                    var assemblyBytes = File.ReadAllBytes(assemblyPath);
                    McpLogger.Log($"[CodeRunner] 程序集文件大小: {assemblyBytes.Length} bytes");

                    if (assemblyBytes.Length > 0)
                    {
                        var loadedAssembly = ReflectionAssembly.Load(assemblyBytes);
                        McpLogger.Log($"[CodeRunner] 程序集加载成功: {assemblyBytes.Length} bytes");
                        callback(true, loadedAssembly, null, compilationMessages);
                    }
                    else
                    {
                        LogError("[CodeRunner] 程序集文件为空");
                        callback(false, null, new[] { "Assembly file is empty" }, compilationMessages);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[CodeRunner] 无法加载程序集: {ex.Message}");
                    callback(false, null, new[] { $"Failed to load assembly: {ex.Message}" }, compilationMessages);
                }
            }
            else
            {
                LogError($"[CodeRunner] 程序集文件不存在: {assemblyPath}");

                // 收集详细的调试信息
                var errorMessages = new List<string>();
                errorMessages.Add("Assembly file not found after compilation");
                errorMessages.Add($"Expected path: {assemblyPath}");

                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        var allFiles = Directory.GetFiles(tempDir);
                        LogError($"[CodeRunner] 临时目录内容: {string.Join(", ", allFiles.Select(Path.GetFileName))}");
                        errorMessages.Add($"Files in temp directory: {string.Join(", ", allFiles.Select(Path.GetFileName))}");

                        // 检查是否有其他dll文件
                        var dllFiles = allFiles.Where(f => f.EndsWith(".dll")).ToArray();
                        if (dllFiles.Length > 0)
                        {
                            McpLogger.Log($"[CodeRunner] 找到其他DLL文件: {string.Join(", ", dllFiles.Select(Path.GetFileName))}");
                            errorMessages.Add($"Other DLL files found: {string.Join(", ", dllFiles.Select(Path.GetFileName))}");
                        }

                        // 检查是否有日志文件
                        var logFiles = allFiles.Where(f => f.EndsWith(".log") || f.Contains("log")).ToArray();
                        foreach (var logFile in logFiles)
                        {
                            try
                            {
                                var logContent = File.ReadAllText(logFile);
                                LogError($"[CodeRunner] 日志文件 {Path.GetFileName(logFile)}: {logContent}");
                                errorMessages.Add($"Log file {Path.GetFileName(logFile)}: {logContent}");
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        LogError($"[CodeRunner] 临时目录不存在: {tempDir}");
                        errorMessages.Add($"Temp directory does not exist: {tempDir}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[CodeRunner] 收集调试信息失败: {ex.Message}");
                    errorMessages.Add($"Failed to collect debug info: {ex.Message}");
                }

                callback(false, null, errorMessages.ToArray(), compilationMessages);
            }
        }
        /// <summary>
        /// 执行完整代码（自动查找可执行方法）
        /// </summary>
        private ExecutionResult ExecuteCompleteCode(ReflectionAssembly assembly, object[] parameters, bool returnOutput)
        {
            // 创建一个收集Unity控制台日志的StringBuilder
            StringBuilder unityLogBuilder = new StringBuilder();

            void OnUnityLogMessageReceived(string logString, string stackTrace, LogType logType)
            {
                string logTypeStr = logType.ToString();
                unityLogBuilder.AppendLine($"[{logTypeStr}] {logString}");
                if (logType == LogType.Error || logType == LogType.Exception)
                {
                    unityLogBuilder.AppendLine(stackTrace);
                }
            }

            ExecutionResult result = null;

            try
            {
                var types = assembly.GetTypes();
                McpLogger.Log($"[CodeRunner] 在完整代码中找到 {types.Length} 个类型");

                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                    foreach (var method in methods)
                    {
                        if (method.IsSpecialName || method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                            continue;

                        McpLogger.Log($"[CodeRunner] 尝试执行方法: {type.FullName}.{method.Name}");

                        var executionResult = new ExecutionResult
                        {
                            MethodName = $"{type.Name}.{method.Name}"
                        };

                        var startTime = DateTime.Now;

                        StringWriter outputWriter = null;
                        TextWriter originalOutput = null;

                        try
                        {
                            // 始终捕获控制台输出，即使 returnOutput 为 false
                            outputWriter = new StringWriter();
                            originalOutput = Console.Out;
                            Console.SetOut(outputWriter);

                            var methodParameters = method.GetParameters();
                            object[] actualParameters = null;

                            if (methodParameters.Length > 0)
                            {
                                actualParameters = new object[methodParameters.Length];
                                for (int i = 0; i < methodParameters.Length && i < parameters.Length; i++)
                                {
                                    try
                                    {
                                        actualParameters[i] = Convert.ChangeType(parameters[i], methodParameters[i].ParameterType);
                                    }
                                    catch
                                    {
                                        actualParameters[i] = parameters[i];
                                    }
                                }
                            }

                            // 添加Unity控制台日志监听
                            Application.logMessageReceived += OnUnityLogMessageReceived;
                            var returnValue = method.Invoke(null, actualParameters);
                            Application.logMessageReceived -= OnUnityLogMessageReceived;

                            executionResult.Success = true;
                            executionResult.Message = "Method executed successfully";
                            executionResult.ReturnValue = returnValue;

                            McpLogger.Log($"[CodeRunner] 方法 {method.Name} 执行成功");
                            if (returnValue != null)
                            {
                                McpLogger.Log($"[CodeRunner] 方法返回值: {returnValue}");
                            }
                        }
                        catch (TargetInvocationException tie)
                        {
                            var innerException = tie.InnerException ?? tie;
                            executionResult.Success = false;
                            executionResult.Message = innerException.Message;
                            executionResult.StackTrace = innerException.StackTrace;
                            LogError($"[CodeRunner] 方法 {method.Name} 执行失败: {innerException.Message}");
                        }
                        catch (Exception e)
                        {
                            executionResult.Success = false;
                            executionResult.Message = e.Message;
                            executionResult.StackTrace = e.StackTrace;
                            LogError($"[CodeRunner] 方法 {method.Name} 执行异常: {e.Message}");
                        }
                        finally
                        {
                            // 恢复控制台输出
                            if (originalOutput != null)
                            {
                                Console.SetOut(originalOutput);
                                if (returnOutput) // Only assign output if returnOutput true
                                {
                                    executionResult.Output = outputWriter?.ToString() ?? "";
                                }
                                outputWriter?.Dispose();
                            }
                            executionResult.Duration = (DateTime.Now - startTime).TotalMilliseconds;
                        }

                        McpLogger.Log($"[CodeRunner] 方法 {method.Name}: {(executionResult.Success ? "SUCCESS" : "FAILED")} ({executionResult.Duration:F2}ms)");

                        // 保存第一个结果（无论成功或失败）
                        if (result == null)
                        {
                            result = executionResult;
                        }

                        // 如果找到成功的方法，立即返回
                        if (executionResult.Success)
                        {
                            result = executionResult;
                            goto AfterMethodExecution;
                        }
                    }
                }

            AfterMethodExecution:

                if (result == null)
                {
                    result = new ExecutionResult
                    {
                        MethodName = "Unknown",
                        Success = false,
                        Message = "No executable public static methods found in the assembly",
                        Output = "",
                        Duration = 0
                    };
                }
            }
            catch (Exception e)
            {
                LogError($"[CodeRunner] 执行完整代码时发生异常: {e.Message}");
                result = new ExecutionResult
                {
                    MethodName = "Unknown",
                    Success = false,
                    Message = $"Failed to execute complete code: {e.Message}",
                    Output = "",
                    Duration = 0,
                    StackTrace = e.StackTrace
                };
            }
            finally
            {
                Application.logMessageReceived -= OnUnityLogMessageReceived;

                if (result != null)
                {
                    string unityLogs = unityLogBuilder.ToString();
                    if (!string.IsNullOrEmpty(unityLogs))
                    {
                        if (!string.IsNullOrEmpty(result.Output))
                        {
                            result.Output = $"{result.Output}\n\n--- Unity Debug Logs ---\n{unityLogs}";
                        }
                        else
                        {
                            result.Output = $"--- Unity Debug Logs ---\n{unityLogs}";
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 执行编译后的代码
        /// </summary>
        private ExecutionResult ExecuteCompiledCode(ReflectionAssembly assembly, string namespaceName, string className, string methodName, object[] parameters, bool returnOutput)
        {
            var fullClassName = $"{namespaceName}.{className}";
            var codeType = assembly.GetType(fullClassName);

            if (codeType == null)
            {
                throw new Exception($"Code class not found: {fullClassName}");
            }

            // 查找指定的方法
            var targetMethod = codeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            if (targetMethod == null)
            {
                // 如果找不到指定方法，尝试查找任何public方法
                var allMethods = codeType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                targetMethod = allMethods.FirstOrDefault(m => !m.IsSpecialName && m.DeclaringType == codeType);

                if (targetMethod != null)
                {
                    LogWarning($"[CodeRunner] Method '{methodName}' not found, using '{targetMethod.Name}' instead");
                    methodName = targetMethod.Name;
                }
            }

            if (targetMethod == null)
            {
                throw new Exception($"No suitable method found in class {fullClassName}");
            }

            var executionResult = new ExecutionResult
            {
                MethodName = methodName
            };

            var startTime = DateTime.Now;

            // 准备控制台输出捕获
            StringWriter outputWriter = null;
            TextWriter originalOutput = null;

            try
            {
                if (returnOutput)
                {
                    outputWriter = new StringWriter();
                    originalOutput = Console.Out;
                    Console.SetOut(outputWriter);
                }

                // 创建实例（如果需要）
                object instance = null;
                if (!targetMethod.IsStatic)
                {
                    instance = Activator.CreateInstance(codeType);
                }

                // 准备方法参数
                var methodParameters = targetMethod.GetParameters();
                object[] actualParameters = null;

                if (methodParameters.Length > 0)
                {
                    actualParameters = new object[methodParameters.Length];
                    for (int i = 0; i < methodParameters.Length && i < parameters.Length; i++)
                    {
                        try
                        {
                            // 尝试转换参数类型
                            actualParameters[i] = Convert.ChangeType(parameters[i], methodParameters[i].ParameterType);
                        }
                        catch
                        {
                            actualParameters[i] = parameters[i];
                        }
                    }
                }

                // 执行方法
                var returnValue = targetMethod.Invoke(instance, actualParameters);

                executionResult.Success = true;
                executionResult.Message = "Code executed successfully";
                executionResult.ReturnValue = returnValue;

                McpLogger.Log($"[CodeRunner] Method {methodName} executed successfully");

                // 如果方法执行了Unity相关操作，确保它们被正确记录
                if (returnValue != null)
                {
                    McpLogger.Log($"[CodeRunner] Method returned: {returnValue}");
                }
            }
            catch (TargetInvocationException tie)
            {
                var innerException = tie.InnerException ?? tie;
                executionResult.Success = false;
                executionResult.Message = innerException.Message;
                executionResult.StackTrace = innerException.StackTrace;
                LogError($"[CodeRunner] Method {methodName} failed: {innerException.Message}");
                Debug.LogException(innerException);
            }
            catch (Exception e)
            {
                executionResult.Success = false;
                executionResult.Message = e.Message;
                executionResult.StackTrace = e.StackTrace;
                LogError($"[CodeRunner] Method {methodName} failed: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                // 恢复控制台输出
                if (returnOutput && originalOutput != null)
                {
                    Console.SetOut(originalOutput);
                    executionResult.Output = outputWriter?.ToString() ?? "";
                    outputWriter?.Dispose();
                }

                executionResult.Duration = (DateTime.Now - startTime).TotalMilliseconds;
            }

            McpLogger.Log($"[CodeRunner] Method {methodName}: {(executionResult.Success ? "SUCCESS" : "FAILED")} ({executionResult.Duration:F2}ms)");

            return executionResult;
        }

        /// <summary>
        /// 分析代码中使用的类型，自动添加所需的命名空间
        /// </summary>
        private string[] AnalyzeCodeIncludes(string code)
        {
            var additionalIncludes = new HashSet<string>();
            var typeAliases = new List<string>();

            // 定义类型到命名空间的映射
            var typeToNamespace = new Dictionary<string, string>
            {
                // Terrain 相关
                { "Terrain", "UnityEngine" },
                { "TerrainData", "UnityEngine" },
                { "TerrainLayer", "UnityEngine" },
                { "TerrainCollider", "UnityEngine" },
                
                // UI 相关
                { "Canvas", "UnityEngine.UI" },
                { "Button", "UnityEngine.UI" },
                { "Text", "UnityEngine.UI" },
                { "Image", "UnityEngine.UI" },
                { "RawImage", "UnityEngine.UI" },
                { "Slider", "UnityEngine.UI" },
                { "ScrollRect", "UnityEngine.UI" },
                { "Dropdown", "UnityEngine.UI" },
                { "InputField", "UnityEngine.UI" },
                { "Toggle", "UnityEngine.UI" },
                { "ToggleGroup", "UnityEngine.UI" },
                { "LayoutElement", "UnityEngine.UI" },
                { "LayoutGroup", "UnityEngine.UI" },
                { "HorizontalLayoutGroup", "UnityEngine.UI" },
                { "VerticalLayoutGroup", "UnityEngine.UI" },
                { "GridLayoutGroup", "UnityEngine.UI" },
                { "ContentSizeFitter", "UnityEngine.UI" },
                { "AspectRatioFitter", "UnityEngine.UI" },
                { "RectMask2D", "UnityEngine.UI" },
                { "Mask", "UnityEngine.UI" },
                { "Selectable", "UnityEngine.UI" },
                { "GraphicRaycaster", "UnityEngine.UI" },
                
                // TextMeshPro
                { "TextMeshPro", "TMPro" },
                { "TextMeshProUGUI", "TMPro" },
                { "TMP_Text", "TMPro" },
                { "TMP_InputField", "TMPro" },
                { "TMP_Dropdown", "TMPro" },
                
                // Physics
                { "Rigidbody", "UnityEngine" },
                { "Rigidbody2D", "UnityEngine" },
                { "Collider", "UnityEngine" },
                { "Collider2D", "UnityEngine" },
                { "BoxCollider", "UnityEngine" },
                { "SphereCollider", "UnityEngine" },
                { "CapsuleCollider", "UnityEngine" },
                { "MeshCollider", "UnityEngine" },
                { "BoxCollider2D", "UnityEngine" },
                { "CircleCollider2D", "UnityEngine" },
                { "PolygonCollider2D", "UnityEngine" },
                { "EdgeCollider2D", "UnityEngine" },
                { "Joint", "UnityEngine" },
                { "FixedJoint", "UnityEngine" },
                { "HingeJoint", "UnityEngine" },
                { "SpringJoint", "UnityEngine" },
                { "CharacterJoint", "UnityEngine" },
                { "ConfigurableJoint", "UnityEngine" },
                
                // Rendering
                { "Camera", "UnityEngine" },
                { "Light", "UnityEngine" },
                { "Material", "UnityEngine" },
                { "Shader", "UnityEngine" },
                { "Texture", "UnityEngine" },
                { "Texture2D", "UnityEngine" },
                { "RenderTexture", "UnityEngine" },
                { "Mesh", "UnityEngine" },
                { "MeshFilter", "UnityEngine" },
                { "MeshRenderer", "UnityEngine" },
                { "SkinnedMeshRenderer", "UnityEngine" },
                { "SpriteRenderer", "UnityEngine" },
                { "LineRenderer", "UnityEngine" },
                { "TrailRenderer", "UnityEngine" },
                { "ParticleSystem", "UnityEngine" },
                { "Skybox", "UnityEngine" },
                { "ReflectionProbe", "UnityEngine.Rendering" },
                { "LightProbeGroup", "UnityEngine.Rendering" },
                
                // Animation
                { "Animation", "UnityEngine" },
                { "Animator", "UnityEngine" },
                { "AnimationClip", "UnityEngine" },
                { "AnimatorController", "UnityEngine.Animations" },
                { "AnimatorOverrideController", "UnityEngine" },
                
                // Audio
                { "AudioSource", "UnityEngine" },
                { "AudioClip", "UnityEngine" },
                { "AudioListener", "UnityEngine" },
                { "AudioMixer", "UnityEngine.Audio" },
                
                // Navigation
                { "NavMeshAgent", "UnityEngine.AI" },
                { "NavMeshObstacle", "UnityEngine.AI" },
                { "NavMesh", "UnityEngine.AI" },
                { "NavMeshSurface", "UnityEngine.AI" },
                { "OffMeshLink", "UnityEngine.AI" },
                
                // Scene Management
                { "Scene", "UnityEngine.SceneManagement" },
                { "SceneManager", "UnityEngine.SceneManagement" },
                
                // Editor
                { "EditorWindow", "UnityEditor" },
                { "EditorGUILayout", "UnityEditor" },
                { "EditorGUI", "UnityEditor" },
                { "SerializedObject", "UnityEditor" },
                { "SerializedProperty", "UnityEditor" },
                { "Handles", "UnityEditor" },
                { "Gizmos", "UnityEngine" },
                { "Selection", "UnityEditor" },
                { "Undo", "UnityEditor" },
                { "PrefabUtility", "UnityEditor" },
                { "AssetDatabase", "UnityEditor" },
                { "BuildPipeline", "UnityEditor" },
                { "EditorUtility", "UnityEditor" },
                
                // EventSystem
                { "EventSystem", "UnityEngine.EventSystems" },
                { "PointerEventData", "UnityEngine.EventSystems" },
                { "BaseEventData", "UnityEngine.EventSystems" },
                { "IPointerClickHandler", "UnityEngine.EventSystems" },
                { "IPointerDownHandler", "UnityEngine.EventSystems" },
                { "IPointerUpHandler", "UnityEngine.EventSystems" },
                { "IDragHandler", "UnityEngine.EventSystems" },
                { "IBeginDragHandler", "UnityEngine.EventSystems" },
                { "IEndDragHandler", "UnityEngine.EventSystems" },
                
                // Other
                { "ScriptableObject", "UnityEngine" },
                { "MonoBehaviour", "UnityEngine" },
                { "Behaviour", "UnityEngine" },
                { "Component", "UnityEngine" },
                { "Transform", "UnityEngine" },
                { "RectTransform", "UnityEngine" },
                { "GameObject", "UnityEngine" },
                { "Object", "UnityEngine" },
                { "Random", "UnityEngine" },
                { "Mathf", "UnityEngine" },
                { "Vector2", "UnityEngine" },
                { "Vector3", "UnityEngine" },
                { "Vector4", "UnityEngine" },
                { "Quaternion", "UnityEngine" },
                { "Matrix4x4", "UnityEngine" },
                { "Color", "UnityEngine" },
                { "Color32", "UnityEngine" },
                { "Rect", "UnityEngine" },
                { "Bounds", "UnityEngine" },
                { "Ray", "UnityEngine" },
                { "RaycastHit", "UnityEngine" },
                { "Physics", "UnityEngine" },
                { "Physics2D", "UnityEngine" },
                { "Time", "UnityEngine" },
                { "Input", "UnityEngine" },
                { "Application", "UnityEngine" },
                { "Screen", "UnityEngine" },
                { "Resources", "UnityEngine" },
                { "PlayerPrefs", "UnityEngine" },
                { "WWW", "UnityEngine" },
                { "Coroutine", "UnityEngine" },
                { "WaitForSeconds", "UnityEngine" },
                { "WaitForEndOfFrame", "UnityEngine" },
                { "YieldInstruction", "UnityEngine" },
            };

            // 定义需要特殊处理的类型（在System和UnityEngine中都存在的类型）
            var ambiguousTypes = new Dictionary<string, (string, string)>
            {
                { "Object", ("System", "UnityEngine") },
                { "Random", ("System", "UnityEngine") },
                { "Math", ("System", "UnityEngine") },
                { "Debug", ("System.Diagnostics", "UnityEngine") },
                { "Exception", ("System", "UnityEngine") },
                { "Mathf", ("System", "UnityEngine") }
            };

            // 分析代码，查找使用的类型
            foreach (var kvp in typeToNamespace)
            {
                var typeName = kvp.Key;
                var namespaceName = kvp.Value;

                // 检查类型是否在代码中使用（使用正则表达式匹配单词边界）
                var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(typeName)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(code, pattern))
                {
                    additionalIncludes.Add(namespaceName);

                    // 检查是否是需要特殊处理的类型
                    if (ambiguousTypes.TryGetValue(typeName, out var namespaces))
                    {
                        // 如果是ambiguousTypes中的类型，添加别名声明
                        string aliasStatement = $"using {typeName} = {namespaceName}.{typeName};";
                        typeAliases.Add(aliasStatement);
                        McpLogger.Log($"[CodeRunner] 添加类型别名: {aliasStatement}");

                        // 同时添加可能冲突的命名空间
                        if (namespaces.Item1 != namespaceName)
                            additionalIncludes.Add(namespaces.Item1);
                    }
                }
            }

            // 将特殊类型别名添加到结果中
            var result = additionalIncludes.ToArray();
            if (result.Length > 0)
            {
                McpLogger.Log($"[CodeRunner] 代码分析发现额外需要的命名空间: {string.Join(", ", result)}");
            }

            if (typeAliases.Count > 0)
            {
                McpLogger.Log($"[CodeRunner] 添加类型别名: {string.Join(", ", typeAliases)}");
            }

            // 返回命名空间列表，类型别名会在GenerateFullCode中处理
            return result;
        }

        /// <summary>
        /// 根据代码内容生成哈希值作为临时目录名
        /// </summary>
        private string GetCodeHash(string code)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(code);
                var hash = sha256.ComputeHash(bytes);
                // 取前8个字节转换为16进制字符串
                var hashString = BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLower();
                McpLogger.Log($"[CodeRunner] 代码哈希值: {hashString}");
                return hashString;
            }
        }

        /// <summary>
        /// 清理临时目录
        /// </summary>
        private void CleanupTempDirectory(string tempDir)
        {
            if (string.IsNullOrEmpty(tempDir) || !Directory.Exists(tempDir))
                return;
            // 判断临时目录下是否存在dll文件，如果没有则不进行清理
            try
            {
                if (!Directory.Exists(tempDir))
                    return;

                var dllFiles = Directory.GetFiles(tempDir, "*.dll", SearchOption.AllDirectories);
                if (dllFiles == null || dllFiles.Length == 0)
                {
                    McpLogger.Log($"[CodeRunner] 临时目录中未找到dll文件，无需清理: {tempDir}");
                    return;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[CodeRunner] 检查临时目录dll文件时发生异常: {tempDir}, 错误: {ex.Message}");
                return;
            }

            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    Directory.Delete(tempDir, true);
                    McpLogger.Log($"[CodeRunner] 临时目录清理成功: {tempDir}");
                    return; // 成功删除，退出
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        McpLogger.Log($"[CodeRunner] 清理临时目录失败，重试 {retryCount}/{maxRetries}: {tempDir}");
                        System.Threading.Thread.Sleep(100 * retryCount);
                    }
                    else
                    {
                        LogWarning($"[CodeRunner] 无法清理临时目录: {tempDir}, 错误: {ex.Message}");
                        // 尝试逐个删除文件
                        CleanupDirectoryFiles(tempDir);
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[CodeRunner] 清理临时目录时发生意外错误: {tempDir}, 错误: {ex.Message}");
                    break; // 非IO错误，不重试
                }
            }
        }

        /// <summary>
        /// 尝试逐个删除目录中的文件
        /// </summary>
        private void CleanupDirectoryFiles(string tempDir)
        {
            try
            {
                var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    CleanupSingleFile(file);
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[CodeRunner] 无法枚举临时目录文件: {tempDir}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理临时文件（保留原方法以兼容）
        /// </summary>
        private void CleanupTempFiles(string tempFilePath, string tempAssemblyPath)
        {
            // 获取临时目录并清理整个目录
            if (!string.IsNullOrEmpty(tempFilePath))
            {
                var tempDir = Path.GetDirectoryName(tempFilePath);
                CleanupTempDirectory(tempDir);
            }
        }

        /// <summary>
        /// 格式化编译消息为可序列化的对象数组
        /// </summary>
        private object[] FormatCompilerMessages(CompilerMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
                return new object[0];

            var formattedMessages = new List<object>();
            foreach (var msg in messages)
            {
                formattedMessages.Add(new
                {
                    type = msg.type.ToString(),
                    message = msg.message,
                    file = msg.file,
                    line = msg.line,
                    column = msg.column
                });
            }
            return formattedMessages.ToArray();
        }

        /// <summary>
        /// 清理单个文件，带重试机制
        /// </summary>
        private void CleanupSingleFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    File.Delete(filePath);
                    return; // 成功删除，退出
                }
                catch (IOException ex)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        McpLogger.Log($"[CodeRunner] Failed to clean file, retry {retryCount}/{maxRetries}: {filePath}");
                        System.Threading.Thread.Sleep(100 * retryCount);
                    }
                    else
                    {
                        LogWarning($"[CodeRunner] Unable to clean temporary file: {filePath}, error: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[CodeRunner] Unexpected error occurred while cleaning file: {filePath}, error: {ex.Message}");
                    break; // 非IO错误，不重试
                }
            }
        }
    }
}