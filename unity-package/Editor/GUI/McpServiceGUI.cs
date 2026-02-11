using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UniMcp.Executer;
using UniMcp.Tools;
using UniMcp;
using Object = UnityEngine.Object;

namespace UniMcp.Gui
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
        private static int groupIndex = 0; // 分组序号

        // 标签页相关变量
        private static int selectedTab = 0; // 0=工具, 1=资源, 2=提示词
        private static Vector2 resourcesScrollPosition;
        private static Vector2 promptsScrollPosition;
        private static Dictionary<int, bool> resourceFoldouts = new Dictionary<int, bool>();
        private static Dictionary<int, bool> promptFoldouts = new Dictionary<int, bool>();

        // MIME类型下拉框相关变量
        private static Dictionary<int, int> resourceMimeTypeSelections = new Dictionary<int, int>(); // 资源索引 -> 选中的MIME类型索引
        private static readonly string[] commonMimeTypes = new string[]
        {
            "application/octet-stream", // 默认值
            "text/plain",
            "text/html",
            "text/css",
            "text/csv",
            "text/markdown",
            "application/json",
            "application/xml",
            "application/yaml",
            "application/javascript",
            "application/typescript",
            "application/pdf",
            "image/png",
            "image/jpeg",
            "image/gif",
            "image/bmp",
            "image/webp",
            "image/svg+xml",
            "image/tiff",
            "audio/mpeg",
            "audio/wav",
            "audio/ogg",
            "audio/flac",
            "video/mp4",
            "video/webm",
            "video/quicktime",
            "application/zip",
            "application/gzip",
            "application/x-tar"
        };


        /// <summary>
        /// 绘制完整的MCP设置GUI
        /// </summary>
        public static void DrawGUI()
        {
            // 使用垂直布局管理整个窗口，确保充分利用空间
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // 美化标题区域 - 添加背景框和渐变效果
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 标题行 - 使用更大的字体和更好的样式
            EditorGUILayout.BeginHorizontal();
            
            // 标题样式
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f) }
            };
            EditorGUILayout.LabelField("⚡ Unity3D MCP Service", titleStyle, GUILayout.ExpandWidth(true));

            // 日志级别下拉菜单 - 美化样式
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(L.T("Log Level", "日志级别"), EditorStyles.miniLabel, GUILayout.Width(60));
            var currentLogLevel = McpLogger.GetLogLevel();
            var newLogLevel = (McpLogger.LogLevel)EditorGUILayout.EnumPopup(currentLogLevel, GUILayout.Width(100));
            if (newLogLevel != currentLogLevel)
            {
                McpLogger.SetLogLevel(newLogLevel);
            }
            EditorGUILayout.EndVertical();

            // 语言切换下拉菜单 - 美化样式
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(L.T("Language", "语言"), EditorStyles.miniLabel, GUILayout.Width(60));
            var currentLanguage = McpService.GetLocalSettings().CurrentLanguage;
            if (string.IsNullOrEmpty(currentLanguage))
            {
                currentLanguage = "中文"; // 默认语言
            }
            string[] languages = new string[] { "中文", "English" };
            int currentIndex = currentLanguage == "English" ? 1 : 0;
            int newIndex = EditorGUILayout.Popup(currentIndex, languages, GUILayout.Width(80));
            if (newIndex != currentIndex)
            {
                string newLanguage = languages[newIndex];
                McpService.GetLocalSettings().CurrentLanguage = newLanguage;
                Debug.Log($"[McpServiceGUI] {L.T("Language switched to", "语言已切换为")}: {newLanguage}");
                
                // 刷新列表：重新发现工具、资源和提示词
                RefreshAllLists();
                
                // 强制刷新 ProjectSettings 窗口
                EditorApplication.delayCall += () =>
                {
                    // 获取当前活动的 EditorWindow
                    var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                    foreach (var window in windows)
                    {
                        // 刷新 ProjectSettings 窗口和其他可能的窗口
                        window.Repaint();
                    }
                    
                    // 重新发现工具以更新描述文本
                    McpService.RediscoverTools();
                };
            }
            EditorGUILayout.EndVertical();

            // 状态窗口按钮 - 美化按钮样式
            GUIStyle statusButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(8, 8, 4, 4),
                fontSize = 11
            };
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.8f); // 淡蓝色
            
            if (GUILayout.Button(L.T("📊 Status Window", "📊 状态窗口"), statusButtonStyle, GUILayout.Width(110), GUILayout.Height(22)))
            {
                McpServiceStatusWindow.ShowWindow();
            }
            
            GUI.backgroundColor = originalBgColor;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // 绘制标签页
            DrawTabs();

            EditorGUILayout.Space(4);

            // 根据选中的标签页绘制相应内容
            switch (selectedTab)
            {
                case 0:
                    DrawMethodsList();
                    break;
                case 1:
                    DrawResourcesList();
                    break;
                case 2:
                    DrawPromptsList();
                    break;
            }

            // 结束主垂直布局
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制标签页
        /// </summary>
        private static void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 4, 4)
            };

            Color originalColor = GUI.backgroundColor;

            // 工具标签
            if (selectedTab == 0)
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.8f);
            if (GUILayout.Button(L.T("🔧 Tools", "🔧 工具"), tabStyle))
                selectedTab = 0;
            GUI.backgroundColor = originalColor;

            // 资源标签
            if (selectedTab == 1)
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.8f);
            if (GUILayout.Button(L.T("📦 Resources", "📦 资源"), tabStyle))
                selectedTab = 1;
            GUI.backgroundColor = originalColor;

            // 提示词标签
            if (selectedTab == 2)
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f, 0.8f);
            if (GUILayout.Button(L.T("💬 Prompts", "💬 提示词"), tabStyle))
                selectedTab = 2;
            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();
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
            // 使用更美观的背景框
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // 美化标题栏：添加背景色和更好的布局
            Rect headerRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28));
            
            // 绘制标题背景渐变效果
            Color headerBgColor = new Color(0.25f, 0.25f, 0.3f, 0.3f);
            EditorGUI.DrawRect(headerRect, headerBgColor);
            
            // 标题样式
            GUIStyle headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) },
                padding = new RectOffset(8, 0, 4, 0)
            };
            EditorGUILayout.LabelField(L.T("🔧 Available Tools", "🔧 可用工具方法"), headerTitleStyle, GUILayout.ExpandWidth(true));

            // 工具信息按钮 - 美化样式
            GUIStyle toolInfoButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 4, 4),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Color toolInfoOriginalColor = GUI.backgroundColor;
            
            int totalToolCount = McpService.GetToolCount();
            int enabledToolCount = McpService.GetEnabledToolCount();
            string toolButtonText = enabledToolCount == totalToolCount ?
                $"✅ {L.T("Tools", "工具")}({enabledToolCount})" :
                $"⚠️ {L.T("Tools", "工具")}({enabledToolCount}/{totalToolCount})";
            
            // 根据启用状态设置按钮颜色
            if (enabledToolCount == totalToolCount)
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.8f); // 绿色 - 全部启用
            }
            else if (enabledToolCount > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.85f, 0.4f, 0.8f); // 橙色 - 部分启用
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.8f); // 红色 - 全部禁用
            }

            if (GUILayout.Button(toolButtonText, toolInfoButtonStyle, GUILayout.Width(100), GUILayout.Height(22)))
            {
                ShowToolDebugInfo();
            }

            GUI.backgroundColor = toolInfoOriginalColor;

            // 调试窗口按钮 - 美化样式
            GUIStyle titleDebugButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 4, 4),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Color titleOriginalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 0.7f, 1f, 0.8f); // 更柔和的蓝色

            if (GUILayout.Button(L.T("🐛 Debug Window", "🐛 调试窗口"), titleDebugButtonStyle, GUILayout.Width(110), GUILayout.Height(22)))
            {
                // 打开调试窗口（不预填充内容）
                McpDebugWindow.ShowWindow();
            }

            GUI.backgroundColor = titleOriginalColor;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(4);

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

            // 滚动视图填充剩余空间
            methodsScrollPosition = EditorGUILayout.BeginScrollView(methodsScrollPosition,
                GUILayout.ExpandHeight(true));

            // 重置分组序号
            groupIndex = 0;
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

                // 检查组内工具的启用状态
                bool hasEnabledTools = methods.Any(m => McpService.GetLocalSettings().IsToolEnabled(m.methodName));
                bool allToolsEnabled = methods.All(m => McpService.GetLocalSettings().IsToolEnabled(m.methodName));

                // 确定组开关的状态：全部启用时为true，部分启用时为mixed，全部禁用时为false
                bool groupToggleState = allToolsEnabled;

                // 绘制分组折叠标题 - 不使用背景色，避免文字模糊
                EditorGUILayout.BeginVertical("box");

                GUIStyle groupFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                    // 使用默认padding，确保展开箭头可见
                };

                // 根据启用状态设置文字颜色
                if (allToolsEnabled)
                {
                    groupFoldoutStyle.normal.textColor = new Color(0.2f, 0.7f, 0.3f);
                    groupFoldoutStyle.onNormal.textColor = new Color(0.2f, 0.7f, 0.3f);
                }
                else if (hasEnabledTools)
                {
                    groupFoldoutStyle.normal.textColor = new Color(0.8f, 0.7f, 0.2f);
                    groupFoldoutStyle.onNormal.textColor = new Color(0.8f, 0.7f, 0.2f);
                }
                else
                {
                    groupFoldoutStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
                    groupFoldoutStyle.onNormal.textColor = new Color(0.9f, 0.3f, 0.3f);
                }
                groupFoldoutStyle.focused.textColor = groupFoldoutStyle.normal.textColor;
                groupFoldoutStyle.onFocused.textColor = groupFoldoutStyle.normal.textColor;

                // 使用GetControlRect手动布局，确保位置正确
                Rect groupRowRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                
                // 计算各个元素的宽度和位置
                float groupToggleWidth = 22f;
                float groupSpacing = 4f;
                
                // 绘制组开关（最左侧）- 美化样式
                Color originalBackgroundColor = GUI.backgroundColor;
                if (allToolsEnabled)
                {
                    GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.6f); // 绿色
                }
                else if (hasEnabledTools)
                {
                    GUI.backgroundColor = new Color(1f, 0.9f, 0.4f, 0.6f); // 黄色
                }
                else
                {
                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.6f); // 红色
                }

                Rect groupToggleRect = new Rect(groupRowRect.x, groupRowRect.y + (groupRowRect.height - 20) / 2, groupToggleWidth, 20);
                bool newGroupToggleState = EditorGUI.Toggle(groupToggleRect, groupToggleState);

                GUI.backgroundColor = originalBackgroundColor;

                // 处理组开关状态变化
                if (newGroupToggleState != groupToggleState)
                {
                    // 使用批量设置方法来提高效率
                    var toolNames = methods.Select(m => m.methodName).ToList();
                    McpService.GetLocalSettings().SetToolsEnabled(toolNames, newGroupToggleState);

                    string statusText = newGroupToggleState ? L.T("enabled", "启用") : L.T("disabled", "禁用");
                    Debug.Log($"[McpServiceGUI] {L.T("Tool group", "工具组")} '{groupName}' {L.T("all tools", "所有工具")}{statusText}");
                }

                groupIndex++;
                
                // 添加状态图标
                string statusIcon = allToolsEnabled ? "✅" : (hasEnabledTools ? "⚠️" : "❌");
                
                // ========== 【一级标题点击展开区域 - 可手动修改】 ==========
                // 计算foldout的位置（在toggle右边）
                float groupFoldoutStartX = groupToggleRect.xMax + groupSpacing;  // ← 修改这里调整展开区域起始位置
                float groupFoldoutWidth = groupRowRect.xMax - groupFoldoutStartX; // ← 修改这里调整展开区域宽度
                
                Rect groupFoldoutRect = new Rect(
                    groupFoldoutStartX,      // ← 展开区域X坐标
                    groupRowRect.y,          // ← 展开区域Y坐标
                    groupFoldoutWidth,       // ← 展开区域宽度
                    groupRowRect.height      // ← 展开区域高度
                );
                // ========== 【一级标题点击展开区域 - 可手动修改】 ==========
                
                // 绘制foldout，确保展开箭头可见
                // 注意：foldoutRect定义了可点击展开的区域范围
                groupFoldouts[groupName] = EditorGUI.Foldout(
                    groupFoldoutRect,        // ← 这个Rect定义了可点击展开的区域
                    groupFoldouts[groupName],
                    $"{statusIcon} {groupIndex}. {groupName} ({methods.Count})",
                    true,
                    groupFoldoutStyle
                );

                // 如果分组展开，显示其中的方法
                if (groupFoldouts[groupName])
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUI.indentLevel++;
                    int methodIndex = 0;
                    foreach (var (methodName, method, assemblyName) in methods)
                    {
                        methodIndex++;
                        // 确保该方法在字典中有一个条目
                        if (!methodFoldouts.ContainsKey(methodName))
                        {
                            methodFoldouts[methodName] = false;
                        }

                        // 绘制方法折叠标题 - 不使用背景色，避免文字模糊
                        EditorGUILayout.BeginVertical("box");
                        
                        // 获取工具启用状态
                        bool toolEnabled = McpService.GetLocalSettings().IsToolEnabled(methodName);

                        // 折叠标题栏样式
                        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 11,
                            padding = new RectOffset(0, 0, 1, 1), // 移除左padding，避免覆盖toggle
                            contentOffset = new Vector2(0, 0) // 确保内容不偏移
                        };

                        // 根据启用状态设置文字颜色
                        if (toolEnabled)
                        {
                            foldoutStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
                            foldoutStyle.onNormal.textColor = new Color(0.7f, 0.9f, 0.7f);
                        }
                        else
                        {
                            foldoutStyle.normal.textColor = new Color(0.9f, 0.6f, 0.6f);
                            foldoutStyle.onNormal.textColor = new Color(0.9f, 0.6f, 0.6f);
                        }
                        foldoutStyle.focused.textColor = foldoutStyle.normal.textColor;
                        foldoutStyle.onFocused.textColor = foldoutStyle.normal.textColor;

                        // 在一行中显示开关、折叠标题、程序集标签、问号按钮和调试按钮
                        // 使用GetControlRect获取整行的Rect，然后手动绘制各个元素
                        Rect rowRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                        
                        // 计算各个元素的宽度和位置
                        float toggleWidth = 22f;
                        float spacing = 4f;
                        float buttonWidth = 20f;
                        float buttonHeight = 18f;
                        float padding = 6f;

                        // 计算程序集标签宽度
                        string assemblyLabel = $"({assemblyName})";
                        GUIStyle assemblyLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                        float calculatedWidth = assemblyLabelStyle.CalcSize(new GUIContent(assemblyLabel)).x;
                        float assemblyLabelWidth = Mathf.Max(calculatedWidth + padding * 2, 90f);

                        // 从右到左计算各区域位置
                        float rightEdge = rowRect.xMax;
                        
                        // 1. 调试按钮区域（最右侧）
                        Rect debugButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            rowRect.y + (rowRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding);

                        // 2. 问号按钮区域
                        Rect helpButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            rowRect.y + (rowRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding);

                        // 3. 程序集标签区域
                        Rect assemblyLabelRect = new Rect(
                            rightEdge - assemblyLabelWidth,
                            rowRect.y,
                            assemblyLabelWidth,
                            rowRect.height
                        );
                        rightEdge -= (assemblyLabelWidth + padding * 2);

                        // ========== 【二级标题点击展开区域 - 可手动修改】 ==========
                        // 4. 折叠标题区域（剩余空间）
                        float minFoldoutWidth = 100f;
                        float foldoutStartX = rowRect.x + toggleWidth + spacing; // ← 修改这里调整展开区域起始位置（toggle宽度 + 间距）
                        float foldoutAvailableWidth = rightEdge - foldoutStartX; // ← 修改这里调整展开区域可用宽度
                        
                        if (foldoutAvailableWidth < minFoldoutWidth)
                        {
                            // 如果空间不够，缩小程序集标签宽度
                            float reduction = minFoldoutWidth - foldoutAvailableWidth;
                            assemblyLabelWidth = Mathf.Max(assemblyLabelWidth - reduction, 60f);
                            assemblyLabelRect.width = assemblyLabelWidth;
                            assemblyLabelRect.x = rightEdge - assemblyLabelWidth;
                            foldoutAvailableWidth = minFoldoutWidth;
                        }

                        Rect foldoutRect = new Rect(
                            foldoutStartX,          // ← 展开区域X坐标
                            rowRect.y,              // ← 展开区域Y坐标
                            foldoutAvailableWidth,  // ← 展开区域宽度
                            rowRect.height          // ← 展开区域高度
                        );
                        // ========== 【二级标题点击展开区域 - 可手动修改】 ==========

                        // 绘制toggle（最左侧）
                        Color toggleOriginalBg = GUI.backgroundColor;
                        if (toolEnabled)
                        {
                            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.5f);
                        }
                        else
                        {
                            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.5f);
                        }
                        Rect toggleRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - 18) / 2, toggleWidth + 10, 18);
                        bool newToolEnabled = EditorGUI.Toggle(toggleRect, toolEnabled);
                        GUI.backgroundColor = toggleOriginalBg;

                        // 绘制foldout（在toggle右边）
                        // 注意：foldoutRect定义了可点击展开的区域范围
                        methodFoldouts[methodName] = EditorGUI.Foldout(
                            foldoutRect,        // ← 这个Rect定义了可点击展开的区域
                            methodFoldouts[methodName],
                            $" {methodName}",
                            true,
                            groupFoldoutStyle);

                        // 绘制程序集标签 - 美化样式
                        Color originalColor = GUI.color;
                        GUI.color = new Color(0.5f, 0.65f, 0.8f, 0.9f); // 淡蓝色

                        // 设置右对齐的标签样式
                        GUIStyle rightAlignedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleRight,
                            fontStyle = FontStyle.Italic,
                            fontSize = 9
                        };

                        EditorGUI.LabelField(assemblyLabelRect, assemblyLabel, rightAlignedLabelStyle);
                        GUI.color = originalColor;

                        // 处理工具开关状态变化
                        if (newToolEnabled != toolEnabled)
                        {
                            McpService.GetLocalSettings().SetToolEnabled(methodName, newToolEnabled);

                            // 如果工具状态发生变化，可以选择性地重新发现工具或更新工具列表
                            // 这里我们只是记录变化，实际的过滤会在McpService中进行
                            string statusText = newToolEnabled ? L.T("enabled", "启用") : L.T("disabled", "禁用");
                            Debug.Log($"[McpServiceGUI] {L.T("Tool", "工具")} '{methodName}' {L.T("status changed to", "状态已更改为")}: {statusText}");
                        }

                        // 绘制问号按钮 - 美化样式
                        GUIStyle helpButtonStyle = new GUIStyle(EditorStyles.miniButton)
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(0, 0, 0, 0)
                        };
                        Color helpButtonOriginalBg = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f, 0.7f); // 淡蓝色

                        if (GUI.Button(helpButtonRect, "?", helpButtonStyle))
                        {
                            // 处理按钮点击事件
                            HandleMethodHelpClick(methodName, method);
                        }
                        GUI.backgroundColor = helpButtonOriginalBg;

                        // 绘制调试按钮 - 美化样式
                        GUIStyle debugButtonStyle = new GUIStyle(EditorStyles.miniButton)
                        {
                            fontSize = 10,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(0, 0, 0, 0)
                        };
                        originalBackgroundColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.5f, 0.7f, 0.95f, 0.8f); // 更柔和的蓝色背景

                        if (GUI.Button(debugButtonRect, "D", debugButtonStyle))
                        {
                            // 处理调试按钮点击事件
                            HandleMethodDebugClick(methodName, method);
                        }

                        GUI.backgroundColor = originalBackgroundColor;

                        // 如果展开，显示预览信息
                        if (methodFoldouts[methodName])
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUILayout.Space(4);

                            // === 参数Keys信息部分 ===
                            EditorGUILayout.LabelField(L.T("📋 Parameters", "📋 参数信息"), EditorStyles.boldLabel);
                            EditorGUILayout.Space(2);
                            
                            Rect paramsBoxRect = EditorGUILayout.BeginVertical("box");
                            Color paramsBgColor = new Color(0.2f, 0.25f, 0.3f, 0.2f);
                            EditorGUI.DrawRect(paramsBoxRect, paramsBgColor);

                            var keys = method.Keys;
                            if (keys != null && keys.Length > 0)
                            {
                                foreach (var key in keys)
                                {
                                    // 创建参数行的样式
                                    EditorGUILayout.BeginHorizontal();
                                    
                                    // 参数名称 - 美化样式
                                    GUIStyle keyStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                                    {
                                        fontSize = 10,
                                        padding = new RectOffset(4, 4, 2, 2)
                                    };
                                    Color originalKeyColor = GUI.color;

                                    // 必需参数用绿色标记，可选参数用橙色标记
                                    if (key.Optional)
                                    {
                                        GUI.color = new Color(1f, 0.7f, 0.3f); // 橙色 - 可选
                                        EditorGUILayout.LabelField("○", EditorStyles.miniLabel, GUILayout.Width(12));
                                    }
                                    else
                                    {
                                        GUI.color = new Color(0.3f, 0.8f, 0.4f); // 绿色 - 必需
                                        EditorGUILayout.LabelField("●", EditorStyles.miniLabel, GUILayout.Width(12));
                                    }
                                    
                                    // 参数名称
                                    EditorGUILayout.SelectableLabel(key.Key, keyStyle, GUILayout.Width(100), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    GUI.color = originalKeyColor;

                                    // 参数描述 - 美化样式
                                    GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                                    {
                                        fontSize = 9,
                                        fontStyle = FontStyle.Italic,
                                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                                        padding = new RectOffset(4, 4, 2, 2)
                                    };
                                    EditorGUILayout.SelectableLabel(key.Desc, descStyle, GUILayout.Width(180), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                                    var paramJson = new JsonClass();

                                    var enumValues = key.EnumValues;
                                    if (enumValues != null && enumValues.Count > 0)
                                    {
                                        var enumArray = new JsonArray();
                                        foreach (var v in enumValues)
                                        {
                                            enumArray.Add(v);
                                        }
                                        paramJson["enum"] = enumArray;
                                    }

                                    var examples = key.Examples;
                                    if (examples != null && examples.Count > 0)
                                    {
                                        var examplesArray = new JsonArray();
                                        foreach (var ex in examples)
                                        {
                                            examplesArray.Add(ex);
                                        }
                                        paramJson["examples"] = examplesArray;
                                    }

                                    var type = key.Type ?? key.GetType().Name;
                                    paramJson["type"] = type;

                                    // Json字符串美化
                                    string paramJsonStr = paramJson.ToString();

                                    // 使用word wrap多行显示JSON - 美化样式
                                    GUIStyle jsonStyle = new GUIStyle(EditorStyles.miniLabel)
                                    {
                                        fontSize = 9,
                                        normal = { textColor = new Color(0.6f, 0.8f, 1f) },
                                        wordWrap = true,
                                        padding = new RectOffset(4, 4, 2, 2)
                                    };
                                    EditorGUILayout.SelectableLabel(paramJsonStr, jsonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    EditorGUILayout.EndHorizontal();
                                    
                                    EditorGUILayout.Space(2);

                                }
                            }
                            else
                            {
                                GUIStyle noParamsStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                                {
                                    fontSize = 10,
                                    fontStyle = FontStyle.Italic
                                };
                                EditorGUILayout.LabelField(L.T("📭 No Parameters", "📭 无参数"), noParamsStyle);
                            }

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(6);

                            // === 状态树结构部分 ===
                            EditorGUILayout.LabelField(L.T("📄 Preview", "📄 预览信息"), EditorStyles.boldLabel);
                            EditorGUILayout.Space(2);
                            
                            Rect previewBoxRect = EditorGUILayout.BeginVertical("box");
                            Color previewBgColor = new Color(0.2f, 0.25f, 0.3f, 0.2f);
                            EditorGUI.DrawRect(previewBoxRect, previewBgColor);

                            // 获取预览信息
                            string preview = method.Preview();

                            // 计算文本行数
                            int lineCount = 1;
                            if (!string.IsNullOrEmpty(preview))
                            {
                                lineCount = preview.Split('\n').Length;
                            }

                            // 显示预览信息 - 美化样式
                            GUIStyle previewStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                            {
                                fontSize = 10,
                                normal = { textColor = new Color(0.85f, 0.85f, 0.9f) },
                                padding = new RectOffset(6, 6, 4, 4),
                                wordWrap = true
                            };
                            EditorGUILayout.SelectableLabel(preview, previewStyle,
                            GUILayout.ExpandWidth(true),
                            GUILayout.Height(EditorGUIUtility.singleLineHeight * Mathf.Max(lineCount * 0.9f, 2f)));

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(4);
                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
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
            return L.T("Ungrouped", "未分组");
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
            else if (assemblyName.StartsWith("UniMcp"))
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

            Debug.LogWarning($"[McpServiceGUI] {L.T("Unable to find script in Project window", "无法在Project窗口中找到脚本")}: {scriptName}");
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

            Debug.LogWarning($"[McpServiceGUI] {L.T("Unable to open script", "无法打开脚本")}: {scriptName}");
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
                Debug.LogError($"[McpServiceGUI] {L.T("Error generating debug example", "生成调试示例时发生错误")}: {e}");
                EditorUtility.DisplayDialog(
                    L.T("Error", "错误"), 
                    $"{L.T("Unable to generate debug example", "无法生成调试示例")}: {e.Message}", 
                    L.T("OK", "确定"));
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
                // 生成普通的同步调用格式（不使用异步）
                var exampleCall = new JsonClass
                {
                    { "func", new JsonData(methodName) },
                    { "args", Json.Parse(Json.FromObject(GenerateExampleArgs(method))) }
                };

                return exampleCall.ToPrettyString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[McpServiceGUI] {L.T("Failed to generate example JSON, using basic template", "生成示例JSON失败，使用基础模板")}: {e.Message}");

                // 如果生成失败，返回基础模板
                var basicCall = new JsonClass
                {
                    { "func", new JsonData(methodName) },
                    { "args", new JsonClass() }
                };

                return basicCall.ToPrettyString();
            }
        }

        /// <summary>
        /// 生成方法的示例参数
        /// </summary>
        /// <param name="method">方法实例</param>
        /// <returns>示例参数对象</returns>
        private static object GenerateExampleArgs(IToolMethod method)
        {
            var exampleArgs = new JsonClass();
            var keys = method.Keys;

            if (keys != null && keys.Length > 0)
            {
                foreach (var key in keys)
                {
                    // 生成全量参数，包括可选参数
                    // 根据 MethodKey 的真实信息生成示例值
                    JsonNode exampleValue = GenerateExampleValueFromKey(key);
                    if (exampleValue != null)
                    {
                        exampleArgs[key.Key] = exampleValue;
                    }
                }
            }

            return exampleArgs;
        }

        /// <summary>
        /// 根据 MethodKey 的真实信息生成示例值
        /// </summary>
        /// <param name="key">MethodKey 对象</param>
        /// <returns>示例值</returns>
        private static JsonNode GenerateExampleValueFromKey(MethodKey key)
        {
            // 1. 优先使用 DefaultValue（但排除空字符串和无效对象）
            if (key.DefaultValue != null)
            {
                // 如果是空字符串，忽略这个默认值
                if (key.DefaultValue is string str && string.IsNullOrEmpty(str))
                {
                    // 跳过，继续尝试其他来源
                }
                // 如果是有效的基本类型或数组，使用它
                else if (IsValidDefaultValue(key.DefaultValue))
                {
                    JsonNode converted = ConvertToJsonNode(key.DefaultValue);
                    if (converted != null)
                        return converted;
                }
            }

            // 2. 如果有 EnumValues，使用第一个枚举值
            if (key.EnumValues != null && key.EnumValues.Count > 0)
            {
                return new JsonData(key.EnumValues[0]);
            }

            // 3. 如果有 Examples，使用第一个示例值
            if (key.Examples != null && key.Examples.Count > 0)
            {
                string example = key.Examples[0];
                
                // 检查是否看起来像 JSON（以 { 或 [ 开头）
                string trimmedExample = example.Trim();
                if (trimmedExample.StartsWith("{") || trimmedExample.StartsWith("["))
                {
                    // 尝试解析为 JSON
                    try
                    {
                        JsonNode parsed = Json.Parse(example);
                        if (parsed != null)
                            return parsed;
                    }
                    catch
                    {
                        // JSON 解析失败，作为字符串返回
                    }
                }
                
                // 不是 JSON 格式或解析失败，作为字符串返回
                return new JsonData(example);
            }

            // 4. 根据类型生成默认值
            return GenerateDefaultValueByType(key);
        }

        /// <summary>
        /// 检查 DefaultValue 是否是有效的值类型
        /// </summary>
        private static bool IsValidDefaultValue(object value)
        {
            if (value == null)
                return false;

            // 接受的类型：基本类型、数组、字符串
            Type type = value.GetType();
            return type.IsPrimitive || type.IsArray || type == typeof(string) || type == typeof(decimal);
        }

        /// <summary>
        /// 将对象转换为 JsonNode
        /// </summary>
        private static JsonNode ConvertToJsonNode(object value)
        {
            if (value == null)
                return null;

            // 处理数组类型
            if (value is Array array)
            {
                var jsonArray = new JsonArray();
                foreach (var item in array)
                {
                    if (item is float f)
                        jsonArray.Add(new JsonData(f));
                    else if (item is int i)
                        jsonArray.Add(new JsonData(i));
                    else if (item is double d)
                        jsonArray.Add(new JsonData(d));
                    else if (item is string s)
                        jsonArray.Add(new JsonData(s));
                    else if (item is bool b)
                        jsonArray.Add(new JsonData(b));
                    else
                        jsonArray.Add(new JsonData(item.ToString()));
                }
                return jsonArray;
            }

            // 处理基本类型
            if (value is string str)
                return new JsonData(str);
            if (value is int intVal)
                return new JsonData(intVal);
            if (value is float floatVal)
                return new JsonData(floatVal);
            if (value is double doubleVal)
                return new JsonData(doubleVal);
            if (value is bool boolVal)
                return new JsonData(boolVal);

            // 其他类型尝试序列化
            try
            {
                return Json.Parse(Json.FromObject(value));
            }
            catch
            {
                return new JsonData(value.ToString());
            }
        }

        /// <summary>
        /// 根据 MethodKey 类型生成默认值
        /// </summary>
        private static JsonNode GenerateDefaultValueByType(MethodKey key)
        {
            string typeName = key.GetType().Name;
            string keyType = key.Type?.ToLower() ?? "";

            // 根据 MethodKey 的具体类型生成默认值
            if (typeName == "MethodBool")
            {
                return new JsonData(false);
            }
            else if (typeName == "MethodInt")
            {
                return new JsonData(0);
            }
            else if (typeName == "MethodFloat")
            {
                return new JsonData(0.0f);
            }
            else if (typeName == "MethodVector")
            {
                // Vector 默认为 [0, 0, 0]
                var arr = new JsonArray();
                arr.Add(new JsonData(0));
                arr.Add(new JsonData(0));
                arr.Add(new JsonData(0));
                return arr;
            }
            else if (typeName == "MethodArr")
            {
                return new JsonArray();
            }
            else if (typeName == "MethodObj")
            {
                return new JsonClass();
            }
            else if (keyType == "string")
            {
                return new JsonData("");
            }
            else if (keyType == "number" || keyType == "integer")
            {
                return new JsonData(0);
            }
            else if (keyType == "boolean")
            {
                return new JsonData(false);
            }
            else if (keyType == "array")
            {
                return new JsonArray();
            }
            else if (keyType == "object")
            {
                return new JsonClass();
            }

            // 默认返回空字符串
            return new JsonData("");
        }

        /// <summary>
        /// 显示工具调试信息
        /// </summary>
        private static void ShowToolDebugInfo()
        {
            var allToolNames = McpService.GetAllToolNames();
            int totalToolCount = McpService.GetToolCount();

            // 筛选出启用的工具
            var enabledToolNames = allToolNames.Where(toolName =>
                McpService.GetLocalSettings().IsToolEnabled(toolName)).ToList();
            int enabledToolCount = enabledToolNames.Count;

            string message = $"{L.T("MCP Tool Debug Information", "MCP工具调试信息")}:\n\n";
            message += $"{L.T("Total registered tools", "已注册工具总数")}: {totalToolCount}\n";
            message += $"{L.T("Enabled tools count", "已启用工具数量")}: {enabledToolCount}\n\n";

            if (enabledToolCount > 0)
            {
                message += $"{L.T("Enabled tools", "已启用的工具")}:\n";
                foreach (var toolName in enabledToolNames)
                {
                    message += $"• {toolName}\n";
                }
            }
            else
            {
                message += $"⚠️ {L.T("No tools enabled!", "没有启用任何工具！")}\n\n";
                message += $"{L.T("Possible reasons", "可能的原因")}:\n";
                message += $"1. {L.T("All tools have been manually disabled", "所有工具都被手动禁用了")}\n";
                message += $"2. {L.T("Tool configuration has issues", "工具配置设置有问题")}\n";
                message += $"3. {L.T("Need to rediscover tools", "需要重新发现工具")}\n";
            }

            if (totalToolCount > enabledToolCount)
            {
                message += $"\n💡 {L.T("Tip", "提示")}: {L.T("There are", "还有")} {totalToolCount - enabledToolCount} {L.T("tools disabled", "个工具被禁用")}";
            }

            message += $"\n\n{L.T("Click 'Rediscover' button to rescan tools.", "点击'重新发现'按钮重新扫描工具。")}";

            if (EditorUtility.DisplayDialog(
                L.T("MCP Tool Debug", "MCP工具调试"), 
                message, 
                L.T("Rediscover", "重新发现"), 
                L.T("Close", "关闭")))
            {
                Debug.Log($"[McpServiceGUI] {L.T("Starting tool rediscovery...", "开始重新发现工具...")}");
                McpService.RediscoverTools();

                // 重新获取工具信息
                var newAllToolNames = McpService.GetAllToolNames();
                int newTotalToolCount = McpService.GetToolCount();

                var newEnabledToolNames = newAllToolNames.Where(toolName =>
                    McpService.GetLocalSettings().IsToolEnabled(toolName)).ToList();
                int newEnabledToolCount = newEnabledToolNames.Count;

                string resultMessage = $"{L.T("Rediscovery completed!", "重新发现完成!")}\n\n";
                resultMessage += $"{L.T("Total tools found", "发现工具总数")}: {newTotalToolCount}\n";
                resultMessage += $"{L.T("Enabled tools count", "启用工具数量")}: {newEnabledToolCount}\n\n";

                if (newEnabledToolCount > 0)
                {
                    resultMessage += $"{L.T("Enabled tools", "启用的工具")}:\n";
                    foreach (var toolName in newEnabledToolNames)
                    {
                        resultMessage += $"• {toolName}\n";
                    }
                }

                EditorUtility.DisplayDialog(
                    L.T("Tool Rediscovery Result", "工具重新发现结果"), 
                    resultMessage, 
                    L.T("OK", "确定"));
            }
        }

        /// <summary>
        /// 绘制资源列表配置界面
        /// </summary>
        private static void DrawResourcesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // 标题栏
            Rect headerRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28));
            Color headerBgColor = new Color(0.25f, 0.25f, 0.3f, 0.3f);
            EditorGUI.DrawRect(headerRect, headerBgColor);

            GUIStyle headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) },
                padding = new RectOffset(8, 0, 4, 0)
            };
            EditorGUILayout.LabelField(L.T("📦 Configurable Resources", "📦 可配置资源"), headerTitleStyle, GUILayout.ExpandWidth(true));

            // 添加资源按钮
            GUIStyle addButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 4, 4),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Color addButtonColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.8f);

            if (GUILayout.Button(L.T("➕ Add Resource", "➕ 添加资源"), addButtonStyle, GUILayout.Width(110), GUILayout.Height(22)))
            {
                var settings = McpSettings.Instance;
                var newResource = new UniMcp.ConfigurableResource(
                    L.T("New Resource", "新资源"), 
                    L.T("Resource Description", "资源描述"), 
                    "https://example.com/resource");
                settings.AddConfigurableResource(newResource);
                settings.SaveSettings();
            }

            GUI.backgroundColor = addButtonColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 滚动视图
            resourcesScrollPosition = EditorGUILayout.BeginScrollView(resourcesScrollPosition, GUILayout.ExpandHeight(true));

            var settings2 = McpSettings.Instance;
            var resources = settings2.GetConfigurableResources();

            if (resources == null || resources.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L.T("No configured resources. Click the \"Add Resource\" button above to add a new resource.", 
                        "暂无配置的资源。点击上方\"添加资源\"按钮添加新资源。"), 
                    MessageType.Info);
            }
            else
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    var resource = resources[i];
                    if (resource == null) continue;

                    EditorGUILayout.BeginVertical("box");

                    // 确保折叠状态存在
                    if (!resourceFoldouts.ContainsKey(i))
                        resourceFoldouts[i] = false;

                    // 资源标题行
                    EditorGUILayout.BeginHorizontal();

                    // 获取资源启用状态
                    bool resourceEnabled = McpService.GetLocalSettings().IsResourceEnabled(resource.Name);

                    // 开关
                    Color toggleOriginalBg = GUI.backgroundColor;
                    if (resourceEnabled)
                    {
                        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.5f);
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.5f);
                    }
                    Rect toggleRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(22));
                    bool newResourceEnabled = EditorGUI.Toggle(toggleRect, resourceEnabled);
                    GUI.backgroundColor = toggleOriginalBg;

                    // 处理资源开关状态变化
                    if (newResourceEnabled != resourceEnabled)
                    {
                        McpService.GetLocalSettings().SetResourceEnabled(resource.Name, newResourceEnabled);
                        // 重新发现资源
                        McpService.RediscoverTools();
                        string statusText = newResourceEnabled ? L.T("enabled", "启用") : L.T("disabled", "禁用");
                        Debug.Log($"[McpServiceGUI] {L.T("Resource", "资源")} '{resource.Name}' {L.T("status changed to", "状态已更改为")}: {statusText}");
                    }

                    // 根据启用状态设置文字颜色
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 11
                    };
                    if (resourceEnabled)
                    {
                        foldoutStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
                        foldoutStyle.onNormal.textColor = new Color(0.7f, 0.9f, 0.7f);
                    }
                    else
                    {
                        foldoutStyle.normal.textColor = new Color(0.9f, 0.6f, 0.6f);
                        foldoutStyle.onNormal.textColor = new Color(0.9f, 0.6f, 0.6f);
                    }
                    foldoutStyle.focused.textColor = foldoutStyle.normal.textColor;
                    foldoutStyle.onFocused.textColor = foldoutStyle.normal.textColor;

                    resourceFoldouts[i] = EditorGUILayout.Foldout(resourceFoldouts[i], $"📦 {resource.Name}", true, foldoutStyle);

                    // 删除按钮
                    GUIStyle deleteButtonStyle = new GUIStyle(EditorStyles.miniButtonRight)
                    {
                        fontSize = 10,
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                    Color deleteButtonColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.8f);

                    if (GUILayout.Button(L.T("Delete", "删除"), deleteButtonStyle, GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog(
                            L.T("Confirm Delete", "确认删除"), 
                            $"{L.T("Are you sure you want to delete resource", "确定要删除资源")} '{resource.Name}' {L.T("?", "吗？")}", 
                            L.T("Delete", "删除"), 
                            L.T("Cancel", "取消")))
                        {
                            settings2.RemoveConfigurableResource(resource.Name);
                            settings2.SaveSettings();
                            resourceFoldouts.Remove(i);
                            // 重新发现资源
                            McpService.RediscoverTools();
                            break;
                        }
                    }

                    GUI.backgroundColor = deleteButtonColor;
                    EditorGUILayout.EndHorizontal();

                    // 资源详情
                    if (resourceFoldouts[i])
                    {
                        EditorGUI.indentLevel++;

                        // 名称
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUILayout.TextField(L.T("Name", "名称"), resource.Name);
                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(newName))
                        {
                            resource.SetName(newName);
                            settings2.SaveSettings();
                        }

                        // 描述
                        EditorGUI.BeginChangeCheck();
                        string newDesc = EditorGUILayout.TextField(L.T("Description", "描述"), resource.Description);
                        if (EditorGUI.EndChangeCheck())
                        {
                            resource.SetDescription(newDesc);
                            settings2.SaveSettings();
                        }

                        // 来源类型
                        EditorGUI.BeginChangeCheck();
                        UniMcp.ResourceSourceType newSourceType = (UniMcp.ResourceSourceType)EditorGUILayout.EnumPopup(L.T("Source Type", "来源类型"), resource.SourceType);
                        if (EditorGUI.EndChangeCheck())
                        {
                            resource.SourceType = newSourceType;
                            settings2.SaveSettings();
                        }

                        // 根据来源类型显示不同字段
                        if (resource.SourceType == UniMcp.ResourceSourceType.Url)
                        {
                            EditorGUI.BeginChangeCheck();
                            string newUrl = EditorGUILayout.TextField("URL", resource.Url);
                            if (EditorGUI.EndChangeCheck())
                            {
                                resource.SetUrl(newUrl);
                                settings2.SaveSettings();
                            }
                        }
                        else // UnityObject
                        {
                            EditorGUI.BeginChangeCheck();
                            Object newObject = EditorGUILayout.ObjectField(L.T("Unity Object", "Unity对象"), resource.UnityObject, typeof(Object), false);
                            if (EditorGUI.EndChangeCheck())
                            {
                                resource.UnityObject = newObject;
                                settings2.SaveSettings();
                            }

                            // 显示转换后的URL
                            if (resource.UnityObject != null)
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField("URL", resource.Url);
                                EditorGUI.EndDisabledGroup();
                            }
                        }

                        // MIME类型 - 文本框 + 下拉框（同一行）
                        EditorGUILayout.BeginHorizontal();
                        
                        // 确保选择索引存在
                        if (!resourceMimeTypeSelections.ContainsKey(i))
                        {
                            // 查找当前MIME类型在列表中的索引
                            int currentIndex = Array.IndexOf(commonMimeTypes, resource.MimeType);
                            resourceMimeTypeSelections[i] = currentIndex >= 0 ? currentIndex : 0;
                        }

                        // 文本框（可以手动编辑）
                        EditorGUI.BeginChangeCheck();
                        string newMimeType = EditorGUILayout.TextField(L.T("MIME Type", "MIME类型"), resource.MimeType);
                        if (EditorGUI.EndChangeCheck())
                        {
                            resource.SetMimeType(newMimeType);
                            settings2.SaveSettings();
                            // 更新下拉框选择（如果新值在列表中）
                            int foundIndex = Array.IndexOf(commonMimeTypes, newMimeType);
                            if (foundIndex >= 0)
                            {
                                resourceMimeTypeSelections[i] = foundIndex;
                            }
                        }

                        // 下拉框选择（在文本框后面）
                        int selectedIndex = resourceMimeTypeSelections[i];
                        int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, commonMimeTypes, GUILayout.Width(200));
                        
                        if (newSelectedIndex != selectedIndex)
                        {
                            resourceMimeTypeSelections[i] = newSelectedIndex;
                            // 选择后自动填入文本框
                            resource.SetMimeType(commonMimeTypes[newSelectedIndex]);
                            settings2.SaveSettings();
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制提示词列表配置界面
        /// </summary>
        private static void DrawPromptsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // 标题栏
            Rect headerRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28));
            Color headerBgColor = new Color(0.25f, 0.25f, 0.3f, 0.3f);
            EditorGUI.DrawRect(headerRect, headerBgColor);

            GUIStyle headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) },
                padding = new RectOffset(8, 0, 4, 0)
            };
            EditorGUILayout.LabelField(L.T("💬 Configurable Prompts", "💬 可配置提示词"), headerTitleStyle, GUILayout.ExpandWidth(true));

            // 添加提示词按钮
            GUIStyle addButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(10, 10, 4, 4),
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Color addButtonColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.8f);

            if (GUILayout.Button(L.T("➕ Add Prompt", "➕ 添加提示词"), addButtonStyle, GUILayout.Width(110), GUILayout.Height(22)))
            {
                var settings = McpSettings.Instance;
                var newPrompt = new UniMcp.ConfigurablePrompt(
                    L.T("New Prompt", "新提示词"), 
                    L.T("Prompt Description", "提示词描述"), 
                    L.T("Prompt Content", "提示词内容"));
                settings.AddConfigurablePrompt(newPrompt);
                settings.SaveSettings();
            }

            GUI.backgroundColor = addButtonColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 滚动视图
            promptsScrollPosition = EditorGUILayout.BeginScrollView(promptsScrollPosition, GUILayout.ExpandHeight(true));

            var settings2 = McpSettings.Instance;
            var prompts = settings2.GetConfigurablePrompts();

            if (prompts == null || prompts.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    L.T("No configured prompts. Click the \"Add Prompt\" button above to add a new prompt.", 
                        "暂无配置的提示词。点击上方\"添加提示词\"按钮添加新提示词。"), 
                    MessageType.Info);
            }
            else
            {
                for (int i = 0; i < prompts.Count; i++)
                {
                    var prompt = prompts[i];
                    if (prompt == null) continue;

                    EditorGUILayout.BeginVertical("box");

                    // 确保折叠状态存在
                    if (!promptFoldouts.ContainsKey(i))
                        promptFoldouts[i] = false;

                    // 提示词标题行
                    EditorGUILayout.BeginHorizontal();

                    // 获取提示词启用状态
                    bool promptEnabled = McpService.GetLocalSettings().IsPromptEnabled(prompt.Name);

                    // 开关
                    Color toggleOriginalBg = GUI.backgroundColor;
                    if (promptEnabled)
                    {
                        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f, 0.5f);
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f, 0.5f);
                    }
                    Rect toggleRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(22));
                    bool newPromptEnabled = EditorGUI.Toggle(toggleRect, promptEnabled);
                    GUI.backgroundColor = toggleOriginalBg;

                    // 处理提示词开关状态变化
                    if (newPromptEnabled != promptEnabled)
                    {
                        McpService.GetLocalSettings().SetPromptEnabled(prompt.Name, newPromptEnabled);
                        // 重新发现提示词
                        McpService.RediscoverTools();
                        string statusText = newPromptEnabled ? L.T("enabled", "启用") : L.T("disabled", "禁用");
                        Debug.Log($"[McpServiceGUI] {L.T("Prompt", "提示词")} '{prompt.Name}' {L.T("status changed to", "状态已更改为")}: {statusText}");
                    }

                    // 根据启用状态设置文字颜色
                    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 11
                    };
                    if (promptEnabled)
                    {
                        foldoutStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
                        foldoutStyle.onNormal.textColor = new Color(0.7f, 0.9f, 0.7f);
                    }
                    else
                    {
                        foldoutStyle.normal.textColor = new Color(0.9f, 0.6f, 0.6f);
                        foldoutStyle.onNormal.textColor = new Color(0.9f, 0.6f, 0.6f);
                    }
                    foldoutStyle.focused.textColor = foldoutStyle.normal.textColor;
                    foldoutStyle.onFocused.textColor = foldoutStyle.normal.textColor;

                    promptFoldouts[i] = EditorGUILayout.Foldout(promptFoldouts[i], $"💬 {prompt.Name}", true, foldoutStyle);

                    // 删除按钮
                    GUIStyle deleteButtonStyle = new GUIStyle(EditorStyles.miniButtonRight)
                    {
                        fontSize = 10,
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                    Color deleteButtonColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.8f);

                    if (GUILayout.Button(L.T("Delete", "删除"), deleteButtonStyle, GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog(
                            L.T("Confirm Delete", "确认删除"), 
                            $"{L.T("Are you sure you want to delete prompt", "确定要删除提示词")} '{prompt.Name}' {L.T("?", "吗？")}", 
                            L.T("Delete", "删除"), 
                            L.T("Cancel", "取消")))
                        {
                            settings2.RemoveConfigurablePrompt(prompt.Name);
                            settings2.SaveSettings();
                            promptFoldouts.Remove(i);
                            // 重新发现提示词
                            McpService.RediscoverTools();
                            break;
                        }
                    }

                    GUI.backgroundColor = deleteButtonColor;
                    EditorGUILayout.EndHorizontal();

                    // 提示词详情
                    if (promptFoldouts[i])
                    {
                        EditorGUI.indentLevel++;

                        // 名称
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUILayout.TextField(L.T("Name", "名称"), prompt.Name);
                        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(newName))
                        {
                            prompt.SetName(newName);
                            settings2.SaveSettings();
                        }

                        // 描述
                        EditorGUI.BeginChangeCheck();
                        string newDesc = EditorGUILayout.TextField(L.T("Description", "描述"), prompt.Description);
                        if (EditorGUI.EndChangeCheck())
                        {
                            prompt.SetDescription(newDesc);
                            settings2.SaveSettings();
                        }

                        // 提示词文本
                        EditorGUI.BeginChangeCheck();
                        string newPromptText = EditorGUILayout.TextArea(prompt.PromptText, GUILayout.Height(100));
                        if (EditorGUI.EndChangeCheck())
                        {
                            prompt.SetPromptText(newPromptText);
                            settings2.SaveSettings();
                        }

                        // 参数列表（只读显示，不提供添加/删除功能）
                        System.Collections.Generic.List<UniMcp.ConfigurableMethodKey> keys = prompt.GetKeys();
                        if (keys != null && keys.Count > 0)
                        {
                            EditorGUILayout.Space(4);
                            EditorGUILayout.LabelField(L.T("Parameter Information (Read-only)", "参数信息（只读）"), EditorStyles.boldLabel);
                            EditorGUILayout.HelpBox(
                                L.T("Parameter information for configuration-based prompts is read-only. To modify parameters, implement the IPrompts interface in code.", 
                                    "配置方式的提示词参数信息为只读显示。如需修改参数，请通过代码实现IPrompts接口。"), 
                                MessageType.Info);
                            
                            for (int j = 0; j < keys.Count; j++)
                            {
                                var key = keys[j];
                                if (key == null) continue;

                                EditorGUILayout.BeginVertical("box");
                                EditorGUILayout.LabelField($"{L.T("Parameter", "参数")} {j + 1}: {key.key}", EditorStyles.miniBoldLabel);
                                EditorGUILayout.LabelField($"{L.T("Type", "类型")}: {key.type}, {L.T("Optional", "可选")}: {key.optional}", EditorStyles.miniLabel);
                                EditorGUILayout.LabelField($"{L.T("Description", "描述")}: {key.desc}", EditorStyles.miniLabel);
                                
                                if (key.examples != null && key.examples.Count > 0)
                                {
                                    EditorGUILayout.LabelField($"{L.T("Examples", "示例")}: {string.Join(", ", key.examples)}", EditorStyles.miniLabel);
                                }
                                
                                if (key.enumValues != null && key.enumValues.Count > 0)
                                {
                                    EditorGUILayout.LabelField($"{L.T("Enum Values", "枚举值")}: {string.Join(", ", key.enumValues)}", EditorStyles.miniLabel);
                                }
                                
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.Space(2);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 刷新所有列表（工具、资源、提示词）
        /// </summary>
        private static void RefreshAllLists()
        {
            Debug.Log("[McpServiceGUI] Refreshing all lists (tools, resources, prompts)...");
            
            // 清除折叠状态，让界面重新渲染
            methodFoldouts.Clear();
            groupFoldouts.Clear();
            resourceFoldouts.Clear();
            promptFoldouts.Clear();
            
            // 清除工具缓存，让工具实例重新创建（这样描述会重新获取）
            ToolsCall.ClearRegisteredMethods();
            
            // 重新发现工具（这会同时刷新工具、资源和提示词）
            McpService.RediscoverTools();
            
            Debug.Log("[McpServiceGUI] All lists refreshed.");
        }

    }
}