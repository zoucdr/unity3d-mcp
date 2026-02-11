using System;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// Example tool demonstrating the new localization system
    /// Corresponding method name: example_tool
    /// </summary>
    [ToolName("example_tool", "Development Tools")]  // ToolName must be in English
    public class ExampleTool : StateMethodBase
    {
        // Description must be in English (for AI prompts)
        public override string Description => "Example tool demonstrating localization with L.T() helper";

        /// <summary>
        /// Create the list of parameter keys supported by this method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // All parameter descriptions must be in English
                new MethodStr("action", "Action type", false)
                    .SetEnumValues("show_message", "execute_operation"),
                
                new MethodStr("message", "Message to display")
                    .AddExamples("Hello World", "Test Message"),
                
                new MethodBool("show_dialog", "Whether to show a confirmation dialog")
                    .SetDefault(false)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("show_message", HandleShowMessage)
                    .Leaf("execute_operation", HandleExecuteOperation)
                .Build();
        }

        /// <summary>
        /// Show a localized message
        /// </summary>
        private object HandleShowMessage(JsonClass args)
        {
            string message = args["message"]?.Value ?? "Hello";
            bool showDialog = args["show_dialog"]?.BoolValue ?? false;

            if (showDialog)
            {
                // Use L.T() for UI text that users can see
                bool confirmed = EditorUtility.DisplayDialog(
                    L.T("Message", "消息"),  // Title
                    message,
                    L.T("OK", "确定"),       // OK button
                    L.T("Cancel", "取消")    // Cancel button
                );

                if (confirmed)
                {
                    // Use L.IsChinese() for dynamic messages
                    string result = L.IsChinese() 
                        ? $"用户确认了消息: {message}" 
                        : $"User confirmed message: {message}";
                    
                    return Response.Success(result);
                }
                else
                {
                    return Response.Fail(L.T("User cancelled", "用户取消了操作"));
                }
            }
            else
            {
                // Debug logs can stay in English if they're for developers
                Debug.Log("Showing message: " + message);
                
                // But user-facing logs should use L.T()
                Debug.Log(L.T("Message displayed", "消息已显示"));
                
                return Response.Success(L.T("Message shown successfully", "消息显示成功"));
            }
        }

        /// <summary>
        /// Execute an operation with progress feedback
        /// </summary>
        private object HandleExecuteOperation(JsonClass args)
        {
            try
            {
                // Use L.T() for progress dialog text
                EditorUtility.DisplayProgressBar(
                    L.T("Processing", "处理中"),
                    L.T("Please wait...", "请稍候..."),
                    0.5f
                );

                // Simulate some work
                System.Threading.Thread.Sleep(1000);

                EditorUtility.ClearProgressBar();

                // Return localized success message
                string successMsg = L.IsChinese() 
                    ? "操作成功完成！" 
                    : "Operation completed successfully!";

                return Response.Success(successMsg);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();

                // Error messages can use L.T() too
                string errorMsg = L.IsChinese() 
                    ? $"操作失败: {e.Message}" 
                    : $"Operation failed: {e.Message}";

                return Response.Fail(errorMsg);
            }
        }
    }

    /// <summary>
    /// Example EditorWindow demonstrating localization in UI
    /// </summary>
    public class ExampleLocalizationWindow : EditorWindow
    {
        [MenuItem("Window/MCP/Example Localization")]
        public static void ShowWindow()
        {
            // Window title uses L.T()
            ExampleLocalizationWindow window = GetWindow<ExampleLocalizationWindow>(
                L.T("Localization Example", "多语言示例")
            );
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

        private string inputText = "";

        private void OnGUI()
        {
            // All UI labels use L.T()
            GUILayout.Label(L.T("Example Window", "示例窗口"), EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            GUILayout.Label(L.T("Current Language:", "当前语言:"));
            
            string currentLang = McpLocalSettings.Instance.CurrentLanguage;
            if (string.IsNullOrEmpty(currentLang))
            {
                GUILayout.Label(L.T("System Default", "系统默认"));
            }
            else
            {
                GUILayout.Label(currentLang);
            }
            
            EditorGUILayout.Space();
            
            // Input field label
            inputText = EditorGUILayout.TextField(
                L.T("Input:", "输入:"), 
                inputText
            );
            
            EditorGUILayout.Space();
            
            // Buttons
            if (GUILayout.Button(L.T("Switch to English", "切换到英文")))
            {
                McpLocalSettings.Instance.CurrentLanguage = "English";
                Repaint();
            }
            
            if (GUILayout.Button(L.T("Switch to Chinese", "切换到中文")))
            {
                McpLocalSettings.Instance.CurrentLanguage = "Chinese";
                Repaint();
            }
            
            if (GUILayout.Button(L.T("Reset to System", "重置为系统语言")))
            {
                McpLocalSettings.Instance.CurrentLanguage = "";
                Repaint();
            }
            
            EditorGUILayout.Space();
            
            // Show current language status
            GUILayout.Label(
                L.IsChinese() 
                    ? "当前使用中文界面" 
                    : "Currently using English interface",
                EditorStyles.helpBox
            );
        }
    }
}
