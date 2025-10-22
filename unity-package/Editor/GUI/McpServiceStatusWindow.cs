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
        private static int currentPort => McpService.currentPort;

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

        private async void OnEnable()
        {
            // 先设置为默认状态
            isServiceRunning = false;
            // 异步检测 - 检查是否有任何端口在使用，并且是否是当前Unity进程
            bool anyPortInUse = await IsAnyPortInRangeInUse();
            isServiceRunning = anyPortInUse && McpService.IsRunning;
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
                isServiceRunning = McpService.IsRunning;
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

            // 日志开关
            bool newEnableLog = EditorGUILayout.ToggleLeft("日志", McpService.EnableLog, GUILayout.Width(60));
            if (newEnableLog != McpService.EnableLog)
            {
                McpService.EnableLog = newEnableLog;
                EditorPrefs.SetBool("mcp_enable_log", newEnableLog);
            }
            EditorGUILayout.EndHorizontal();
            var installStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            DrawStatusDot(installStatusRect, isServiceRunning ? Color.green : Color.red);
            EditorGUILayout.LabelField($"       Status: {(isServiceRunning ? "Running" : "Stopped")}");

            // 显示端口信息
            if (isServiceRunning && currentPort != -1)
            {
                EditorGUILayout.LabelField($"Port: {currentPort} (Range: {unityPortStart}-{unityPortEnd})");
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

        private static async void ToggleService()
        {
            if (isServiceRunning)
            {
                McpService.Stop();
                isServiceRunning = false;
            }
            else
            {
                // 尝试启动 - Unity MCP 会自动选择可用端口
                bool hasConflicts = false;
                List<int> conflictPorts = new List<int>();

                // 检查端口范围内是否有冲突
                for (int port = unityPortStart; port <= unityPortEnd; port++)
                {
                    bool inUse = await IsPortInUseAsync(port);
                    if (inUse)
                    {
                        // 检查是否是外部进程占用
                        bool isExternalProcess = await IsPortUsedByExternalProcess(port);
                        if (isExternalProcess)
                        {
                            hasConflicts = true;
                            conflictPorts.Add(port);
                        }
                    }
                }

                // 如果有外部进程占用端口，询问用户是否清理
                if (hasConflicts)
                {
                    string conflictPortsStr = string.Join(", ", conflictPorts);
                    if (EditorUtility.DisplayDialog("端口冲突",
                        $"端口 {conflictPortsStr} 被外部进程占用。\n\n" +
                        "选择'清理'将尝试终止占用进程，\n" +
                        "选择'继续'将使用其他可用端口启动。", "清理", "继续"))
                    {
                        // 用户选择清理冲突端口
                        await ClearConflictPorts(conflictPorts);
                    }
                }

                // 尝试启动Unity MCP，它会自动选择可用端口
                McpService.Start();

                // 检查启动是否成功
                if (McpService.IsRunning)
                {
                    isServiceRunning = true;
                    Debug.Log($"Unity MCP Bridge 已启动，使用端口: {McpService.currentPort}");
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
            int clientCount = McpService.ConnectedClientCount;
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

                    var clients = McpService.GetConnectedClients();
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

        private static async Task<bool> IsPortInUseAsync(int port)
        {
            // 使用托管 API 判断是否有"正在监听"的端口，避免误把连接状态（TIME_WAIT/CLOSE_WAIT/ESTABLISHED）当作占用
            return await Task.Run(() =>
            {
                try
                {
                    IPGlobalProperties ipProps = IPGlobalProperties.GetIPGlobalProperties();
                    IPEndPoint[] listeners = ipProps.GetActiveTcpListeners();
                    foreach (var ep in listeners)
                    {
                        if (ep.Port == port)
                        {
                            return true; // 仅当端口处于 LISTENING 才认为被占用
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"检查端口占用时发生错误: {ex.Message}");
                    return false;
                }
            });
        }

        private static async Task<bool> IsAnyPortInRangeInUse()
        {
            for (int port = unityPortStart; port <= unityPortEnd; port++)
            {
                if (await IsPortInUseAsync(port))
                {
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> IsPortUsedByExternalProcess(int port)
        {
            return await Task.Run(() =>
            {
                try
                {
                    int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

#if UNITY_EDITOR_WIN
                    // Windows: 使用netstat检查端口占用
                    System.Diagnostics.Process netstat = new System.Diagnostics.Process();
                    netstat.StartInfo.FileName = "cmd.exe";
                    netstat.StartInfo.Arguments = $"/c netstat -ano | findstr :{port}";
                    netstat.StartInfo.RedirectStandardOutput = true;
                    netstat.StartInfo.UseShellExecute = false;
                    netstat.StartInfo.CreateNoWindow = true;
                    netstat.Start();
                    string output = netstat.StandardOutput.ReadToEnd();
                    netstat.WaitForExit();

                    string[] lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                        {
                            if (pid != currentProcessId && line.Contains("LISTENING"))
                            {
                                return true; // 外部进程占用
                            }
                        }
                    }
#elif UNITY_EDITOR_OSX
                    // macOS: 使用lsof检查端口占用
                    System.Diagnostics.Process lsof = new System.Diagnostics.Process();
                    lsof.StartInfo.FileName = "/bin/bash";
                    lsof.StartInfo.Arguments = $"-c \"lsof -i :{port} -sTCP:LISTEN\"";
                    lsof.StartInfo.RedirectStandardOutput = true;
                    lsof.StartInfo.UseShellExecute = false;
                    lsof.StartInfo.CreateNoWindow = true;
                    lsof.Start();
                    string output = lsof.StandardOutput.ReadToEnd();
                    lsof.WaitForExit();

                    string[] lines = output.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains("LISTEN"))
                        {
                            var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && int.TryParse(parts[1], out int pid))
                            {
                                if (pid != currentProcessId)
                                {
                                    return true; // 外部进程占用
                                }
                            }
                        }
                    }
#endif
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"检查端口是否被外部进程占用时发生错误: {ex.Message}");
                    return false;
                }
            });
        }

        private static async Task ClearConflictPorts(List<int> conflictPorts)
        {
            await Task.Run(() =>
            {
                try
                {
                    int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

                    foreach (int port in conflictPorts)
                    {
#if UNITY_EDITOR_WIN
                        // Windows: 查找并杀死占用端口的进程
                        System.Diagnostics.Process netstat = new System.Diagnostics.Process();
                        netstat.StartInfo.FileName = "cmd.exe";
                        netstat.StartInfo.Arguments = $"/c netstat -ano | findstr :{port}";
                        netstat.StartInfo.RedirectStandardOutput = true;
                        netstat.StartInfo.UseShellExecute = false;
                        netstat.StartInfo.CreateNoWindow = true;
                        netstat.Start();
                        string output = netstat.StandardOutput.ReadToEnd();
                        netstat.WaitForExit();

                        string[] lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                        HashSet<int> pids = new HashSet<int>();
                        foreach (var line in lines)
                        {
                            var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                            {
                                if (pid != currentProcessId && line.Contains("LISTENING"))
                                {
                                    pids.Add(pid);
                                }
                            }
                        }

                        foreach (int pid in pids)
                        {
                            System.Diagnostics.Process kill = new System.Diagnostics.Process();
                            kill.StartInfo.FileName = "taskkill";
                            kill.StartInfo.Arguments = $"/PID {pid} /F";
                            kill.StartInfo.CreateNoWindow = true;
                            kill.StartInfo.UseShellExecute = false;
                            kill.Start();
                            kill.WaitForExit();
                        }
#elif UNITY_EDITOR_OSX
                        // macOS: 查找并杀死占用端口的进程
                        System.Diagnostics.Process lsof = new System.Diagnostics.Process();
                        lsof.StartInfo.FileName = "/bin/bash";
                        lsof.StartInfo.Arguments = $"-c \"lsof -i :{port} -sTCP:LISTEN -t\"";
                        lsof.StartInfo.RedirectStandardOutput = true;
                        lsof.StartInfo.UseShellExecute = false;
                        lsof.StartInfo.CreateNoWindow = true;
                        lsof.Start();
                        string output = lsof.StandardOutput.ReadToEnd();
                        lsof.WaitForExit();

                        string[] pids = output.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (var pidStr in pids)
                        {
                            if (int.TryParse(pidStr, out int pid) && pid != currentProcessId)
                            {
                                System.Diagnostics.Process kill = new System.Diagnostics.Process();
                                kill.StartInfo.FileName = "/bin/bash";
                                kill.StartInfo.Arguments = $"-c \"kill -9 {pid}\"";
                                kill.StartInfo.CreateNoWindow = true;
                                kill.StartInfo.UseShellExecute = false;
                                kill.Start();
                                kill.WaitForExit();
                            }
                        }
#endif
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"清理冲突端口时发生错误: {ex.Message}");
                }
            });
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
                McpService.Stop();
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
            yield return new WaitForSeconds(0.5f);

            EditorUtility.DisplayProgressBar("重启MCP服务器", "正在启动服务器...", 0.7f);

            // 启动服务器
            try
            {
                McpService.Start();
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
            if (McpService.IsRunning)
            {
                isServiceRunning = true;
                EditorUtility.DisplayDialog(
                    "重启成功",
                    $"MCP服务器已成功重启！\n\n服务端口: {McpService.currentPort}",
                    "确定"
                );
                Debug.Log($"[McpServiceStatusWindow] MCP服务器已重启，端口: {McpService.currentPort}");

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
