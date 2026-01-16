using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UniMcp.Models;
using UniMcp.Executer;
using System.Runtime.Remoting.Messaging;

namespace UniMcp.Gui
{
    /// <summary>
    /// MCP调试客户端窗口 - 用于测试和调试MCP函数调用
    /// </summary>
    public class McpDebugWindow : EditorWindow
    {
        [MenuItem("Window/MCP/Debug")]
        public static void ShowWindow()
        {
            McpDebugWindow window = GetWindow<McpDebugWindow>("MCP Debug Window");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        /// <summary>
        /// 打开调试窗口并预填充指定的JSON内容
        /// </summary>
        /// <param name="jsonContent">要预填充的JSON内容</param>
        public static void ShowWindowWithContent(string jsonContent)
        {
            McpDebugWindow window = GetWindow<McpDebugWindow>("MCP Debug Window");
            window.minSize = new Vector2(500, 300);
            window.SetInputJson(jsonContent);
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// 设置输入框的JSON内容
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        public void SetInputJson(string jsonContent)
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                inputJson = jsonContent;
                ClearResults(); // 清空之前的结果
                Repaint(); // 刷新界面
            }
        }

        // UI状态变量
        private Vector2 inputScrollPosition;
        private Vector2 resultScrollPosition;
        private string inputJson = "{\n  \"id\": \"test_task_1\",\n  \"type\": \"in\",\n  \"func\": \"hierarchy_create\",\n  \"args\": {\n    \"source\": \"primitive\",\n    \"primitive_type\": \"Cube\",\n    \"name\": \"RedCube\",\n    \"position\": [\n      0,\n      0,\n      0\n    ]\n  }\n}";
        private string resultText = "";
        private bool showResult = false;
        private bool isExecuting = false;
        private int currentExecutionIndex = 0; // 当前执行的任务索引
        private int totalExecutionCount = 0; // 总任务数

        private JsonNode currentResult = null; // 存储当前执行结果

        // 执行记录相关变量
        private ReorderableList recordList;
        private int selectedRecordIndex = -1;
        private Vector2 recordScrollPosition; // 记录列表滚动位置

        // 分组相关变量
        private bool showGroupManager = false; // 是否显示分组管理界面
        private string newGroupName = ""; // 新分组名称
        private string newGroupDescription = ""; // 新分组描述
        private Vector2 groupScrollPosition; // 分组列表滚动位置
        // 移除未使用的选中分组索引字段

        // 编辑相关变量
        private int editingRecordIndex = -1; // 当前正在编辑的记录索引
        private string editingText = ""; // 编辑中的文本
        private double lastClickTime = 0; // 上次点击时间，用于检测双击
        private int lastClickedIndex = -1; // 上次点击的索引
        private bool editingStarted = false; // 标记编辑是否刚开始

        // 分栏布局相关变量
        private float splitterPos = 0.3f; // 默认左侧占30%
        private bool isDraggingSplitter = false;
        private const float SplitterWidth = 4f;

        // 布局参数
        private const float MinInputHeight = 100f;
        private const float MaxInputHeight = 300f;
        private const float LineHeight = 16f;
        private const float ResultAreaHeight = 200f;

