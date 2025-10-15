using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using UnityMcp.Executer;
using System.Linq;

namespace UnityMcp.Gui
{
    /// <summary>
    /// MCPConnection managementGUIClass，Provide all drawing functionality as static methods
    /// Used inProjectSettingsShow inMCPSetting
    /// </summary>
    public static class McpConnectGUI
    {
        // Tool method list related variables
        private static Dictionary<string, bool> methodFoldouts = new Dictionary<string, bool>();
        private static Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>(); // Group collapse state
        private static Vector2 methodsScrollPosition;
        private static Dictionary<string, double> methodClickTimes = new Dictionary<string, double>();
        private const double doubleClickTime = 0.3; // Double-click threshold（Seconds）

        // Client connection state related variables
        private static Vector2 clientsScrollPosition;
        private static bool showClientDetails = false;

        // Service run status
        private static bool isUnityBridgeRunning = false;
        private static int unityPortStart => McpConnect.unityPortStart;
        private static int unityPortEnd => McpConnect.unityPortEnd;
        private static int currentPort => McpConnect.currentPort;

        /// <summary>
        /// InitializationGUIState，Check service running state
        /// </summary>
        public static async void Initialize()
        {
            // Set to default first
            isUnityBridgeRunning = false;

            // Async check - Check if any port is in use，And whether it is currentUnityProcess
            bool anyPortInUse = await IsAnyPortInRangeInUse();
            isUnityBridgeRunning = anyPortInUse && McpConnect.IsRunning;
        }

