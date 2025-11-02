using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Unity.Mcp.Executer;

namespace Unity.Mcp.Gui
{
    /// <summary>
    /// MCP连接管理GUI类，提供所有绘制功能的静态方法
    /// 用于在ProjectSettings中显示MCP设置
    /// </summary>
    public static class McpServiceGUI
    {
        // 工具方法列表相关变量
        private static Dictionary<string, bool> methodFoldouts = new Dictionary<string, bool>();
        private static Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>(); // 分组折叠状态
        private static Vector2 methodsScrollPosition;
        private static Dictionary<string, double> methodClickTimes = new Dictionary<string, double>();
        private const double doubleClickTime = 0.3; // 双击判定时间（秒）
        
        // 端口配置相关变量
        private static string portInputString = "";
        private static bool portInputInitialized = false;

        /// <summary>
        /// 绘制完整的MCP设置GUI
        /// </summary>
        public static void DrawGUI()
        {
            // 使用垂直布局管理整个窗口，确保充分利用空间
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity3D MCP Service", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // 端口配置
            DrawPortConfiguration();

            // 重启服务器按钮
            GUIStyle restartButtonStyle = new GUIStyle(GUI.skin.button);
            Color restartOriginalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // 橙色背景

            if (GUILayout.Button("重启服务器", restartButtonStyle, GUILayout.Width(90)))
            {
                RestartServer();
            }

            GUI.backgroundColor = restartOriginalColor;

            // 状态窗口按钮
            if (GUILayout.Button("状态窗口", GUILayout.Width(80)))
            {
                McpServiceStatusWindow.ShowWindow();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 添加工具方法列表 - 让它填充剩余空间
            DrawMethodsList();

            // 结束主垂直布局
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 动态计算工具方法列表的可用高度
        /// </summary>
        private static float CalculateAvailableMethodsHeight()
        {
            // 使用固定高度，因为在ProjectSettings中不需要动态计算
            float windowHeight = 800f;

            // 估算已占用的空间
            float usedHeight = 0f;

            // 标题和按钮区域高度 (约 50px)
            usedHeight += 50f;

            // 工具方法列表标题和间距 (约 50px)
            usedHeight += 50f;

            // 窗口边距和滚动条等 (约 30px)
            usedHeight += 30f;

            // 计算剩余可用高度，至少保留 150px
            float availableHeight = Mathf.Max(windowHeight - usedHeight, 500f);

            return availableHeight;
        }

        /// <summary>
        /// 绘制工具方法列表，支持折叠展开，按分组分类显示，程序集信息显示在方法名后
        /// </summary>
        private static void DrawMethodsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // 标题栏：左侧显示标题，右侧显示调试按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("可用工具方法", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // 工具信息按钮
            GUIStyle toolInfoButtonStyle = new GUIStyle(EditorStyles.miniButton);
            Color toolInfoOriginalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.9f, 0.7f); // 淡橙色背景

            int toolCount = McpService.GetToolCount();
            if (GUILayout.Button($"工具({toolCount})", toolInfoButtonStyle, GUILayout.Width(60)))
            {
                ShowToolDebugInfo();
            }

            GUI.backgroundColor = toolInfoOriginalColor;

            // 调试窗口按钮
            GUIStyle titleDebugButtonStyle = new GUIStyle(EditorStyles.miniButton);
            Color titleOriginalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // 淡蓝色背景

            if (GUILayout.Button("调试窗口", titleDebugButtonStyle, GUILayout.Width(70)))
            {
                // 打开调试窗口（不预填充内容）
                McpDebugWindow.ShowWindow();
            }

            GUI.backgroundColor = titleOriginalColor;
            EditorGUILayout.EndHorizontal();

            // 确保方法已注册
            ToolsCall.EnsureMethodsRegisteredStatic();
            var methodNames = ToolsCall.GetRegisteredMethodNames();

            // 按分组分类方法
            var methodsByGroup = new Dictionary<string, List<(string methodName, IToolMethod method, string assemblyName)>>();

            foreach (var methodName in methodNames)
            {
                var method = ToolsCall.GetRegisteredMethod(methodName);
                if (method == null) continue;

                // 获取分组名称
                string groupName = GetMethodGroupName(method);
                // 获取程序集名称
                string assemblyName = GetAssemblyDisplayName(method.GetType().Assembly);

                if (!methodsByGroup.ContainsKey(groupName))
                {
                    methodsByGroup[groupName] = new List<(string, IToolMethod, string)>();
                }

                methodsByGroup[groupName].Add((methodName, method, assemblyName));
            }

            // 动态计算可用高度并应用到滚动视图
            float availableHeight = CalculateAvailableMethodsHeight();
            methodsScrollPosition = EditorGUILayout.BeginScrollView(methodsScrollPosition,
                GUILayout.Height(availableHeight));

            // 按分组名称排序并绘制
            foreach (var groupKvp in methodsByGroup.OrderBy(kvp => kvp.Key))
            {
                string groupName = groupKvp.Key;
                var methods = groupKvp.Value.OrderBy(m => m.methodName).ToList();

                // 确保分组在折叠字典中有条目
                if (!groupFoldouts.ContainsKey(groupName))
                {
                    groupFoldouts[groupName] = false;
                }

                // 绘制分组折叠标题
                EditorGUILayout.BeginVertical("box");

                GUIStyle groupFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };

                EditorGUILayout.BeginHorizontal();
                groupFoldouts[groupName] = EditorGUILayout.Foldout(
                    groupFoldouts[groupName],
                    $"🔧 {groupName} ({methods.Count})",
                    true,
                    groupFoldoutStyle
                );
                EditorGUILayout.EndHorizontal();

                // 如果分组展开，显示其中的方法
                if (groupFoldouts[groupName])
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUI.indentLevel++;

                    foreach (var (methodName, method, assemblyName) in methods)
                    {
                        // 确保该方法在字典中有一个条目
                        if (!methodFoldouts.ContainsKey(methodName))
                        {
                            methodFoldouts[methodName] = false;
                        }

                        // 绘制方法折叠标题
                        EditorGUILayout.BeginVertical("box");

                        // 折叠标题栏样式
                        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                        {
                            fontStyle = FontStyle.Bold
                        };

                        // 在一行中显示折叠标题、问号按钮和调试按钮
                        EditorGUILayout.BeginHorizontal();

                        // 绘制折叠标题
                        Rect foldoutRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

                        // 计算按钮和程序集标签的位置
                        float buttonWidth = 20f;
                        float buttonHeight = 18f;
                        float padding = 4f; // 增加间距
                        float totalButtonsWidth = (buttonWidth + padding) * 2; // 两个按钮的总宽度

                        // 计算程序集标签宽度
                        string assemblyLabel = $"({assemblyName})";
                        GUIStyle assemblyLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                        // 确保标签有足够的宽度，避免文本被截断
                        float calculatedWidth = assemblyLabelStyle.CalcSize(new GUIContent(assemblyLabel)).x;
                        float assemblyLabelWidth = Mathf.Max(calculatedWidth + padding * 2, 80f); // 最小宽度80px

                        // 从右到左计算各区域位置
                        float rightEdge = foldoutRect.xMax;

                        // 1. 调试按钮区域（最右侧）
                        Rect debugButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            foldoutRect.y + (foldoutRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding);

                        // 2. 问号按钮区域
                        Rect helpButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            foldoutRect.y + (foldoutRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding * 2); // 按钮后增加更多间距

                        // 3. 程序集标签区域
                        Rect assemblyLabelRect = new Rect(
                            rightEdge - assemblyLabelWidth,
                            foldoutRect.y,
                            assemblyLabelWidth,
                            foldoutRect.height
                        );
                        rightEdge -= (assemblyLabelWidth + padding * 2); // 标签后增加更多间距

                        // 4. 折叠标题区域（剩余空间）
                        Rect actualFoldoutRect = new Rect(
                            foldoutRect.x,
                            foldoutRect.y,
                            rightEdge - foldoutRect.x,
                            foldoutRect.height
                        );

                        // 绘制折叠标题（只显示方法名）
                        methodFoldouts[methodName] = EditorGUI.Foldout(
                            actualFoldoutRect,
                            methodFoldouts[methodName],
                            methodName,
                            true,
                            foldoutStyle);

                        // 绘制程序集标签
                        Color originalColor = GUI.color;
                        GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f); // 更淡的灰色

                        // 设置右对齐的标签样式
                        GUIStyle rightAlignedLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                        rightAlignedLabelStyle.alignment = TextAnchor.MiddleRight;

                        EditorGUI.LabelField(assemblyLabelRect, assemblyLabel, rightAlignedLabelStyle);
                        GUI.color = originalColor;

                        // 绘制问号按钮
                        GUIStyle helpButtonStyle = new GUIStyle(EditorStyles.miniButton);

                        if (GUI.Button(helpButtonRect, "?", helpButtonStyle))
                        {
                            // 处理按钮点击事件
                            HandleMethodHelpClick(methodName, method);
                        }

                        // 绘制调试按钮
                        GUIStyle debugButtonStyle = new GUIStyle(EditorStyles.miniButton);
                        Color originalBackgroundColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // 淡蓝色背景

                        if (GUI.Button(debugButtonRect, "T", debugButtonStyle))
                        {
                            // 处理调试按钮点击事件
                            HandleMethodDebugClick(methodName, method);
                        }

                        GUI.backgroundColor = originalBackgroundColor;

                        EditorGUILayout.EndHorizontal();

                        // 如果展开，显示预览信息
                        if (methodFoldouts[methodName])
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                            // === 参数Keys信息部分 ===
                            EditorGUILayout.BeginVertical("box");

                            var keys = method.Keys;
                            if (keys != null && keys.Length > 0)
                            {
                                foreach (var key in keys)
                                {
                                    // 创建参数行的样式
                                    EditorGUILayout.BeginHorizontal();
                                    // 参数名称 - 必需参数用粗体，可选参数用普通字体
                                    GUIStyle keyStyle = EditorStyles.miniBoldLabel;
                                    Color originalKeyColor = GUI.color;

                                    // 必需参数用红色标记，可选参数用灰色标记
                                    GUI.color = key.Optional ? Color.red : Color.green;
                                    // 参数名称
                                    EditorGUILayout.SelectableLabel(key.Key, keyStyle, GUILayout.Width(120), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    GUI.color = originalKeyColor;

                                    // 参数描述
                                    EditorGUILayout.SelectableLabel(key.Desc, keyStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("无参数", EditorStyles.centeredGreyMiniLabel);
                            }

                            EditorGUILayout.EndVertical();

                            // 添加一些间距
                            EditorGUILayout.Space(3);

                            // === 状态树结构部分 ===
                            EditorGUILayout.BeginVertical("box");

                            // 获取预览信息
                            string preview = method.Preview();

                            // 计算文本行数
                            int lineCount = 1;
                            if (!string.IsNullOrEmpty(preview))
                            {
                                lineCount = preview.Split('\n').Length;
                            }

                            // 显示预览信息
                            EditorGUILayout.SelectableLabel(preview, EditorStyles.wordWrappedLabel,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight * lineCount * 0.8f));

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 获取方法的分组名称
        /// </summary>
        /// <param name="method">方法实例</param>
        /// <returns>分组名称</returns>
        private static string GetMethodGroupName(IToolMethod method)
        {
            // 通过反射获取ToolNameAttribute
            var methodType = method.GetType();
            var toolNameAttribute = methodType.GetCustomAttributes(typeof(ToolNameAttribute), false)
                                             .FirstOrDefault() as ToolNameAttribute;

            if (toolNameAttribute != null)
            {
                return toolNameAttribute.GroupName;
            }

            // 如果没有ToolNameAttribute，返回默认分组
            return "未分组";
        }

        /// <summary>
        /// 获取程序集的显示名称
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <returns>程序集显示名称</returns>
        private static string GetAssemblyDisplayName(System.Reflection.Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;

            // Return all names in English
            if (assemblyName.StartsWith("Assembly-CSharp"))
            {
                return "Main Project Assembly";
            }
            else if (assemblyName.StartsWith("Unity.Mcp"))
            {
                return "Unity MCP";
            }
            else if (assemblyName.StartsWith("Unity."))
            {
                return $"Unity Built-in ({assemblyName.Replace("Unity.", "")})";
            }
            else if (assemblyName == "mscorlib" || assemblyName == "System" || assemblyName.StartsWith("System."))
            {
                return ".NET System Library";
            }
            else
            {
                return assemblyName;
            }
        }

        /// <summary>
        /// 处理方法帮助按钮的点击事件
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="method">方法实例</param>
        private static void HandleMethodHelpClick(string methodName, IToolMethod method)
        {
            // 获取当前时间
            double currentTime = EditorApplication.timeSinceStartup;

            // 检查是否存在上次点击时间记录
            if (methodClickTimes.TryGetValue(methodName, out double lastClickTime))
            {
                // 判断是否为双击（时间间隔小于doubleClickTime）
                if (currentTime - lastClickTime < doubleClickTime)
                {
                    // 双击：打开脚本文件
                    OpenMethodScript(method);
                    // 重置点击时间，防止连续多次点击被判定为多个双击
                    methodClickTimes[methodName] = 0;
                    return;
                }
            }

            // 单击：定位到脚本文件
            PingMethodScript(method);
            // 记录本次点击时间
            methodClickTimes[methodName] = currentTime;
        }

        /// <summary>
        /// 在Project窗口中定位到方法所在的脚本文件
        /// </summary>
        /// <param name="method">方法实例</param>
        private static void PingMethodScript(IToolMethod method)
        {
            // 获取方法类型
            Type methodType = method.GetType();

            // 查找脚本资源
            string scriptName = methodType.Name + ".cs";
            string[] guids = AssetDatabase.FindAssets(methodType.Name + " t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    // 在Project窗口中高亮显示该资源
                    UnityEngine.Object scriptObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (scriptObj != null)
                    {
                        EditorGUIUtility.PingObject(scriptObj);
                        return;
                    }
                }
            }

            // 如果没有找到脚本，尝试直接使用类型名称查找
            string[] allScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (string guid in allScriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    UnityEngine.Object scriptObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (scriptObj != null)
                    {
                        EditorGUIUtility.PingObject(scriptObj);
                        return;
                    }
                }
            }

            Debug.LogWarning($"无法在Project窗口中找到脚本: {scriptName}");
        }

        /// <summary>
        /// 打开方法所在的脚本文件
        /// </summary>
        /// <param name="method">方法实例</param>
        private static void OpenMethodScript(IToolMethod method)
        {
            // 获取方法类型
            Type methodType = method.GetType();

            // 查找脚本资源
            string scriptName = methodType.Name + ".cs";
            string[] guids = AssetDatabase.FindAssets(methodType.Name + " t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    // 加载并打开脚本
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null)
                    {
                        AssetDatabase.OpenAsset(script);
                        return;
                    }
                }
            }

