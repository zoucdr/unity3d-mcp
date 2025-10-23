using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Mcp;

namespace Unity.Mcp.Gui
{
    /// <summary>
    /// Figma设置提供器，用于在Unity的ProjectSettings窗口中显示Figma相关设置
    /// </summary>
    public class FigmaSettingsProvider
    {
        private static Vector2 scrollPosition;
        private static bool apiSettingsFoldout = true;
        private static bool downloadSettingsFoldout = true;
        private static bool aiPromptFoldout = true;
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

            // Figma简介
            EditorGUILayout.LabelField("Figma 集成配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "配置与Figma的集成设置，包括访问令牌和下载选项。" +
                "这些设置将影响从Figma获取设计资源的行为。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // API设置
            apiSettingsFoldout = EditorGUILayout.Foldout(apiSettingsFoldout, "API设置", true, EditorStyles.foldoutHeader);

            if (apiSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                string token = settings.figmaSettings.figma_access_token;
                token = EditorGUILayout.PasswordField(
                    "Figma访问令牌",
                    token);
                settings.figmaSettings.figma_access_token = token;
                EditorGUILayout.LabelField("💾", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "访问令牌保存在本地编辑器设置中，不会被提交到版本控制。",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // 下载设置
            downloadSettingsFoldout = EditorGUILayout.Foldout(downloadSettingsFoldout, "下载设置", true, EditorStyles.foldoutHeader);

            if (downloadSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                settings.figmaSettings.default_download_path = EditorGUILayout.TextField(
                    "默认下载路径",
                    settings.figmaSettings.default_download_path);

                settings.figmaSettings.figma_assets_path = EditorGUILayout.TextField(
                    "Figma数据资产路径",
                    settings.figmaSettings.figma_assets_path);

                settings.figmaSettings.figma_preview_path = EditorGUILayout.TextField(
                    "Figma预览图保存路径",
                    settings.figmaSettings.figma_preview_path);

                settings.figmaSettings.auto_download_images = EditorGUILayout.Toggle(
                    "自动下载图片",
                    settings.figmaSettings.auto_download_images);

                settings.figmaSettings.image_scale = EditorGUILayout.FloatField(
                    "图片缩放倍数",
                    settings.figmaSettings.image_scale);

                settings.figmaSettings.preview_max_size = EditorGUILayout.IntSlider(
                    "预览图最大尺寸",
                    settings.figmaSettings.preview_max_size,
                    50, 600);

                settings.figmaSettings.auto_convert_to_sprite = EditorGUILayout.Toggle(
                    "自动转换为Sprite",
                    settings.figmaSettings.auto_convert_to_sprite);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // AI转换提示词
            aiPromptFoldout = EditorGUILayout.Foldout(aiPromptFoldout, "AI转换提示词", true, EditorStyles.foldoutHeader);

            if (aiPromptFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "配置AI进行Figma到Unity转换时的提示词，用于指导坐标转换、布局设置等。",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // 添加UI类型选择
                EditorGUILayout.LabelField("UI框架类型:", EditorStyles.boldLabel);

                // 使用EnumPopup绘制UI类型选择器
                settings.figmaSettings.selectedUIType = (UIType)EditorGUILayout.EnumPopup(
                    "选择UI框架",
                    settings.figmaSettings.selectedUIType);

                EditorGUILayout.Space(5);

                // 显示多行文本编辑器
                EditorGUILayout.LabelField(string.Format("提示词内容 ({0}):", settings.figmaSettings.selectedUIType.ToString()), EditorStyles.boldLabel);

                // 创建一个滚动视图来显示多行文本
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    richText = false
                };

                // 根据选择的UI类型显示对应的提示词
                string currentPrompt = settings.figmaSettings.GetPromptForUIType(settings.figmaSettings.selectedUIType, false);
                string newPrompt = EditorGUILayout.TextArea(
                    currentPrompt,
                    textAreaStyle,
                    GUILayout.MinHeight(300),
                    GUILayout.MaxHeight(600));

                // 如果提示词被修改，更新对应UI类型的提示词
                if (newPrompt != currentPrompt)
                {
                    settings.figmaSettings.SetPromptForUIType(settings.figmaSettings.selectedUIType, newPrompt);
                }

                EditorGUILayout.Space(5);

                // 重置按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(string.Format("重置{0}提示词为默认值", settings.figmaSettings.selectedUIType.ToString()), GUILayout.Width(200)))
                {
                    if (EditorUtility.DisplayDialog("确认重置",
                        string.Format("确定要将{0}的AI提示词重置为默认值吗？\n当前的自定义内容将丢失。", settings.figmaSettings.selectedUIType.ToString()),
                        "确定", "取消"))
                    {
                        // 重置当前选择的UI类型的提示词为默认值
                        settings.figmaSettings.SetPromptForUIType(settings.figmaSettings.selectedUIType, settings.figmaSettings.GetDefaultPrompt());
                        GUI.changed = true;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // 引擎支持效果设置
            engineEffectsFoldout = EditorGUILayout.Foldout(engineEffectsFoldout, "引擎支持效果", true, EditorStyles.foldoutHeader);

            if (engineEffectsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox(
                    "配置Unity引擎对特定UI效果的支持。启用这些选项可以避免下载某些可以通过Unity原生组件实现的效果。",
                    MessageType.Info);

                // 初始化engineSupportEffect如果为null
                if (settings.figmaSettings.engineSupportEffect == null)
                    settings.figmaSettings.engineSupportEffect = new FigmaSettings.EngineSupportEffect();

                // 圆角支持
                EditorGUILayout.BeginHorizontal();
                settings.figmaSettings.engineSupportEffect.roundCorner = EditorGUILayout.Toggle(
                    "圆角支持 (ProceduralUIImage)",
                    settings.figmaSettings.engineSupportEffect.roundCorner,
                    GUILayout.Width(200));

                if (settings.figmaSettings.engineSupportEffect.roundCorner)
                {
                    settings.figmaSettings.engineSupportEffect.roundCornerPrompt = EditorGUILayout.TextField(
                        settings.figmaSettings.engineSupportEffect.roundCornerPrompt);
                }
                EditorGUILayout.EndHorizontal();

                // 描边支持
                EditorGUILayout.BeginHorizontal();
                settings.figmaSettings.engineSupportEffect.outLineImg = EditorGUILayout.Toggle(
                    "描边支持 (Outline组件)",
                    settings.figmaSettings.engineSupportEffect.outLineImg,
                    GUILayout.Width(200));

                if (settings.figmaSettings.engineSupportEffect.outLineImg)
                {
                    settings.figmaSettings.engineSupportEffect.outLinePrompt = EditorGUILayout.TextField(
                        settings.figmaSettings.engineSupportEffect.outLinePrompt);
                }
                EditorGUILayout.EndHorizontal();

                // 渐变支持
                EditorGUILayout.BeginHorizontal();
                settings.figmaSettings.engineSupportEffect.gradientImg = EditorGUILayout.Toggle(
                    "渐变支持 (UI Gradient)",
                    settings.figmaSettings.engineSupportEffect.gradientImg,
                    GUILayout.Width(200));

                if (settings.figmaSettings.engineSupportEffect.gradientImg)
                {
                    settings.figmaSettings.engineSupportEffect.gradientPrompt = EditorGUILayout.TextField(
                        settings.figmaSettings.engineSupportEffect.gradientPrompt);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // 帮助信息
            helpInfoFoldout = EditorGUILayout.Foldout(helpInfoFoldout, "使用说明", true, EditorStyles.foldoutHeader);

            if (helpInfoFoldout)
            {
                EditorGUI.indentLevel++;

                // API设置说明
                EditorGUILayout.LabelField("API设置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• 访问令牌：在Figma中生成个人访问令牌用于API访问\n" +
                    "• 获取方式：登录Figma → Settings → Personal access tokens → Generate new token\n" +
                    "• 安全性：访问令牌保存在本地EditorPrefs中，不会被提交到Git",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // 下载设置说明
                EditorGUILayout.LabelField("下载设置", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• 下载路径：图片和资源的本地保存位置\n" +
                    "• 数据资产路径：Figma节点数据和简化数据的保存位置\n" +
                    "• 预览图保存路径：使用preview功能时保存预览图的位置\n" +
                    "• 缩放倍数：控制下载图片的分辨率（建议2.0用于高清显示）\n" +
                    "• 预览图最大尺寸：控制预览图的最大尺寸（像素）\n" +
                    "• 自动转换为Sprite：下载图片后自动设置为Sprite格式（推荐开启）",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // AI转换提示词说明
                EditorGUILayout.LabelField("AI转换提示词", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• 用途：指导AI进行Figma到Unity的精确转换\n" +
                    "• 内容：包含坐标转换公式、布局规则、转换要求等\n" +
                    "• 自定义：可根据项目需求修改提示词内容\n" +
                    "• 重置：点击'重置为默认值'按钮恢复默认提示词\n" +
                    "• 建议：首次使用建议保持默认值，根据实际转换效果进行调整",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // 引擎支持效果说明
                EditorGUILayout.LabelField("引擎支持效果", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "• 圆角支持：启用后，圆角矩形将使用ProceduralUIImage而非下载图片\n" +
                    "• 描边支持：启用后，描边效果将使用Outline组件而非下载图片\n" +
                    "• 渐变支持：启用后，渐变效果将使用UI Gradient组件而非下载图片\n" +
                    "• 优势：减少资源占用，提高性能，支持运行时动态调整",
                    MessageType.Info);

                EditorGUILayout.Space(5);

                // 使用流程说明
                EditorGUILayout.LabelField("使用流程", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "1. 配置Figma访问令牌\n" +
                    "2. 设置合适的下载路径和缩放倍数\n" +
                    "3. 根据项目需求配置AI转换提示词（可选）\n" +
                    "4. 根据项目需求启用引擎支持效果\n" +
                    "5. 在MCP工具中使用figma_manage下载设计资源\n" +
                    "6. 使用AI和提示词进行精确的UI布局转换\n" +
                    "7. 通过UI生成工具自动创建Unity UI组件",
                    MessageType.Info);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            // 自动保存
            if (GUI.changed)
            {
                settings.SaveSettings();
            }
        }
    }
}
