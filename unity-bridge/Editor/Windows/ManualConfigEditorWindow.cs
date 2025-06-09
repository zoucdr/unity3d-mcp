using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Models;

namespace UnityMcpBridge.Editor.Windows
{
    // Editor window to display manual configuration instructions
    public class ManualConfigEditorWindow : EditorWindow
    {
        private string configPath;
        private string configJson;
        private Vector2 scrollPos;
        private bool pathCopied = false;
        private bool jsonCopied = false;
        private float copyFeedbackTimer = 0;
        private McpClient mcpClient;

        public static void ShowWindow(string configPath, string configJson, McpClient mcpClient)
        {
            var window = GetWindow<ManualConfigEditorWindow>("Manual Configuration");
            window.configPath = configPath;
            window.configJson = configJson;
            window.mcpClient = mcpClient;
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header with improved styling
            EditorGUILayout.Space(10);
            Rect titleRect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.DrawRect(
                new Rect(titleRect.x, titleRect.y, titleRect.width, titleRect.height),
                new Color(0.2f, 0.2f, 0.2f, 0.1f)
            );
            GUI.Label(
                new Rect(titleRect.x + 10, titleRect.y + 6, titleRect.width - 20, titleRect.height),
                mcpClient.name + " Manual Configuration",
                EditorStyles.boldLabel
            );
            EditorGUILayout.Space(10);

            // Instructions with improved styling
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect headerRect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height),
                new Color(0.1f, 0.1f, 0.1f, 0.2f)
            );
            GUI.Label(
                new Rect(
                    headerRect.x + 8,
                    headerRect.y + 4,
                    headerRect.width - 16,
                    headerRect.height
                ),
                "The automatic configuration failed. Please follow these steps:",
                EditorStyles.boldLabel
            );
            EditorGUILayout.Space(10);

            GUIStyle instructionStyle = new(EditorStyles.wordWrappedLabel)
            {
                margin = new RectOffset(10, 10, 5, 5),
            };

            EditorGUILayout.LabelField(
                "1. Open " + mcpClient.name + " config file by either:",
                instructionStyle
            );
            if (mcpClient.mcpType == McpTypes.ClaudeDesktop)
            {
                EditorGUILayout.LabelField(
                    "    a) Going to Settings > Developer > Edit Config",
                    instructionStyle
                );
            }
            else if (mcpClient.mcpType == McpTypes.Cursor)
            {
                EditorGUILayout.LabelField(
                    "    a) Going to File > Preferences > Cursor Settings > MCP > Add new global MCP server",
                    instructionStyle
                );
            }
            EditorGUILayout.LabelField("    OR", instructionStyle);
            EditorGUILayout.LabelField(
                "    b) Opening the configuration file at:",
                instructionStyle
            );

            // Path section with improved styling
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string displayPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                displayPath = mcpClient.windowsConfigPath;
            }
            else if (
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            )
            {
                displayPath = mcpClient.linuxConfigPath;
            }
            else
            {
                displayPath = configPath;
            }

            // Prevent text overflow by allowing the text field to wrap
            GUIStyle pathStyle = new(EditorStyles.textField) { wordWrap = true };

            EditorGUILayout.TextField(
                displayPath,
                pathStyle,
                GUILayout.Height(EditorGUIUtility.singleLineHeight)
            );

            // Copy button with improved styling
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIStyle copyButtonStyle = new(GUI.skin.button)
            {
                padding = new RectOffset(15, 15, 5, 5),
                margin = new RectOffset(10, 10, 5, 5),
            };

            if (
                GUILayout.Button(
                    "Copy Path",
                    copyButtonStyle,
                    GUILayout.Height(25),
                    GUILayout.Width(100)
                )
            )
            {
                EditorGUIUtility.systemCopyBuffer = displayPath;
                pathCopied = true;
                copyFeedbackTimer = 2f;
            }

            if (
                GUILayout.Button(
                    "Open File",
                    copyButtonStyle,
                    GUILayout.Height(25),
                    GUILayout.Width(100)
                )
            )
            {
                // Open the file using the system's default application
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = displayPath,
                        UseShellExecute = true,
                    }
                );
            }

            if (pathCopied)
            {
                GUIStyle feedbackStyle = new(EditorStyles.label);
                feedbackStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("Copied!", feedbackStyle, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "2. Paste the following JSON configuration:",
                instructionStyle
            );

            // JSON section with improved styling
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Improved text area for JSON with syntax highlighting colors
            GUIStyle jsonStyle = new(EditorStyles.textArea)
            {
                font = EditorStyles.boldFont,
                wordWrap = true,
            };
            jsonStyle.normal.textColor = new Color(0.3f, 0.6f, 0.9f); // Syntax highlighting blue

            // Draw the JSON in a text area with a taller height for better readability
            EditorGUILayout.TextArea(configJson, jsonStyle, GUILayout.Height(200));

            // Copy JSON button with improved styling
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (
                GUILayout.Button(
                    "Copy JSON",
                    copyButtonStyle,
                    GUILayout.Height(25),
                    GUILayout.Width(100)
                )
            )
            {
                EditorGUIUtility.systemCopyBuffer = configJson;
                jsonCopied = true;
                copyFeedbackTimer = 2f;
            }

            if (jsonCopied)
            {
                GUIStyle feedbackStyle = new(EditorStyles.label);
                feedbackStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("Copied!", feedbackStyle, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(
                "3. Save the file and restart " + mcpClient.name,
                instructionStyle
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Close button at the bottom
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(30), GUILayout.Width(100)))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            // Handle the feedback message timer
            if (copyFeedbackTimer > 0)
            {
                copyFeedbackTimer -= Time.deltaTime;
                if (copyFeedbackTimer <= 0)
                {
                    pathCopied = false;
                    jsonCopied = false;
                    Repaint();
                }
            }
        }
    }
}
