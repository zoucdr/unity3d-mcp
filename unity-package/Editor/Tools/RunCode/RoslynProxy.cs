/*-*-* Copyright (c) Mycoria@Mycoria
 * Author: zouhunter
 * Creation Date: 2026-02-28
 * Version: 1.0.0
 * Description: 通过反射从 MonoBleedingEdge 加载 Roslyn，无需在项目中放置 Roslyn 程序集
 *_*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniMcp.Tools
{
    /// <summary>
    /// 通过反射封装 Roslyn 编译能力。
    /// Editor 启动时由 [InitializeOnLoad] 从 MonoBleedingEdge\lib\mono\{version}\ 自动加载 Roslyn DLL。
    /// </summary>
    [InitializeOnLoad]
    public static class RoslynProxy
    {
        // ── 状态 ─────────────────────────────────────────────────────────────
        private static bool   _initialized;
        private static string _initError;

        // ── 缓存的 Roslyn 类型 ────────────────────────────────────────────────
        private static Type _tSyntaxTree;
        private static Type _tCompilation;
        private static Type _tOptions;
        private static Type _tOutputKind;
        private static Type _tMetaRef;
        private static Type _tDiagSeverity;

        // ── 缓存的反射方法/构造函数 ───────────────────────────────────────────
        private static MethodInfo      _parseText;
        private static MethodInfo      _createFromFile;
        private static MethodInfo      _compilationCreate;
        private static MethodInfo      _compilationEmit;
        private static ConstructorInfo _optionsCtor;

        // ─────────────────────────────────────────────────────────────────────
        static RoslynProxy() => EnsureLoaded();

        public static bool IsAvailable => _initialized && _initError == null;

        // ── 初始化入口 ────────────────────────────────────────────────────────
        public static void EnsureLoaded()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                LoadAssemblies();
                CacheTypes();
                CacheMethods();
                Debug.Log("[RoslynProxy] Roslyn 反射初始化完成");
            }
            catch (Exception e)
            {
                _initError = e.Message;
                Debug.LogWarning($"[RoslynProxy] 初始化失败: {e}");
            }
        }

        // ── 程序集加载 ────────────────────────────────────────────────────────
        private static void LoadAssemblies()
        {
            string editorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            string monoBase  = Path.Combine(editorDir, "Data", "MonoBleedingEdge", "lib", "mono");

            // 在子目录中找到第一个包含 Microsoft.CodeAnalysis.dll 的目录（按名称升序，4.5 先于字母）
            string roslynDir = Directory.GetDirectories(monoBase)
                .OrderBy(d => Path.GetFileName(d))
                .FirstOrDefault(d => File.Exists(Path.Combine(d, "Microsoft.CodeAnalysis.dll")));

            if (roslynDir == null)
                throw new Exception($"[RoslynProxy] 在 {monoBase} 子目录中未找到 Microsoft.CodeAnalysis.dll");

            string facadesDir = Path.Combine(roslynDir, "Facades");
            Debug.Log($"[RoslynProxy] Roslyn 目录: {roslynDir}");

            var dllPaths = new[]
            {
                Path.Combine(roslynDir,  "Microsoft.CodeAnalysis.dll"),
                Path.Combine(roslynDir,  "Microsoft.CodeAnalysis.CSharp.dll"),
                Path.Combine(roslynDir,  "System.Collections.Immutable.dll"),
                Path.Combine(roslynDir,  "System.Reflection.Metadata.dll"),
                Path.Combine(roslynDir,  "System.Memory.dll"),
                Path.Combine(roslynDir,  "System.Runtime.CompilerServices.Unsafe.dll"),
                Path.Combine(roslynDir,  "System.Numerics.Vectors.dll"),
                Path.Combine(roslynDir,  "System.Threading.Tasks.Extensions.dll"),
                Path.Combine(facadesDir, "System.Buffers.dll"),
            };

            foreach (var path in dllPaths)
            {
                if (!File.Exists(path)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool already = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
                if (already) continue;
                try   { Assembly.LoadFrom(path); }
                catch (Exception e) { Debug.LogWarning($"[RoslynProxy] 加载 {name} 失败: {e.Message}"); }
            }
        }

        // ── 类型/方法缓存 ─────────────────────────────────────────────────────
        private static Assembly GetAsm(string name) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new Exception($"[RoslynProxy] 程序集 {name} 未在 AppDomain 中找到");

        private static void CacheTypes()
        {
            var coreAsm   = GetAsm("Microsoft.CodeAnalysis");
            var csharpAsm = GetAsm("Microsoft.CodeAnalysis.CSharp");

            _tSyntaxTree   = csharpAsm.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
            _tCompilation  = csharpAsm.GetType("Microsoft.CodeAnalysis.CSharp.CSharpCompilation");
            _tOptions      = csharpAsm.GetType("Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions");
            _tOutputKind   = coreAsm.GetType("Microsoft.CodeAnalysis.OutputKind");
            _tMetaRef      = coreAsm.GetType("Microsoft.CodeAnalysis.MetadataReference");
            _tDiagSeverity = coreAsm.GetType("Microsoft.CodeAnalysis.DiagnosticSeverity");
        }

        private static void CacheMethods()
        {
            // CSharpSyntaxTree.ParseText(string text, ...)
            _parseText = _tSyntaxTree
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "ParseText" && m.GetParameters()[0].ParameterType == typeof(string))
                .OrderBy(m => m.GetParameters().Length).First();

            // MetadataReference.CreateFromFile(string path, ...)
            _createFromFile = _tMetaRef
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "CreateFromFile" && m.GetParameters()[0].ParameterType == typeof(string))
                .OrderBy(m => m.GetParameters().Length).First();

            // CSharpCompilation.Create(string assemblyName, ...)
            _compilationCreate = _tCompilation
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Create")
                .OrderBy(m => m.GetParameters().Length).Last();

            // compilation.Emit(Stream stream, ...)
            _compilationEmit = _tCompilation.GetMethods()
                .Where(m => m.Name == "Emit" && m.GetParameters()[0].ParameterType == typeof(Stream))
                .OrderBy(m => m.GetParameters().Length).First();

            // new CSharpCompilationOptions(OutputKind, ...)
            _optionsCtor = _tOptions.GetConstructors()
                .OrderBy(c => c.GetParameters().Length).First();
        }

        // ── 公开 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// 将 C# 代码编译到内存字节数组。
        /// 返回 (成功, 字节数组, 错误列表)
        /// </summary>
        public static (bool success, byte[] bytes, string[] errors) CompileToBytes(string code)
        {
            EnsureLoaded();

            if (!IsAvailable)
                return (false, null, new[] { $"RoslynProxy 未就绪: {_initError}" });

            // ParseText
            var parseArgs  = MakeArgs(_parseText, 0, code);
            var syntaxTree = _parseText.Invoke(null, parseArgs);

            // 收集元数据引用
            var metaRefs = CollectMetadataReferences();

            // 编译选项：DLL 输出 + allowUnsafe
            var optArgs = _optionsCtor.GetParameters().Select(p => {
                if (p.ParameterType == _tOutputKind) return Enum.Parse(_tOutputKind, "DynamicallyLinkedLibrary");
                if (p.Name == "allowUnsafe")          return (object)true;
                return p.HasDefaultValue ? (object)p.DefaultValue : null;
            }).ToArray();
            var options = _optionsCtor.Invoke(optArgs);

            // 构建 SyntaxTree[] 和 MetadataReference[]
            var stArr = Array.CreateInstance(syntaxTree.GetType().BaseType ?? syntaxTree.GetType(), 1);
            stArr.SetValue(syntaxTree, 0);
            var mrArr = Array.CreateInstance(_tMetaRef, metaRefs.Count);
            for (int i = 0; i < metaRefs.Count; i++) mrArr.SetValue(metaRefs[i], i);

            // CSharpCompilation.Create
            var createArgs = _compilationCreate.GetParameters().Select((p, i) => {
                if (i == 0) return (object)("DynAsm_" + Guid.NewGuid().ToString("N"));
                if (i == 1) return (object)stArr;
                if (i == 2) return (object)mrArr;
                if (i == 3) return options;
                return p.HasDefaultValue ? (object)p.DefaultValue : null;
            }).ToArray();
            var compilation = _compilationCreate.Invoke(null, createArgs);

            // Emit
            using (var ms = new MemoryStream())
            {
                var emitArgs   = MakeArgs(_compilationEmit, 0, (Stream)ms);
                var emitResult = _compilationEmit.Invoke(compilation, emitArgs);
                bool success   = (bool)emitResult.GetType().GetProperty("Success").GetValue(emitResult);

                if (!success)
                {
                    var errVal = Enum.Parse(_tDiagSeverity, "Error");
                    var diags  = (System.Collections.IEnumerable)emitResult.GetType()
                        .GetProperty("Diagnostics").GetValue(emitResult);
                    var errorList = new List<string>();
                    foreach (var d in diags)
                        if (d.GetType().GetProperty("Severity").GetValue(d).Equals(errVal))
                            errorList.Add(d.ToString());
                    return (false, null, errorList.ToArray());
                }

                ms.Seek(0, SeekOrigin.Begin);
                return (true, ms.ToArray(), null);
            }
        }

        // ── 元数据引用收集 ────────────────────────────────────────────────────
        private static List<object> CollectMetadataReferences()
        {
            var result     = new List<object>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
                    if (!File.Exists(asm.Location))                           continue;
                    if (!addedPaths.Add(asm.Location))                        continue;

                    var r = InvokeCreateFromFile(asm.Location);
                    if (r != null) result.Add(r);
                }
                catch { }
            }

            // 确保核心程序集存在
            foreach (var loc in new[]
            {
                typeof(object).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(Debug).Assembly.Location,
                typeof(EditorApplication).Assembly.Location,
            })
            {
                if (string.IsNullOrEmpty(loc) || !File.Exists(loc) || !addedPaths.Add(loc)) continue;
                try { var r = InvokeCreateFromFile(loc); if (r != null) result.Add(r); } catch { }
            }

            return result;
        }

        private static object InvokeCreateFromFile(string path)
        {
            var args = MakeArgs(_createFromFile, 0, path);
            return _createFromFile.Invoke(null, args);
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────
        /// <summary>根据方法参数列表生成调用参数数组，将 fixedIndex 位置替换为 fixedValue</summary>
        private static object[] MakeArgs(MethodBase method, int fixedIndex, object fixedValue)
        {
            return method.GetParameters().Select((p, i) =>
                i == fixedIndex ? fixedValue :
                (p.HasDefaultValue ? (object)p.DefaultValue : null)).ToArray();
        }
    }
}