            // 如果没有找到脚本，尝试直接使用类型名称查找
            string[] allScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (string guid in allScriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null)
                    {
                        AssetDatabase.OpenAsset(script);
                        return;
                    }
                }
            }

            Debug.LogWarning($"无法打开脚本: {scriptName}");
        }

        /// <summary>
        /// 处理方法调试按钮的点击事件
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="method">方法实例</param>
        private static void HandleMethodDebugClick(string methodName, IToolMethod method)
        {
            try
            {
                // 生成方法调用的示例JSON
                string exampleJson = GenerateMethodExampleJson(methodName, method);

                // 打开McpDebugWindow并预填充示例
                McpDebugWindow.ShowWindowWithContent(exampleJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMcpEditorWindow] 生成调试示例时发生错误: {e}");
                EditorUtility.DisplayDialog("错误", $"无法生成调试示例: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 生成方法调用的示例JSON
        /// </summary>
        /// <param name="methodName">方法名称</param>
        /// <param name="method">方法实例</param>
        /// <returns>示例JSON字符串</returns>
        private static string GenerateMethodExampleJson(string methodName, IToolMethod method)
        {
            try
            {
                var exampleCall = new
                {
                    func = methodName,
                    args = GenerateExampleArgs(method)
                };

                return Json.FromObject(exampleCall);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"生成示例JSON失败，使用基础模板: {e.Message}");

                // 如果生成失败，返回基础模板
                var basicCall = new
                {
                    func = methodName,
                    args = new { }
                };

                return Json.FromObject(basicCall);
            }
        }

        /// <summary>
        /// 生成方法的示例参数
        /// </summary>
        /// <param name="method">方法实例</param>
        /// <returns>示例参数对象</returns>
        private static object GenerateExampleArgs(IToolMethod method)
        {
            var exampleArgs = new Dictionary<string, object>();
            var keys = method.Keys;

            if (keys != null && keys.Length > 0)
            {
                foreach (var key in keys)
                {
                    // 根据参数名和描述生成示例值
                    object exampleValue = GenerateExampleValue(key.Key, key.Desc, key.Optional);
                    if (exampleValue != null)
                    {
                        exampleArgs[key.Key] = exampleValue;
                    }
                }
            }

            return exampleArgs;
        }

        /// <summary>
        /// 根据参数信息生成示例值
        /// </summary>
        /// <param name="keyName">参数名</param>
        /// <param name="description">参数描述</param>
        /// <param name="isOptional">是否可选</param>
        /// <returns>示例值</returns>
        private static object GenerateExampleValue(string keyName, string description, bool isOptional)
        {
            // 转换为小写用于模式匹配
            string lowerKey = keyName.ToLower();
            string lowerDesc = description?.ToLower() ?? "";

            // 根据参数名和描述推断类型和示例值
            switch (lowerKey)
            {
                case "action":
                    return "modify"; // 默认操作

                case "from":
                    return "primitive";

                case "primitive_type":
                    return "Cube";

                case "name":
                    return "ExampleObject";

                case "path":
                    if (lowerDesc.Contains("material"))
                        return "Assets/Materials/ExampleMaterial.mat";
                    if (lowerDesc.Contains("prefab"))
                        return "Assets/Prefabs/ExamplePrefab.prefab";
                    if (lowerDesc.Contains("script"))
                        return "Assets/Scripts/ExampleScript.cs";
                    if (lowerDesc.Contains("texture"))
                        return "Assets/Textures/ExampleTexture.png";
                    return "Assets/Example.asset";

                case "target":
                    return "ExampleTarget";

                case "position":
                    return new float[] { 0, 0, 0 };

                case "rotation":
                    return new float[] { 0, 0, 0 };

                case "scale":
                    return new float[] { 1, 1, 1 };

                case "shader":
                    return "Standard";

                case "properties":
                    if (lowerDesc.Contains("color") || lowerKey.Contains("color"))
                        return new { _Color = new { r = 1.0f, g = 0.0f, b = 0.0f, a = 1.0f } };
                    return new { };

                case "active":
                    return true;

                case "tag":
                    return "Untagged";

                case "layer":
                    return "Default";

                case "component_type":
                    return "Rigidbody";

                case "search_type":
                    return "by_name";

                case "url":
                    return "https://httpbin.org/get";

                case "timeout":
                    return 30;

                case "build_index":
                    return 0;

                case "texture_type":
                    return "Sprite";

                case "mesh_type":
                    return "cube";

                default:
                    // 根据描述内容推断
                    if (lowerDesc.Contains("bool") || lowerDesc.Contains("是否"))
                        return !isOptional; // 必需参数默认true，可选参数默认false

                    if (lowerDesc.Contains("array") || lowerDesc.Contains("list") || lowerDesc.Contains("数组"))
                        return new object[] { };

                    if (lowerDesc.Contains("number") || lowerDesc.Contains("int") || lowerDesc.Contains("数字"))
                        return 0;

                    if (lowerDesc.Contains("float") || lowerDesc.Contains("浮点"))
                        return 0.0f;

                    // 如果是可选参数且无法推断类型，返回null（不添加到参数中）
                    if (isOptional)
                        return null;

                    // 必需参数默认返回空字符串
                    return "";
            }
        }

        /// <summary>
        /// 重启MCP服务器
        /// </summary>
        private static void RestartServer()
        {
            try
            {
                // 显示确认对话框
                bool confirm = EditorUtility.DisplayDialog(
                    "重启MCP服务器",
                    "确定要重启MCP服务器吗？\n\n这将断开所有当前连接的客户端。",
                    "确定",
                    "取消"
                );

                if (!confirm)
                {
                    return;
                }

                // 显示进度条
                EditorUtility.DisplayProgressBar("重启MCP服务器", "正在停止服务器...", 0.3f);

                // 停止服务器
                McpService.StopService();

                // 等待一小段时间确保资源释放
                System.Threading.Thread.Sleep(500);

                EditorUtility.DisplayProgressBar("重启MCP服务器", "正在启动服务器...", 0.7f);

                // 启动服务器
                McpService.StartService();

                // 清除进度条
                EditorUtility.ClearProgressBar();

                // 显示成功提示
                if (McpService.Instance.IsRunning)
                {
                    EditorUtility.DisplayDialog(
                        "重启成功",
                        $"MCP服务器已成功重启！\n\n端口: {McpService.mcpPort}",
                        "确定"
                    );
                    Debug.Log($"[McpServiceGUI] MCP服务器已重启，端口: {McpService.mcpPort}");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "重启失败",
                        "MCP服务器重启失败，请查看控制台日志了解详情。",
                        "确定"
                    );
                    Debug.LogError("[McpServiceGUI] MCP服务器重启失败");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "重启错误",
                    $"重启MCP服务器时发生错误：\n\n{ex.Message}",
                    "确定"
                );
                Debug.LogError($"[McpServiceGUI] 重启MCP服务器时发生错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 绘制端口配置
        /// </summary>
        private static void DrawPortConfiguration()
        {
            // 初始化端口输入字符串
            if (!portInputInitialized)
            {
                portInputString = McpService.mcpPort.ToString();
                portInputInitialized = true;
            }

            // 端口标签
            EditorGUILayout.LabelField("端口:", GUILayout.Width(30));

            // 端口输入框
            GUI.SetNextControlName("PortInput");
            string newPortString = EditorGUILayout.TextField(portInputString, GUILayout.Width(60));
            
            // 检测输入变化
            if (newPortString != portInputString)
            {
                // 只允许数字输入
                if (System.Text.RegularExpressions.Regex.IsMatch(newPortString, @"^\d*$"))
                {
                    portInputString = newPortString;
                }
            }

            // 应用按钮
            bool isValidPort = false;
            int portValue = 0;
            
            if (int.TryParse(portInputString, out portValue))
            {
                isValidPort = McpService.IsValidPort(portValue);
            }

            // 根据端口有效性设置按钮颜色
            Color originalColor = GUI.backgroundColor;
            if (!isValidPort && !string.IsNullOrEmpty(portInputString))
            {
                GUI.backgroundColor = Color.red;
            }
            else if (isValidPort && portValue != McpService.mcpPort)
            {
                GUI.backgroundColor = Color.green;
            }

            bool buttonEnabled = isValidPort && portValue != McpService.mcpPort;
            GUI.enabled = buttonEnabled;

            if (GUILayout.Button("应用", GUILayout.Width(40)))
            {
                if (McpService.SetMcpPort(portValue))
                {
                    Debug.Log($"[McpServiceGUI] 端口已更改为: {portValue}");
                    if (McpService.Instance.IsRunning)
                    {
                        EditorUtility.DisplayDialog("端口更改", $"端口已更改为 {portValue}，服务器已自动重启。", "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("端口错误", $"无效的端口号: {portValue}\n端口范围应为 1024-65535", "确定");
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = originalColor;

            // 端口诊断按钮
            if (GUILayout.Button("诊断", GUILayout.Width(40)))
            {
                ShowPortDiagnostics();
            }

            // 显示端口状态提示
            if (!string.IsNullOrEmpty(portInputString) && !isValidPort)
            {
                Color originalTextColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField("无效", EditorStyles.miniLabel, GUILayout.Width(30));
                GUI.color = originalTextColor;
            }
            else if (isValidPort && portValue == McpService.mcpPort)
            {
                Color originalTextColor = GUI.color;
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("当前", EditorStyles.miniLabel, GUILayout.Width(30));
                GUI.color = originalTextColor;
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(30)); // 占位符保持布局一致
            }
        }

        /// <summary>
        /// 显示工具调试信息
        /// </summary>
        private static void ShowToolDebugInfo()
        {
            var toolNames = McpService.GetAllToolNames();
            int toolCount = McpService.GetToolCount();
            
            string message = $"MCP工具调试信息:\n\n";
            message += $"已注册工具数量: {toolCount}\n\n";
            
            if (toolCount > 0)
            {
                message += "已注册的工具:\n";
                foreach (var toolName in toolNames)
                {
                    message += $"• {toolName}\n";
                }
            }
            else
            {
                message += "⚠️ 没有发现任何工具！\n\n";
                message += "可能的原因:\n";
                message += "1. 工具类没有实现IToolMethod接口\n";
                message += "2. 程序集加载问题\n";
                message += "3. 工具类是抽象类或接口\n";
            }
            
            message += "\n点击'重新发现'按钮重新扫描工具。";
            
            if (EditorUtility.DisplayDialog("MCP工具调试", message, "重新发现", "关闭"))
            {
                Debug.Log("[McpServiceGUI] 开始重新发现工具...");
                McpService.RediscoverTools();
                
                // 重新获取工具信息
                var newToolNames = McpService.GetAllToolNames();
                int newToolCount = McpService.GetToolCount();
                
                string resultMessage = $"重新发现完成!\n\n";
                resultMessage += $"发现工具数量: {newToolCount}\n\n";
                
                if (newToolCount > 0)
                {
                    resultMessage += "发现的工具:\n";
                    foreach (var toolName in newToolNames)
                    {
                        resultMessage += $"• {toolName}\n";
                    }
                }
                
                EditorUtility.DisplayDialog("工具重新发现结果", resultMessage, "确定");
            }
        }

        /// <summary>
        /// 显示端口诊断信息
        /// </summary>
        private static void ShowPortDiagnostics()
        {
            int currentPort = McpService.mcpPort;
            bool isRunning = McpService.Instance.IsRunning;
            
            string message = $"MCP服务器端口诊断:\n\n";
            message += $"当前端口: {currentPort}\n";
            message += $"服务状态: {(isRunning ? "运行中" : "已停止")}\n\n";
            
            // 获取详细的端口状态信息
            string portInfo = McpService.GetPortStatusInfo(currentPort);
            message += portInfo;
            
            message += "\n建议:\n";
            message += "1. 如果端口被占用，尝试更换端口\n";
            message += "2. 检查防火墙是否阻止了连接\n";
            message += "3. 确保Cursor MCP客户端连接到正确的端口\n";
            message += "4. 查看Unity控制台的详细日志\n";
            
            if (EditorUtility.DisplayDialog("端口诊断", message, "测试连接", "关闭"))
            {
                TestMcpConnection();
            }
        }

        /// <summary>
        /// 测试MCP连接
        /// </summary>
        private static async void TestMcpConnection()
        {
            if (!McpService.Instance.IsRunning)
            {
                EditorUtility.DisplayDialog("连接测试", "MCP服务器未运行，无法测试连接。", "确定");
                return;
            }

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    string url = $"http://127.0.0.1:{McpService.mcpPort}/";
                    Debug.Log($"[McpServiceGUI] 测试连接到: {url}");
                    
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    
                    string result = $"连接测试成功!\n\n";
                    result += $"URL: {url}\n";
                    result += $"状态码: {response.StatusCode}\n";
                    result += $"响应长度: {content.Length} 字符\n\n";
                    result += $"响应内容预览:\n{content.Substring(0, Math.Min(200, content.Length))}";
                    
                    if (content.Length > 200)
                    {
                        result += "...";
                    }
                    
                    EditorUtility.DisplayDialog("连接测试结果", result, "确定");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"连接测试失败!\n\n";
                errorMsg += $"错误: {ex.Message}\n\n";
                errorMsg += "可能的原因:\n";
                errorMsg += "1. 服务器未正确启动\n";
                errorMsg += "2. 端口被防火墙阻止\n";
                errorMsg += "3. 网络配置问题\n";
                
                EditorUtility.DisplayDialog("连接测试失败", errorMsg, "确定");
                Debug.LogError($"[McpServiceGUI] 连接测试失败: {ex}");
            }
        }
    }
}