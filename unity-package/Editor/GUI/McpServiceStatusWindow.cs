using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UniMcp;
using UniMcp.Executer;
using UnityEditorInternal;

namespace UniMcp.Gui
{
    /// <summary>
    /// MCP服务状态窗口，用于显示服务运行状态和客户端连接信息
    /// </summary>
    public class McpServiceStatusWindow : EditorWindow
    {
        // 客户端连接状态相关变量
        private static Vector2 clientsScrollPosition;

        // HTTP请求记录列表
        private ReorderableList httpRequestRecordsList;
        private Dictionary<string, bool> recordFoldoutStates = new Dictionary<string, bool>();

        // 服务运行状态
        private static bool isServiceRunning = false;
        private static int mcpPort => McpService.mcpPort;

        // 端口配置相关变量
        private static string portInputString = "";
        private static bool portInputInitialized = false;

        // 窗口实例
        private static McpServiceStatusWindow instance;

        /// <summary>
        /// 打开MCP服务状态窗口
        /// </summary>
        [MenuItem("Window/MCP/Status")]
        public static void ShowWindow()
        {
            instance = GetWindow<McpServiceStatusWindow>(L.T("MCP Service Status", "MCP服务状态"));
            instance.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            isServiceRunning = McpService.Instance.IsRunning;
            // 注册编辑器更新事件，用于定期刷新状态
            EditorApplication.update += OnEditorUpdate;

            // 初始化ReorderableList
            InitializeHttpRequestRecordsList();
        }

        /// <summary>
        /// 初始化HTTP请求记录的ReorderableList
        /// </summary>
        private void InitializeHttpRequestRecordsList()
        {
            // 获取记录并按时间降序排序（最新的在前）
            var records = new List<UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord>(
                UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords()
            );
            records.Sort((a, b) => b.requestTime.CompareTo(a.requestTime)); // 时间大的排前面
            
            httpRequestRecordsList = new ReorderableList(
                records,
                typeof(UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord),
                false, // 不可拖拽
                true,  // 显示标题
                false, // 不可添加
                false  // 不可删除
            );

            // 设置标题样式
            httpRequestRecordsList.drawHeaderCallback = (Rect rect) =>
            {
                // 绘制背景
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
                
                // 绘制标题文字
                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.normal.textColor = Color.white;
                headerStyle.alignment = TextAnchor.MiddleLeft;
                headerStyle.padding = new RectOffset(10, 0, 0, 0);
                
                EditorGUI.LabelField(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), 
                    L.T("HTTP Request Records", "HTTP请求记录"), headerStyle);
            };

            // 设置元素高度
            httpRequestRecordsList.elementHeightCallback = (int index) =>
            {
                // 获取最新的记录列表并排序
                var currentRecords = new List<UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord>(
                    UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords()
                );
                currentRecords.Sort((a, b) => b.requestTime.CompareTo(a.requestTime)); // 时间大的排前面
                
                if (index >= currentRecords.Count) return EditorGUIUtility.singleLineHeight;

                var record = currentRecords[index];
                bool isExpanded = false;

                // 检查该记录是否已展开
                if (recordFoldoutStates.TryGetValue(record.id, out bool state))
                {
                    isExpanded = state;
                }

                // 基本高度为单行高度
                float height = EditorGUIUtility.singleLineHeight + 4;

                // 如果展开，增加高度以显示详细信息
                if (isExpanded)
                {
                    // 基本信息行（客户端、请求时间、处理时间、HTTP方法、状态码、状态）
                    height += (EditorGUIUtility.singleLineHeight + 2) * 6; // 6行基本信息，每行间距2
                    
                    height += 4; // 分隔间距

                    // 请求内容和响应内容区域
                    if (!string.IsNullOrEmpty(record.requestContent))
                    {
                        height += EditorGUIUtility.singleLineHeight + 2; // 标题行
                        height += 80; // 请求内容文本区域（增加高度）
                        height += 4; // 间距
                    }

                    if (!string.IsNullOrEmpty(record.responseContent))
                    {
                        height += EditorGUIUtility.singleLineHeight + 2; // 标题行
                        height += 80; // 响应内容文本区域（增加高度）
                        height += 4; // 间距
                    }

                    // 处理时长行
                    height += EditorGUIUtility.singleLineHeight + 2;
                    
                    height += 12; // 底部额外间距
                }

                return height;
            };

            // 设置绘制元素
            httpRequestRecordsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // 获取最新的记录列表并排序
                var currentRecords = new List<UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord>(
                    UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords()
                );
                currentRecords.Sort((a, b) => b.requestTime.CompareTo(a.requestTime));
                
