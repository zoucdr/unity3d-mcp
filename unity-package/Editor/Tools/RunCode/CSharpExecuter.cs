/*-*-* Copyright (c) Mycoria@Mycoria
 * Author: zouhunter
 * Creation Date: 2026-02-27
 * Version: 1.0.0
 * Description: 使用 Roslyn 在内存中编译并执行 C# 代码，无需写临时文件，同步执行
 *_*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// 使用 Roslyn 在内存中编译并执行 C# 代码
    /// 对应方法名: code_run
    /// </summary>
    [ToolName("csharp_executer", "Development Tools", "开发工具")]
    public class CSharpExecuter : StateMethodBase
    {
        public override string Description => "使用 Roslyn 在内存中编译并执行 C# 代码（无临时文件，同步执行）";

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

        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                new MethodStr("action", "操作类型", false)
                    .SetEnumValues("execute", "validate")
                    .SetDefault("execute"),

                new MethodStr("code", "要执行的 C# 代码内容")
                    .SetDefault(""),

                new MethodStr("description", "代码功能描述")
                    .SetDefault(""),

                new MethodStr("class_name", "类名，默认为 CodeClass")
                    .SetDefault("CodeClass"),

                new MethodStr("entry_method", "入口方法名称，默认为 Execute")
                    .SetDefault("Execute"),

                new MethodStr("namespace", "命名空间，默认为 CodeNamespace")
                    .SetDefault("CodeNamespace"),

                new MethodArr("includes", "using 语句列表，JSON 数组格式")
                    .SetItemType("string")
                    .AddExample("[\"System\", \"UnityEngine\"]"),

                new MethodArr("parameters", "方法参数，JSON 数组格式")
                    .SetItemType("object")
                    .AddExample("[{\"name\": \"value\", \"type\": \"int\", \"value\": 42}]"),

                new MethodInt("timeout", "执行超时时间（秒），默认 30 秒")
                    .SetRange(1, 300)
                    .SetDefault(30),

                new MethodBool("return_output", "捕获并返回控制台输出，默认为 true")
                    .SetDefault(true)
            };
        }

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

        // ── 操作处理 ──────────────────────────────────────────────────────────

        private object HandleExecuteCode(JsonClass args)
        {
            McpLogger.Log("[CodeRunTool] 开始执行代码（Roslyn 内存编译）");

            string code = args["code"]?.Value;
            if (string.IsNullOrEmpty(code))
                return Response.Error("code parameter is required");

            string className   = GetStringArg(args, "class_name",   "CodeClass");
            string methodName  = GetStringArg(args, "entry_method",  "Execute");
            string nsName      = GetStringArg(args, "namespace",     "CodeNamespace");
            var    includes    = (args["includes"] as JsonArray)?.ToStringList()?.ToArray() ?? new string[0];
            bool   returnOutput = args["return_output"].AsBoolDefault(true);

            string fullCode;
            try
            {
                fullCode = GenerateFullCode(code, className, methodName, nsName, includes);
            }
            catch (Exception e)
            {
                LogError($"[CodeRunTool] 生成代码失败: {e.Message}");
                return Response.Error($"生成代码失败: {e.Message}", new { source_code = code });
            }

            var (success, assembly, errors) = CompileInMemory(fullCode);

            if (!success)
            {
                return Response.Error("代码编译失败", new
                {
                    operation = "execute",
                    errors    = string.Join("\n", errors ?? new[] { "未知编译错误" }),
                    final_code = fullCode
                });
            }

            try
            {
                bool isCompleteCode = fullCode.Contains("using ") || fullCode.Contains("namespace ") ||
                                      fullCode.Contains("public class") || fullCode.Contains("public static class");

                ExecutionResult result = isCompleteCode
                    ? ExecuteCompleteCode(assembly, new object[0], returnOutput)
                    : ExecuteCompiledCode(assembly, nsName, className, methodName, new object[0], returnOutput);

                return Response.Success(
                    result.Success ? "代码执行成功" : "代码执行完成（含错误）",
                    new
                    {
                        operation    = "execute",
                        class_name   = className,
                        entry_method = methodName,
                        namespace_name = nsName,
                        success      = result.Success,
                        message      = result.Message,
                        output       = result.Output,
                        return_value = result.ReturnValue?.ToString() ?? "null",
                        duration     = result.Duration,
                        stack_trace  = result.StackTrace
                    });
            }
            catch (Exception e)
            {
                LogError($"[CodeRunTool] 执行编译后代码失败: {e.Message}");
                return Response.Error($"执行编译后代码失败: {e.Message}", new { final_code = fullCode });
            }
        }

        private object HandleValidateCode(JsonClass args)
        {
            McpLogger.Log("[CodeRunTool] 开始验证代码（Roslyn 语法检查）");

            string code = args["code"]?.Value;
            if (string.IsNullOrEmpty(code))
                return Response.Error("code parameter is required");

            string className  = GetStringArg(args, "class_name",  "CodeClass");
            string methodName = GetStringArg(args, "entry_method", "Execute");
            string nsName     = GetStringArg(args, "namespace",    "CodeNamespace");
            var    includes   = (args["includes"] as JsonArray)?.ToStringList()?.ToArray() ?? new string[0];

            string fullCode;
            try
            {
                fullCode = GenerateFullCode(code, className, methodName, nsName, includes);
            }
            catch (Exception e)
            {
                LogError($"[CodeRunTool] 生成验证代码失败: {e.Message}");
                return Response.Error($"生成代码失败: {e.Message}", new { source_code = code });
            }

            var (success, _, errors) = CompileInMemory(fullCode);

            if (success)
            {
                return Response.Success("代码语法验证通过", new
                {
                    operation      = "validate",
                    class_name     = className,
                    entry_method   = methodName,
                    namespace_name = nsName,
                    generated_code = fullCode
                });
            }
            else
            {
                return Response.Error("代码语法验证失败", new
                {
                    operation      = "validate",
                    errors         = string.Join("\n", errors ?? new[] { "未知验证错误" }),
                    final_code     = fullCode
                });
            }
        }

        // ── Roslyn 内存编译（通过 RoslynProxy 反射调用）─────────────────────────

        /// <summary>
        /// 使用 Roslyn 在内存中编译代码，返回 (成功, 程序集, 错误列表)
        /// </summary>
        private (bool success, Assembly assembly, string[] errors) CompileInMemory(string code)
        {
            McpLogger.Log("[CodeRunTool] Roslyn 开始编译...");

            var (success, bytes, errors) = RoslynProxy.CompileToBytes(code);

            if (!success)
            {
                LogError($"[CodeRunTool] 编译失败，{errors?.Length ?? 0} 个错误");
                foreach (var err in errors ?? Array.Empty<string>())
                    LogError($"  {err}");
                return (false, null, errors);
            }

            var assembly = Assembly.Load(bytes);
            McpLogger.Log($"[CodeRunTool] 编译成功，程序集大小: {bytes.Length} bytes");
            return (true, assembly, null);
        }

        // ── 代码生成（与 CodeRunner 保持一致）────────────────────────────────

        private string GenerateFullCode(string code, string className, string methodName, string namespaceName, string[] includes)
        {
            bool hasClassDef   = code.Contains("public class") || code.Contains("public static class") ||
                                 (code.Contains("class ") && code.Contains("{") && code.Contains("}"));
            bool hasNsOrUsing  = code.Contains("namespace ") || code.Contains("using ");
            bool isCompleteCode = hasClassDef && hasNsOrUsing;

            if (isCompleteCode)
            {
                McpLogger.Log("[CodeRunTool] 检测到完整代码，直接使用");
                return code;
            }

            var allIncludes = new List<string>(includes);
            var defaultUsings = new List<string>
            {
                "System", "System.Collections", "System.Collections.Generic",
                "System.IO", "System.Linq", "System.Text",
                "System.Threading", "System.Threading.Tasks",
                "UnityEngine", "UnityEngine.SceneManagement",
                "UnityEngine.EventSystems", "UnityEngine.UI",
                "UnityEditor", "UnityEditorInternal", "UnityEngine.Rendering"
            };
            foreach (var ns in defaultUsings)
            {
                if (!allIncludes.Contains(ns))
                    allIncludes.Add(ns);
            }

            // 提取代码中已有的 using 和类型别名
            var existingTypeAliases = new List<string>();
            var codeLines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var codeWithoutUsings = new List<string>();

            foreach (var line in codeLines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    if (trimmed.Contains("="))
                    {
                        existingTypeAliases.Add(trimmed);
                    }
                    else
                    {
                        var ns = trimmed.Substring(6, trimmed.Length - 7).Trim();
                        if (!allIncludes.Contains(ns))
                            allIncludes.Add(ns);
                    }
                }
                else
                {
                    codeWithoutUsings.Add(line);
                }
            }

            var codeBody = string.Join("\n", codeWithoutUsings).TrimStart('\r', '\n').TrimEnd();

            // 根据代码内容自动补充命名空间
            var analyzed = AnalyzeCodeIncludes(codeBody);
            foreach (var ns in analyzed)
            {
                if (!allIncludes.Contains(ns))
                    allIncludes.Add(ns);
            }

            var sb = new StringBuilder();
            foreach (var inc in allIncludes)
                sb.AppendLine($"using {inc};");

            // 类型别名：用户显式声明的 + 自动检测的
            foreach (var alias in existingTypeAliases)
                sb.AppendLine(alias);

            foreach (var alias in GetTypeAliasesForCode(codeBody))
            {
                bool exists = existingTypeAliases.Any(e =>
                    e.Replace(" ", "").Equals(alias.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    sb.AppendLine(alias);
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            bool hasMethodDef =
                System.Text.RegularExpressions.Regex.IsMatch(codeBody,
                    @"\b(public|private|protected|internal)\s+(static\s+)?\w+\s+\w+\s*\([^\)]*\)\s*\{") ||
                System.Text.RegularExpressions.Regex.IsMatch(codeBody,
                    @"\b(public|private|protected|internal)\s+(class|struct)\s+\w+");

            if (hasMethodDef)
            {
                foreach (var line in codeBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    sb.AppendLine($"        {line}");
            }
            else
            {
                McpLogger.Log("[CodeRunTool] 检测到顶层语句，包装为方法");
                bool noReturn = !codeBody.ToLower().Contains("return") || codeBody.ToLower().Contains("return;");
                string returnType = noReturn ? "void" : "object";

                sb.AppendLine($"        public static {returnType} {methodName}()");
                sb.AppendLine("        {");
                foreach (var line in codeBody.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        sb.AppendLine();
                    else
                        sb.AppendLine($"            {line}");
                }
                if (!noReturn)
                    sb.AppendLine("            return \"Execution completed\";");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            var generatedCode = sb.ToString();
            McpLogger.Log($"[CodeRunTool] 生成代码完成 ({generatedCode.Length} chars)");
            McpLogger.Log(generatedCode);
            return generatedCode;
        }

        private List<string> GetTypeAliasesForCode(string code)
        {
            var aliases = new List<string>();
            var ambiguous = new Dictionary<string, string>
            {
                { "Object", "UnityEngine" },
                { "Random", "UnityEngine" },
                { "Debug",  "UnityEngine" }
            };

            foreach (var kvp in ambiguous)
            {
                var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(code, pattern))
                {
                    string alias = $"using {kvp.Key} = {kvp.Value}.{kvp.Key};";
                    aliases.Add(alias);
                    McpLogger.Log($"[CodeRunTool] 添加类型别名: {alias}");
                }
            }
            return aliases;
        }

        private string[] AnalyzeCodeIncludes(string code)
        {
            var result = new HashSet<string>();

            var typeToNs = new Dictionary<string, string>
            {
                { "Canvas", "UnityEngine.UI" }, { "Button", "UnityEngine.UI" },
                { "Text", "UnityEngine.UI" }, { "Image", "UnityEngine.UI" },
                { "Slider", "UnityEngine.UI" }, { "ScrollRect", "UnityEngine.UI" },
                { "InputField", "UnityEngine.UI" }, { "Toggle", "UnityEngine.UI" },
                { "HorizontalLayoutGroup", "UnityEngine.UI" }, { "VerticalLayoutGroup", "UnityEngine.UI" },
                { "GridLayoutGroup", "UnityEngine.UI" }, { "ContentSizeFitter", "UnityEngine.UI" },
                { "TextMeshPro", "TMPro" }, { "TextMeshProUGUI", "TMPro" },
                { "TMP_Text", "TMPro" }, { "TMP_InputField", "TMPro" }, { "TMP_Dropdown", "TMPro" },
                { "NavMeshAgent", "UnityEngine.AI" }, { "NavMesh", "UnityEngine.AI" },
                { "AudioMixer", "UnityEngine.Audio" },
                { "Scene", "UnityEngine.SceneManagement" }, { "SceneManager", "UnityEngine.SceneManagement" },
                { "ReflectionProbe", "UnityEngine.Rendering" }, { "LightProbeGroup", "UnityEngine.Rendering" },
                { "EditorWindow", "UnityEditor" }, { "EditorGUILayout", "UnityEditor" },
                { "Selection", "UnityEditor" }, { "Undo", "UnityEditor" },
                { "PrefabUtility", "UnityEditor" }, { "AssetDatabase", "UnityEditor" },
                { "EditorUtility", "UnityEditor" }, { "Handles", "UnityEditor" },
                { "EventSystem", "UnityEngine.EventSystems" },
                { "IPointerClickHandler", "UnityEngine.EventSystems" },
            };

            foreach (var kvp in typeToNs)
            {
                var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b";
                if (System.Text.RegularExpressions.Regex.IsMatch(code, pattern))
                    result.Add(kvp.Value);
            }

            if (result.Count > 0)
                McpLogger.Log($"[CodeRunTool] 自动补充命名空间: {string.Join(", ", result)}");

            return result.ToArray();
        }

        // ── 执行编译后程序集（逻辑与 CodeRunner 一致）──────────────────────

        private ExecutionResult ExecuteCompleteCode(Assembly assembly, object[] parameters, bool returnOutput)
        {
            var unityLogSb = new StringBuilder();
            Application.LogCallback onLog = (msg, stack, type) =>
            {
                unityLogSb.AppendLine($"[{type}] {msg}");
                if (type == LogType.Error || type == LogType.Exception)
                    unityLogSb.AppendLine(stack);
            };
            Application.logMessageReceived += onLog;

            ExecutionResult result = null;

            try
            {
                var types = assembly.GetTypes();
                McpLogger.Log($"[CodeRunTool] 程序集中找到 {types.Length} 个类型");

                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    foreach (var method in methods)
                    {
                        if (method.IsSpecialName || method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                            continue;

                        McpLogger.Log($"[CodeRunTool] 尝试执行: {type.FullName}.{method.Name}");

                        var er = new ExecutionResult { MethodName = $"{type.Name}.{method.Name}" };
                        var t0 = DateTime.Now;

                        StringWriter sw = null;
                        TextWriter orig = null;

                        try
                        {
                            sw   = new StringWriter();
                            orig = Console.Out;
                            Console.SetOut(sw);

                            var mParams = method.GetParameters();
                            object[] actual = mParams.Length > 0 ? BuildParameters(mParams, parameters) : null;

                            er.ReturnValue = method.Invoke(null, actual);
                            er.Success     = true;
                            er.Message     = "方法执行成功";
                        }
                        catch (TargetInvocationException tie)
                        {
                            var inner = tie.InnerException ?? tie;
                            er.Success     = false;
                            er.Message     = inner.Message;
                            er.StackTrace  = inner.StackTrace;
                        }
                        catch (Exception ex)
                        {
                            er.Success    = false;
                            er.Message    = ex.Message;
                            er.StackTrace = ex.StackTrace;
                        }
                        finally
                        {
                            if (orig != null)
                            {
                                Console.SetOut(orig);
                                if (returnOutput)
                                    er.Output = sw?.ToString() ?? "";
                                sw?.Dispose();
                            }
                            er.Duration = (DateTime.Now - t0).TotalMilliseconds;
                        }

                        if (result == null) result = er;
                        if (er.Success) { result = er; goto Done; }
                    }
                }

                Done:
                if (result == null)
                    result = new ExecutionResult
                    {
                        MethodName = "Unknown", Success = false,
                        Message    = "程序集中未找到可执行的公有静态方法"
                    };
            }
            catch (Exception e)
            {
                result = new ExecutionResult
                {
                    MethodName = "Unknown", Success = false,
                    Message    = $"执行完整代码时发生异常: {e.Message}",
                    StackTrace = e.StackTrace
                };
            }
            finally
            {
                Application.logMessageReceived -= onLog;
                string unityLogs = unityLogSb.ToString();
                if (!string.IsNullOrEmpty(unityLogs) && result != null)
                {
                    result.Output = string.IsNullOrEmpty(result.Output)
                        ? $"--- Unity Debug Logs ---\n{unityLogs}"
                        : $"{result.Output}\n\n--- Unity Debug Logs ---\n{unityLogs}";
                }
            }

            return result;
        }

        private ExecutionResult ExecuteCompiledCode(Assembly assembly, string namespaceName, string className, string methodName, object[] parameters, bool returnOutput)
        {
            var fullClassName = $"{namespaceName}.{className}";
            var codeType = assembly.GetType(fullClassName);
            if (codeType == null)
                throw new Exception($"未找到类: {fullClassName}");

            var targetMethod = codeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                               ?? codeType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                          .FirstOrDefault(m => !m.IsSpecialName && m.DeclaringType == codeType);

            if (targetMethod == null)
                throw new Exception($"类 {fullClassName} 中未找到合适的方法");

            var er = new ExecutionResult { MethodName = methodName };
            var t0 = DateTime.Now;

            StringWriter sw   = null;
            TextWriter   orig = null;

            try
            {
                if (returnOutput)
                {
                    sw   = new StringWriter();
                    orig = Console.Out;
                    Console.SetOut(sw);
                }

                object instance = targetMethod.IsStatic ? null : Activator.CreateInstance(codeType);
                var mParams     = targetMethod.GetParameters();
                object[] actual = mParams.Length > 0 ? BuildParameters(mParams, parameters) : null;

                er.ReturnValue = targetMethod.Invoke(instance, actual);
                er.Success     = true;
                er.Message     = "代码执行成功";
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                er.Success    = false;
                er.Message    = inner.Message;
                er.StackTrace = inner.StackTrace;
                Debug.LogException(inner);
            }
            catch (Exception ex)
            {
                er.Success    = false;
                er.Message    = ex.Message;
                er.StackTrace = ex.StackTrace;
                Debug.LogException(ex);
            }
            finally
            {
                if (returnOutput && orig != null)
                {
                    Console.SetOut(orig);
                    er.Output = sw?.ToString() ?? "";
                    sw?.Dispose();
                }
                er.Duration = (DateTime.Now - t0).TotalMilliseconds;
            }

            return er;
        }

        // ── 辅助方法 ─────────────────────────────────────────────────────────

        private static object[] BuildParameters(ParameterInfo[] mParams, object[] provided)
        {
            var actual = new object[mParams.Length];
            for (int i = 0; i < mParams.Length && i < provided.Length; i++)
            {
                try   { actual[i] = Convert.ChangeType(provided[i], mParams[i].ParameterType); }
                catch { actual[i] = provided[i]; }
            }
            return actual;
        }

        private static string GetStringArg(JsonClass args, string key, string defaultValue)
        {
            var v = args[key]?.Value;
            return string.IsNullOrEmpty(v) ? defaultValue : v;
        }
    }
}