        // 样式
        private GUIStyle headerStyle;
        private GUIStyle codeStyle;
        private GUIStyle inputStyle;  // 专门用于输入框的样式
        private GUIStyle resultStyle;
        private ToolsCall methodsCall = new ToolsCall();
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    fontStyle = FontStyle.Bold
                };
            }

            if (codeStyle == null)
            {
                codeStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,  // 启用自动换行
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    stretchWidth = false,  // 不自动拉伸，使用固定宽度
                    stretchHeight = true   // 拉伸以适应容器高度
                };
            }

            if (inputStyle == null)
            {
                inputStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,        // 强制启用自动换行
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    stretchWidth = false,   // 不自动拉伸宽度
                    stretchHeight = true,   // 允许高度拉伸
                    fixedWidth = 0,         // 不使用固定宽度
                    fixedHeight = 0,        // 不使用固定高度
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(4, 4, 4, 4)
                };
                inputStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                inputStyle.normal.background = Texture2D.blackTexture;
                inputStyle.active.textColor = Color.white;
                inputStyle.focused.textColor = Color.white;
            }

            if (resultStyle == null)
            {
                resultStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,        // 启用自动换行
                    fontSize = 12,          // 与输入框保持一致的字体大小
                    fontStyle = FontStyle.Normal,
                    richText = true,        // 支持富文本，方便显示格式化内容
                    stretchWidth = false,   // 与输入框保持一致，不自动拉伸宽度
                    stretchHeight = true,   // 拉伸以适应容器高度
                    margin = new RectOffset(2, 2, 2, 2),    // 与输入框保持一致的边距
                    padding = new RectOffset(4, 4, 4, 4)    // 与输入框保持一致的内边距
                };
                resultStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                resultStyle.normal.background = Texture2D.blackTexture;
                resultStyle.active.textColor = Color.white;
                resultStyle.focused.textColor = Color.white;
            }

            InitializeRecordList();
        }

        private void InitializeRecordList()
        {
            if (recordList == null)
            {
                // 获取当前分组的记录（分组功能会自动初始化）
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                recordList = new ReorderableList(records, typeof(McpExecuteRecordObject.McpExecuteRecord), false, true, false, true);

                recordList.drawHeaderCallback = (Rect rect) =>
                {
                    // 绘制背景
                    EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
                    
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    int successCount = records.Where(r => r.success).Count();
                    int errorCount = records.Count - successCount;
                    var recordObject = McpExecuteRecordObject.instance;

                    // 分组下拉框直接作为标题
                    var groups = recordObject.recordGroups;
                    if (groups.Count == 0)
                    {
                        GUIStyle headerLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                        headerLabelStyle.normal.textColor = Color.white;
                        headerLabelStyle.alignment = TextAnchor.MiddleLeft;
                        headerLabelStyle.padding = new RectOffset(10, 0, 0, 0);
                        EditorGUI.LabelField(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), "暂无分组", headerLabelStyle);
                    }
                    else
                    {
                        // 构建分组选项（包含统计信息）
                        string[] groupNames = groups.Select(g =>
                        {
                            string stats = recordObject.GetGroupStatistics(g.id);
                            return $"{g.name} ({stats})";
                        }).ToArray();

                        int currentIndex = groups.FindIndex(g => g.id == recordObject.currentGroupId);
                        if (currentIndex == -1) currentIndex = 0;

                        GUIStyle popupStyle = new GUIStyle(EditorStyles.boldLabel);
                        popupStyle.normal.textColor = Color.white;
                        popupStyle.alignment = TextAnchor.MiddleLeft;
                        popupStyle.padding = new RectOffset(10, 0, 0, 0);

                        EditorGUI.BeginChangeCheck();
                        int newIndex = EditorGUI.Popup(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), currentIndex, groupNames, popupStyle);
                        if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < groups.Count)
                        {
                            recordObject.SwitchToGroup(groups[newIndex].id);
                            recordList = null;
                            EditorApplication.delayCall += () =>
                            {
                                InitializeRecordList();
                                Repaint();
                            };
                        }
                    }
                };

                recordList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    if (index >= 0 && index < records.Count)
                    {
                        var record = records[records.Count - 1 - index]; // 倒序显示
                        DrawRecordElement(rect, record, records.Count - 1 - index, isActive, isFocused);
                    }
                };

                recordList.onSelectCallback = (ReorderableList list) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    if (list.index >= 0 && list.index < records.Count)
                    {
                        int actualIndex = records.Count - 1 - list.index; // 转换为实际索引
                        SelectRecord(actualIndex);
                    }
                };

                recordList.onRemoveCallback = (ReorderableList list) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    if (list.index >= 0 && list.index < records.Count)
                    {
                        int actualIndex = records.Count - 1 - list.index;
                        if (EditorUtility.DisplayDialog("确认删除", $"确定要删除这条执行记录吗？\n函数: {records[actualIndex].name}", "删除", "取消"))
                        {
                            records.RemoveAt(actualIndex);
                            McpExecuteRecordObject.instance.saveRecords();
                            if (selectedRecordIndex == actualIndex)
                            {
                                selectedRecordIndex = -1;
                            }
                        }
                    }
                };

                recordList.elementHeight = 40f; // 设置元素高度
            }
        }

        /// <summary>
        /// 根据文本内容动态计算输入框高度（考虑自动换行和固定宽度）
        /// </summary>
        private float CalculateInputHeight()
        {
            if (string.IsNullOrEmpty(inputJson))
                return MinInputHeight;

            // 基础行数计算
            int basicLineCount = inputJson.Split('\n').Length;

            // 根据固定宽度估算换行，考虑字体大小和宽度限制
            // 估算每行可显示的字符数（基于12px字体和可用宽度）
            const int avgCharsPerLine = 60; // 保守估计，适应较窄的面板
            int totalChars = inputJson.Length;
            int estimatedWrappedLines = Mathf.CeilToInt((float)totalChars / avgCharsPerLine);

            // 取较大值作为实际行数估算，但给换行更多权重
            int estimatedLineCount = Mathf.Max(basicLineCount, (int)(estimatedWrappedLines * 0.8f));

            // 根据行数计算高度，加上适当的padding
            float calculatedHeight = estimatedLineCount * LineHeight + 40f; // 适当的padding

            // 限制在最小和最大高度之间
            return Mathf.Clamp(calculatedHeight, MinInputHeight, MaxInputHeight);
        }

        /// <summary>
        /// 计算标题区域的实际高度
        /// </summary>
        private float CalculateHeaderHeight()
        {
            // 标题文字高度（基于headerStyle的fontSize）
            float titleHeight = headerStyle?.fontSize ?? 14;
            titleHeight += 16; // 标题的上下边距，增加更多空间

            // 间距
            float spacing = 10; // 增加间距

            // 总高度，确保有足够空间显示标题
            return titleHeight + spacing + 10; // 增加额外边距
        }

        private void OnGUI()
        {
            InitializeStyles();

            // 绘制窗口背景
            Rect windowRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(windowRect, new Color(0.22f, 0.22f, 0.22f, 1f));

            // 计算标题区域的实际高度
            float headerHeight = CalculateHeaderHeight();

            // 分栏布局
            DrawSplitView(headerHeight);

            // 处理分栏拖拽
            HandleSplitterEvents(headerHeight);
        }

        /// <summary>
        /// 在右上角绘制连接状态和详情按钮
        /// </summary>
        private void DrawConnectionStatus(Rect rect)
        {
            // Style for the status button, looks like a label but is clickable
            GUIStyle statusButtonStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true,
                fontSize = 11,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                padding = new RectOffset(0, 10, 0, 0)
            };

            // Generate the status text
            bool isRunning = McpService.Instance.IsRunning;
            Color statusColor = isRunning ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            string statusText = isRunning ? "Running" : "Stopped";
            int clientCount = McpService.Instance.ConnectedClientCount;
            string portText = McpService.Instance.IsRunning ? $" on port {McpService.mcpPort}" : "";

            string fullStatusText = $"<color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>●</color> <b>{statusText}</b> ({clientCount} 请求记录{portText})";

            // Draw a single button that covers the whole area and acts as the status display
            // 移除背景，只显示文字，避免遮挡标题
            if (GUI.Button(rect, new GUIContent(fullStatusText, "点击查看请求记录详情"), statusButtonStyle))
            {
                McpServiceStatusWindow.ShowWindow();
            }
        }

        private void DrawSplitView(float headerHeight)
        {
            Rect windowRect = new Rect(0, headerHeight, position.width, position.height - headerHeight);
            float leftWidth = windowRect.width * splitterPos;
            float rightWidth = windowRect.width * (1 - splitterPos) - SplitterWidth;

            // 左侧区域 - 执行记录
            Rect leftRect = new Rect(windowRect.x, windowRect.y, leftWidth, windowRect.height);
            DrawLeftPanel(leftRect);

            // 分隔条
            Rect splitterRect = new Rect(leftRect.xMax, windowRect.y, SplitterWidth, windowRect.height);
            DrawSplitter(splitterRect);

            // 右侧区域 - 原有功能
            Rect rightRect = new Rect(splitterRect.xMax, windowRect.y, rightWidth, windowRect.height);
            DrawRightPanel(headerHeight, rightRect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            // 使用更精确的垂直布局
            float currentY = 5; // 恢复原来的起始位置
            float padding = 5;  // 恢复原来的内边距

            // 记录列表操作按钮区域
            Rect buttonRect = new Rect(padding, currentY, rect.width - padding * 2, 28);
            GUI.BeginGroup(buttonRect);
            GUILayout.BeginArea(new Rect(0, 0, buttonRect.width, buttonRect.height));

            GUILayout.BeginHorizontal();

            Color originalBg = GUI.backgroundColor;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 10;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("刷新", buttonStyle, GUILayout.Width(50), GUILayout.Height(22)))
            {
                recordList = null;
                InitializeRecordList();
                Repaint();
            }

            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("清空当前分组", buttonStyle, GUILayout.Width(100), GUILayout.Height(22)))
            {
                string confirmMessage = $"确定要清空当前分组 '{GetCurrentGroupDisplayName()}' 的所有记录吗？\n此操作不会影响其他分组。";

                if (EditorUtility.DisplayDialog("确认清空", confirmMessage, "确定", "取消"))
                {
                    // 仅清空当前分组，禁止清空全部
                    McpExecuteRecordObject.instance.ClearCurrentGroupRecords();
                    McpExecuteRecordObject.instance.saveRecords();
                    selectedRecordIndex = -1;
                    recordList = null;
                    InitializeRecordList();
                }
            }

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.7f);
            if (GUILayout.Button(showGroupManager ? "隐藏" : "管理", buttonStyle, GUILayout.Width(60), GUILayout.Height(22)))
            {
                showGroupManager = !showGroupManager;
            }

            GUI.backgroundColor = originalBg;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.EndGroup();
            currentY += 30;


            // 分组管理区域
            if (showGroupManager)
            {
                float groupManagerHeight = CalculateGroupManagerHeight();
                Rect groupManagerRect = new Rect(padding, currentY, rect.width - padding * 2, groupManagerHeight);
                GUI.BeginGroup(groupManagerRect);
                GUILayout.BeginArea(new Rect(0, 0, groupManagerRect.width, groupManagerRect.height));
                DrawGroupManager(groupManagerRect.width);
                GUILayout.EndArea();
                GUI.EndGroup();
                currentY += groupManagerHeight + padding;
            }

            // 记录列表区域
            if (recordList != null)
            {
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                recordList.list = records;

                float listContentHeight = recordList.GetHeight();
                // 让列表完全填充到底部，不留边距
                float availableHeight = rect.height;
                // 列表区域直接填充到底部，左右保留padding，底部不留边距
                Rect scrollViewRect = new Rect(padding, currentY, rect.width - padding * 2, availableHeight);
                Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width - 16, listContentHeight);

                recordScrollPosition = GUI.BeginScrollView(scrollViewRect, recordScrollPosition, scrollContentRect, false, true);
                recordList.DoList(new Rect(0, 0, scrollContentRect.width, listContentHeight));
                GUI.EndScrollView();
            }
        }

        private void DrawRightPanel(float headerHeight, Rect rect)
        {
            var rightWidth = position.width - rect.x;
            
            // 绘制标题区域背景
            Rect titleRect = new Rect(rect.x, 0, rightWidth, headerHeight);
            
            // 在Repaint阶段绘制背景，确保不遮挡文字
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(titleRect, new Color(0.25f, 0.25f, 0.25f, 1f));
                
                // 绘制边框
                EditorGUI.DrawRect(new Rect(titleRect.x, titleRect.y, titleRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
                EditorGUI.DrawRect(new Rect(titleRect.x, titleRect.y + titleRect.height - 1, titleRect.width, 1), new Color(0.4f, 0.4f, 0.4f));
            }
            
            // 先绘制标题在顶部居中
            GUI.BeginGroup(titleRect);
            GUILayout.BeginArea(new Rect(0, 0, titleRect.width, titleRect.height));
            GUILayout.Space(8); // 顶部间距
            GUILayout.Label("Unity MCP Debug Client", headerStyle);
            GUILayout.EndArea();
            GUI.EndGroup();

            // 在标题下方绘制连接状态，避免覆盖标题
            Rect statusRect = new Rect(rect.x + rightWidth - 350, titleRect.yMax - 2, 340, 20);
            DrawConnectionStatus(statusRect);

            GUILayout.BeginArea(rect);

            // 使用垂直布局组来控制整体宽度，允许扩展高度
            GUILayout.BeginVertical(GUILayout.MaxWidth(rect.width), GUILayout.ExpandHeight(true));

            // JSON输入框区域
            DrawInputArea(rect.width);

            GUILayout.Space(10);

            // 操作按钮区域
            DrawControlButtons();

            GUILayout.Space(10);

            // 结果显示区域 - 填充剩余空间
            if (showResult)
            {
                // 使用FlexibleSpace让结果区域填充剩余空间
                GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                DrawResultArea(rect.width);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawSplitter(Rect rect)
        {
            // 绘制分隔条背景
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            
            // 绘制中间线
            float centerX = rect.x + rect.width / 2;
            EditorGUI.DrawRect(new Rect(centerX - 0.5f, rect.y, 1, rect.height), new Color(0.5f, 0.5f, 0.5f));

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        }

        private void HandleSplitterEvents(float headerHeight)
        {
            Event e = Event.current;
            Rect windowRect = new Rect(0, headerHeight, position.width, position.height - headerHeight);
            float splitterX = windowRect.width * splitterPos;
            Rect splitterRect = new Rect(splitterX, headerHeight, SplitterWidth, windowRect.height);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition))
                    {
                        isDraggingSplitter = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isDraggingSplitter)
                    {
                        float newSplitterPos = e.mousePosition.x / position.width;
                        splitterPos = Mathf.Clamp(newSplitterPos, 0.2f, 0.8f); // 限制在20%-80%之间
                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDraggingSplitter)
                    {
                        isDraggingSplitter = false;
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawRecordElement(Rect rect, McpExecuteRecordObject.McpExecuteRecord record, int index, bool isActive, bool isFocused)
        {
            Color originalColor = GUI.color;

            // 添加padding - 每个元素都有padding
            const float padding = 6f;
            Rect paddedRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2, rect.height - padding * 2);

            // 处理鼠标事件（双击检测）
            HandleRecordElementMouseEvents(rect, index);

            // 绘制背景（交替颜色）
            Color bgColor = index % 2 == 0 ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.22f, 0.22f, 0.22f, 0.5f);
            if (isActive || selectedRecordIndex == index)
            {
                // 选中时显示背景颜色
                bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f); // 蓝色高亮
            }
            EditorGUI.DrawRect(rect, bgColor);

            // 绘制边框
            Color borderColor = new Color(0.4f, 0.4f, 0.4f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), borderColor);

            GUI.color = originalColor;

            // 绘制内容（在box内部）
            const float numberWidth = 24f; // 序号宽度
            const float iconWidth = 20f;
            const float boxMargin = 4f; // box内部边距

            // 计算box内部的绘制区域
            Rect contentRect = new Rect(paddedRect.x + boxMargin, paddedRect.y + boxMargin,
                paddedRect.width - boxMargin * 2, paddedRect.height - boxMargin * 2);

            // 序号显示（左上角，在box内部）
            var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
            int displayIndex = index + 1; // 正序显示序号，从1开始
            Rect numberRect = new Rect(contentRect.x, contentRect.y, numberWidth, 14f);
            Color numberColor = new Color(0.6f, 0.6f, 0.6f);
            Color originalContentColor = GUI.contentColor;
            GUI.contentColor = numberColor;
            GUIStyle numberStyle = new GUIStyle(EditorStyles.miniLabel);
            numberStyle.fontStyle = FontStyle.Bold;
            GUI.Label(numberRect, $"#{displayIndex}", numberStyle);
            GUI.contentColor = originalContentColor;

            // 状态图标（在box内部）- 使用彩色指示条
            Rect statusRect = new Rect(contentRect.x + numberWidth + 2f, contentRect.y + 2f, 3, 12f);
            Color statusColor = record.success ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
            EditorGUI.DrawRect(statusRect, statusColor);
            
            // 状态文字
            string statusText = record.success ? "✓" : "✗";
            Rect iconRect = new Rect(contentRect.x + numberWidth + 6f, contentRect.y, iconWidth, 16f);
            Color iconColor = record.success ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
            GUI.contentColor = iconColor;
            GUI.Label(iconRect, statusText, EditorStyles.boldLabel);
            GUI.contentColor = originalContentColor;

            // 函数名（第一行）- 在box内部，为序号和图标留出空间
            Rect funcRect = new Rect(contentRect.x + numberWidth + iconWidth + 4f, contentRect.y,
                contentRect.width - numberWidth - iconWidth - 4f, 16f);

            // 检查是否正在编辑此记录
            if (editingRecordIndex == index)
            {
                // 编辑模式：显示文本输入框
                GUI.SetNextControlName($"RecordEdit_{index}");

                // 先处理键盘事件，确保优先级
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        // 回车确认编辑
                        FinishEditing(index, editingText);
                        Event.current.Use();
                        return; // 直接返回，避免继续处理其他事件
                    }
                    else if (Event.current.keyCode == KeyCode.Escape)
                    {
                        // ESC取消编辑
                        CancelEditing();
                        Event.current.Use();
                        return; // 直接返回，避免继续处理其他事件
                    }
                }

                // 设置焦点（只在刚开始编辑时设置）
                if (editingStarted)
                {
                    GUI.FocusControl($"RecordEdit_{index}");
                    editingStarted = false;
                }

                // 绘制输入框背景
                EditorGUI.DrawRect(funcRect, new Color(0.1f, 0.1f, 0.1f, 1f));
                
                // 使用BeginChangeCheck来检测文本变化
                EditorGUI.BeginChangeCheck();
                GUIStyle editStyle = new GUIStyle(EditorStyles.textField);
                editStyle.normal.textColor = Color.white;
                editStyle.normal.background = Texture2D.blackTexture;
                string newName = EditorGUI.TextField(funcRect, editingText, editStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    editingText = newName;
                }

                // 检测失去焦点
                if (Event.current.type == EventType.Repaint)
                {
                    string focusedControl = GUI.GetNameOfFocusedControl();
                    if (string.IsNullOrEmpty(focusedControl) || focusedControl != $"RecordEdit_{index}")
                    {
                        // 延迟一帧检查，避免刚设置焦点就检测到失去焦点
                        EditorApplication.delayCall += () =>
                        {
                            if (editingRecordIndex == index && GUI.GetNameOfFocusedControl() != $"RecordEdit_{index}")
                            {
                                FinishEditing(index, editingText);
                            }
                        };
                    }
                }
            }
            else
            {
                // 正常模式：显示函数名
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
                nameStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                GUI.Label(funcRect, record.name, nameStyle);
            }

            // 时间和来源（第二行）- 在box内部
            Rect timeRect = new Rect(contentRect.x + numberWidth + iconWidth + 4f, contentRect.y + 18f,
                contentRect.width - numberWidth - iconWidth - 4f, 14f);
            string timeInfo = $"{record.timestamp} | [{record.source}]";
            if (record.duration > 0)
            {
                // 根据时长设置颜色
                Color durationColor = record.duration < 100 ? new Color(0.4f, 0.8f, 0.4f) : 
                                    (record.duration < 500 ? new Color(0.8f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));
                timeInfo += $" | <color=#{ColorUtility.ToHtmlStringRGB(durationColor)}>{record.duration:F1}ms</color>";
            }

            // 为时间信息设置较淡的颜色
            Color timeColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.contentColor = timeColor;
            GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel);
            timeStyle.richText = true;
            GUI.Label(timeRect, timeInfo, timeStyle);
            GUI.contentColor = originalContentColor;
        }

        /// <summary>
        /// 绘制输入区域（带滚动和动态高度）
        /// </summary>
        private void DrawInputArea(float availableWidth)
        {
            // 标题样式
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("MCP调用 (JSON格式):", labelStyle);

            float inputHeight = CalculateInputHeight();
            float textAreaWidth = availableWidth; // 减去边距和滚动条宽度

            // 创建输入框的滚动区域，限制宽度避免水平滚动
            Rect inputBoxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(inputBoxRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // 绘制边框
            EditorGUI.DrawRect(new Rect(inputBoxRect.x, inputBoxRect.y, inputBoxRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(inputBoxRect.x, inputBoxRect.y + inputBoxRect.height - 1, inputBoxRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(inputBoxRect.x, inputBoxRect.y, 1, inputBoxRect.height), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(inputBoxRect.x + inputBoxRect.width - 1, inputBoxRect.y, 1, inputBoxRect.height), new Color(0.3f, 0.3f, 0.3f));
            
            inputScrollPosition = EditorGUILayout.BeginScrollView(
                inputScrollPosition,
                false, true,  // 禁用水平滚动条，启用垂直滚动条
                GUILayout.Height(inputHeight),
                GUILayout.ExpandWidth(true)
            );

            // 输入框，使用专门的输入样式确保自动换行
            inputJson = EditorGUILayout.TextArea(
                inputJson,
                inputStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(textAreaWidth),
                GUILayout.MaxWidth(textAreaWidth)  // 确保不会超过指定宽度
            );

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // 显示行数信息
            int lineCount = inputJson?.Split('\n').Length ?? 0;
            GUIStyle infoStyle = new GUIStyle(EditorStyles.miniLabel);
            infoStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label($"行数: {lineCount} | 高度: {inputHeight:F0}px", infoStyle);
        }

        /// <summary>
        /// 绘制控制按钮区域
        /// </summary>
        private void DrawControlButtons()
        {
            // 获取剪贴板可用性
            bool clipboardAvailable = IsClipboardAvailable();

            // 第一行按钮
            GUILayout.BeginHorizontal();

            Color originalBg = GUI.backgroundColor;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 11;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;

            GUI.enabled = !isExecuting;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            if (GUILayout.Button("执行", buttonStyle, GUILayout.Height(30), GUILayout.Width(100)))
            {
                ExecuteCall();
            }

            GUI.enabled = !isExecuting && clipboardAvailable;
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("执行剪贴板", buttonStyle, GUILayout.Height(30), GUILayout.Width(100)))
            {
                ExecuteClipboard();
            }
            GUI.enabled = true;

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.7f);
            if (GUILayout.Button("格式化JSON", buttonStyle, GUILayout.Height(30), GUILayout.Width(120)))
            {
                FormatJson();
            }

            GUI.backgroundColor = new Color(0.7f, 0.4f, 0.4f);
            if (GUILayout.Button("清空", buttonStyle, GUILayout.Height(30), GUILayout.Width(60)))
            {
                inputJson = "{}";
                ClearResults();
            }

            GUI.backgroundColor = originalBg;

            if (isExecuting)
            {
                GUIStyle executingStyle = new GUIStyle(EditorStyles.label);
                executingStyle.normal.textColor = new Color(0.8f, 0.8f, 0.4f);
                executingStyle.fontStyle = FontStyle.Bold;
                if (totalExecutionCount > 1)
                {
                    GUILayout.Label($"执行中... ({currentExecutionIndex}/{totalExecutionCount})", executingStyle, GUILayout.Width(150));
                }
                else
                {
                    GUILayout.Label("执行中...", executingStyle, GUILayout.Width(100));
                }
            }

            GUILayout.EndHorizontal();

            // 第二行按钮（剪贴板操作）
            GUILayout.BeginHorizontal();

            // 剪贴板操作按钮 - 根据剪贴板内容动态启用/禁用
            GUI.enabled = clipboardAvailable;
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
            if (GUILayout.Button("粘贴到输入框", buttonStyle, GUILayout.Height(25), GUILayout.Width(100)))
            {
                PasteFromClipboard();
            }

            GUI.backgroundColor = new Color(0.4f, 0.5f, 0.7f);
            if (GUILayout.Button("预览剪贴板", buttonStyle, GUILayout.Height(25), GUILayout.Width(100)))
            {
                PreviewClipboard();
            }
            GUI.enabled = true;
            GUI.backgroundColor = originalBg;

            // 显示剪贴板状态 - 带颜色指示
            DrawClipboardStatus();

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制结果显示区域（带滚动）
        /// </summary>
        private void DrawResultArea(float availableWidth)
        {
            // 标题样式
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            labelStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("执行结果", labelStyle);

            float textAreaWidth = availableWidth - 40; // 减去边距和滚动条宽度

            // 创建结果显示的滚动区域，限制宽度避免水平滚动
            Rect resultBoxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(resultBoxRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // 绘制边框
            EditorGUI.DrawRect(new Rect(resultBoxRect.x, resultBoxRect.y, resultBoxRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(resultBoxRect.x, resultBoxRect.y + resultBoxRect.height - 1, resultBoxRect.width, 1), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(resultBoxRect.x, resultBoxRect.y, 1, resultBoxRect.height), new Color(0.3f, 0.3f, 0.3f));
            EditorGUI.DrawRect(new Rect(resultBoxRect.x + resultBoxRect.width - 1, resultBoxRect.y, 1, resultBoxRect.height), new Color(0.3f, 0.3f, 0.3f));
            
            resultScrollPosition = EditorGUILayout.BeginScrollView(
                resultScrollPosition,
                false, true,  // 禁用水平滚动条，启用垂直滚动条
                GUILayout.ExpandHeight(true),  // 填充剩余空间
                GUILayout.MaxWidth(availableWidth)
            );

            // 结果文本区域，限制宽度以防止水平溢出
            EditorGUILayout.TextArea(
                resultText,
                resultStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(textAreaWidth)
            );

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // 结果操作按钮
            GUILayout.BeginHorizontal();

            Color originalBg = GUI.backgroundColor;
            GUIStyle resultButtonStyle = new GUIStyle(GUI.skin.button);
            resultButtonStyle.fontSize = 10;
            resultButtonStyle.fontStyle = FontStyle.Bold;
            resultButtonStyle.normal.textColor = Color.white;
            resultButtonStyle.hover.textColor = Color.white;
            resultButtonStyle.active.textColor = Color.white;

            // 记录结果按钮 - 只有当有执行结果且不是从历史记录加载时才显示
            if (currentResult != null && !string.IsNullOrEmpty(inputJson))
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                if (GUILayout.Button("记录结果", resultButtonStyle, GUILayout.Width(80)))
                {
                    RecordCurrentResult();
                }
            }

            // 格式化结果按钮 - 只有当有结果文本时才显示
            if (!string.IsNullOrEmpty(resultText))
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.7f);
                if (GUILayout.Button("格式化结果", resultButtonStyle, GUILayout.Width(80)))
                {
                    FormatResultText();
                }
            }

            // 检查是否为批量结果，如果是则显示额外操作
            if (IsBatchResultDisplayed())
            {
                GUI.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
                if (GUILayout.Button("复制统计", resultButtonStyle, GUILayout.Width(80)))
                {
                    CopyBatchStatistics();
                }

                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.3f);
                if (GUILayout.Button("仅显示错误", resultButtonStyle, GUILayout.Width(80)))
                {
                    ShowOnlyErrors();
                }
            }

            GUI.backgroundColor = originalBg;
            GUILayout.EndHorizontal();
        }

        private void FormatJson()
        {
            try
            {
                JsonClass jsonObj = Json.Parse(inputJson) as JsonClass;
                inputJson = Json.FromObject(jsonObj).ToPrettyString();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("JSON格式错误", $"无法解析JSON: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 格式化结果文本，支持JSON和YAML
        /// </summary>
        private void FormatResultText()
        {
            if (string.IsNullOrEmpty(resultText))
            {
                EditorUtility.DisplayDialog("提示", "没有可格式化的结果", "确定");
                return;
            }

            try
            {
                StringBuilder formattedResult = new StringBuilder();
                string[] lines = resultText.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

                int i = 0;
                while (i < lines.Length)
                {
                    string line = lines[i];
                    if (string.IsNullOrEmpty(line))
                    {
                        i++;
                        continue;
                    }

                    // 检查当前行是否是 Json 对象或数组的开始
                    string trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWith("{") || trimmedLine.StartsWith("["))
                    {
                        // 收集完整的 Json 块
                        string jsonBlock = CollectJsonBlock(lines, ref i);

                        // 尝试格式化 Json
                        string formattedJson = TryFormatJson(jsonBlock);
                        if (formattedJson != null)
                        {
                            formattedResult.AppendLine(formattedJson);
                        }
                        else
                        {
                            // 格式化失败，保持原样
                            formattedResult.AppendLine(jsonBlock);
                        }
                    }
                    else if (!string.IsNullOrEmpty(line))
                    {
                        // 非 Json 行，检查是否包含内嵌 Json
                        string processedLine = ProcessLineWithEmbeddedJson(line);
                        formattedResult.AppendLine(processedLine);
                        i++;
                    }
                    else
                    {
                        // 空行，保持原样
                        formattedResult.AppendLine(line);
                        i++;
                    }
                }

                resultText = formattedResult.ToString().TrimEnd();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[McpDebugWindow] 格式化结果时发生错误: {e.Message}");
                EditorUtility.DisplayDialog("格式化失败", $"无法格式化结果: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 收集完整的 Json 块（处理多行 JSON）
        /// </summary>
        private string CollectJsonBlock(string[] lines, ref int startIndex)
        {
            StringBuilder jsonBlock = new StringBuilder();
            int braceCount = 0;
            int bracketCount = 0;
            bool inString = false;
            bool isFirstLine = true;

            for (int i = startIndex; i < lines.Length; i++)
            {
                string line = lines[i];
                jsonBlock.AppendLine(line);

                // 计算括号平衡
                foreach (char c in line)
                {
                    if (c == '"' && (jsonBlock.Length == 0 || jsonBlock[jsonBlock.Length - 2] != '\\'))
                    {
                        inString = !inString;
                    }

                    if (!inString)
                    {
                        if (c == '{') braceCount++;
                        else if (c == '}') braceCount--;
                        else if (c == '[') bracketCount++;
                        else if (c == ']') bracketCount--;
                    }
                }

                // 检查是否完成
                if (!isFirstLine && braceCount == 0 && bracketCount == 0)
                {
                    startIndex = i + 1;
                    return jsonBlock.ToString().TrimEnd();
                }

                isFirstLine = false;
            }

            startIndex = lines.Length;
            return jsonBlock.ToString().TrimEnd();
        }

        /// <summary>
        /// 处理包含内嵌 Json 的行（如 "执行结果: {...}"）
        /// </summary>
        private string ProcessLineWithEmbeddedJson(string line)
        {
            // 查找第一个 { 或 [ 的位置
            int jsonStartIndex = -1;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '{' || line[i] == '[')
                {
                    jsonStartIndex = i;
                    break;
                }
            }

            if (jsonStartIndex > 0)
            {
                // 有前缀文本和 Json
                string prefix = line.Substring(0, jsonStartIndex);
                string jsonPart = line.Substring(jsonStartIndex);

                // 尝试格式化 Json 部分
                string formattedJson = TryFormatJson(jsonPart);
                if (formattedJson != null)
                {
                    // 成功格式化，返回前缀 + 格式化后的 JSON（缩进对齐）
                    string[] jsonLines = formattedJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    StringBuilder result = new StringBuilder();
                    result.AppendLine(prefix);
                    foreach (string jsonLine in jsonLines)
                    {
                        result.AppendLine(jsonLine);
                    }
                    return result.ToString().TrimEnd();
                }
            }

            return line;
        }

        /// <summary>
        /// 尝试将文本格式化为JSON
        /// </summary>
        private string TryFormatJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                // 尝试作为JObject解析
                try
                {
                    JsonClass jsonObj = Json.Parse(text) as JsonClass;
                    string formatted = Json.FromObject(jsonObj).ToPrettyString();

                    // 序列化后展开 yaml 字段中的 \n
                    return ExpandYamlInFormattedJson(formatted);
                }
                catch
                {
                    // 尝试作为JArray解析
                    JsonArray jsonArray = Json.Parse(text) as JsonArray;
                    string formatted = Json.FromObject(jsonArray).ToPrettyString();

                    // 序列化后展开 yaml 字段中的 \n
                    return ExpandYamlInFormattedJson(formatted);
                }
            }
            catch
            {
                // 不是有效的JSON
                return null;
            }
        }

        /// <summary>
        /// 在格式化后的 Json 文本中展开所有字符串字段的换行符
        /// </summary>
        private string ExpandYamlInFormattedJson(string formattedJson)
        {
            if (string.IsNullOrEmpty(formattedJson))
                return formattedJson;

            // 匹配所有的 "fieldName": "value" 模式，不限定字段名
            // 匹配任意字段名和其字符串值
            string pattern = @"""([^""]+)"":\s*""([^""\\]*(\\.[^""\\]*)*)""";

            return System.Text.RegularExpressions.Regex.Replace(formattedJson, pattern, match =>
            {
                string fieldName = match.Groups[1].Value;
                string fieldValue = match.Groups[2].Value;

                // 只有包含 \r\n 或 \n 才处理
                if (!fieldValue.Contains("\\r\\n") && !fieldValue.Contains("\\n"))
                    return match.Value;

                // 展开 \r\n 和 \n 为实际换行，并保持 Json 的缩进
                string expandedValue = fieldValue.Replace("\\r\\n", "\n").Replace("\\n", "\n");

                // 获取当前的缩进级别
                int indentLevel = GetIndentLevel(formattedJson, match.Index);
                string indent = new string(' ', indentLevel + 2); // +2 是因为字符串内容需要额外缩进

                // 为内容的每一行添加适当的缩进（除了第一行）
                string[] lines = expandedValue.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    lines[i] = indent + lines[i];
                }
                expandedValue = string.Join("\n", lines);

                // 返回格式化后的结果
                return $"\"{fieldName}\": \"{expandedValue}\"";
            });
        }

        /// <summary>
        /// 获取指定位置的缩进级别
        /// </summary>
        private int GetIndentLevel(string text, int position)
        {
            // 向前查找到行首
            int lineStart = position;
            while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
            {
                lineStart--;
            }

            // 计算缩进空格数
            int indent = 0;
            for (int i = lineStart; i < position && i < text.Length; i++)
            {
                if (text[i] == ' ')
                    indent++;
                else if (text[i] == '\t')
                    indent += 4; // 制表符算作4个空格
                else
                    break;
            }

            return indent;
        }

        /// <summary>
        /// 尝试格式化YAML文本
        /// </summary>
        private string TryFormatYaml(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                // 检测YAML特征：包含冒号和换行，但不是JSON格式
                if (!text.Contains(":") || text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
                    return null;

                // 简单的YAML格式化：确保每个键值对单独一行，适当缩进
                string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder formatted = new StringBuilder();

                int indentLevel = 0;
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    // 检测缩进变化
                    if (trimmedLine.Contains(":"))
                    {
                        // 键值对
                        string[] parts = trimmedLine.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            // 如果值为空或是列表/对象开始，可能需要增加缩进
                            if (string.IsNullOrEmpty(value) || value == "[" || value == "{")
                            {
                                formatted.AppendLine($"{new string(' ', indentLevel * 2)}{key}:");
                                if (value == "[" || value == "{")
                                {
                                    indentLevel++;
                                }
                            }
                            else
                            {
                                formatted.AppendLine($"{new string(' ', indentLevel * 2)}{key}: {value}");
                            }
                        }
                        else
                        {
                            formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                        }
                    }
                    else if (trimmedLine == "]" || trimmedLine == "}")
                    {
                        indentLevel = Math.Max(0, indentLevel - 1);
                        formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                    }
                    else if (trimmedLine.StartsWith("-"))
                    {
                        // 列表项
                        formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                    }
                    else
                    {
                        formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                    }
                }

                string result = formatted.ToString().TrimEnd();

                // 如果格式化后与原文本差异很大，可能不是YAML，返回null
                if (result.Length > text.Length * 2 || result.Length < text.Length / 2)
                    return null;

                return result;
            }
            catch
            {
                return null;
            }
        }

        private void ExecuteCall()
        {
            if (string.IsNullOrWhiteSpace(inputJson))
            {
                EditorUtility.DisplayDialog("错误", "请输入JSON内容", "确定");
                return;
            }

            isExecuting = true;
            showResult = true;
            resultText = "正在执行...";

            try
            {
                DateTime startTime = DateTime.Now;
                JsonNode result = ExecuteJsonCall(startTime);

                // 如果结果为null，表示异步执行
                if (result == null)
                {
                    resultText = "异步执行中...";
                    // 刷新界面显示异步状态
                    Repaint();
                    // 注意：isExecuting保持为true，等待异步回调完成
                }
                else
                {
                    // 同步执行完成
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;
                    CompleteExecution(result, duration);
                }
            }
            catch (Exception e)
            {
                string errorResult = $"执行错误:\n{e.Message}\n\n堆栈跟踪:\n{e.StackTrace}";
                resultText = errorResult;
                isExecuting = false;

                Debug.LogException(new Exception("ExecuteCall error", e));
            }
        }

        /// <summary>
        /// 完成执行并更新UI显示
        /// </summary>
        private void CompleteExecution(JsonNode result, TimeSpan duration)
        {

            try
            {
                // 存储当前结果并格式化
                currentResult = result;
                string formattedResult = FormatResult(result, duration);
                resultText = formattedResult;

                // 刷新界面
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("CompleteExecution error", e));
            }
            finally
            {
                isExecuting = false;
            }
        }

        private JsonNode ExecuteJsonCall(DateTime startTime)
        {
            return ExecuteJsonCallInternal(inputJson, startTime, (result, duration) =>
            {
                CompleteExecution(result, duration);
            });
        }

        /// <summary>
        /// 执行JSON调用的内部通用方法
        /// </summary>
        private JsonNode ExecuteJsonCallInternal(string jsonString, DateTime startTime, System.Action<JsonNode, TimeSpan> onSingleComplete)
        {
            JsonNode inputObj = Json.Parse(jsonString);
            if (inputObj == null)
                return Response.Error("无法解析JSON输入");

            // 检查是否为批量调用
            if (inputObj is JsonArray jArray)
            {
                // 批量函数调用
                var functionsCall = new BatchCall();
                JsonNode callResult = null;
                bool callbackExecuted = false;
                functionsCall.HandleCommand(jArray, (result) =>
                {
                    callResult = result;
                    callbackExecuted = true;

                    // 如果是异步回调，更新UI
                    if (isExecuting)
                    {
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        onSingleComplete?.Invoke(result, duration);
                    }
                });
                // 如果回调立即执行，返回结果；否则返回null表示异步执行
                return callbackExecuted ? callResult : null;
            }
            else if (inputObj is JsonClass inputObjClass && inputObjClass.ContainsKey("func") && (inputObjClass.ContainsKey("args")))
            {
                // 单个函数调用 (兼容旧格式和新的异步格式)
                var functionCall = new ToolsCall();
                functionCall.SetToolName(inputObjClass["func"].Value);
                JsonNode callResult = null;
                bool callbackExecuted = false;
                functionCall.HandleCommand(inputObjClass["args"], (result) =>
                {
                    callResult = result;
                    callbackExecuted = true;

                    // 如果是异步回调，更新UI
                    if (isExecuting)
                    {
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        onSingleComplete?.Invoke(result, duration);
                    }
                });
                // 如果回调立即执行，返回结果；否则返回null表示异步执行
                return callbackExecuted ? callResult : null;
            }
            else
            {
                throw new ArgumentException("输入的JSON必须包含 'id' 字段（异步调用）、'func' 字段（单个调用）或 'funcs' 字段（批量调用）");
            }
        }


        private string FormatResult(JsonNode result, TimeSpan duration)
        {
            string formattedResult = $"执行时间: {duration.TotalMilliseconds:F2}ms\n\n";

            // 判断结果的 status
            string status = "success";
            // 创建包装后的结果对象
            JsonClass wrappedResult = new JsonClass();
            wrappedResult["status"] = status;
            wrappedResult["result"] = result;

            if (result != null)
            {
                // 不再反射判断类型，直接使用 JsonNode 的结构判断是否为批量调用结果
                if (IsBatchCallResult(result))
                {
                    formattedResult += FormatBatchCallResult(wrappedResult);
                }
                else
                {
                    formattedResult += wrappedResult.ToPrettyString("  ");
                }
            }
            else
            {
                formattedResult += wrappedResult.ToPrettyString("  ");
            }

            return formattedResult;
        }

        /// <summary>
        /// 检查是否为批量调用结果
        /// </summary>
        private bool IsBatchCallResult(JsonNode result)
        {
            try
            {
                // 直接判断result本身的结构（避免二次Parse带来的问题）
                if (result is JsonClass obj)
                {
                    return obj.ContainsKey("results") &&
                           obj.ContainsKey("total_calls") &&
                           obj.ContainsKey("successful_calls") &&
                           obj.ContainsKey("failed_calls");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 格式化批量调用结果，分条显示
        /// </summary>
        private string FormatBatchCallResult(object result)
        {
            try
            {
                var resultJson = Json.FromObject(result);
                var wrappedObj = Json.Parse(resultJson) as JsonClass;

                // 从包装对象中提取实际的结果
                var actualResult = wrappedObj["result"];
                var actualResultJson = Json.FromObject(actualResult);
                var resultObj = Json.Parse(actualResultJson) as JsonClass;

                var results = resultObj["results"] as JsonArray;
                var errors = resultObj["errors"] as JsonArray;
                var totalCalls = resultObj["total_calls"]?.AsInt ?? 0;
                var successfulCalls = resultObj["successful_calls"]?.AsInt ?? 0;
                var failedCalls = resultObj["failed_calls"]?.AsInt ?? 0;
                var overallSuccess = resultObj["success"]?.AsBool ?? false;

                var output = new StringBuilder();

                // 显示总体统计
                output.AppendLine("=== 批量调用执行结果 ===");
                output.AppendLine($"总调用数: {totalCalls}");
                output.AppendLine($"成功: {successfulCalls}");
                output.AppendLine($"失败: {failedCalls}");
                output.AppendLine($"整体状态: {(overallSuccess ? "成功" : "部分失败")}");
                output.AppendLine();

                // 分条显示每个结果
                if (results != null)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        output.AppendLine($"--- 调用 #{i + 1} ---");

                        var singleResult = results[i];

                        // 判断单个结果的成功状态：检查 success 字段
                        bool isSuccess = false;
                        if (singleResult != null && !singleResult.type.Equals(JsonNodeType.Null))
                        {
                            // 如果结果是 JsonClass，检查其 success 字段
                            if (singleResult is JsonClass resultObj2)
                            {
                                var successNode = resultObj2["success"];
                                isSuccess = successNode != null && successNode.Value == "true";
                            }
                            else
                            {
                                // 如果不是 JsonClass，默认为成功（非 null 且非空结果）
                                isSuccess = true;
                            }
                        }

                        if (isSuccess)
                        {
                            output.AppendLine("✅ 成功");
                            try
                            {
                                string formattedSingleResult = Json.FromObject(singleResult);
                                output.AppendLine(formattedSingleResult);
                            }
                            catch
                            {
                                output.AppendLine(singleResult.Value);
                            }
                        }
                        else
                        {
                            output.AppendLine("❌ 失败");

                            // 显示结果（如果有）
                            if (singleResult != null && !singleResult.type.Equals(JsonNodeType.Null))
                            {
                                try
                                {
                                    string formattedSingleResult = Json.FromObject(singleResult);
                                    output.AppendLine("结果详情:");
                                    output.AppendLine(formattedSingleResult);
                                }
                                catch
                                {
                                    output.AppendLine(singleResult.Value);
                                }
                            }

                            // 显示错误信息
                            if (errors != null && i < errors.Count && errors[i] != null && !string.IsNullOrEmpty(errors[i].Value))
                            {
                                output.AppendLine($"错误信息: {errors[i]}");
                            }
                        }
                        output.AppendLine();
                    }
                }

                // 显示所有错误汇总
                if (errors != null && errors.Count > 0)
                {
                    output.AppendLine("=== 错误汇总 ===");
                    for (int i = 0; i < errors.Count; i++)
                    {
                        if (errors[i] != null && !string.IsNullOrEmpty(errors[i].Value))
                        {
                            output.AppendLine($"{i + 1}. {errors[i]}");
                        }
                    }
                }

                return output.ToString();
            }
            catch (Exception e)
            {
                return $"批量结果格式化失败: {e.Message}\n\n原始结果:\n{Json.FromObject(result)}";
            }
        }

        /// <summary>
        /// 清理所有结果
        /// </summary>
        private void ClearResults()
        {
            resultText = "";
            showResult = false;
            currentResult = null;
            currentExecutionIndex = 0;
            totalExecutionCount = 0;
        }

        /// <summary>
        /// 检查当前显示的是否为批量结果
        /// </summary>
        private bool IsBatchResultDisplayed()
        {
            return currentResult != null && IsBatchCallResult(currentResult);
        }

        /// <summary>
        /// 复制批量调用的统计信息
        /// </summary>
        private void CopyBatchStatistics()
        {
            if (currentResult == null || !IsBatchCallResult(currentResult))
                return;

            try
            {
                var resultJson = Json.FromObject(currentResult);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                var totalCalls = resultObj["total_calls"]?.AsInt ?? 0;
                var successfulCalls = resultObj["successful_calls"]?.AsInt ?? 0;
                var failedCalls = resultObj["failed_calls"]?.AsInt ?? 0;
                var overallSuccess = resultObj["success"]?.AsBool ?? false;

                var statistics = $"批量调用统计:\n" +
                               $"总调用数: {totalCalls}\n" +
                               $"成功: {successfulCalls}\n" +
                               $"失败: {failedCalls}\n" +
                               $"整体状态: {(overallSuccess ? "成功" : "部分失败")}";

                EditorGUIUtility.systemCopyBuffer = statistics;
                EditorUtility.DisplayDialog("已复制", "统计信息已复制到剪贴板", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("复制失败", $"无法复制统计信息: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 仅显示错误信息
        /// </summary>
        private void ShowOnlyErrors()
        {
            if (currentResult == null || !IsBatchCallResult(currentResult))
                return;

            try
            {
                var resultJson = Json.FromObject(currentResult);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                var errors = resultObj["errors"] as JsonArray;
                var failedCalls = resultObj["failed_calls"]?.AsInt ?? 0;

                var output = new StringBuilder();
                output.AppendLine("=== 错误信息汇总 ===");
                output.AppendLine($"失败调用数: {failedCalls}");
                output.AppendLine();

                if (errors != null && errors.Count > 0)
                {
                    for (int i = 0; i < errors.Count; i++)
                    {
                        if (errors[i] != null && !string.IsNullOrEmpty(errors[i].Value))
                        {
                            output.AppendLine($"错误 #{i + 1}:");
                            output.AppendLine($"  {errors[i]}");
                            output.AppendLine();
                        }
                    }
                }
                else
                {
                    output.AppendLine("没有发现错误信息。");
                }

                resultText = output.ToString();
            }
            catch (Exception e)
            {
                resultText = $"显示错误信息失败: {e.Message}";
            }
        }

        /// <summary>
        /// 执行剪贴板中的JSON内容
        /// </summary>
        private void ExecuteClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("错误", "剪贴板为空", "确定");
                    return;
                }

                // 验证JSON格式
                if (!ValidateClipboardJson(clipboardContent, out string errorMessage))
                {
                    EditorUtility.DisplayDialog("JSON格式错误", $"剪贴板内容不是有效的JSON:\n{errorMessage}", "确定");
                    return;
                }

                // 执行剪贴板内容
                isExecuting = true;
                showResult = true;
                resultText = "正在执行剪贴板内容...";

                try
                {
                    DateTime startTime = DateTime.Now;
                    JsonNode result = ExecuteJsonCallFromString(clipboardContent, startTime);

                    // 如果结果为null，表示异步执行
                    if (result == null)
                    {
                        resultText = "异步执行剪贴板内容中...";
                        // 刷新界面显示异步状态
                        Repaint();
                        // 注意：isExecuting保持为true，等待异步回调完成
                    }
                    else
                    {
                        // 同步执行完成
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;

                        // 存储当前结果并格式化
                        currentResult = result;
                        string formattedResult = FormatResult(result, duration);
                        resultText = $"📋 从剪贴板执行\n原始JSON:\n{clipboardContent}\n\n{formattedResult}";

                        // 刷新界面
                        Repaint();
                        isExecuting = false;
                    }
                }
                catch (Exception e)
                {
                    string errorResult = $"执行剪贴板内容错误:\n{e.Message}\n\n堆栈跟踪:\n{e.StackTrace}";
                    resultText = errorResult;

                    Debug.LogError($"[McpDebugWindow] 执行剪贴板内容时发生错误: {e}");
                }
                finally
                {
                    isExecuting = false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("执行失败", $"无法执行剪贴板内容: {e.Message}", "确定");
                isExecuting = false;
            }
        }

        /// <summary>
        /// 从字符串执行JSON调用，支持异步回调
        /// </summary>
        private JsonNode ExecuteJsonCallFromString(string jsonString, DateTime startTime)
        {
            return ExecuteJsonCallInternal(jsonString, startTime, (result, duration) =>
            {
                // 剪贴板格式的UI更新
                currentResult = result;
                string formattedResult = FormatResult(result, duration);
                resultText = $"📋 从剪贴板执行\n原始JSON:\n{jsonString}\n\n{formattedResult}";

                // 刷新界面
                Repaint();
                isExecuting = false;
            });
        }



        /// <summary>
        /// 粘贴剪贴板内容到输入框
        /// </summary>
        private void PasteFromClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("提示", "剪贴板为空", "确定");
                    return;
                }

                // 验证JSON格式
                if (!ValidateClipboardJson(clipboardContent, out string errorMessage))
                {
                    bool proceed = EditorUtility.DisplayDialog("JSON格式警告",
                        $"剪贴板内容可能不是有效的JSON:\n{errorMessage}\n\n是否仍要粘贴？",
                        "仍要粘贴", "取消");

                    if (!proceed) return;
                }

                inputJson = clipboardContent;
                EditorUtility.DisplayDialog("成功", "已粘贴剪贴板内容到输入框", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("粘贴失败", $"无法粘贴剪贴板内容: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 预览剪贴板内容
        /// </summary>
        private void PreviewClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("剪贴板预览", "剪贴板为空", "确定");
                    return;
                }

                // 限制预览长度
                string preview = clipboardContent;
                if (preview.Length > 500)
                {
                    preview = preview.Substring(0, 500) + "\n...(内容过长，已截断)";
                }

                // 验证JSON格式
                string jsonStatus = ValidateClipboardJson(clipboardContent, out string errorMessage)
                    ? "✅ 有效的JSON格式"
                    : $"❌ JSON格式错误: {errorMessage}";

                EditorUtility.DisplayDialog("剪贴板预览",
                    $"格式状态: {jsonStatus}\n\n内容预览:\n{preview}", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("预览失败", $"无法预览剪贴板内容: {e.Message}", "确定");
            }
        }

        /// <summary>
        /// 检查剪贴板是否可用（包含有效JSON内容）
        /// </summary>
        private bool IsClipboardAvailable()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                    return false;

                return ValidateClipboardJson(clipboardContent, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 绘制带颜色指示的剪贴板状态
        /// </summary>
        private void DrawClipboardStatus()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;
                Color statusColor;
                string statusText;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    statusColor = Color.red;
                    statusText = "剪切板: 空";
                }
                else
                {
                    bool isValidJson = ValidateClipboardJson(clipboardContent, out _);
                    if (isValidJson)
                    {
                        statusColor = Color.green;
                        statusText = $"剪切板: ✅ Json ({clipboardContent.Length} 字符)";
                    }
                    else
                    {
                        statusColor = new Color(1f, 0.5f, 0f); // 橙色
                        statusText = $"剪切板: ❌ 非JSON ({clipboardContent.Length} 字符)";
                    }
                }

                // 显示带颜色的状态
                Color originalColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(statusText, EditorStyles.miniLabel);
                GUI.color = originalColor;
            }
            catch
            {
                Color originalColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("剪切板: 读取失败", EditorStyles.miniLabel);
                GUI.color = originalColor;
            }
        }

        /// <summary>
        /// 验证剪贴板JSON格式
        /// </summary>
        private bool ValidateClipboardJson(string content, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(content))
            {
                errorMessage = "内容为空";
                return false;
            }

            try
            {
                Json.Parse(content);
                return true;
            }
            catch (System.Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        /// <summary>
        /// 检查结果是否为错误响应
        /// </summary>
        private bool IsErrorResponse(object result)
        {
            if (result == null) return true;

            try
            {
                var resultJson = Json.FromObject(result);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                // 检查是否有success字段且为false
                if (resultObj.ContainsKey("success"))
                {
                    return !resultObj["success"]?.AsBool ?? true;
                }

                // 检查是否有error字段
                return resultObj.ContainsKey("error");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从错误响应中提取错误消息
        /// </summary>
        private string ExtractErrorMessage(object result)
        {
            if (result == null) return "结果为空";

            try
            {
                var resultJson = Json.FromObject(result);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                // 尝试从error字段获取错误信息
                if (resultObj.ContainsKey("error"))
                {
                    return resultObj["error"]?.Value ?? "未知错误";
                }

                // 尝试从message字段获取错误信息
                if (resultObj.ContainsKey("message"))
                {
                    return resultObj["message"]?.Value ?? "未知错误";
                }

                return result.ToString();
            }
            catch
            {
                return result.ToString();
            }
        }

        /// <summary>
        /// 手动记录当前执行结果
        /// </summary>
        private void RecordCurrentResult()
        {
            if (currentResult == null || string.IsNullOrEmpty(inputJson))
            {
                EditorUtility.DisplayDialog("无法记录", "没有可记录的执行结果", "确定");
                return;
            }

            try
            {
                // 解析输入的JSON来获取函数名和参数
                JsonClass inputObj = Json.Parse(inputJson) as JsonClass;

                // 检查是否为批量调用
                if (inputObj.ContainsKey("funcs"))
                {
                    // 批量调用记录
                    RecordBatchResult(inputObj, currentResult);
                }
                else if (inputObj.ContainsKey("func") || inputObj.ContainsKey("id"))
                {
                    // 单个函数调用记录（兼容旧格式和新的异步格式）
                    RecordSingleResult(inputObj, currentResult);
                }
                else
                {
                    EditorUtility.DisplayDialog("记录失败", "无法解析输入的JSON格式", "确定");
                    return;
                }

                EditorUtility.DisplayDialog("记录成功", "执行结果已保存到记录中", "确定");

                // 刷新记录列表
                recordList = null;
                InitializeRecordList();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("记录失败", $"记录执行结果时发生错误: {e.Message}", "确定");
                Debug.LogError($"[McpDebugWindow] 手动记录结果时发生错误: {e}");
            }
        }

        /// <summary>
        /// 记录单个函数调用结果
        /// </summary>
        private void RecordSingleResult(JsonClass inputObj, object result)
        {
            // 兼容新的异步格式和旧格式
            var funcName = inputObj["func"]?.Value ??
                          (inputObj["id"]?.Value != null ? $"async_{inputObj["id"]?.Value}" : "Unknown");
            var argsJson = inputObj["args"]?.Value ?? "{}";
            var recordObject = McpExecuteRecordObject.instance;

            bool isSuccess = result != null && !IsErrorResponse(result);
            string errorMsg = "";
            string resultJson = "";

            if (isSuccess)
            {
                resultJson = Json.FromObject(result);
                if (result is JsonClass resultObj && resultObj["success"] != null && resultObj["success"].type == JsonNodeType.Boolean && resultObj["success"].AsBool == false)
                {
                    errorMsg = resultObj["error"]?.Value ?? "执行失败";
                }
            }
            else
            {
                errorMsg = result != null ? ExtractErrorMessage(result) : "执行失败，返回null";
                resultJson = result != null ? Json.FromObject(result) : "";
            }

            recordObject.addRecord(
                funcName,
                argsJson,
                resultJson,
                errorMsg,
                0, // 手动记录时没有执行时间
                "Debug Window (手动记录)"
            );
            recordObject.saveRecords();
        }

        /// <summary>
        /// 记录批量函数调用结果
        /// </summary>
        private void RecordBatchResult(JsonClass inputObj, object result)
        {
            try
            {
                var resultJson = Json.FromObject(result);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                var results = resultObj["results"] as JsonArray;
                var errors = resultObj["errors"] as JsonArray;
                var funcsArray = inputObj["funcs"] as JsonArray;

                if (funcsArray != null && results != null)
                {
                    var recordObject = McpExecuteRecordObject.instance;

                    for (int i = 0; i < funcsArray.Count && i < results.Count; i++)
                    {
                        var funcCall = funcsArray[i] as JsonClass;
                        if (funcCall == null) continue;

                        var funcName = funcCall["func"]?.Value ?? "Unknown";
                        var argsJson = funcCall["args"]?.Value ?? "{}";
                        var singleResult = results[i];

                        bool isSuccess = singleResult != null && !singleResult.type.Equals(JsonNodeType.Null);
                        string errorMsg = "";
                        string singleResultJson = "";

                        if (isSuccess)
                        {
                            singleResultJson = Json.FromObject(singleResult);
                        }
                        else
                        {
                            if (errors != null && i < errors.Count && errors[i] != null)
                            {
                                errorMsg = errors[i].Value;
                            }
                            else
                            {
                                errorMsg = "批量调用中此项失败";
                            }
                        }

                        recordObject.addRecord(
                            funcName,
                            argsJson,
                            singleResultJson,
                            errorMsg,
                            0, // 手动记录时没有执行时间
                            $"Debug Window (手动记录 {i + 1}/{funcsArray.Count})"
                        );
                    }

                    recordObject.saveRecords();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"记录批量结果时发生错误: {e.Message}", e);
            }
        }


        /// <summary>
        /// 选择记录并刷新到界面
        /// </summary>
        private void SelectRecord(int index)
        {
            var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
            if (index < 0 || index >= records.Count) return;

            selectedRecordIndex = index;
            var record = records[index];

            inputJson = record.cmd;

            // 将执行结果刷新到结果区域
            if (!string.IsNullOrEmpty(record.result) || !string.IsNullOrEmpty(record.error))
            {
                showResult = true;
                var resultBuilder = new StringBuilder();
                resultBuilder.AppendLine($"📋 从执行记录加载 (索引: {index})");
                resultBuilder.AppendLine($"函数: {record.name}");
                resultBuilder.AppendLine($"时间: {record.timestamp}");
                resultBuilder.AppendLine($"来源: {record.source}");
                resultBuilder.AppendLine($"状态: {(record.success ? "成功" : "失败")}");
                if (record.duration > 0)
                {
                    resultBuilder.AppendLine($"执行时间: {record.duration:F2}ms");
                }
                resultBuilder.AppendLine();

                if (!string.IsNullOrEmpty(record.result))
                {
                    resultBuilder.AppendLine("执行结果:");
                    resultBuilder.AppendLine(record.result);
                }

                if (!string.IsNullOrEmpty(record.error))
                {
                    resultBuilder.AppendLine("错误信息:");
                    resultBuilder.AppendLine(record.error);
                }

                resultText = resultBuilder.ToString();
                currentResult = null; // 清空当前结果，因为这是历史记录
            }

            Repaint();
        }

        /// <summary>
        /// 处理记录元素的鼠标事件（双击检测）
        /// </summary>
        private void HandleRecordElementMouseEvents(Rect rect, int index)
        {
            Event e = Event.current;

            // 只在函数名区域检测双击，避免与整个元素的选择冲突
            const float numberWidth = 24f;
            const float iconWidth = 20f;
            const float padding = 6f;
            const float boxMargin = 4f;

            Rect funcNameRect = new Rect(
                rect.x + padding + boxMargin + numberWidth + iconWidth + 4f,
                rect.y + padding + boxMargin,
                rect.width - padding * 2 - boxMargin * 2 - numberWidth - iconWidth - 4f,
                16f
            );

            if (e.type == EventType.MouseDown && e.button == 0 && funcNameRect.Contains(e.mousePosition))
            {
                double currentTime = EditorApplication.timeSinceStartup;

                // 检测双击
                if (lastClickedIndex == index && (currentTime - lastClickTime) < 0.5) // 500ms内的双击
                {
                    // 开始编辑
                    StartEditing(index);
                    e.Use();
                }
                else
                {
                    // 单击，记录时间和索引
                    lastClickTime = currentTime;
                    lastClickedIndex = index;
                }
            }
        }

        /// <summary>
        /// 开始编辑记录名称
        /// </summary>
        private void StartEditing(int index)
        {
            var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
            if (index >= 0 && index < records.Count)
            {
                editingRecordIndex = index;
                editingText = records[index].name;
                editingStarted = true;

                Repaint();
            }
        }

        /// <summary>
        /// 完成编辑并保存
        /// </summary>
        private void FinishEditing(int index, string newName)
        {
            if (editingRecordIndex == index && !string.IsNullOrWhiteSpace(newName))
            {
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                if (index >= 0 && index < records.Count)
                {
                    // 更新记录名称
                    records[index].name = newName.Trim();
                    McpExecuteRecordObject.instance.saveRecords();

                    // 显示成功提示（可选）
                    Debug.Log($"[McpDebugWindow] 记录名称已更新: {newName.Trim()}");
                }
            }

            // 退出编辑模式
            editingRecordIndex = -1;
            editingText = "";
            editingStarted = false;
            GUI.FocusControl(null); // 清除焦点
            Repaint();
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEditing()
        {
            editingRecordIndex = -1;
            editingText = "";
            editingStarted = false;
            GUI.FocusControl(null); // 清除焦点
            Repaint();
        }

        #region 分组管理UI

        /// <summary>
        /// 绘制分组管理界面
        /// </summary>
        // 静态变量存储背景纹理，避免重复创建
        private static Texture2D groupManagerBgTexture = null;

        private void DrawGroupManager(float width)
        {
            var recordObject = McpExecuteRecordObject.instance;

            // 使用helpBox样式，但自定义背景颜色
            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox);
            // 创建或重用深色背景纹理
            if (groupManagerBgTexture == null)
            {
                groupManagerBgTexture = new Texture2D(1, 1);
                groupManagerBgTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f, 1f));
                groupManagerBgTexture.Apply();
            }
            boxStyle.normal.background = groupManagerBgTexture;
            
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.Space(5);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 12;
            titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUILayout.Label("分组管理", titleStyle);
            EditorGUILayout.Space(5);

            // 创建新分组
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            labelStyle.fontSize = 10;
            GUILayout.Label("创建新分组:", labelStyle);
            
            // 输入框样式 - 使用正常的TextField样式，只修改文字颜色
            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField);
            textFieldStyle.normal.textColor = Color.white;
            textFieldStyle.focused.textColor = Color.white;
            textFieldStyle.active.textColor = Color.white;
            // 不设置background，使用默认样式以确保可编辑
            
            // 保存原始标签宽度
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            // 设置较小的标签宽度，让输入框占用更多空间
            EditorGUIUtility.labelWidth = 40;
            
            newGroupName = EditorGUILayout.TextField("名称", newGroupName, textFieldStyle);
            newGroupDescription = EditorGUILayout.TextField("描述", newGroupDescription, textFieldStyle);
            
            // 恢复原始标签宽度
            EditorGUIUtility.labelWidth = originalLabelWidth;

            GUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrWhiteSpace(newGroupName);
            Color createButtonBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            GUIStyle createButtonStyle = new GUIStyle(GUI.skin.button);
            createButtonStyle.fontSize = 10;
            createButtonStyle.fontStyle = FontStyle.Bold;
            createButtonStyle.normal.textColor = Color.white;
            if (GUILayout.Button("创建分组", createButtonStyle, GUILayout.Width(80), GUILayout.Height(22)))
            {
                string groupId = System.Guid.NewGuid().ToString("N")[..8];
                string groupNameTrimmed = newGroupName.Trim();
                if (recordObject.CreateGroup(groupId, groupNameTrimmed, newGroupDescription.Trim()))
                {
                    newGroupName = "";
                    newGroupDescription = "";
                    EditorUtility.DisplayDialog("成功", $"分组 '{groupNameTrimmed}' 创建成功！", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("失败", "创建分组失败，请检查名称是否重复。", "确定");
                }
            }
            GUI.enabled = true;
            GUI.backgroundColor = createButtonBg;
            GUILayout.EndHorizontal();

            // 分组列表（缩小高度）
            if (recordObject.recordGroups.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("现有分组:", labelStyle);

                // 使用固定高度的滚动区域
                groupScrollPosition = GUILayout.BeginScrollView(groupScrollPosition, GUILayout.Height(120));

                for (int i = 0; i < recordObject.recordGroups.Count; i++)
                {
                    var group = recordObject.recordGroups[i];

                    // 绘制分组项背景
                    Rect groupItemRect = EditorGUILayout.BeginVertical();
                    Color itemBgColor = i % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : new Color(0.18f, 0.18f, 0.18f, 0.5f);
                    EditorGUI.DrawRect(groupItemRect, itemBgColor);
                    
                    EditorGUILayout.Space(3);
                    GUILayout.BeginHorizontal();

                    // 分组信息（简化显示）
                    GUILayout.BeginVertical();
                    GUIStyle groupNameStyle = new GUIStyle(EditorStyles.boldLabel);
                    groupNameStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                    groupNameStyle.fontSize = 11;
                    GUILayout.Label($"{group.name}", groupNameStyle);
                    
                    GUIStyle statsStyle = new GUIStyle(EditorStyles.miniLabel);
                    statsStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                    GUILayout.Label($"{recordObject.GetGroupStatistics(group.id)}", statsStyle);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // 操作按钮（水平排列）
                    Color groupButtonBg = GUI.backgroundColor;
                    GUIStyle groupButtonStyle = new GUIStyle(GUI.skin.button);
                    groupButtonStyle.fontSize = 9;
                    groupButtonStyle.fontStyle = FontStyle.Bold;
                    groupButtonStyle.normal.textColor = Color.white;
                    
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
                    if (GUILayout.Button("切换", groupButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        recordObject.SwitchToGroup(group.id);
                        recordList = null;
                        InitializeRecordList();
                    }

                    GUI.enabled = !group.isDefault;
                    GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                    if (GUILayout.Button("删除", groupButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("确认删除",
                            $"确定要删除分组 '{group.name}' 吗？\n\n该分组的所有记录将被移动到默认分组。",
                            "删除", "取消"))
                        {
                            recordObject.DeleteGroup(group.id);
                            recordList = null;
                            InitializeRecordList();
                        }
                    }
                    GUI.enabled = true;
                    GUI.backgroundColor = groupButtonBg;

                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space(3);
                    EditorGUILayout.EndVertical();
                }

                GUILayout.EndScrollView();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 计算分组管理区域的高度
        /// </summary>
        private float CalculateGroupManagerHeight()
        {
            var recordObject = McpExecuteRecordObject.instance;
            float baseHeight = 120; // 基本高度（标题 + 创建区域）

            if (recordObject.recordGroups.Count > 0)
            {
                baseHeight += 140; // 分组列表区域（标题 + 固定高度的滚动区域）
            }

            return baseHeight;
        }

        /// <summary>
        /// 获取当前分组的显示名称
        /// </summary>
        private string GetCurrentGroupDisplayName()
        {
            var recordObject = McpExecuteRecordObject.instance;
            var currentGroup = recordObject.GetCurrentGroup();
            return currentGroup?.name ?? "未知分组";
        }

        #endregion

    }
}