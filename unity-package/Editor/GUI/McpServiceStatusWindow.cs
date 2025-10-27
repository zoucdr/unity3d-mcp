using System;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Unity.Mcp.Executer;

namespace Unity.Mcp.Gui
{
    /// <summary>
    /// MCP服务状态窗口，用于显示服务运行状态和客户端连接信息
    /// </summary>
    public class McpServiceStatusWindow : EditorWindow
    {
        // 客户端连接状态相关变量
        private static Vector2 clientsScrollPosition;
        private static bool showClientDetails = false;

        // 服务运行状态
        private static bool isServiceRunning = false;
        private static int unityPortStart => McpService.unityPortStart;
        private static int unityPortEnd => McpService.unityPortEnd;
        private static List<int> activePorts => McpService.GetActivePorts();

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
                Repaint(); // 刷新窗口
            }
        }

        private void OnGUI()
        {
            // 使用垂直布局管理整个窗口
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // Unity Bridge Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行和日志开关在同一行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity MCP Services", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // 日志级别下拉菜单
            var currentLogLevel = McpLogger.GetLogLevel();
            var newLogLevel = (McpLogger.LogLevel)EditorGUILayout.EnumPopup("日志级别", currentLogLevel, GUILayout.Width(150));
            if (newLogLevel != currentLogLevel)
            {
                McpLogger.SetLogLevel(newLogLevel);
            }
            EditorGUILayout.EndHorizontal();
            var installStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            DrawStatusDot(installStatusRect, isServiceRunning ? Color.green : Color.red);
            EditorGUILayout.LabelField($"       Status: {(isServiceRunning ? "Running" : "Stopped")}");

            // 显示端口信息
            if (isServiceRunning && activePorts != null && activePorts.Count > 0)
            {
                string portsStr = string.Join(", ", activePorts);
                EditorGUILayout.LabelField($"Active Ports: {portsStr} (Range: {unityPortStart}-{unityPortEnd})");
            }
            else
            {
                EditorGUILayout.LabelField($"Port Range: {unityPortStart}-{unityPortEnd}");
            }
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

        private static  void ToggleService()
        {
            if (isServiceRunning)
            {
                McpService.StopService();
                isServiceRunning = false;
            }
            else
            {
                // 尝试启动Unity MCP，它会自动选择可用端口
                McpService.StartService();

                // 检查启动是否成功
                if (McpService.Instance.IsRunning)
                {
                    isServiceRunning = true;
                    var ports = McpService.GetActivePorts();
                    string portsStr = ports.Count > 0 ? string.Join(", ", ports) : "无";
                    Debug.Log($"Unity MCP Bridge 已启动，激活端口: {portsStr}");
                }
                else
                {
                    isServiceRunning = false;
                    EditorUtility.DisplayDialog("启动失败",
                        $"无法在端口范围 {unityPortStart}-{unityPortEnd} 内启动Unity MCP Bridge。\n" +
                        "请检查是否有其他进程占用了所有端口。", "确定");
                }
            }
            EditorPrefs.SetBool("mcp_open_state", isServiceRunning);
        }

        /// <summary>
        /// 绘制客户端连接状态
        /// </summary>
        private static void DrawClientConnectionStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 客户端连接状态标题
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("客户端连接状态", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // 显示连接数量
            int clientCount = McpService.GetConnectedClientCount();
            Color countColor = clientCount > 0 ? Color.green : Color.gray;
            GUIStyle countStyle = new GUIStyle(EditorStyles.label);
            countStyle.normal.textColor = countColor;
            countStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField($"连接数: {clientCount}", countStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (clientCount > 0)
            {
                // 详细信息折叠控制
                showClientDetails = EditorGUILayout.Foldout(showClientDetails, "显示详细信息", true);

                if (showClientDetails)
                {
                    EditorGUILayout.Space(5);

                    // 客户端列表滚动视图
                    clientsScrollPosition = EditorGUILayout.BeginScrollView(clientsScrollPosition,
                        GUILayout.MinHeight(80), GUILayout.MaxHeight(220));

                    var clients = McpService.GetAllConnectedClients();
                    foreach (var client in clients)
                    {
                        EditorGUILayout.BeginVertical("box");

                        // 客户端基本信息
                        EditorGUILayout.LabelField($"端点: {client.EndPoint}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"连接时间: {client.ConnectedAt:HH:mm:ss}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"最后活动: {client.LastActivity:HH:mm:ss}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"命令数: {client.CommandCount}", EditorStyles.miniLabel);

                        // 计算连接持续时间
                        TimeSpan duration = DateTime.Now - client.ConnectedAt;
                        string durationText = duration.TotalMinutes < 1
                            ? $"{duration.Seconds}秒"
                            : $"{(int)duration.TotalMinutes}分{duration.Seconds}秒";
                        EditorGUILayout.LabelField($"连接时长: {durationText}", EditorStyles.miniLabel);

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.EndScrollView();
                }
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
                    Debug.LogError($"[McpServiceStatusWindow] 重启协程异常: {ex.Message}\n{ex.StackTrace}");
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
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "停止服务器错误",
                    $"停止MCP服务器时发生错误：\n\n{ex.Message}",
                    "确定"
                );
                Debug.LogError($"[McpServiceStatusWindow] 停止MCP服务器时发生错误: {ex.Message}\n{ex.StackTrace}");
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
                Debug.LogError($"[McpServiceStatusWindow] 启动MCP服务器时发生错误: {ex.Message}\n{ex.StackTrace}");
                yield break; // 终止协程
            }

            // 清除进度条
            EditorUtility.ClearProgressBar();

            // 检查启动状态
            if (McpService.Instance.IsRunning)
            {
                isServiceRunning = true;
                var ports = McpService.GetActivePorts();
                string portsStr = ports.Count > 0 ? string.Join(", ", ports) : "无";
                EditorUtility.DisplayDialog(
                    "重启成功",
                    $"MCP服务器已成功重启！\n\n激活端口: {portsStr}",
                    "确定"
                );
                Debug.Log($"[McpServiceStatusWindow] MCP服务器已重启，激活端口: {portsStr}");

                // 更新EditorPrefs状态
                EditorPrefs.SetBool("mcp_open_state", true);
            }
            else
            {
                isServiceRunning = false;
                EditorUtility.DisplayDialog(
                    "重启失败",
                    "MCP服务器重启失败，请查看控制台日志了解详情。",
                    "确定"
                );
                Debug.LogError("[McpServiceStatusWindow] MCP服务器重启失败");

                // 更新EditorPrefs状态
                EditorPrefs.SetBool("mcp_open_state", false);
            }

            // 刷新窗口显示
            if (instance != null)
            {
                instance.Repaint();
            }
        }
    }
}
