using UnityEngine;
using UnityEditor;

namespace UnityMcp.Gui
{
    /// <summary>
    /// MCPSettings provider，Used inUnityOfProjectSettingsDisplay in windowMCPSetting
    /// </summary>
    public static class McpSettingsProvider
    {
        private static bool isInitialized = false;

        [SettingsProvider]
        public static SettingsProvider CreateMcpSettingsProvider()
        {
            var provider = new SettingsProvider("Project/MCP", SettingsScope.Project)
            {
                label = "MCP",
                guiHandler = (searchContext) =>
                {
                    DrawMcpSettings();
                },
                activateHandler = (searchContext, rootElement) =>
                {
                    // Initialize on activationGUIStatus
                    if (!isInitialized)
                    {
                        McpConnectGUI.Initialize();
                        isInitialized = true;
                    }
                },
                keywords = new[] { "MCP", "Settings", "Configuration", "Debug", "Bridge", "Server" }
            };

            return provider;
        }

        private static void DrawMcpSettings()
        {
            var settings = McpSettings.Instance;

            EditorGUILayout.LabelField("MCP (Model Context Protocol)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "MCPIs a powerfulUnityExtension tool，Provides intelligentUIGenerate、Code management and project optimization features。" +
                "Through andAIDeep integration of models，MCPCan help developers quickly create high-qualityUnityProject。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Draw completeMCPManagementGUI
            McpConnectGUI.DrawGUI();

            // Auto save
            if (GUI.changed)
            {
                settings.SaveSettings();
            }
        }
    }
}
