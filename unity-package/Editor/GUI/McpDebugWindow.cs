using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityMcp.Models;
using UnityMcp.Executer;

namespace UnityMcp.Gui
{
    /// <summary>
    /// MCPDebug client window - For testing and debuggingMCPFunction call
    /// </summary>
    public class McpDebugWindow : EditorWindow
    {
        [MenuItem("Window/MCP/Debug Window")]
        public static void ShowWindow()
        {
            McpDebugWindow window = GetWindow<McpDebugWindow>("MCP Debug Window");
            window.minSize = new Vector2(500, 300);
            window.Show();
        }

        /// <summary>
        /// Open debug window and prefill with specifiedJSONContent
        /// </summary>
        /// <param name="jsonContent">To prefillJSONContent</param>
        public static void ShowWindowWithContent(string jsonContent)
        {
            McpDebugWindow window = GetWindow<McpDebugWindow>("MCP Debug Window");
            window.minSize = new Vector2(500, 300);
            window.SetInputJson(jsonContent);
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Set for input boxJSONContent
        /// </summary>
        /// <param name="jsonContent">JSONContent</param>
        public void SetInputJson(string jsonContent)
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                inputJson = jsonContent;
                ClearResults(); // Clear previous result
                Repaint(); // Refresh UI
            }
        }

        // UIStatus variable
        private Vector2 inputScrollPosition;
        private Vector2 resultScrollPosition;
        private string inputJson = "{\n  \"func\": \"hierarchy_create\",\n  \"args\": {\n    \"from\": \"primitive\",\n    \"primitive_type\": \"Cube\",\n    \"name\": \"RedCube\",\n    \"position\": [\n      0,\n      0,\n      0\n    ]\n  }\n}";
        private string resultText = "";
        private bool showResult = false;
        private bool isExecuting = false;
        private int currentExecutionIndex = 0; // Current executing task index
        private int totalExecutionCount = 0; // Total task count

        private JsonNode currentResult = null; // Store current execution result

        // Execution record related variable
        private ReorderableList recordList;
        private int selectedRecordIndex = -1;
        private Vector2 recordScrollPosition; // Record list scroll position

        // Group related variables
        private bool showGroupManager = false; // Whether to show group management UI
        private string newGroupName = ""; // New group name
        private string newGroupDescription = ""; // New group description
        private Vector2 groupScrollPosition; // Group list scroll position
        private int selectedGroupIndex = -1; // Selected group index

        // Edit related variable
        private int editingRecordIndex = -1; // Index of currently editing record
        private string editingText = ""; // Editing text
        private double lastClickTime = 0; // Last click time，For detecting double-click
        private int lastClickedIndex = -1; // Last clicked index
        private bool editingStarted = false; // Flag whether editing just started

        // Column layout related variable
        private float splitterPos = 0.3f; // Default take left30%
        private bool isDraggingSplitter = false;
        private const float SplitterWidth = 4f;

        // Layout parameter
        private const float MinInputHeight = 100f;
        private const float MaxInputHeight = 300f;
        private const float LineHeight = 16f;
        private const float ResultAreaHeight = 200f;