                if (index >= currentRecords.Count) return;

                var record = currentRecords[index];

                // 确保记录ID在字典中存在
                if (!recordFoldoutStates.ContainsKey(record.id))
                {
                    recordFoldoutStates[record.id] = false;
                }

                // 绘制背景（交替颜色）
                Color bgColor = index % 2 == 0 ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.22f, 0.22f, 0.22f, 0.5f);
                if (isActive)
                {
                    bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
                }
                EditorGUI.DrawRect(rect, bgColor);

                // 绘制折叠控件和基本信息
                Rect foldoutRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, EditorGUIUtility.singleLineHeight);

                // 创建标题文本，优化显示
                string statusText = record.success ? L.T("✓ Success", "✓ 成功") : L.T("✗ Failed", "✗ 失败");
                Color statusColor = record.success ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
                string title = $"[{record.requestTime:HH:mm:ss}] {record.httpMethod} | {record.endPoint} | {statusText}";

                // 绘制状态颜色指示
                Rect statusRect = new Rect(rect.x + 2, rect.y + 4, 3, EditorGUIUtility.singleLineHeight - 4);
                EditorGUI.DrawRect(statusRect, statusColor);

                // 绘制折叠控件
                GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;
                foldoutStyle.normal.textColor = Color.white;
                foldoutStyle.onNormal.textColor = Color.white;
                foldoutStyle.focused.textColor = Color.white;
                foldoutStyle.onFocused.textColor = Color.white;
                
                recordFoldoutStates[record.id] = EditorGUI.Foldout(foldoutRect, recordFoldoutStates[record.id], title, true, foldoutStyle);

                // 如果展开，绘制详细信息
                if (recordFoldoutStates[record.id])
                {
                    float yOffset = rect.y + EditorGUIUtility.singleLineHeight + 6;
                    float detailWidth = rect.width - 20;

                    // 绘制详细信息区域背景
                    Rect detailsRect = new Rect(rect.x + 10, yOffset, detailWidth, rect.height - EditorGUIUtility.singleLineHeight - 6);
                    EditorGUI.DrawRect(detailsRect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
                    
                    // 绘制边框
                    EditorGUI.DrawRect(new Rect(detailsRect.x, detailsRect.y, detailsRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
                    EditorGUI.DrawRect(new Rect(detailsRect.x, detailsRect.y + detailsRect.height - 1, detailsRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
                    EditorGUI.DrawRect(new Rect(detailsRect.x, detailsRect.y, 1, detailsRect.height), new Color(0.4f, 0.4f, 0.4f));
                    EditorGUI.DrawRect(new Rect(detailsRect.x + detailsRect.width - 1, detailsRect.y, 1, detailsRect.height), new Color(0.4f, 0.4f, 0.4f));

                    // 内容区域
                    float contentX = detailsRect.x + 10;
                    float contentWidth = detailsRect.width - 20;
                    float contentY = yOffset + 8;

                    // 基本信息 - 使用更好的样式
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                    labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                    
                    GUIStyle valueStyle = new GUIStyle(EditorStyles.miniLabel);
                    valueStyle.normal.textColor = Color.white;
                    valueStyle.fontStyle = FontStyle.Bold;

                    // 两列布局显示信息
                    float labelWidth = 80;
                    float valueWidth = contentWidth - labelWidth - 10;

                    DrawInfoRow(contentX, contentY, L.T("Client", "客户端"), record.endPoint, labelWidth, valueWidth, labelStyle, valueStyle);
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    DrawInfoRow(contentX, contentY, L.T("Request Time", "请求时间"), record.requestTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), labelWidth, valueWidth, labelStyle, valueStyle);
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    DrawInfoRow(contentX, contentY, L.T("Response Time", "处理时间"), record.responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), labelWidth, valueWidth, labelStyle, valueStyle);
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    DrawInfoRow(contentX, contentY, L.T("HTTP Method", "HTTP方法"), record.httpMethod, labelWidth, valueWidth, labelStyle, valueStyle);
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    // 状态码带颜色
                    Color statusCodeColor = record.statusCode >= 200 && record.statusCode < 300 ? 
                        new Color(0.4f, 0.8f, 0.4f) : 
                        (record.statusCode >= 400 ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.8f, 0.8f, 0.4f));
                    valueStyle.normal.textColor = statusCodeColor;
                    DrawInfoRow(contentX, contentY, L.T("Status Code", "状态码"), record.statusCode.ToString(), labelWidth, valueWidth, labelStyle, valueStyle);
                    valueStyle.normal.textColor = Color.white;
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    // 成功状态带颜色
                    valueStyle.normal.textColor = record.success ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
                    DrawInfoRow(contentX, contentY, L.T("Status", "状态"), record.success ? L.T("Success", "成功") : L.T("Failed", "失败"), labelWidth, valueWidth, labelStyle, valueStyle);
                    valueStyle.normal.textColor = Color.white;
                    contentY += EditorGUIUtility.singleLineHeight + 2;

                    contentY += 4;

                    // 请求内容
                    if (!string.IsNullOrEmpty(record.requestContent))
                    {
                        GUIStyle sectionStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                        sectionStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);
                        EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                            L.T("Request Content:", "请求内容:"), sectionStyle);
                        contentY += EditorGUIUtility.singleLineHeight + 2;

                        // 绘制文本框背景和边框
                        Rect textAreaRect = new Rect(contentX, contentY, contentWidth, 80);
                        EditorGUI.DrawRect(textAreaRect, new Color(0.1f, 0.1f, 0.1f, 1f));
                        
                        // 绘制边框
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y, textAreaRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y + textAreaRect.height - 1, textAreaRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y, 1, textAreaRect.height), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x + textAreaRect.width - 1, textAreaRect.y, 1, textAreaRect.height), new Color(0.3f, 0.3f, 0.3f));

                        // 创建深色主题的文本框样式
                        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                        textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                        textAreaStyle.normal.background = Texture2D.blackTexture;
                        textAreaStyle.active.textColor = Color.white;
                        textAreaStyle.focused.textColor = Color.white;
                        textAreaStyle.wordWrap = true;
                        
                        EditorGUI.TextArea(new Rect(contentX + 4, contentY + 4, contentWidth - 8, 72),
                            record.requestContent, textAreaStyle);
                        contentY += 84;
                    }

                    // 响应内容
                    if (!string.IsNullOrEmpty(record.responseContent))
                    {
                        GUIStyle sectionStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                        sectionStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);
                        EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                            L.T("Response Content:", "响应内容:"), sectionStyle);
                        contentY += EditorGUIUtility.singleLineHeight + 2;

                        // 绘制文本框背景和边框
                        Rect textAreaRect = new Rect(contentX, contentY, contentWidth, 80);
                        EditorGUI.DrawRect(textAreaRect, new Color(0.1f, 0.1f, 0.1f, 1f));
                        
                        // 绘制边框
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y, textAreaRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y + textAreaRect.height - 1, textAreaRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x, textAreaRect.y, 1, textAreaRect.height), new Color(0.3f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(textAreaRect.x + textAreaRect.width - 1, textAreaRect.y, 1, textAreaRect.height), new Color(0.3f, 0.3f, 0.3f));

                        // 创建深色主题的文本框样式
                        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                        textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                        textAreaStyle.normal.background = Texture2D.blackTexture;
                        textAreaStyle.active.textColor = Color.white;
                        textAreaStyle.focused.textColor = Color.white;
                        textAreaStyle.wordWrap = true;
                        
                        EditorGUI.TextArea(new Rect(contentX + 4, contentY + 4, contentWidth - 8, 72),
                            record.responseContent, textAreaStyle);
                        contentY += 84;
                    }

                    // 处理时长
                    Color durationColor = record.duration < 100 ? new Color(0.4f, 0.8f, 0.4f) : 
                                        (record.duration < 500 ? new Color(0.8f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));
                    valueStyle.normal.textColor = durationColor;
                    DrawInfoRow(contentX, contentY, L.T("Processing Time", "处理时长"), $"{record.duration:F2} {L.T("ms", "毫秒")}", labelWidth, valueWidth, labelStyle, valueStyle);
                }
            };
        }

        /// <summary>
        /// 绘制信息行（标签+值）
        /// </summary>
        private void DrawInfoRow(float x, float y, string label, string value, float labelWidth, float valueWidth, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            EditorGUI.LabelField(new Rect(x, y, labelWidth, EditorGUIUtility.singleLineHeight), label + ":", labelStyle);
            EditorGUI.LabelField(new Rect(x + labelWidth + 5, y, valueWidth, EditorGUIUtility.singleLineHeight), value, valueStyle);
        }

        private void OnDisable()
        {
            // 取消注册编辑器更新事件
            EditorApplication.update -= OnEditorUpdate;
        }

        // 更新计时器
        private double lastUpdateTime = 0;
        private const double updateInterval = 2.0; // 每2秒更新一次

        private void OnEditorUpdate()
        {
            // 定期检查服务状态
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = currentTime;
                isServiceRunning = McpService.Instance.IsRunning;

                // 定期清理旧的请求记录（每次更新时清理超过30分钟的记录）
                if (isServiceRunning)
                {
                    UniMcp.Models.McpExecuteRecordObject.instance.CleanupOldHttpRequestRecords(30);
                }

                // 更新ReorderableList - 重新初始化以确保数据同步
                if (httpRequestRecordsList != null)
                {
                    var records = new List<UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord>(
                        UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords()
                    );
                    records.Sort((a, b) => b.requestTime.CompareTo(a.requestTime)); // 时间大的排前面
                    
                    // 检查数据是否发生变化
                    if (httpRequestRecordsList.list.Count != records.Count)
                    {
                        // 数据发生变化，重新初始化ReorderableList
                        InitializeHttpRequestRecordsList();
                    }
                    else
                    {
                        // 数据没有变化，只更新引用（保持排序）
                        httpRequestRecordsList.list = records;
                    }
                }

                Repaint(); // 刷新窗口
            }
        }

        private void OnGUI()
        {
            // 动态更新窗口标题以支持语言切换
            titleContent = new GUIContent(L.T("MCP Service Status", "MCP服务状态"));
            
            // 绘制窗口背景
            Rect windowRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(windowRect, new Color(0.22f, 0.22f, 0.22f, 1f));

            // 使用垂直布局管理整个窗口
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            EditorGUILayout.Space(5);

            // Unity Bridge Section - 美化背景
            Rect sectionRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(sectionRect, new Color(0.25f, 0.25f, 0.25f, 1f));
            
            // 绘制边框
            EditorGUI.DrawRect(new Rect(sectionRect.x, sectionRect.y, sectionRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(sectionRect.x, sectionRect.y + sectionRect.height - 1, sectionRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(sectionRect.x, sectionRect.y, 1, sectionRect.height), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(sectionRect.x + sectionRect.width - 1, sectionRect.y, 1, sectionRect.height), new Color(0.4f, 0.4f, 0.4f));

            EditorGUILayout.Space(8);

            // 标题行 - 美化样式
            EditorGUILayout.BeginHorizontal();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            EditorGUILayout.LabelField("Unity MCP Services", titleStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 状态行
            EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
            
            // 先绘制状态图标，使用固定宽度
            Rect statusIconRect = EditorGUILayout.GetControlRect(false, 22, GUILayout.Width(30));
            DrawStatusDot(statusIconRect, isServiceRunning ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f));
            
            // 添加足够的间距，避免图标和文字重叠
            EditorGUILayout.Space(12);
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.fontSize = 11;
            statusStyle.normal.textColor = isServiceRunning ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
            statusStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField($"Status: {(isServiceRunning ? "Running" : "Stopped")}", statusStyle);

            // 端口配置
            DrawPortConfiguration();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 按钮区域
            EditorGUILayout.BeginHorizontal();
            
            // 启动/停止按钮
            Color originalBg = GUI.backgroundColor;
            Color buttonColor = isServiceRunning ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.3f, 0.7f, 0.3f);
            GUI.backgroundColor = buttonColor;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 11;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;
            
            if (GUILayout.Button(isServiceRunning ? "Stop Server" : "Start Server", buttonStyle, GUILayout.Height(28)))
            {
                ToggleService();
            }

            // 重启服务器按钮（只在服务运行时显示）
            if (isServiceRunning)
            {
                GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
                if (GUILayout.Button("Restart Server", buttonStyle, GUILayout.Height(28)))
                {
                    RestartServer();
                }
            }
            
            GUI.backgroundColor = originalBg;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndVertical();

            // 客户端连接状态部分
            if (isServiceRunning)
            {
                DrawClientConnectionStatus();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatusDot(Rect statusRect, Color statusColor)
        {
            // 图标居中绘制在提供的矩形内
            float iconSize = 14;
            float iconX = statusRect.x + (statusRect.width - iconSize) / 2;
            float iconY = statusRect.y + (statusRect.height - iconSize) / 2;
            Rect dotRect = new(iconX, iconY, iconSize, iconSize);
            
            Vector3 center = new(
                dotRect.x + (dotRect.width / 2),
                dotRect.y + (dotRect.height / 2),
                0
            );
            float radius = dotRect.width / 2;

            // Draw glow effect (outer ring)
            Color glowColor = new Color(statusColor.r, statusColor.g, statusColor.b, 0.3f);
            Handles.color = glowColor;
            Handles.DrawSolidDisc(center, Vector3.forward, radius + 2);

            // Draw the main dot
            Handles.color = statusColor;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);

            // Draw the border
            Color borderColor = new Color(
                Mathf.Max(0, statusColor.r - 0.2f),
                Mathf.Max(0, statusColor.g - 0.2f),
                Mathf.Max(0, statusColor.b - 0.2f)
            );
            Handles.color = borderColor;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private static void ToggleService()
        {
            if (isServiceRunning)
            {
                McpService.StopService();
                isServiceRunning = false;
                // 停止服务时设置 ResourcesCapability 为 false
                McpLocalSettings.Instance.ResourcesCapability = false;
            }
            else
            {
                // 尝试启动Unity MCP，它会自动选择可用端口
                McpService.StartService();

                // 检查启动是否成功
                if (McpService.Instance.IsRunning)
                {
                    isServiceRunning = true;
                    // 启动成功时设置 ResourcesCapability 为 true
                    McpLocalSettings.Instance.ResourcesCapability = true;
                    McpLogger.Log($"{L.T("Unity MCP Bridge started, port", "Unity MCP Bridge 已启动，端口")}: {mcpPort}");
                }
                else
                {
                    isServiceRunning = false;
                    // 启动失败时设置 ResourcesCapability 为 false
                    McpLocalSettings.Instance.ResourcesCapability = false;
                    EditorUtility.DisplayDialog(
                        L.T("Startup Failed", "启动失败"),
                        L.IsChinese()
                            ? $"无法在端口 {mcpPort} 启动Unity MCP Bridge。\n请检查是否有其他进程占用了所有端口。"
                            : $"Cannot start Unity MCP Bridge on port {mcpPort}.\nPlease check if other processes are occupying all ports.",
                        L.T("OK", "确定"));
                }
            }
            McpLocalSettings.Instance.McpOpenState = isServiceRunning;
        }

        /// <summary>
        /// 强制刷新HTTP请求记录列表
        /// </summary>
        private void RefreshHttpRequestRecordsList()
        {
            InitializeHttpRequestRecordsList();
            Repaint();
        }

        // HTTP请求记录列表的滚动位置
        private Vector2 httpRequestRecordsScrollPosition;

        /// <summary>
        /// 绘制客户端请求记录
        /// </summary>
        private void DrawClientConnectionStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // 客户端请求记录标题和工具栏
            EditorGUILayout.BeginHorizontal();
            
            // 标题样式优化
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 13;
            titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            EditorGUILayout.LabelField(L.T("HTTP Request Records", "HTTP请求记录"), titleStyle, GUILayout.ExpandWidth(true));

            // 显示记录数量
            int clientCount = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords().Count;
            Color countColor = clientCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : Color.gray;
            GUIStyle countStyle = new GUIStyle(EditorStyles.label);
            countStyle.normal.textColor = countColor;
            countStyle.fontStyle = FontStyle.Bold;
            countStyle.fontSize = 11;

            EditorGUILayout.LabelField($"{L.T("Records", "记录数")}: {clientCount}", countStyle, GUILayout.Width(100));

            // 刷新按钮
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            GUIStyle refreshButtonStyle = new GUIStyle(GUI.skin.button);
            refreshButtonStyle.fontSize = 10;
            refreshButtonStyle.fontStyle = FontStyle.Bold;
            refreshButtonStyle.normal.textColor = Color.white;
            refreshButtonStyle.hover.textColor = Color.white;
            refreshButtonStyle.active.textColor = Color.white;
            
            if (GUILayout.Button(L.T("Refresh", "刷新"), refreshButtonStyle, GUILayout.Width(60), GUILayout.Height(22)))
            {
                RefreshHttpRequestRecordsList();
            }
            GUI.backgroundColor = originalBg;

            // 清空记录按钮
            if (clientCount > 0)
            {
                GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button(L.T("Clear", "清空"), refreshButtonStyle, GUILayout.Width(60), GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog(
                        L.T("Clear Request Records", "清空请求记录"), 
                        L.T("Are you sure you want to clear all HTTP request records?", "确定要清空所有HTTP请求记录吗？"), 
                        L.T("Confirm", "确定"), 
                        L.T("Cancel", "取消")))
                    {
                        UniMcp.Models.McpExecuteRecordObject.instance.ClearHttpRequestRecords();
                        // 清空折叠状态字典
                        recordFoldoutStates.Clear();
                        // 刷新列表
                        RefreshHttpRequestRecordsList();
                    }
                }
                GUI.backgroundColor = originalBg;
            }

            EditorGUILayout.EndHorizontal();

            if (clientCount > 0)
            {
                EditorGUILayout.Space(5);

                // 确保ReorderableList已初始化
                if (httpRequestRecordsList == null)
                {
                    InitializeHttpRequestRecordsList();
                }

                // 用ScrollView包裹ReorderableList，使其可滚动并填充剩余空间
                httpRequestRecordsScrollPosition = EditorGUILayout.BeginScrollView(
                    httpRequestRecordsScrollPosition, 
                    false,  // 不显示横向滚动条（根据需要可以设置为true）
                    true,   // 显示纵向滚动条
                    GUILayout.ExpandHeight(true));  // 扩展以填充剩余空间

                // 直接绘制ReorderableList
                if (httpRequestRecordsList != null)
                {
                    try
                    {
                        httpRequestRecordsList.DoLayoutList();
                    }
                    catch (System.Exception ex)
                    {
                        // 如果绘制失败，重新初始化列表
                        Debug.LogWarning($"ReorderableList绘制失败，重新初始化: {ex.Message}");
                        InitializeHttpRequestRecordsList();
                        if (httpRequestRecordsList != null)
                        {
                            httpRequestRecordsList.DoLayoutList();
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.Space(20);
                GUIStyle emptyLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                emptyLabelStyle.fontSize = 12;
                emptyLabelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                emptyLabelStyle.fontStyle = FontStyle.Italic;
                EditorGUILayout.LabelField(L.T("No HTTP request records", "暂无HTTP请求记录"), emptyLabelStyle);
                EditorGUILayout.Space(20);
            }

            EditorGUILayout.EndVertical();
        }
        /// <summary>
        /// 重启MCP服务器
        /// </summary>
        private static void RestartServer()
        {
            // 显示确认对话框
            bool confirm = EditorUtility.DisplayDialog(
                L.T("Restart MCP Server", "重启MCP服务器"),
                L.T("Are you sure you want to restart the MCP server?\n\nThis will disconnect all currently connected clients.", 
                    "确定要重启MCP服务器吗？\n\n这将断开所有当前连接的客户端。"),
                L.T("Confirm", "确定"),
                L.T("Cancel", "取消")
            );

            if (!confirm)
            {
                return;
            }

            // 启动协程执行重启流程
            CoroutineRunner.StartCoroutine(RestartServerCoroutine(), (result) =>
            {
                // 协程完成回调
                if (result is Exception ex)
                {
                    McpLogger.LogError($"[McpServiceStatusWindow] 重启协程异常: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        /// <summary>
        /// 重启服务器的协程
        /// </summary>
        private static IEnumerator RestartServerCoroutine()
        {
            // 显示进度条
            EditorUtility.DisplayProgressBar(
                L.T("Restarting MCP Server", "重启MCP服务器"), 
                L.T("Stopping server...", "正在停止服务器..."), 
                0.3f);

            // 停止服务器
            try
            {
                McpService.StopService();
                isServiceRunning = false;
                // 停止服务时设置 ResourcesCapability 为 false
                McpLocalSettings.Instance.ResourcesCapability = false;
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    L.T("Stop Server Error", "停止服务器错误"),
                    L.IsChinese()
                        ? $"停止MCP服务器时发生错误：\n\n{ex.Message}"
                        : $"Error stopping MCP server:\n\n{ex.Message}",
                    L.T("OK", "确定")
                );
                McpLogger.LogError($"[McpServiceStatusWindow] {L.T("Error stopping MCP server", "停止MCP服务器时发生错误")}: {ex.Message}\n{ex.StackTrace}");
                yield break; // 终止协程
            }

            // 等待0.5秒确保资源释放（不能在try-catch中使用yield return）
            yield return new WaitForSeconds(1);

            EditorUtility.DisplayProgressBar(
                L.T("Restarting MCP Server", "重启MCP服务器"), 
                L.T("Starting server...", "正在启动服务器..."), 
                0.7f);

            // 启动服务器
            try
            {
                McpService.StartService();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    L.T("Start Server Error", "启动服务器错误"),
                    L.IsChinese()
                        ? $"启动MCP服务器时发生错误：\n\n{ex.Message}"
                        : $"Error starting MCP server:\n\n{ex.Message}",
                    L.T("OK", "确定")
                );
                McpLogger.LogError($"[McpServiceStatusWindow] {L.T("Error starting MCP server", "启动MCP服务器时发生错误")}: {ex.Message}\n{ex.StackTrace}");
                // 启动失败时确保 ResourcesCapability 为 false
                McpLocalSettings.Instance.ResourcesCapability = false;
                yield break; // 终止协程
            }

            // 清除进度条
            EditorUtility.ClearProgressBar();

            // 检查启动状态
            if (McpService.Instance.IsRunning)
            {
                isServiceRunning = true;
                // 启动成功时设置 ResourcesCapability 为 true
                McpLocalSettings.Instance.ResourcesCapability = true;
                // 检查服务状态
                EditorUtility.DisplayDialog(
                    L.T("Restart Successful", "重启成功"),
                    L.IsChinese()
                        ? $"MCP服务器已成功重启！\n\n端口: {mcpPort}"
                        : $"MCP server restarted successfully!\n\nPort: {mcpPort}",
                    L.T("OK", "确定")
                );
                McpLogger.Log($"[McpServiceStatusWindow] {L.T("MCP server restarted, port", "MCP服务器已重启，端口")}: {mcpPort}");

                // 更新McpLocalSettings状态
                McpLocalSettings.Instance.McpOpenState = true;
            }
            else
            {
                isServiceRunning = false;
                // 启动失败时设置 ResourcesCapability 为 false
                McpLocalSettings.Instance.ResourcesCapability = false;
                EditorUtility.DisplayDialog(
                    L.T("Restart Failed", "重启失败"),
                    L.T("MCP server restart failed. Please check the console logs for details.", "MCP服务器重启失败，请查看控制台日志了解详情。"),
                    L.T("OK", "确定")
                );
                McpLogger.LogError($"[McpServiceStatusWindow] {L.T("MCP server restart failed", "MCP服务器重启失败")}");

                // 更新McpLocalSettings状态
                McpLocalSettings.Instance.McpOpenState = false;
            }

            // 刷新窗口显示
            if (instance != null)
            {
                instance.Repaint();
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

            GUILayout.FlexibleSpace();

            // 端口标签 - 美化样式
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            labelStyle.fontSize = 11;
            EditorGUILayout.LabelField(L.T("Port:", "端口:"), labelStyle, GUILayout.Width(40));

            // 端口输入框 - 美化样式
            GUI.SetNextControlName("PortInput");
            
            // 绘制输入框背景
            Rect textFieldRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(70));
            EditorGUI.DrawRect(textFieldRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUI.DrawRect(new Rect(textFieldRect.x, textFieldRect.y, textFieldRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(textFieldRect.x, textFieldRect.y + textFieldRect.height - 1, textFieldRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(textFieldRect.x, textFieldRect.y, 1, textFieldRect.height), new Color(0.4f, 0.4f, 0.4f));
            EditorGUI.DrawRect(new Rect(textFieldRect.x + textFieldRect.width - 1, textFieldRect.y, 1, textFieldRect.height), new Color(0.4f, 0.4f, 0.4f));
            
            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField);
            textFieldStyle.normal.textColor = Color.white;
            textFieldStyle.normal.background = Texture2D.blackTexture;
            textFieldStyle.alignment = TextAnchor.MiddleCenter;
            textFieldStyle.fontSize = 11;
            
            string newPortString = EditorGUI.TextField(new Rect(textFieldRect.x + 2, textFieldRect.y + 1, textFieldRect.width - 4, textFieldRect.height - 2), 
                portInputString, textFieldStyle);

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
            Color originalTextColor = GUI.color;
            
            if (!isValidPort && !string.IsNullOrEmpty(portInputString))
            {
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            }
            else if (isValidPort && portValue != McpService.mcpPort)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.2f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            }

            bool buttonEnabled = isValidPort && portValue != McpService.mcpPort;
            GUI.enabled = buttonEnabled;

            GUIStyle applyButtonStyle = new GUIStyle(GUI.skin.button);
            applyButtonStyle.fontSize = 10;
            applyButtonStyle.normal.textColor = Color.white;
            applyButtonStyle.hover.textColor = Color.white;
            applyButtonStyle.active.textColor = Color.white;

            if (GUILayout.Button(L.T("Apply", "应用"), applyButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
            {
                if (McpService.SetMcpPort(portValue))
                {
                    Debug.Log($"[McpServiceStatusWindow] {L.T("Port changed to", "端口已更改为")}: {portValue}");
                    if (McpService.Instance.IsRunning)
                    {
                        EditorUtility.DisplayDialog(
                            L.T("Port Changed", "端口更改"), 
                            L.IsChinese()
                                ? $"端口已更改为 {portValue}，服务器已自动重启。"
                                : $"Port changed to {portValue}. Server restarted automatically.", 
                            L.T("OK", "确定"));
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        L.T("Port Error", "端口错误"), 
                        L.IsChinese()
                            ? $"无效的端口号: {portValue}\n端口范围应为 1024-65535"
                            : $"Invalid port number: {portValue}\nPort range should be 1024-65535", 
                        L.T("OK", "确定"));
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = originalColor;

            // 端口诊断按钮
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            if (GUILayout.Button(L.T("Diagnose", "诊断"), applyButtonStyle, GUILayout.Width(60), GUILayout.Height(20)))
            {
                ShowPortDiagnostics();
            }
            GUI.backgroundColor = originalColor;

            // 显示端口状态提示
            GUIStyle statusLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            statusLabelStyle.fontSize = 9;
            
            if (!string.IsNullOrEmpty(portInputString) && !isValidPort)
            {
                statusLabelStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
                EditorGUILayout.LabelField(L.T("Invalid", "无效"), statusLabelStyle, GUILayout.Width(40));
            }
            else if (isValidPort && portValue == McpService.mcpPort)
            {
                statusLabelStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                EditorGUILayout.LabelField(L.T("Current", "当前"), statusLabelStyle, GUILayout.Width(40));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(40)); // 占位符保持布局一致
            }
            
            GUI.color = originalTextColor;
        }

        /// <summary>
        /// 显示端口诊断信息
        /// </summary>
        private static void ShowPortDiagnostics()
        {
            int currentPort = McpService.mcpPort;
            bool isRunning = McpService.Instance.IsRunning;

            string title = L.T("MCP Server Port Diagnostics", "MCP服务器端口诊断");
            string message = L.IsChinese()
                ? $"MCP服务器端口诊断:\n\n当前端口: {currentPort}\n服务状态: {(isRunning ? "运行中" : "已停止")}\n\n"
                : $"MCP Server Port Diagnostics:\n\nCurrent Port: {currentPort}\nService Status: {(isRunning ? "Running" : "Stopped")}\n\n";

            // 获取详细的端口状态信息
            string portInfo = McpService.GetPortStatusInfo(currentPort);
            message += portInfo;

            if (L.IsChinese())
            {
                message += "\n建议:\n";
                message += "1. 如果端口被占用，尝试更换端口\n";
                message += "2. 检查防火墙是否阻止了连接\n";
                message += "3. 确保Cursor MCP客户端连接到正确的端口\n";
                message += "4. 查看Unity控制台的详细日志\n";
            }
            else
            {
                message += "\nSuggestions:\n";
                message += "1. If port is occupied, try changing to another port\n";
                message += "2. Check if firewall is blocking the connection\n";
                message += "3. Ensure Cursor MCP client connects to the correct port\n";
                message += "4. Check Unity console for detailed logs\n";
            }

            if (EditorUtility.DisplayDialog(
                title, 
                message, 
                L.T("Test Connection", "测试连接"), 
                L.T("Close", "关闭")))
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
                EditorUtility.DisplayDialog(
                    L.T("Connection Test", "连接测试"), 
                    L.T("MCP server is not running, cannot test connection.", "MCP服务器未运行，无法测试连接。"), 
                    L.T("OK", "确定"));
                return;
            }

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);

                    string url = $"http://127.0.0.1:{McpService.mcpPort}/";
                    Debug.Log($"[McpServiceStatusWindow] {L.T("Testing connection to", "测试连接到")}: {url}");

                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();

                    string result = L.IsChinese()
                        ? $"连接测试成功!\n\nURL: {url}\n状态码: {response.StatusCode}\n响应长度: {content.Length} 字符\n\n响应内容预览:\n{content.Substring(0, Math.Min(200, content.Length))}"
                        : $"Connection test successful!\n\nURL: {url}\nStatus Code: {response.StatusCode}\nResponse Length: {content.Length} chars\n\nResponse Preview:\n{content.Substring(0, Math.Min(200, content.Length))}";

                    if (content.Length > 200)
                    {
                        result += "...";
                    }

                    EditorUtility.DisplayDialog(
                        L.T("Connection Test Result", "连接测试结果"), 
                        result, 
                        L.T("OK", "确定"));
                }
            }
            catch (Exception ex)
            {
                string errorMsg = L.IsChinese()
                    ? $"连接测试失败!\n\n错误: {ex.Message}\n\n可能的原因:\n1. 服务器未正确启动\n2. 端口被防火墙阻止\n3. 网络配置问题\n"
                    : $"Connection test failed!\n\nError: {ex.Message}\n\nPossible causes:\n1. Server not started correctly\n2. Port blocked by firewall\n3. Network configuration issue\n";

                EditorUtility.DisplayDialog(
                    L.T("Connection Test Failed", "连接测试失败"), 
                    errorMsg, 
                    L.T("OK", "确定"));
                Debug.LogError($"[McpServiceStatusWindow] {L.T("Connection test failed", "连接测试失败")}: {ex}");
            }
        }
    }
}
