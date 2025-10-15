using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityMcp;

namespace UnityMcp.Gui
{
    /// <summary>
    /// FigmaSettings provider，Used inUnityOfProjectSettingsDisplay in windowFigmaRelated settings
    /// </summary>
    public class FigmaSettingsProvider
    {
        private static Vector2 scrollPosition;
        private static bool apiSettingsFoldout = true;
        private static bool downloadSettingsFoldout = true;
        private static bool engineEffectsFoldout = true;
        private static bool helpInfoFoldout = false;

        [SettingsProvider]
        public static SettingsProvider CreateFigmaSettingsProvider()
        {
            var provider = new SettingsProvider("Project/MCP/Figma", SettingsScope.Project)
            {
                label = "Figma",
                guiHandler = (searchContext) =>
                {
                    DrawFigmaSettings();
                },
                keywords = new[] { "Figma", "Design", "Token", "Download", "Images", "API", "File" }
            };

            return provider;
        }

        private static void DrawFigmaSettings()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                settings.figmaSettings = new FigmaSettings();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // FigmaIntroduction
            EditorGUILayout.LabelField("Figma Integration configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configuration andFigmaIntegration settings，Includes access token and download options。" +
                "These settings will affect fromFigmaBehavior of obtaining design resources。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // APISetting
            apiSettingsFoldout = EditorGUILayout.Foldout(apiSettingsFoldout, "APISetting", true, EditorStyles.foldoutHeader);

            if (apiSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                string token = settings.figmaSettings.figma_access_token;
                token = EditorGUILayout.PasswordField(
                    "FigmaAccess token",
                    token);
                settings.figmaSettings.figma_access_token = token;
                EditorGUILayout.LabelField("💾", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "Access token is saved in local editor settings，Will not be committed to version control。",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Download settings
            downloadSettingsFoldout = EditorGUILayout.Foldout(downloadSettingsFoldout, "Download settings", true, EditorStyles.foldoutHeader);

            if (downloadSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                settings.figmaSettings.default_download_path = EditorGUILayout.TextField(
                    "Default download path",
                    settings.figmaSettings.default_download_path);

                settings.figmaSettings.figma_assets_path = EditorGUILayout.TextField(
                    "FigmaData asset path",
                    settings.figmaSettings.figma_assets_path);

                settings.figmaSettings.figma_preview_path = EditorGUILayout.TextField(
                    "FigmaPreview image save path",
                    settings.figmaSettings.figma_preview_path);

                settings.figmaSettings.auto_download_images = EditorGUILayout.Toggle(
                    "Automatically download images",
                    settings.figmaSettings.auto_download_images);

                settings.figmaSettings.image_scale = EditorGUILayout.FloatField(
                    "Image scaling factor",
                    settings.figmaSettings.image_scale);

                settings.figmaSettings.preview_max_size = EditorGUILayout.IntSlider(
                    "Maximum size of preview image",
                    settings.figmaSettings.preview_max_size,
                    50, 600);

                settings.figmaSettings.auto_convert_to_sprite = EditorGUILayout.Toggle(
                    "Automatically convert toSprite",
                    settings.figmaSettings.auto_convert_to_sprite);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Engine-supported feature settings
            engineEffectsFoldout = EditorGUILayout.Foldout(engineEffectsFoldout, "Engine-supported features", true, EditorStyles.foldoutHeader);

            if (engineEffectsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "ConfigurationUnityEngine for specific (features)UISupport for effects。Enabling these options can avoid downloading certain things that can be (handled) viaUnityFeatures implemented by native components。",
                    MessageType.Info);

                // InitializationengineSupportEffectIf (it) isnull
                if (settings.figmaSettings.engineSupportEffect == null)
                    settings.figmaSettings.engineSupportEffect = new FigmaSettings.EngineSupportEffect();

                settings.figmaSettings.engineSupportEffect.roundCorner = EditorGUILayout.Toggle(
                    "Rounded corner support (ProceduralUIImage)",
                    settings.figmaSettings.engineSupportEffect.roundCorner);

                settings.figmaSettings.engineSupportEffect.outLineImg = EditorGUILayout.Toggle(
                    "Stroke support (OutlineComponent)",
                    settings.figmaSettings.engineSupportEffect.outLineImg);

                settings.figmaSettings.engineSupportEffect.gradientImg = EditorGUILayout.Toggle(
                    "Gradient support (UI Gradient)",
                    settings.figmaSettings.engineSupportEffect.gradientImg);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Help information
            helpInfoFoldout = EditorGUILayout.Foldout(helpInfoFoldout, "Instructions", true, EditorStyles.foldoutHeader);

            if (helpInfoFoldout)
            {
                EditorGUI.indentLevel++;

                // APISettings description
                EditorGUILayout.LabelField("APISetting", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• Access token：AtFigmaGenerate personal access token in (xxx) forAPIAccess\n" +
                    "• Acquisition method：LoginFigma → Settings → Personal access tokens → Generate new token\n" +
                    "• Security：Access token saved locallyEditorPrefsIn，Will not be committed toGit",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // Download settings description
                EditorGUILayout.LabelField("Download settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• Download path：Local save location for images and resources\n" +
                    "• Data asset path：FigmaSave location for node and simplified data\n" +
                    "• Preview image save path：UsepreviewWhere to save preview images when using the feature\n" +
                    "• Scaling factor：Control the resolution of downloaded images（Recommendation2.0For high definition display）\n" +
                    "• Maximum size of preview image：Control the maximum size of preview images（Pixel）\n" +
                    "• Automatically convert toSprite：Automatically set to after downloading imagesSpriteFormat（Recommended to enable）",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // Engine-supported feature description
                EditorGUILayout.LabelField("Engine-supported features", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• Rounded corner support：After enabling，Round-cornered rectangle will useProceduralUIImageInstead of downloading images\n" +
                    "• Stroke support：After enabling，Stroke effect will useOutlineComponents instead of downloaded images\n" +
                    "• Gradient support：After enabling，Gradient effect will useUI GradientComponents instead of downloaded images\n" +
                    "• Advantage：Reduce resource usage，Improve performance，Support runtime dynamic adjustment",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // Usage process description
                EditorGUILayout.LabelField("Usage process", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "1. ConfigurationFigmaAccess token\n" +
                    "2. Set appropriate download path and scaling factor\n" +
                    "3. Enable engine-supported features based on project requirements\n" +
                    "4. AtMCPUsed in toolfigma_manageDownload design resources\n" +
                    "5. ThroughUIAutomatically created by generation toolUnity UIComponent",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            // Auto save
            if (GUI.changed)
            {
                settings.SaveSettings();
            }
        }
    }
}