        private static async Task<bool> IsPortInUseAsync(int port)
        {
            // Use managed API Determine if exists"Listening"Port of，Avoid misidentifying connection state（TIME_WAIT/CLOSE_WAIT/ESTABLISHED）Regard as occupied
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
                            return true; // Only when port is at LISTENING Then considered occupied
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error occurred while checking port occupation: {ex.Message}");
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

        /// <summary>
        /// Draw fullMCPSettingGUI
        /// </summary>
        public static void DrawGUI()
        {
            // Use vertical layout to manage the whole window，Ensure full usage of space
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            // Unity Bridge Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title and log switch are on the same row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unity MCP Bridge", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Log switch
            bool newEnableLog = EditorGUILayout.ToggleLeft("Log", McpConnect.EnableLog, GUILayout.Width(60));
            if (newEnableLog != McpConnect.EnableLog)
            {
                McpConnect.EnableLog = newEnableLog;
                EditorPrefs.SetBool("mcp_enable_log", newEnableLog);
            }
            EditorGUILayout.EndHorizontal();
            var installStatusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            DrawStatusDot(installStatusRect, isUnityBridgeRunning ? Color.green : Color.red);
            EditorGUILayout.LabelField($"       Status: {(isUnityBridgeRunning ? "Running" : "Stopped")}");

            // Show port info
            if (isUnityBridgeRunning && currentPort != -1)
            {
                EditorGUILayout.LabelField($"Port: {currentPort} (Range: {unityPortStart}-{unityPortEnd})");
            }
            else
            {
                EditorGUILayout.LabelField($"Port Range: {unityPortStart}-{unityPortEnd}");
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(isUnityBridgeRunning ? "Stop Bridge" : "Start Bridge"))
            {
                ToggleUnityBridge();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

            // Client connection state section
            if (isUnityBridgeRunning)
            {
                DrawClientConnectionStatus();
            }

            // Control panel moved to title row，No longer shown alone
            // DrawControlPanel();

            // Add tool method list - Let it fill remaining space
            EditorGUILayout.Space(10);
            DrawMethodsList();

            // End main vertical layout
            EditorGUILayout.EndVertical();
        }

        private static async void ToggleUnityBridge()
        {
            if (isUnityBridgeRunning)
            {
                McpConnect.Stop();
                isUnityBridgeRunning = false;
            }
            else
            {
                // Attempt start - Unity MCP Will automatically select available port
                bool hasConflicts = false;
                List<int> conflictPorts = new List<int>();

                // Check for conflicts in port range
                for (int port = unityPortStart; port <= unityPortEnd; port++)
                {
                    bool inUse = await IsPortInUseAsync(port);
                    if (inUse)
                    {
                        // Check whether it is occupied by external process
                        bool isExternalProcess = await IsPortUsedByExternalProcess(port);
                        if (isExternalProcess)
                        {
                            hasConflicts = true;
                            conflictPorts.Add(port);
                        }
                    }
                }

                // If external process occupies port，Ask user whether to clean
                if (hasConflicts)
                {
                    string conflictPortsStr = string.Join(", ", conflictPorts);
                    if (EditorUtility.DisplayDialog("Port conflict",
                        $"Port {conflictPortsStr} Occupied by external process。\n\n" +
                        "Select'Clear'Will attempt to terminate occupying process，\n" +
                        "Select'Continue'Will start using other available ports。", "Clear", "Continue"))
                    {
                        // User selects to clean conflicting ports
                        await ClearConflictPorts(conflictPorts);
                    }
                }

                // Attempt startUnity MCP，It will auto select available port
                McpConnect.Start();

                // Check if startup succeeded
                if (McpConnect.IsRunning)
                {
                    isUnityBridgeRunning = true;
                    Debug.Log($"Unity MCP Bridge Started，Use port: {McpConnect.currentPort}");
                }
                else
                {
                    isUnityBridgeRunning = false;
                    EditorUtility.DisplayDialog("Startup failure",
                        $"Cannot within port range {unityPortStart}-{unityPortEnd} Inner startupUnity MCP Bridge。\n" +
                        "Please check if other processes are occupying all ports。", "Confirm");
                }
            }
            EditorPrefs.SetBool("mcp_open_state", isUnityBridgeRunning);
        }

        private static async Task<bool> IsPortUsedByExternalProcess(int port)
        {
            return await Task.Run(() =>
            {
                try
                {
                    int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

#if UNITY_EDITOR_WIN
                    // Windows: UsenetstatCheck port usage
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
                                return true; // Occupied by external process
                            }
                        }
                    }
#elif UNITY_EDITOR_OSX
                    // macOS: UselsofCheck port usage
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
                                    return true; // Occupied by external process
                                }
                            }
                        }
                    }
#endif
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error occurred while checking if port is occupied by external process: {ex.Message}");
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
                        // Windows: Find and kill process occupying the port
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
                        // macOS: Find and kill process occupying the port
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
                    Debug.LogError($"Error occurred while cleaning conflicting ports: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Dynamically calculate available height of tool method list
        /// </summary>
        private static float CalculateAvailableMethodsHeight()
        {
            // Use fixed height，Because inProjectSettingsNo dynamic calculation needed here
            float windowHeight = 800f;

            // Estimate used space
            float usedHeight = 0f;

            // Unity Bridge Section Estimate height (About 120-140px)
            usedHeight += 100;

            // Client connection state section（If show）
            if (isUnityBridgeRunning)
            {
                int clientCount = McpConnect.ConnectedClientCount;
                if (clientCount > 0)
                {
                    // If displaying details，Extra increase for scroll view height
                    if (showClientDetails)
                    {
                        usedHeight += 80f;
                        // Scroll view：Minimum80px，Each client takes about100px，Maximum220px，And add margin
                        usedHeight += Mathf.Max(80f, Mathf.Min(clientCount * 100f, 220f)) + 10f;
                    }
                }
            }

            // Tool method list header and spacing (About 50px)
            usedHeight += 50f;

            // Window margins and scrollbars (About 30px)
            usedHeight += 30f;

            // Calculate remaining usable height，Retain at least 150px
            float availableHeight = Mathf.Max(windowHeight - usedHeight, 150f);

            return availableHeight;
        }

        /// <summary>
        /// Draw tool method list，Support folding，Group by category to display，Assembly info displayed after method name
        /// </summary>
        private static void DrawMethodsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            // Title bar：Show title at left，Show debug button on right
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Available tool methods", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Debug window button
            GUIStyle titleDebugButtonStyle = new GUIStyle(EditorStyles.miniButton);
            Color titleOriginalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // Light blue background

            if (GUILayout.Button("Debug window", titleDebugButtonStyle, GUILayout.Width(70)))
            {
                // Open debug window（Do not prefill content）
                McpDebugWindow.ShowWindow();
            }

            GUI.backgroundColor = titleOriginalColor;
            EditorGUILayout.EndHorizontal();

            // Ensure method registered
            ToolsCall.EnsureMethodsRegisteredStatic();
            var methodNames = ToolsCall.GetRegisteredMethodNames();

            // Group by category method
            var methodsByGroup = new Dictionary<string, List<(string methodName, IToolMethod method, string assemblyName)>>();

            foreach (var methodName in methodNames)
            {
                var method = ToolsCall.GetRegisteredMethod(methodName);
                if (method == null) continue;

                // Get group name
                string groupName = GetMethodGroupName(method);
                // Get assembly name
                string assemblyName = GetAssemblyDisplayName(method.GetType().Assembly);

                if (!methodsByGroup.ContainsKey(groupName))
                {
                    methodsByGroup[groupName] = new List<(string, IToolMethod, string)>();
                }

                methodsByGroup[groupName].Add((methodName, method, assemblyName));
            }

            // Dynamically calculate available height and apply to scroll view
            float availableHeight = CalculateAvailableMethodsHeight();
            methodsScrollPosition = EditorGUILayout.BeginScrollView(methodsScrollPosition,
                GUILayout.Height(availableHeight));

            // Sort and draw by group name
            foreach (var groupKvp in methodsByGroup.OrderBy(kvp => kvp.Key))
            {
                string groupName = groupKvp.Key;
                var methods = groupKvp.Value.OrderBy(m => m.methodName).ToList();

                // Ensure group has an entry in the collapse dictionary
                if (!groupFoldouts.ContainsKey(groupName))
                {
                    groupFoldouts[groupName] = false;
                }

                // Draw group collapse header
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

                // If group expanded，Show methods among them
                if (groupFoldouts[groupName])
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUI.indentLevel++;

                    foreach (var (methodName, method, assemblyName) in methods)
                    {
                        // Ensure the method has an entry in the dictionary
                        if (!methodFoldouts.ContainsKey(methodName))
                        {
                            methodFoldouts[methodName] = false;
                        }

                        // Draw method collapse header
                        EditorGUILayout.BeginVertical("box");

                        // Collapse header bar style
                        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
                        {
                            fontStyle = FontStyle.Bold
                        };

                        // Display collapse header in one row、Question mark and debug buttons
                        EditorGUILayout.BeginHorizontal();

                        // Draw collapse header
                        Rect foldoutRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

                        // Calculate position of button and assembly label
                        float buttonWidth = 20f;
                        float buttonHeight = 18f;
                        float padding = 4f; // Increase spacing
                        float totalButtonsWidth = (buttonWidth + padding) * 2; // Total width of two buttons

                        // Calculate width of assembly label
                        string assemblyLabel = $"({assemblyName})";
                        GUIStyle assemblyLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                        // Ensure label is wide enough，Avoid text truncation
                        float calculatedWidth = assemblyLabelStyle.CalcSize(new GUIContent(assemblyLabel)).x;
                        float assemblyLabelWidth = Mathf.Max(calculatedWidth + padding * 2, 80f); // Min width80px

                        // Calculate positions of areas from right to left
                        float rightEdge = foldoutRect.xMax;

                        // 1. Debug button area（Rightmost）
                        Rect debugButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            foldoutRect.y + (foldoutRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding);

                        // 2. Question mark area
                        Rect helpButtonRect = new Rect(
                            rightEdge - buttonWidth,
                            foldoutRect.y + (foldoutRect.height - buttonHeight) / 2,
                            buttonWidth,
                            buttonHeight
                        );
                        rightEdge -= (buttonWidth + padding * 2); // Add more spacing after button

                        // 3. Assembly label area
                        Rect assemblyLabelRect = new Rect(
                            rightEdge - assemblyLabelWidth,
                            foldoutRect.y,
                            assemblyLabelWidth,
                            foldoutRect.height
                        );
                        rightEdge -= (assemblyLabelWidth + padding * 2); // Add more spacing after label

                        // 4. Collapse header area（Remaining space）
                        Rect actualFoldoutRect = new Rect(
                            foldoutRect.x,
                            foldoutRect.y,
                            rightEdge - foldoutRect.x,
                            foldoutRect.height
                        );

                        // Draw collapse header（Show only method name）
                        methodFoldouts[methodName] = EditorGUI.Foldout(
                            actualFoldoutRect,
                            methodFoldouts[methodName],
                            methodName,
                            true,
                            foldoutStyle);

                        // Draw assembly label
                        Color originalColor = GUI.color;
                        GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f); // Lighter gray

                        // Set label style to right-align
                        GUIStyle rightAlignedLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                        rightAlignedLabelStyle.alignment = TextAnchor.MiddleRight;

                        EditorGUI.LabelField(assemblyLabelRect, assemblyLabel, rightAlignedLabelStyle);
                        GUI.color = originalColor;

                        // Draw question button
                        GUIStyle helpButtonStyle = new GUIStyle(EditorStyles.miniButton);

                        if (GUI.Button(helpButtonRect, "?", helpButtonStyle))
                        {
                            // Handle button click event
                            HandleMethodHelpClick(methodName, method);
                        }

                        // Draw debug button
                        GUIStyle debugButtonStyle = new GUIStyle(EditorStyles.miniButton);
                        Color originalBackgroundColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f); // Light blue background

                        if (GUI.Button(debugButtonRect, "T", debugButtonStyle))
                        {
                            // Handle debug button click event
                            HandleMethodDebugClick(methodName, method);
                        }

                        GUI.backgroundColor = originalBackgroundColor;

                        EditorGUILayout.EndHorizontal();

                        // If expand，Show preview info
                        if (methodFoldouts[methodName])
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                            // === ParameterKeysInfo section ===
                            EditorGUILayout.BeginVertical("box");

                            var keys = method.Keys;
                            if (keys != null && keys.Length > 0)
                            {
                                foreach (var key in keys)
                                {
                                    // Create param row style
                                    EditorGUILayout.BeginHorizontal();
                                    // Parameter name - Mark required parameters in bold，Optional parameters use regular font
                                    GUIStyle keyStyle = EditorStyles.miniBoldLabel;
                                    Color originalKeyColor = GUI.color;

                                    // Mark required parameters in red，Mark optional parameters in gray
                                    GUI.color = key.Optional ? Color.red : Color.green;
                                    // Parameter name
                                    EditorGUILayout.SelectableLabel(key.Key, keyStyle, GUILayout.Width(120), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    GUI.color = originalKeyColor;

                                    // Parameter description
                                    EditorGUILayout.SelectableLabel(key.Desc, keyStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("No parameters", EditorStyles.centeredGreyMiniLabel);
                            }

                            EditorGUILayout.EndVertical();

                            // Add some spacing
                            EditorGUILayout.Space(3);

                            // === State tree structure section ===
                            EditorGUILayout.BeginVertical("box");

                            // Get preview info
                            string preview = method.Preview();

                            // Calculate text lines
                            int lineCount = 1;
                            if (!string.IsNullOrEmpty(preview))
                            {
                                lineCount = preview.Split('\n').Length;
                            }

                            // Show preview info
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
        /// Get method's group name
        /// </summary>
        /// <param name="method">Method instance</param>
        /// <returns>Group name</returns>
        private static string GetMethodGroupName(IToolMethod method)
        {
            // Get via reflectionToolNameAttribute
            var methodType = method.GetType();
            var toolNameAttribute = methodType.GetCustomAttributes(typeof(ToolNameAttribute), false)
                                             .FirstOrDefault() as ToolNameAttribute;

            if (toolNameAttribute != null)
            {
                return toolNameAttribute.GroupName;
            }

            // If noneToolNameAttribute，Return to default group
            return "Not grouped";
        }

        /// <summary>
        /// Get display name for assembly
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <returns>Assembly display name</returns>
        private static string GetAssemblyDisplayName(System.Reflection.Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;

            // Return all names in English
            if (assemblyName.StartsWith("Assembly-CSharp"))
            {
                return "Main Project Assembly";
            }
            else if (assemblyName.StartsWith("UnityMcp"))
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
        /// Handle click event of method help button
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="method">Method instance</param>
        private static void HandleMethodHelpClick(string methodName, IToolMethod method)
        {
            // Get current time
            double currentTime = EditorApplication.timeSinceStartup;

            // Check whether there is a last click time record
            if (methodClickTimes.TryGetValue(methodName, out double lastClickTime))
            {
                // Determine if double-click（Time interval less thandoubleClickTime）
                if (currentTime - lastClickTime < doubleClickTime)
                {
                    // Double-click：Open script file
                    OpenMethodScript(method);
                    // Reset click time，Prevent multiple consecutive clicks from being treated as double-clicks
                    methodClickTimes[methodName] = 0;
                    return;
                }
            }

            // Click：Locate script file
            PingMethodScript(method);
            // Record this click time
            methodClickTimes[methodName] = currentTime;
        }

        /// <summary>
        /// AtProjectLocate the script file of the method in the window
        /// </summary>
        /// <param name="method">Method instance</param>
        private static void PingMethodScript(IToolMethod method)
        {
            // Get method type
            Type methodType = method.GetType();

            // Find script resource
            string scriptName = methodType.Name + ".cs";
            string[] guids = AssetDatabase.FindAssets(methodType.Name + " t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    // AtProjectHighlight resource in window
                    UnityEngine.Object scriptObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (scriptObj != null)
                    {
                        EditorGUIUtility.PingObject(scriptObj);
                        return;
                    }
                }
            }

            // If script not found，Try to look up directly by type name
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

            Debug.LogWarning($"Cannot onProjectFind script in window: {scriptName}");
        }

        /// <summary>
        /// Open the script file of the method
        /// </summary>
        /// <param name="method">Method instance</param>
        private static void OpenMethodScript(IToolMethod method)
        {
            // Get method type
            Type methodType = method.GetType();

            // Find script resource
            string scriptName = methodType.Name + ".cs";
            string[] guids = AssetDatabase.FindAssets(methodType.Name + " t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(scriptName))
                {
                    // Load and open script
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null)
                    {
                        AssetDatabase.OpenAsset(script);
                        return;
                    }
                }
            }

            // If script not found，Try to look up directly by type name
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

            Debug.LogWarning($"Unable to open script: {scriptName}");
        }

        /// <summary>
        /// Handle click event of method debug button
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="method">Method instance</param>
        private static void HandleMethodDebugClick(string methodName, IToolMethod method)
        {
            try
            {
                // Generate example method invocationJSON
                string exampleJson = GenerateMethodExampleJson(methodName, method);

                // OpenMcpDebugWindowAnd prefill sample
                McpDebugWindow.ShowWindowWithContent(exampleJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMcpEditorWindow] Error occurred while generating debug sample: {e}");
                EditorUtility.DisplayDialog("Error", $"Unable to generate debug sample: {e.Message}", "Confirm");
            }
        }

        /// <summary>
        /// Generate example method invocationJSON
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="method">Method instance</param>
        /// <returns>ExampleJSONString</returns>
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
                Debug.LogWarning($"Generate exampleJSONFailure，Use base template: {e.Message}");

                // If generation failed，Return base template
                var basicCall = new
                {
                    func = methodName,
                    args = new { }
                };

                return Json.FromObject(basicCall);
            }
        }

        /// <summary>
        /// Generate example parameters for method
        /// </summary>
        /// <param name="method">Method instance</param>
        /// <returns>Sample parameter object</returns>
        private static object GenerateExampleArgs(IToolMethod method)
        {
            var exampleArgs = new Dictionary<string, object>();
            var keys = method.Keys;

            if (keys != null && keys.Length > 0)
            {
                foreach (var key in keys)
                {
                    // Generate example value by parameter name and description
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
        /// Generate sample value by parameter info
        /// </summary>
        /// <param name="keyName">Parameter name</param>
        /// <param name="description">Parameter description</param>
        /// <param name="isOptional">Optional or not</param>
        /// <returns>Example value</returns>
        private static object GenerateExampleValue(string keyName, string description, bool isOptional)
        {
            // Convert to lowercase for pattern match
            string lowerKey = keyName.ToLower();
            string lowerDesc = description?.ToLower() ?? "";

            // Infer type and example value from parameter name and description
            switch (lowerKey)
            {
                case "action":
                    return "modify"; // Default action

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
                    // Infer from description content
                    if (lowerDesc.Contains("bool") || lowerDesc.Contains("Whether"))
                        return !isOptional; // Required parameter defaulttrue，Optional parameter defaultfalse

                    if (lowerDesc.Contains("array") || lowerDesc.Contains("list") || lowerDesc.Contains("Array"))
                        return new object[] { };

                    if (lowerDesc.Contains("number") || lowerDesc.Contains("int") || lowerDesc.Contains("Number"))
                        return 0;

                    if (lowerDesc.Contains("float") || lowerDesc.Contains("Float"))
                        return 0.0f;

                    // If it is optional and type cannot be inferred，Returnnull（Do not add to parameters）
                    if (isOptional)
                        return null;

                    // Required parameters default return empty string
                    return "";
            }
        }

        /// <summary>
        /// Draw client connection state
        /// </summary>
        private static void DrawClientConnectionStatus()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Client connection state title
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Client connection status", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Show connection count
            int clientCount = McpConnect.ConnectedClientCount;
            Color countColor = clientCount > 0 ? Color.green : Color.gray;
            GUIStyle countStyle = new GUIStyle(EditorStyles.label);
            countStyle.normal.textColor = countColor;
            countStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField($"Connection count: {clientCount}", countStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (clientCount > 0)
            {
                // Detail collapse control
                showClientDetails = EditorGUILayout.Foldout(showClientDetails, "Show details", true);

                if (showClientDetails)
                {
                    EditorGUILayout.Space(5);

                    // Client list scroll view
                    clientsScrollPosition = EditorGUILayout.BeginScrollView(clientsScrollPosition,
                        GUILayout.MinHeight(80), GUILayout.MaxHeight(220));

                    var clients = McpConnect.GetConnectedClients();
                    foreach (var client in clients)
                    {
                        EditorGUILayout.BeginVertical("box");

                        // Client basic information
                        EditorGUILayout.LabelField($"Endpoint: {client.EndPoint}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Connection time: {client.ConnectedAt:HH:mm:ss}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Last activity: {client.LastActivity:HH:mm:ss}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Command count: {client.CommandCount}", EditorStyles.miniLabel);

                        // Calculate connection duration
                        TimeSpan duration = DateTime.Now - client.ConnectedAt;
                        string durationText = duration.TotalMinutes < 1
                            ? $"{duration.Seconds}Seconds"
                            : $"{(int)duration.TotalMinutes}Minute{duration.Seconds}Seconds";
                        EditorGUILayout.LabelField($"Connection duration: {durationText}", EditorStyles.miniLabel);

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No client connection", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