        // Style
        private GUIStyle headerStyle;
        private GUIStyle codeStyle;
        private GUIStyle inputStyle;  // Style dedicated to input box
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
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
            }

            if (codeStyle == null)
            {
                codeStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,  // Enable word wrap
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    stretchWidth = false,  // Do not auto stretch，Use fixed width
                    stretchHeight = true   // Stretch to fit container height
                };
            }

            if (inputStyle == null)
            {
                inputStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,        // Force enable word wrap
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black },
                    stretchWidth = false,   // Do not auto stretch width
                    stretchHeight = true,   // Allow height stretch
                    fixedWidth = 0,         // Not fixed width
                    fixedHeight = 0,        // Not fixed height
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(4, 4, 4, 4)
                };
            }

            if (resultStyle == null)
            {
                resultStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,        // Enable word wrap
                    fontSize = 12,          // Keep font size consistent with input box
                    fontStyle = FontStyle.Normal,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black },
                    richText = true,        // Support rich text，Easy to display formatted content
                    stretchWidth = false,   // Consistent with input box，Do not auto stretch width
                    stretchHeight = true,   // Stretch to fit container height
                    margin = new RectOffset(2, 2, 2, 2),    // Margin consistent with input box
                    padding = new RectOffset(4, 4, 4, 4)    // Keep padding consistent with input box
                };
            }

            InitializeRecordList();
        }

        private void InitializeRecordList()
        {
            if (recordList == null)
            {
                // Get records of current group（Group feature auto-initializes）
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                recordList = new ReorderableList(records, typeof(McpExecuteRecordObject.McpExecuteRecord), false, true, false, true);

                recordList.drawHeaderCallback = (Rect rect) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    int successCount = records.Where(r => r.success).Count();
                    int errorCount = records.Count - successCount;
                    var recordObject = McpExecuteRecordObject.instance;

                    // Group dropdown used directly as title
                    var groups = recordObject.recordGroups;
                    if (groups.Count == 0)
                    {
                        EditorGUI.LabelField(rect, "No group");
                    }
                    else
                    {
                        // Build group options（Contain stats info）
                        string[] groupNames = groups.Select(g =>
                        {
                            string stats = recordObject.GetGroupStatistics(g.id);
                            return $"{g.name} ({stats})";
                        }).ToArray();

                        int currentIndex = groups.FindIndex(g => g.id == recordObject.currentGroupId);
                        if (currentIndex == -1) currentIndex = 0;

                        EditorGUI.BeginChangeCheck();
                        int newIndex = EditorGUI.Popup(rect, currentIndex, groupNames, EditorStyles.boldLabel);
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
                        var record = records[records.Count - 1 - index]; // Reverse order display
                        DrawRecordElement(rect, record, records.Count - 1 - index, isActive, isFocused);
                    }
                };

                recordList.onSelectCallback = (ReorderableList list) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    if (list.index >= 0 && list.index < records.Count)
                    {
                        int actualIndex = records.Count - 1 - list.index; // Convert to actual index
                        SelectRecord(actualIndex);
                    }
                };

                recordList.onRemoveCallback = (ReorderableList list) =>
                {
                    var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                    if (list.index >= 0 && list.index < records.Count)
                    {
                        int actualIndex = records.Count - 1 - list.index;
                        if (EditorUtility.DisplayDialog("Confirm deletion", $"Are you sure to delete this execution record?？\nFunction: {records[actualIndex].name}", "Delete", "Cancel"))
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

                recordList.elementHeight = 40f; // Set element height
            }
        }

        /// <summary>
        /// Dynamically calculate input box height based on text（Consider word wrap and fixed width）
        /// </summary>
        private float CalculateInputHeight()
        {
            if (string.IsNullOrEmpty(inputJson))
                return MinInputHeight;

            // Base line number calculation
            int basicLineCount = inputJson.Split('\n').Length;

            // Estimate line break by fixed width，Consider font size and width limit
            // Estimate characters per line（Based on12pxFont and available width）
            const int avgCharsPerLine = 60; // Conservative estimate，Fits narrow panel
            int totalChars = inputJson.Length;
            int estimatedWrappedLines = Mathf.CeilToInt((float)totalChars / avgCharsPerLine);

            // Take the larger value as line count estimate，But give line break more weight
            int estimatedLineCount = Mathf.Max(basicLineCount, (int)(estimatedWrappedLines * 0.8f));

            // Calculate height by line count，Add properpadding
            float calculatedHeight = estimatedLineCount * LineHeight + 40f; // Appropriatepadding

            // Limit between minimum and maximum height
            return Mathf.Clamp(calculatedHeight, MinInputHeight, MaxInputHeight);
        }

        /// <summary>
        /// Calculate actual height of title area
        /// </summary>
        private float CalculateHeaderHeight()
        {
            // Title text height（Based onheaderStyleOffontSize）
            float titleHeight = headerStyle?.fontSize ?? 14;
            titleHeight += 16; // Title vertical margin，Add more space

            // Spacing
            float spacing = 10; // Increase spacing

            // Total height，Ensure enough space for title
            return titleHeight + spacing + 10; // Add extra margin
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Calculate actual height of title area
            float headerHeight = CalculateHeaderHeight();

            // Column layout
            DrawSplitView(headerHeight);

            // Handle column dragging
            HandleSplitterEvents(headerHeight);
        }

        private void DrawSplitView(float headerHeight)
        {
            Rect windowRect = new Rect(0, headerHeight, position.width, position.height - headerHeight);
            float leftWidth = windowRect.width * splitterPos;
            float rightWidth = windowRect.width * (1 - splitterPos) - SplitterWidth;

            // Left area - Execution record
            Rect leftRect = new Rect(windowRect.x, windowRect.y, leftWidth, windowRect.height);
            DrawLeftPanel(leftRect);

            // Separator
            Rect splitterRect = new Rect(leftRect.xMax, windowRect.y, SplitterWidth, windowRect.height);
            DrawSplitter(splitterRect);

            // Right area - Existing functionality
            Rect rightRect = new Rect(splitterRect.xMax, windowRect.y, rightWidth, windowRect.height);
            DrawRightPanel(headerHeight, rightRect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            // Use more precise vertical layout
            float currentY = 5; // Revert to original start position
            float padding = 5;  // Revert to original padding

            // Button region for record list actions
            Rect buttonRect = new Rect(padding, currentY, rect.width - padding * 2, 28);
            GUI.BeginGroup(buttonRect);
            GUILayout.BeginArea(new Rect(0, 0, buttonRect.width, buttonRect.height));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(50)))
            {
                recordList = null;
                InitializeRecordList();
                Repaint();
            }

            if (GUILayout.Button("Clear current group", GUILayout.Width(100)))
            {
                string confirmMessage = $"Confirm to clear current group '{GetCurrentGroupDisplayName()}' All records of？\nThis action does not affect other groups。";

                if (EditorUtility.DisplayDialog("Confirm clear", confirmMessage, "Confirm", "Cancel"))
                {
                    // Clear only current group，Prohibit clearing all
                    McpExecuteRecordObject.instance.ClearCurrentGroupRecords();
                    McpExecuteRecordObject.instance.saveRecords();
                    selectedRecordIndex = -1;
                    recordList = null;
                    InitializeRecordList();
                }
            }

            if (GUILayout.Button(showGroupManager ? "Hide" : "Manage", GUILayout.Width(60)))
            {
                showGroupManager = !showGroupManager;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.EndGroup();
            currentY += 30;


            // Group management area
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

            // Record list area
            if (recordList != null)
            {
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                recordList.list = records;

                float listContentHeight = recordList.GetHeight();
                float availableHeight = rect.height - currentY - padding;

                // Ensure minimum height
                if (availableHeight < 100)
                {
                    availableHeight = 100;
                }

                Rect scrollViewRect = new Rect(padding, currentY, rect.width - padding * 2, availableHeight);
                Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width - 16, listContentHeight);

                recordScrollPosition = GUI.BeginScrollView(scrollViewRect, recordScrollPosition, scrollContentRect, false, true);
                recordList.DoList(new Rect(0, 0, scrollContentRect.width, listContentHeight));
                GUI.EndScrollView();
            }
        }

        private void DrawRightPanel(float headerHeight, Rect rect)
        {
            // Draw title at the top and center first
            Rect titleRect = new Rect(rect.x, 0, position.width, headerHeight);
            GUI.BeginGroup(titleRect);
            GUILayout.BeginArea(new Rect(0, 0, titleRect.width, titleRect.height));
            GUILayout.Space(8); // Top margin
            GUILayout.Label("Unity MCP Debug Client", headerStyle);
            GUILayout.EndArea();
            GUI.EndGroup();


            GUILayout.BeginArea(rect);

            // Use vertical layout group to control overall width
            GUILayout.BeginVertical(GUILayout.MaxWidth(rect.width));

            // Description
            EditorGUILayout.HelpBox(
                "Input single function call:\n{\"func\": \"function_name\", \"args\": {...}}\n\n" +
                "Or batch call (Sequential execution):\n{\"funcs\": [{\"func\": \"...\", \"args\": {...}}, ...]}",
                MessageType.Info);

            GUILayout.Space(5);

            // JSONInput box area
            DrawInputArea(rect.width);

            GUILayout.Space(10);

            // Operation button area
            DrawControlButtons();

            GUILayout.Space(10);

            // Result display area
            if (showResult)
            {
                DrawResultArea(rect.width);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawSplitter(Rect rect)
        {
            Color originalColor = GUI.color;
            GUI.color = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = originalColor;

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
                        splitterPos = Mathf.Clamp(newSplitterPos, 0.2f, 0.8f); // Limit to20%-80%Between
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

            // Addpadding - Each element haspadding
            const float padding = 6f;
            Rect paddedRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2, rect.height - padding * 2);

            // Handle mouse event（Double-click detection）
            HandleRecordElementMouseEvents(rect, index);

            if (isActive || selectedRecordIndex == index)
            {
                // Show background color when selected（In originalrectDraw above，Not subject topaddingImpact）
                GUI.color = new Color(0.3f, 0.7f, 1f, 0.3f); // Blue highlight
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            }
            else
            {
                // Draw when not selectedboxBorder
                Color boxColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                GUI.color = boxColor;
                GUI.Box(paddedRect, "", EditorStyles.helpBox);
            }

            GUI.color = originalColor;

            // Draw content（AtboxInternal）
            const float numberWidth = 24f; // Serial width
            const float iconWidth = 20f;
            const float boxMargin = 4f; // boxInternal padding

            // CalculateboxInternal draw area
            Rect contentRect = new Rect(paddedRect.x + boxMargin, paddedRect.y + boxMargin,
                paddedRect.width - boxMargin * 2, paddedRect.height - boxMargin * 2);

            // Show serial number（Top left，AtboxInternal）
            var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
            int displayIndex = index + 1; // Show serial in order，From1Start
            Rect numberRect = new Rect(contentRect.x, contentRect.y, numberWidth, 14f);
            Color numberColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
            Color originalContentColor = GUI.contentColor;
            GUI.contentColor = numberColor;
            GUI.Label(numberRect, $"#{displayIndex}", EditorStyles.miniLabel);
            GUI.contentColor = originalContentColor;

            // Status icon（AtboxInternal）
            string statusIcon = record.success ? "●" : "×";
            Rect iconRect = new Rect(contentRect.x + numberWidth + 2f, contentRect.y, iconWidth, 16f);

            // Set color for status icon
            Color iconColor = record.success ? Color.green : Color.red;
            GUI.contentColor = iconColor;
            GUI.Label(iconRect, statusIcon, EditorStyles.boldLabel);
            GUI.contentColor = originalContentColor;

            // Function name（First row）- AtboxInternal，Reserve space for number and icon
            Rect funcRect = new Rect(contentRect.x + numberWidth + iconWidth + 4f, contentRect.y,
                contentRect.width - numberWidth - iconWidth - 4f, 16f);

            // Check if editing this record
            if (editingRecordIndex == index)
            {
                // Edit mode：Show text input box
                GUI.SetNextControlName($"RecordEdit_{index}");

                // Handle keyboard event first，Ensure priority
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        // Enter to confirm edit
                        FinishEditing(index, editingText);
                        Event.current.Use();
                        return; // Directly return，Avoid further handling of other events
                    }
                    else if (Event.current.keyCode == KeyCode.Escape)
                    {
                        // ESCCancel editing
                        CancelEditing();
                        Event.current.Use();
                        return; // Directly return，Avoid further handling of other events
                    }
                }

                // Set focus（Only set at start of editing）
                if (editingStarted)
                {
                    GUI.FocusControl($"RecordEdit_{index}");
                    editingStarted = false;
                }

                // UseBeginChangeCheckTo detect text change
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUI.TextField(funcRect, editingText);
                if (EditorGUI.EndChangeCheck())
                {
                    editingText = newName;
                }

                // Detect focus loss
                if (Event.current.type == EventType.Repaint)
                {
                    string focusedControl = GUI.GetNameOfFocusedControl();
                    if (string.IsNullOrEmpty(focusedControl) || focusedControl != $"RecordEdit_{index}")
                    {
                        // Delay check by one frame，Avoid detecting focus loss immediately after setting focus
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
                // Normal mode：Show function name
                GUI.Label(funcRect, record.name, EditorStyles.boldLabel);
            }

            // Time and source（Second row）- AtboxInternal
            Rect timeRect = new Rect(contentRect.x + numberWidth + iconWidth + 4f, contentRect.y + 18f,
                contentRect.width - numberWidth - iconWidth - 4f, 14f);
            string timeInfo = $"{record.timestamp} | [{record.source}]";
            if (record.duration > 0)
            {
                timeInfo += $" | {record.duration:F1}ms";
            }

            // Set lighter color for time info
            Color timeColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.4f, 0.4f, 0.4f);
            GUI.contentColor = timeColor;
            GUI.Label(timeRect, timeInfo, EditorStyles.miniLabel);
            GUI.contentColor = originalContentColor;
        }

        /// <summary>
        /// Draw input area（With scroll and dynamic height）
        /// </summary>
        private void DrawInputArea(float availableWidth)
        {
            GUILayout.Label("MCPCall (JSONFormat):");

            float inputHeight = CalculateInputHeight();
            float textAreaWidth = availableWidth; // Minus margin and scrollbar width

            // Create scroll area for input box，Limit width to avoid horizontal scroll
            GUILayout.BeginVertical(EditorStyles.helpBox);
            inputScrollPosition = EditorGUILayout.BeginScrollView(
                inputScrollPosition,
                false, true,  // Disable horizontal scrollbar，Enable vertical scrollbar
                GUILayout.Height(inputHeight),
                GUILayout.ExpandWidth(true)
            );

            // Input box，Use a dedicated input style for automatic wrapping
            inputJson = EditorGUILayout.TextArea(
                inputJson,
                inputStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(textAreaWidth),
                GUILayout.MaxWidth(textAreaWidth)  // Ensure not to exceed specified width
            );

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Show line number info
            int lineCount = inputJson?.Split('\n').Length ?? 0;
            GUILayout.Label($"Line count: {lineCount} | Height: {inputHeight:F0}px", EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draw control button region
        /// </summary>
        private void DrawControlButtons()
        {
            // Get clipboard availability
            bool clipboardAvailable = IsClipboardAvailable();

            // First row button
            GUILayout.BeginHorizontal();

            GUI.enabled = !isExecuting;
            if (GUILayout.Button("Execute", GUILayout.Height(30), GUILayout.Width(100)))
            {
                ExecuteCall();
            }

            GUI.enabled = !isExecuting && clipboardAvailable;
            if (GUILayout.Button("Execute clipboard", GUILayout.Height(30), GUILayout.Width(100)))
            {
                ExecuteClipboard();
            }
            GUI.enabled = true;

            if (GUILayout.Button("FormatJSON", GUILayout.Height(30), GUILayout.Width(120)))
            {
                FormatJson();
            }

            if (GUILayout.Button("Clear", GUILayout.Height(30), GUILayout.Width(60)))
            {
                inputJson = "{}";
                ClearResults();
            }

            if (isExecuting)
            {
                if (totalExecutionCount > 1)
                {
                    GUILayout.Label($"Executing... ({currentExecutionIndex}/{totalExecutionCount})", GUILayout.Width(150));
                }
                else
                {
                    GUILayout.Label("Executing...", GUILayout.Width(100));
                }
            }

            GUILayout.EndHorizontal();

            // Second row button（Clipboard operation）
            GUILayout.BeginHorizontal();

            // Clipboard operation button - Enable dynamically based on clipboard content/Disable
            GUI.enabled = clipboardAvailable;
            if (GUILayout.Button("Paste to input box", GUILayout.Height(25), GUILayout.Width(100)))
            {
                PasteFromClipboard();
            }

            if (GUILayout.Button("Preview clipboard", GUILayout.Height(25), GUILayout.Width(100)))
            {
                PreviewClipboard();
            }
            GUI.enabled = true;

            // Show clipboard state - With color indicator
            DrawClipboardStatus();

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw result display area（With scroll）
        /// </summary>
        private void DrawResultArea(float availableWidth)
        {
            EditorGUILayout.LabelField("Execution result", EditorStyles.boldLabel);

            float textAreaWidth = availableWidth - 40; // Minus margin and scrollbar width

            // Create scrolling region for result display，Limit width to avoid horizontal scroll
            GUILayout.BeginVertical(EditorStyles.helpBox);
            resultScrollPosition = EditorGUILayout.BeginScrollView(
                resultScrollPosition,
                false, true,  // Disable horizontal scrollbar，Enable vertical scrollbar
                GUILayout.Height(ResultAreaHeight),
                GUILayout.MaxWidth(availableWidth)
            );

            // Result text area，Limit width to prevent horizontal overflow
            EditorGUILayout.TextArea(
                resultText,
                resultStyle,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(textAreaWidth)
            );

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Result action button
            GUILayout.BeginHorizontal();

            // Record result button - Only displayed if there is an execution result and not loaded from history
            if (currentResult != null && !string.IsNullOrEmpty(inputJson))
            {
                if (GUILayout.Button("Record result", GUILayout.Width(80)))
                {
                    RecordCurrentResult();
                }
            }

            // Format result button - Only show when there is result text
            if (!string.IsNullOrEmpty(resultText))
            {
                if (GUILayout.Button("Format result", GUILayout.Width(80)))
                {
                    FormatResultText();
                }
            }

            // Check if batch result，Show extra operations if yes
            if (IsBatchResultDisplayed())
            {
                if (GUILayout.Button("Copy statistics", GUILayout.Width(80)))
                {
                    CopyBatchStatistics();
                }

                if (GUILayout.Button("Show only error", GUILayout.Width(80)))
                {
                    ShowOnlyErrors();
                }
            }

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
                EditorUtility.DisplayDialog("JSONFormat error", $"Unable to parseJSON: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Format result text，SupportJSONAndYAML
        /// </summary>
        private void FormatResultText()
        {
            if (string.IsNullOrEmpty(resultText))
            {
                EditorUtility.DisplayDialog("Prompt", "No result to format", "Confirm");
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

                    // Check if current line is Json Start of object or array
                    string trimmedLine = line.TrimStart();

                    if (trimmedLine.StartsWith("{") || trimmedLine.StartsWith("["))
                    {
                        // Collect complete Json Block
                        string jsonBlock = CollectJsonBlock(lines, ref i);

                        // Try to format Json
                        string formattedJson = TryFormatJson(jsonBlock);
                        if (formattedJson != null)
                        {
                            formattedResult.AppendLine(formattedJson);
                        }
                        else
                        {
                            // Formatting failed，Keep as is
                            formattedResult.AppendLine(jsonBlock);
                        }
                    }
                    else if (!string.IsNullOrEmpty(line))
                    {
                        // Non Json Line，Check for embedded Json
                        string processedLine = ProcessLineWithEmbeddedJson(line);
                        formattedResult.AppendLine(processedLine);
                        i++;
                    }
                    else
                    {
                        // Empty line，Keep as is
                        formattedResult.AppendLine(line);
                        i++;
                    }
                }

                resultText = formattedResult.ToString().TrimEnd();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[McpDebugWindow] Error occurred while formatting result: {e.Message}");
                EditorUtility.DisplayDialog("Formatting failed", $"Unable to format result: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Collect complete Json Block（Handle multiline JSON）
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

                // Calculate bracket balance
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

                // Check if completed
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
        /// Handle embedded Json Row of（Such as "Execution result: {...}"）
        /// </summary>
        private string ProcessLineWithEmbeddedJson(string line)
        {
            // Find first { Or [ Position of
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
                // Has prefix text and Json
                string prefix = line.Substring(0, jsonStartIndex);
                string jsonPart = line.Substring(jsonStartIndex);

                // Try to format Json Partial
                string formattedJson = TryFormatJson(jsonPart);
                if (formattedJson != null)
                {
                    // Format successful，Return prefix + Formatted JSON（Indent align）
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
        /// Try to format text asJSON
        /// </summary>
        private string TryFormatJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                // Try asJObjectParse
                try
                {
                    JsonClass jsonObj = Json.Parse(text) as JsonClass;
                    string formatted = Json.FromObject(jsonObj).ToPrettyString();

                    // Expand after serialization yaml In field \n
                    return ExpandYamlInFormattedJson(formatted);
                }
                catch
                {
                    // Try asJArrayParse
                    JsonArray jsonArray = Json.Parse(text) as JsonArray;
                    string formatted = Json.FromObject(jsonArray).ToPrettyString();

                    // Expand after serialization yaml In field \n
                    return ExpandYamlInFormattedJson(formatted);
                }
            }
            catch
            {
                // Not validJSON
                return null;
            }
        }

        /// <summary>
        /// In formatted Json Expand all newline characters in string fields within text
        /// </summary>
        private string ExpandYamlInFormattedJson(string formattedJson)
        {
            if (string.IsNullOrEmpty(formattedJson))
                return formattedJson;

            // Match all "fieldName": "value" Mode，No limit on field name
            // Match any field name and its string value
            string pattern = @"""([^""]+)"":\s*""([^""\\]*(\\.[^""\\]*)*)""";

            return System.Text.RegularExpressions.Regex.Replace(formattedJson, pattern, match =>
            {
                string fieldName = match.Groups[1].Value;
                string fieldValue = match.Groups[2].Value;

                // Only contains \r\n Or \n Process only then
                if (!fieldValue.Contains("\\r\\n") && !fieldValue.Contains("\\n"))
                    return match.Value;

                // Expand \r\n And \n For actual line break，And keep Json Indent of
                string expandedValue = fieldValue.Replace("\\r\\n", "\n").Replace("\\n", "\n");

                // Get current indent level
                int indentLevel = GetIndentLevel(formattedJson, match.Index);
                string indent = new string(' ', indentLevel + 2); // +2 Because string content needs extra indentation

                // Add proper indentation to each content line（Except first line）
                string[] lines = expandedValue.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    lines[i] = indent + lines[i];
                }
                expandedValue = string.Join("\n", lines);

                // Return the formatted result
                return $"\"{fieldName}\": \"{expandedValue}\"";
            });
        }

        /// <summary>
        /// Get indent level for specified position
        /// </summary>
        private int GetIndentLevel(string text, int position)
        {
            // Move forward to line start
            int lineStart = position;
            while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
            {
                lineStart--;
            }

            // Calculate indent spaces
            int indent = 0;
            for (int i = lineStart; i < position && i < text.Length; i++)
            {
                if (text[i] == ' ')
                    indent++;
                else if (text[i] == '\t')
                    indent += 4; // Tab counts as4Spaces
                else
                    break;
            }

            return indent;
        }

        /// <summary>
        /// Try to formatYAMLText
        /// </summary>
        private string TryFormatYaml(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                // DetectYAMLFeature：Contains colon and line break，But notJSONFormat
                if (!text.Contains(":") || text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("["))
                    return null;

                // SimpleYAMLFormat：Ensure each key-value pair is in a separate line，Proper indent
                string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder formatted = new StringBuilder();

                int indentLevel = 0;
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    // Detect indent change
                    if (trimmedLine.Contains(":"))
                    {
                        // Key-value pair
                        string[] parts = trimmedLine.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            // If value is empty or list/Object start，May need extra indentation
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
                        // Item in list
                        formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                    }
                    else
                    {
                        formatted.AppendLine($"{new string(' ', indentLevel * 2)}{trimmedLine}");
                    }
                }

                string result = formatted.ToString().TrimEnd();

                // If formatted text differs greatly from original，May not beYAML，Returnnull
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
                EditorUtility.DisplayDialog("Error", "Please enterJSONContent", "Confirm");
                return;
            }

            isExecuting = true;
            showResult = true;
            resultText = "Executing...";

            try
            {
                DateTime startTime = DateTime.Now;
                JsonNode result = ExecuteJsonCall(startTime);

                // If result isnull，Indicate async execution
                if (result == null)
                {
                    resultText = "Async executing...";
                    // Refresh UI to show async state
                    Repaint();
                    // Note：isExecutingKeep astrue，Wait for asynchronous callback to complete
                }
                else
                {
                    // Synchronous execution finished
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;
                    CompleteExecution(result, duration);
                }
            }
            catch (Exception e)
            {
                string errorResult = $"Execution error:\n{e.Message}\n\nStack trace:\n{e.StackTrace}";
                resultText = errorResult;
                isExecuting = false;

                Debug.LogException(new Exception("ExecuteCall error", e));
            }
        }

        /// <summary>
        /// Finish execution and updateUIShow
        /// </summary>
        private void CompleteExecution(JsonNode result, TimeSpan duration)
        {

            try
            {
                // Store and format current result
                currentResult = result;
                string formattedResult = FormatResult(result, duration);
                resultText = formattedResult;

                // Refresh UI
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
        /// ExecuteJSONInternal general method called
        /// </summary>
        private JsonNode ExecuteJsonCallInternal(string jsonString, DateTime startTime, System.Action<JsonNode, TimeSpan> onSingleComplete)
        {
            JsonNode inputObj = Json.Parse(jsonString);
            if (inputObj == null)
                return Response.Error("Unable to parseJSONInput");

            // Check if batch call
            if (inputObj is JsonArray jArray)
            {
                // Batch function call
                var functionsCall = new BatchCall();
                JsonNode callResult = null;
                bool callbackExecuted = false;
                functionsCall.HandleCommand(jArray, (result) =>
                {
                    callResult = result;
                    callbackExecuted = true;

                    // If async callback，UpdateUI
                    if (isExecuting)
                    {
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        onSingleComplete?.Invoke(result, duration);
                    }
                });
                // If callback executes immediately，Return result；Otherwise returnnullIndicate async execution
                return callbackExecuted ? callResult : null;
            }
            else if (inputObj is JsonClass inputObjClass && inputObjClass.ContainsKey("func"))
            {
                // Single function call
                var functionCall = new SingleCall();
                JsonNode callResult = null;
                bool callbackExecuted = false;

                functionCall.HandleCommand(inputObj, (result) =>
                {
                    callResult = result;
                    callbackExecuted = true;

                    // If async callback，UpdateUI
                    if (isExecuting)
                    {
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        onSingleComplete?.Invoke(result, duration);
                    }
                });

                // If callback executes immediately，Return result；Otherwise returnnullIndicate async execution
                return callbackExecuted ? callResult : null;
            }
            else
            {
                throw new ArgumentException("InputJSONMust contain 'func' Field（Single call）Or 'funcs' Field（Batch call）");
            }
        }


        private string FormatResult(JsonNode result, TimeSpan duration)
        {
            string formattedResult = $"Execution time: {duration.TotalMilliseconds:F2}ms\n\n";

            // Judge result's status
            string status = "success";
            if (result != null && result is JsonClass resultObj)
            {
                var successNode = resultObj["success"];
                if (successNode != null && successNode.Value == "false")
                {
                    status = "error";
                }
            }

            // Create wrapped result object
            JsonClass wrappedResult = new JsonClass();
            wrappedResult["status"] = status;
            wrappedResult["result"] = result;

            if (result != null)
            {
                // No longer reflect to determine type，Direct use JsonNode Determine if structure is batch call result
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
        /// Check if it is a batch call result
        /// </summary>
        private bool IsBatchCallResult(JsonNode result)
        {
            try
            {
                // Directly determineresultOwn structure（Avoid twiceParseThe caused issues）
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
        /// Format batch call result，Display line by line
        /// </summary>
        private string FormatBatchCallResult(object result)
        {
            try
            {
                var resultJson = Json.FromObject(result);
                var wrappedObj = Json.Parse(resultJson) as JsonClass;

                // Extract actual result from wrapped object
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

                // Show overall stats
                output.AppendLine("=== Batch call results ===");
                output.AppendLine($"Total calls: {totalCalls}");
                output.AppendLine($"Success: {successfulCalls}");
                output.AppendLine($"Failure: {failedCalls}");
                output.AppendLine($"Overall status: {(overallSuccess ? "Success" : "Partial failure")}");
                output.AppendLine();

                // Show each result in list
                if (results != null)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        output.AppendLine($"--- Call #{i + 1} ---");

                        var singleResult = results[i];

                        // Determine success state of single result：Check success Field
                        bool isSuccess = false;
                        if (singleResult != null && !singleResult.type.Equals(JsonNodeType.Null))
                        {
                            // If result is JsonClass，Check its success Field
                            if (singleResult is JsonClass resultObj2)
                            {
                                var successNode = resultObj2["success"];
                                isSuccess = successNode != null && successNode.Value == "true";
                            }
                            else
                            {
                                // If not JsonClass，Default is success（Non null And result not empty）
                                isSuccess = true;
                            }
                        }

                        if (isSuccess)
                        {
                            output.AppendLine("✅ Success");
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
                            output.AppendLine("❌ Failure");

                            // Show result（If any）
                            if (singleResult != null && !singleResult.type.Equals(JsonNodeType.Null))
                            {
                                try
                                {
                                    string formattedSingleResult = Json.FromObject(singleResult);
                                    output.AppendLine("Result details:");
                                    output.AppendLine(formattedSingleResult);
                                }
                                catch
                                {
                                    output.AppendLine(singleResult.Value);
                                }
                            }

                            // Show error info
                            if (errors != null && i < errors.Count && errors[i] != null && !string.IsNullOrEmpty(errors[i].Value))
                            {
                                output.AppendLine($"Error info: {errors[i]}");
                            }
                        }
                        output.AppendLine();
                    }
                }

                // Show all error summary
                if (errors != null && errors.Count > 0)
                {
                    output.AppendLine("=== Error summary ===");
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
                return $"Failed to format batch result: {e.Message}\n\nOriginal result:\n{Json.FromObject(result)}";
            }
        }

        /// <summary>
        /// Clear all results
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
        /// Check if current display is batch result
        /// </summary>
        private bool IsBatchResultDisplayed()
        {
            return currentResult != null && IsBatchCallResult(currentResult);
        }

        /// <summary>
        /// Copy batch call statistics
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

                var statistics = $"Batch call statistics:\n" +
                               $"Total calls: {totalCalls}\n" +
                               $"Success: {successfulCalls}\n" +
                               $"Failure: {failedCalls}\n" +
                               $"Overall status: {(overallSuccess ? "Success" : "Partial failure")}";

                EditorGUIUtility.systemCopyBuffer = statistics;
                EditorUtility.DisplayDialog("Copied", "Stats have been copied to clipboard", "Confirm");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Copy failed", $"Unable to copy stats: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Only show error info
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
                output.AppendLine("=== Error message summary ===");
                output.AppendLine($"Failed call count: {failedCalls}");
                output.AppendLine();

                if (errors != null && errors.Count > 0)
                {
                    for (int i = 0; i < errors.Count; i++)
                    {
                        if (errors[i] != null && !string.IsNullOrEmpty(errors[i].Value))
                        {
                            output.AppendLine($"Error #{i + 1}:");
                            output.AppendLine($"  {errors[i]}");
                            output.AppendLine();
                        }
                    }
                }
                else
                {
                    output.AppendLine("No error info found。");
                }

                resultText = output.ToString();
            }
            catch (Exception e)
            {
                resultText = $"Failed to display error message: {e.Message}";
            }
        }

        /// <summary>
        /// Execute from clipboardJSONContent
        /// </summary>
        private void ExecuteClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("Error", "Clipboard empty", "Confirm");
                    return;
                }

                // ValidateJSONFormat
                if (!ValidateClipboardJson(clipboardContent, out string errorMessage))
                {
                    EditorUtility.DisplayDialog("JSONFormat error", $"Clipboard content is not validJSON:\n{errorMessage}", "Confirm");
                    return;
                }

                // Execute clipboard content
                isExecuting = true;
                showResult = true;
                resultText = "Executing clipboard content...";

                try
                {
                    DateTime startTime = DateTime.Now;
                    JsonNode result = ExecuteJsonCallFromString(clipboardContent, startTime);

                    // If result isnull，Indicate async execution
                    if (result == null)
                    {
                        resultText = "Async execution of clipboard content...";
                        // Refresh UI to show async state
                        Repaint();
                        // Note：isExecutingKeep astrue，Wait for asynchronous callback to complete
                    }
                    else
                    {
                        // Synchronous execution finished
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;

                        // Store and format current result
                        currentResult = result;
                        string formattedResult = FormatResult(result, duration);
                        resultText = $"📋 Execute from clipboard\nOriginalJSON:\n{clipboardContent}\n\n{formattedResult}";

                        // Refresh UI
                        Repaint();
                        isExecuting = false;
                    }
                }
                catch (Exception e)
                {
                    string errorResult = $"Error executing clipboard content:\n{e.Message}\n\nStack trace:\n{e.StackTrace}";
                    resultText = errorResult;

                    Debug.LogError($"[McpDebugWindow] Error occurred while executing clipboard content: {e}");
                }
                finally
                {
                    isExecuting = false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Execution failed", $"Unable to execute clipboard content: {e.Message}", "Confirm");
                isExecuting = false;
            }
        }

        /// <summary>
        /// Execute from stringJSONCall，Support async callback
        /// </summary>
        private JsonNode ExecuteJsonCallFromString(string jsonString, DateTime startTime)
        {
            return ExecuteJsonCallInternal(jsonString, startTime, (result, duration) =>
            {
                // Clipboard format ofUIUpdate
                currentResult = result;
                string formattedResult = FormatResult(result, duration);
                resultText = $"📋 Execute from clipboard\nOriginalJSON:\n{jsonString}\n\n{formattedResult}";

                // Refresh UI
                Repaint();
                isExecuting = false;
            });
        }



        /// <summary>
        /// Paste clipboard content to input box
        /// </summary>
        private void PasteFromClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("Prompt", "Clipboard empty", "Confirm");
                    return;
                }

                // ValidateJSONFormat
                if (!ValidateClipboardJson(clipboardContent, out string errorMessage))
                {
                    bool proceed = EditorUtility.DisplayDialog("JSONFormat warning",
                        $"Clipboard content may be invalidJSON:\n{errorMessage}\n\nStill paste?？",
                        "Still paste", "Cancel");

                    if (!proceed) return;
                }

                inputJson = clipboardContent;
                EditorUtility.DisplayDialog("Success", "Clipboard content pasted to input box", "Confirm");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Paste failed", $"Cannot paste clipboard content: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Preview clipboard content
        /// </summary>
        private void PreviewClipboard()
        {
            try
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrWhiteSpace(clipboardContent))
                {
                    EditorUtility.DisplayDialog("Clipboard preview", "Clipboard empty", "Confirm");
                    return;
                }

                // Limit preview length
                string preview = clipboardContent;
                if (preview.Length > 500)
                {
                    preview = preview.Substring(0, 500) + "\n...(Content too long，Truncated)";
                }

                // ValidateJSONFormat
                string jsonStatus = ValidateClipboardJson(clipboardContent, out string errorMessage)
                    ? "✅ ValidJSONFormat"
                    : $"❌ JSONFormat error: {errorMessage}";

                EditorUtility.DisplayDialog("Clipboard preview",
                    $"Format state: {jsonStatus}\n\nContent preview:\n{preview}", "Confirm");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Preview failed", $"Cannot preview clipboard content: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Check if clipboard is available（Has validJSONContent）
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
        /// Draw clipboard state with color indicator
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
                    statusText = "Clipboard: Empty";
                }
                else
                {
                    bool isValidJson = ValidateClipboardJson(clipboardContent, out _);
                    if (isValidJson)
                    {
                        statusColor = Color.green;
                        statusText = $"Clipboard: ✅ Json ({clipboardContent.Length} Character)";
                    }
                    else
                    {
                        statusColor = new Color(1f, 0.5f, 0f); // Orange
                        statusText = $"Clipboard: ❌ NonJSON ({clipboardContent.Length} Character)";
                    }
                }

                // Show colored state
                Color originalColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(statusText, EditorStyles.miniLabel);
                GUI.color = originalColor;
            }
            catch
            {
                Color originalColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label("Clipboard: Read failed", EditorStyles.miniLabel);
                GUI.color = originalColor;
            }
        }

        /// <summary>
        /// Validate clipboardJSONFormat
        /// </summary>
        private bool ValidateClipboardJson(string content, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(content))
            {
                errorMessage = "Content empty";
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
        /// Check if result is error response
        /// </summary>
        private bool IsErrorResponse(object result)
        {
            if (result == null) return true;

            try
            {
                var resultJson = Json.FromObject(result);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                // Check if there issuccessField and isfalse
                if (resultObj.ContainsKey("success"))
                {
                    return !resultObj["success"]?.AsBool ?? true;
                }

                // Check if there iserrorField
                return resultObj.ContainsKey("error");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extract error message from error response
        /// </summary>
        private string ExtractErrorMessage(object result)
        {
            if (result == null) return "Result is empty";

            try
            {
                var resultJson = Json.FromObject(result);
                var resultObj = Json.Parse(resultJson) as JsonClass;

                // Try fromerrorField get error info
                if (resultObj.ContainsKey("error"))
                {
                    return resultObj["error"]?.Value ?? "Unknown error";
                }

                // Try frommessageField get error info
                if (resultObj.ContainsKey("message"))
                {
                    return resultObj["message"]?.Value ?? "Unknown error";
                }

                return result.ToString();
            }
            catch
            {
                return result.ToString();
            }
        }

        /// <summary>
        /// Manually record current execution result
        /// </summary>
        private void RecordCurrentResult()
        {
            if (currentResult == null || string.IsNullOrEmpty(inputJson))
            {
                EditorUtility.DisplayDialog("Unable to record", "No executable result to record", "Confirm");
                return;
            }

            try
            {
                // Parse inputJSONTo get function name and parameters
                JsonClass inputObj = Json.Parse(inputJson) as JsonClass;

                // Check if batch call
                if (inputObj.ContainsKey("funcs"))
                {
                    // Batch call record
                    RecordBatchResult(inputObj, currentResult);
                }
                else if (inputObj.ContainsKey("func"))
                {
                    // Single function call record
                    RecordSingleResult(inputObj, currentResult);
                }
                else
                {
                    EditorUtility.DisplayDialog("Record failed", "Unable to parse inputJSONFormat", "Confirm");
                    return;
                }

                EditorUtility.DisplayDialog("Record success", "Execution result has been saved to records", "Confirm");

                // Refresh record list
                recordList = null;
                InitializeRecordList();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Record failed", $"Error occurred when recording execution result: {e.Message}", "Confirm");
                Debug.LogError($"[McpDebugWindow] Error occurred when manually recording result: {e}");
            }
        }

        /// <summary>
        /// Record single function call result
        /// </summary>
        private void RecordSingleResult(JsonClass inputObj, object result)
        {
            var funcName = inputObj["func"]?.Value ?? "Unknown";
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
                    errorMsg = resultObj["error"]?.Value ?? "Execution failed";
                }
            }
            else
            {
                errorMsg = result != null ? ExtractErrorMessage(result) : "Execution failed，Returnnull";
                resultJson = result != null ? Json.FromObject(result) : "";
            }

            recordObject.addRecord(
                funcName,
                argsJson,
                resultJson,
                errorMsg,
                0, // No execution time for manual record
                "Debug Window (Manual record)"
            );
            recordObject.saveRecords();
        }

        /// <summary>
        /// Record batch function call result
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
                                errorMsg = "This item failed in batch call";
                            }
                        }

                        recordObject.addRecord(
                            funcName,
                            argsJson,
                            singleResultJson,
                            errorMsg,
                            0, // No execution time for manual record
                            $"Debug Window (Manual record {i + 1}/{funcsArray.Count})"
                        );
                    }

                    recordObject.saveRecords();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error occurred when recording batch result: {e.Message}", e);
            }
        }


        /// <summary>
        /// Select record and refresh UI
        /// </summary>
        private void SelectRecord(int index)
        {
            var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
            if (index < 0 || index >= records.Count) return;

            selectedRecordIndex = index;
            var record = records[index];

            inputJson = record.cmd;

            // Refresh execution result to result area
            if (!string.IsNullOrEmpty(record.result) || !string.IsNullOrEmpty(record.error))
            {
                showResult = true;
                var resultBuilder = new StringBuilder();
                resultBuilder.AppendLine($"📋 Load from execution record (Index: {index})");
                resultBuilder.AppendLine($"Function: {record.name}");
                resultBuilder.AppendLine($"Time: {record.timestamp}");
                resultBuilder.AppendLine($"Source: {record.source}");
                resultBuilder.AppendLine($"Status: {(record.success ? "Success" : "Failure")}");
                if (record.duration > 0)
                {
                    resultBuilder.AppendLine($"Execution time: {record.duration:F2}ms");
                }
                resultBuilder.AppendLine();

                if (!string.IsNullOrEmpty(record.result))
                {
                    resultBuilder.AppendLine("Execution result:");
                    resultBuilder.AppendLine(record.result);
                }

                if (!string.IsNullOrEmpty(record.error))
                {
                    resultBuilder.AppendLine("Error info:");
                    resultBuilder.AppendLine(record.error);
                }

                resultText = resultBuilder.ToString();
                currentResult = null; // Clear current result，Because this is history record
            }

            Repaint();
        }

        /// <summary>
        /// Handle mouse events for record elements（Double-click detection）
        /// </summary>
        private void HandleRecordElementMouseEvents(Rect rect, int index)
        {
            Event e = Event.current;

            // Detect double click only in function name area，Avoid conflict with selection of the entire element
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

                // Detect double-click
                if (lastClickedIndex == index && (currentTime - lastClickTime) < 0.5) // 500msInner double-click
                {
                    // Start editing
                    StartEditing(index);
                    e.Use();
                }
                else
                {
                    // Click，Record time and index
                    lastClickTime = currentTime;
                    lastClickedIndex = index;
                }
            }
        }

        /// <summary>
        /// Start editing record name
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
        /// Finish editing and save
        /// </summary>
        private void FinishEditing(int index, string newName)
        {
            if (editingRecordIndex == index && !string.IsNullOrWhiteSpace(newName))
            {
                var records = McpExecuteRecordObject.instance.GetCurrentGroupRecords();
                if (index >= 0 && index < records.Count)
                {
                    // Update record name
                    records[index].name = newName.Trim();
                    McpExecuteRecordObject.instance.saveRecords();

                    // Show success prompt（Optional）
                    Debug.Log($"[McpDebugWindow] Record name updated: {newName.Trim()}");
                }
            }

            // Exit edit mode
            editingRecordIndex = -1;
            editingText = "";
            editingStarted = false;
            GUI.FocusControl(null); // Clear focus
            Repaint();
        }

        /// <summary>
        /// Cancel editing
        /// </summary>
        private void CancelEditing()
        {
            editingRecordIndex = -1;
            editingText = "";
            editingStarted = false;
            GUI.FocusControl(null); // Clear focus
            Repaint();
        }

        #region Group managementUI

        /// <summary>
        /// Draw group management UI
        /// </summary>
        private void DrawGroupManager(float width)
        {
            var recordObject = McpExecuteRecordObject.instance;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Group management", EditorStyles.boldLabel);

            // Create new group
            GUILayout.Label("Create new group:");
            newGroupName = EditorGUILayout.TextField("Name", newGroupName);
            newGroupDescription = EditorGUILayout.TextField("Description", newGroupDescription);

            GUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrWhiteSpace(newGroupName);
            if (GUILayout.Button("Create group", GUILayout.Width(80)))
            {
                string groupId = System.Guid.NewGuid().ToString("N")[..8];
                string groupNameTrimmed = newGroupName.Trim();
                if (recordObject.CreateGroup(groupId, groupNameTrimmed, newGroupDescription.Trim()))
                {
                    newGroupName = "";
                    newGroupDescription = "";
                    EditorUtility.DisplayDialog("Success", $"Group '{groupNameTrimmed}' Created successfully！", "Confirm");
                }
                else
                {
                    EditorUtility.DisplayDialog("Failure", "Create group failed，Please check for duplicate name。", "Confirm");
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            // Group list（Reduce height）
            if (recordObject.recordGroups.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Existing group:");

                // Use fixed-height scroll region
                groupScrollPosition = GUILayout.BeginScrollView(groupScrollPosition, GUILayout.Height(120));

                for (int i = 0; i < recordObject.recordGroups.Count; i++)
                {
                    var group = recordObject.recordGroups[i];

                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    GUILayout.BeginHorizontal();

                    // Group info（Simplify display）
                    GUILayout.BeginVertical();
                    GUILayout.Label($"{group.name}", EditorStyles.boldLabel);
                    GUILayout.Label($"{recordObject.GetGroupStatistics(group.id)}", EditorStyles.miniLabel);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // Operation button（Horizontal arrangement）
                    if (GUILayout.Button("Switch", GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        recordObject.SwitchToGroup(group.id);
                        recordList = null;
                        InitializeRecordList();
                    }

                    GUI.enabled = !group.isDefault;
                    if (GUILayout.Button("Delete", GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm deletion",
                            $"Confirm to delete group '{group.name}' ?？\n\nAll records in this group will be moved to the default group。",
                            "Delete", "Cancel"))
                        {
                            recordObject.DeleteGroup(group.id);
                            recordList = null;
                            InitializeRecordList();
                        }
                    }
                    GUI.enabled = true;

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Calculate height of group management area
        /// </summary>
        private float CalculateGroupManagerHeight()
        {
            var recordObject = McpExecuteRecordObject.instance;
            float baseHeight = 120; // Basic height（Title + Create area）

            if (recordObject.recordGroups.Count > 0)
            {
                baseHeight += 140; // Group list area（Title + Fixed height scroll area）
            }

            return baseHeight;
        }

        /// <summary>
        /// Get display name of current group
        /// </summary>
        private string GetCurrentGroupDisplayName()
        {
            var recordObject = McpExecuteRecordObject.instance;
            var currentGroup = recordObject.GetCurrentGroup();
            return currentGroup?.name ?? "Unknown group";
        }

        #endregion

    }
}