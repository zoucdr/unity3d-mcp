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
            instance = GetWindow<McpServiceStatusWindow>("MCP服务状态");
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
            // 使用一个固定的列表引用，避免每次都创建新的列表
            var records = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords();
            httpRequestRecordsList = new ReorderableList(
                records,
                typeof(UniMcp.Models.McpExecuteRecordObject.HttpRequestRecord),
                false, // 不可拖拽
                true,  // 显示标题
                false, // 不可添加
                false  // 不可删除
            );

            // 设置标题
            httpRequestRecordsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "HTTP请求记录");
            };

            // 设置元素高度
            httpRequestRecordsList.elementHeightCallback = (int index) =>
            {
                // 获取最新的记录列表
                var currentRecords = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords();
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
                    // 基本信息行 + 请求内容 + 响应内容 + 处理时长
                    height += EditorGUIUtility.singleLineHeight * 8; // 基本信息行

                    // 请求内容和响应内容区域
                    if (!string.IsNullOrEmpty(record.requestContent))
                    {
                        height += 40; // 请求内容文本区域
                    }

                    if (!string.IsNullOrEmpty(record.responseContent))
                    {
                        height += 40; // 响应内容文本区域
                    }

                    height += 8; // 额外间距
                }

                return height;
            };

            // 设置绘制元素
            httpRequestRecordsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // 获取最新的记录列表
                var currentRecords = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords();
                if (index >= currentRecords.Count) return;

                var record = currentRecords[index];

                // 确保记录ID在字典中存在
                if (!recordFoldoutStates.ContainsKey(record.id))
                {
                    recordFoldoutStates[record.id] = false;
                }

                // 绘制折叠控件和基本信息
                Rect foldoutRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

                // 创建标题文本
                string title = $"{record.endPoint} - {record.httpMethod} - {record.requestTime:HH:mm:ss} - {(record.success ? "成功" : "失败")}";

                // 绘制折叠控件
                recordFoldoutStates[record.id] = EditorGUI.Foldout(foldoutRect, recordFoldoutStates[record.id], title, true);

                // 如果展开，绘制详细信息
                if (recordFoldoutStates[record.id])
                {
                    float yOffset = rect.y + EditorGUIUtility.singleLineHeight + 4;
                    float detailWidth = rect.width - 10;

                    // 绘制详细信息区域背景
                    Rect detailsRect = new Rect(rect.x + 5, yOffset, detailWidth, rect.height - EditorGUIUtility.singleLineHeight - 4);
                    GUI.Box(detailsRect, "", EditorStyles.helpBox);

                    // 内容区域
                    float contentX = detailsRect.x + 5;
                    float contentWidth = detailsRect.width - 10;
                    float contentY = yOffset + 5;

                    // 基本信息
                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"客户端: {record.endPoint}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"请求时间: {record.requestTime:HH:mm:ss}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"处理时间: {record.responseTime:HH:mm:ss}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"HTTP方法: {record.httpMethod}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"状态码: {record.statusCode}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"成功: {(record.success ? "是" : "否")}", EditorStyles.miniLabel);
                    contentY += EditorGUIUtility.singleLineHeight;

                    // 请求内容
                    if (!string.IsNullOrEmpty(record.requestContent))
                    {
                        EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                            "请求内容:", EditorStyles.miniBoldLabel);
                        contentY += EditorGUIUtility.singleLineHeight;

                        EditorGUI.TextArea(new Rect(contentX, contentY, contentWidth, 40),
                            record.requestContent, EditorStyles.textArea);
                        contentY += 40;
                    }

                    // 响应内容
                    if (!string.IsNullOrEmpty(record.responseContent))
                    {
                        EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                            "响应内容:", EditorStyles.miniBoldLabel);
                        contentY += EditorGUIUtility.singleLineHeight;

                        EditorGUI.TextArea(new Rect(contentX, contentY, contentWidth, 40),
                            record.responseContent, EditorStyles.textArea);
                        contentY += 40;
                    }

                    // 处理时长
                    EditorGUI.LabelField(new Rect(contentX, contentY, contentWidth, EditorGUIUtility.singleLineHeight),
                        $"处理时长: {record.duration:F2}毫秒", EditorStyles.miniLabel);
                }
            };
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
                    var records = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords();
                    // 检查数据是否发生变化
                    if (httpRequestRecordsList.list.Count != records.Count)
                    {
                        // 数据发生变化，重新初始化ReorderableList
                        InitializeHttpRequestRecordsList();
                    }
                    else
                    {
                        // 数据没有变化，只更新引用
                        httpRequestRecordsList.list = records;
                    }
                }

                Repaint(); // 刷新窗口
            }
        }

        private void OnGUI()
        {
            // 使用垂直布局管理整个窗口
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // Unity Bridge Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity MCP Services", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            var installStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            DrawStatusDot(installStatusRect, isServiceRunning ? Color.green : Color.red);
            EditorGUILayout.LabelField($"       Status: {(isServiceRunning ? "Running" : "Stopped")}");

            // 端口配置
            DrawPortConfiguration();
            EditorGUILayout.EndHorizontal();

            // 启动/停止按钮和重启按钮在同一行
            if (GUILayout.Button(isServiceRunning ? "Stop Server" : "Start Server"))
            {
                ToggleService();
            }

            // 重启服务器按钮（只在服务运行时显示）
            if (isServiceRunning)
            {
                if (GUILayout.Button("Restart Server"))
                {
                    RestartServer();
                }
            }

            EditorGUILayout.Space(5);
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
            Rect dotRect = new(statusRect.x + 6, statusRect.y + 4, 12, 12);
            Vector3 center = new(
                dotRect.x + (dotRect.width / 2),
                dotRect.y + (dotRect.height / 2),
                0
            );
            float radius = dotRect.width / 2;

            // Draw the main dot
            Handles.color = statusColor;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);

            // Draw the border
            Color borderColor = new(
                statusColor.r * 0.7f,
                statusColor.g * 0.7f,
                statusColor.b * 0.7f
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
                    McpLogger.Log($"Unity MCP Bridge 已启动，端口: {mcpPort}");
                }
                else
                {
                    isServiceRunning = false;
                    // 启动失败时设置 ResourcesCapability 为 false
                    McpLocalSettings.Instance.ResourcesCapability = false;
                    EditorUtility.DisplayDialog("启动失败",
                        $"无法在端口 {mcpPort} 启动Unity MCP Bridge。\n" +
                        "请检查是否有其他进程占用了所有端口。", "确定");
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

        /// <summary>
        /// 绘制客户端请求记录
        /// </summary>
        private void DrawClientConnectionStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 客户端请求记录标题和工具栏
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HTTP请求记录", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // 显示记录数量
            int clientCount = UniMcp.Models.McpExecuteRecordObject.instance.GetHttpRequestRecords().Count;
            Color countColor = clientCount > 0 ? Color.green : Color.gray;
            GUIStyle countStyle = new GUIStyle(EditorStyles.label);
            countStyle.normal.textColor = countColor;
            countStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField($"记录数: {clientCount}", countStyle, GUILayout.Width(80));

            // 刷新按钮
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                RefreshHttpRequestRecordsList();
            }

            // 清空记录按钮
            if (clientCount > 0 && GUILayout.Button("清空", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("清空请求记录", "确定要清空所有HTTP请求记录吗？", "确定", "取消"))
                {
                    UniMcp.Models.McpExecuteRecordObject.instance.ClearHttpRequestRecords();
                    // 清空折叠状态字典
                    recordFoldoutStates.Clear();
                    // 刷新列表
                    RefreshHttpRequestRecordsList();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (clientCount > 0)
            {
                EditorGUILayout.Space(5);

                // 使用ReorderableList显示HTTP请求记录
                clientsScrollPosition = EditorGUILayout.BeginScrollView(clientsScrollPosition,
                    GUILayout.MinHeight(100), GUILayout.MaxHeight(300));

                // 确保ReorderableList已初始化
                if (httpRequestRecordsList == null)
                {
                    InitializeHttpRequestRecordsList();
                }

                // 绘制ReorderableList
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
                EditorGUILayout.LabelField("暂无客户端连接", EditorStyles.centeredGreyMiniLabel);
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
                "重启MCP服务器",
                "确定要重启MCP服务器吗？\n\n这将断开所有当前连接的客户端。",
                "确定",
                "取消"
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
            EditorUtility.DisplayProgressBar("重启MCP服务器", "正在停止服务器...", 0.3f);

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
                    "停止服务器错误",
                    $"停止MCP服务器时发生错误：\n\n{ex.Message}",
                    "确定"
                );
                McpLogger.LogError($"[McpServiceStatusWindow] 停止MCP服务器时发生错误: {ex.Message}\n{ex.StackTrace}");
                yield break; // 终止协程
            }

            // 等待0.5秒确保资源释放（不能在try-catch中使用yield return）
            yield return new WaitForSeconds(1);

            EditorUtility.DisplayProgressBar("重启MCP服务器", "正在启动服务器...", 0.7f);

            // 启动服务器
            try
            {
                McpService.StartService();
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "启动服务器错误",
                    $"启动MCP服务器时发生错误：\n\n{ex.Message}",
                    "确定"
                );
                McpLogger.LogError($"[McpServiceStatusWindow] 启动MCP服务器时发生错误: {ex.Message}\n{ex.StackTrace}");
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
                    "重启成功",
                    $"MCP服务器已成功重启！\n\n端口: {mcpPort}",
                    "确定"
                );
                McpLogger.Log($"[McpServiceStatusWindow] MCP服务器已重启，端口: {mcpPort}");

                // 更新McpLocalSettings状态
                McpLocalSettings.Instance.McpOpenState = true;
            }
            else
            {
                isServiceRunning = false;
                // 启动失败时设置 ResourcesCapability 为 false
                McpLocalSettings.Instance.ResourcesCapability = false;
                EditorUtility.DisplayDialog(
                    "重启失败",
                    "MCP服务器重启失败，请查看控制台日志了解详情。",
                    "确定"
                );
                McpLogger.LogError("[McpServiceStatusWindow] MCP服务器重启失败");

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
                    Debug.Log($"[McpServiceStatusWindow] 端口已更改为: {portValue}");
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
                    Debug.Log($"[McpServiceStatusWindow] 测试连接到: {url}");

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
                Debug.LogError($"[McpServiceStatusWindow] 连接测试失败: {ex}");
            }
        }
    }
}
