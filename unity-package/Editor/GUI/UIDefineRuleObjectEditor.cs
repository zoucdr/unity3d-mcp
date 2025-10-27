using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Mcp.Models;
using Unity.Mcp.Tools;
using System.Collections.Generic;
namespace Unity.Mcp.Gui
{
    /// <summary>
    /// UIDefineRuleObject的自定义Inspector，使用ReorderableList绘制node_names和node_sprites
    /// </summary>
    [CustomEditor(typeof(UIDefineRuleObject))]
    public class UIDefineRuleObjectEditor : UnityEditor.Editor
    {
        private ReorderableList nodeNamesList;
        private ReorderableList nodeSpritesList;
        private ReorderableList modifyRecordsList;

        private SerializedProperty linkUrlProp;
        private SerializedProperty pictureUrlProp;
        private SerializedProperty imageScaleProp;
        private SerializedProperty useExistsComponentsProp;
        private SerializedProperty descriptionsProp;
        private SerializedProperty modifyRecordsProp;
        private SerializedProperty nodeNamesProp;
        private SerializedProperty nodeSpritesProp;

        // 折叠状态
        private bool nodeNamesFoldout = true;
        private bool nodeSpritesFoldout = true;
        private bool modifyRecordsFoldout = true;

        //Unity 准备打开某个资源（双击 / 右键 Open）时调用
        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);

            if (obj is UIDefineRuleObject ruleObject)
            {
                // 通过默认脚本编辑器打开 asset 对应文件
                // 检查如果是文本文件，调用默认文本编辑器
                var assetPath = AssetDatabase.GetAssetPath(ruleObject);
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(assetPath, 1);
                string markdownText = UIDefineRuleObjectEditor.GetUIRuleText(ruleObject);
                GUIUtility.systemCopyBuffer = markdownText;
                return true; // 返回 true 表示“阻止 Unity 默认打开逻辑”
            }
            // 返回 false 让 Unity 继续正常处理（打开代码文件或其他）
            return false;
        }

        void OnEnable()
        {
            // 获取序列化属性
            linkUrlProp = serializedObject.FindProperty("link_url");
            pictureUrlProp = serializedObject.FindProperty("img_save_to");
            imageScaleProp = serializedObject.FindProperty("image_scale");
            useExistsComponentsProp = serializedObject.FindProperty("use_exists_components");
            descriptionsProp = serializedObject.FindProperty("descriptions");
            modifyRecordsProp = serializedObject.FindProperty("modify_records");
            nodeNamesProp = serializedObject.FindProperty("node_names");
            nodeSpritesProp = serializedObject.FindProperty("node_sprites");

            // 创建 Node Names 的 ReorderableList
            nodeNamesList = new ReorderableList(serializedObject, nodeNamesProp, true, false, true, true);
            nodeNamesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = nodeNamesList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                var idProp = element.FindPropertyRelative("id");
                var nameProp = element.FindPropertyRelative("name");
                var originNameProp = element.FindPropertyRelative("originName");

                var labelWidth = 45f;
                var spacing = 3f;

                var totalLabelWidth = labelWidth * 3;
                var remainingWidth = rect.width - totalLabelWidth - spacing * 4;
                var idWidth = remainingWidth * 0.35f;
                var nameWidth = remainingWidth * 0.35f;
                var originNameWidth = remainingWidth * 0.3f;

                var currentX = rect.x;

                // Node ID
                var idLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var idRect = new Rect(currentX, rect.y, idWidth, rect.height);
                currentX += idWidth + spacing;

                // Name
                var nameLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var nameRect = new Rect(currentX, rect.y, nameWidth, rect.height);
                currentX += nameWidth + spacing;

                // Origin Name
                var originNameLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var originNameRect = new Rect(currentX, rect.y, originNameWidth, rect.height);

                EditorGUI.LabelField(idLabelRect, "ID:");
                EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

                EditorGUI.LabelField(nameLabelRect, "Name:");
                EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);

                EditorGUI.LabelField(originNameLabelRect, "Origin:");
                EditorGUI.PropertyField(originNameRect, originNameProp, GUIContent.none);
            };

            // 创建 Node Sprites 的 ReorderableList
            nodeSpritesList = new ReorderableList(serializedObject, nodeSpritesProp, true, false, true, true);
            nodeSpritesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = nodeSpritesList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                var idProp = element.FindPropertyRelative("id");
                var fileNameProp = element.FindPropertyRelative("fileName");
                var spriteProp = element.FindPropertyRelative("sprite");

                var labelWidth = 30f;
                var spacing = 3f;

                var totalLabelWidth = labelWidth * 3;
                var remainingWidth = rect.width - totalLabelWidth - spacing * 4;
                var fieldWidth = remainingWidth / 3f; // 每个字段占1/3宽度
                var idWidth = fieldWidth;
                var fileNameWidth = fieldWidth;
                var spriteFieldWidth = fieldWidth;

                var currentX = rect.x;

                // Node ID
                var idLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var idRect = new Rect(currentX, rect.y, idWidth, rect.height);
                currentX += idWidth + spacing;

                // File Name
                var fileNameLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var fileNameRect = new Rect(currentX, rect.y, fileNameWidth, rect.height);
                currentX += fileNameWidth + spacing;

                // Sprite
                var spriteLabelRect = new Rect(currentX, rect.y, labelWidth, rect.height);
                currentX += labelWidth;
                var spriteRect = new Rect(currentX, rect.y, spriteFieldWidth, rect.height);

                EditorGUI.LabelField(idLabelRect, "ID:");
                EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

                EditorGUI.LabelField(fileNameLabelRect, "File:");
                EditorGUI.PropertyField(fileNameRect, fileNameProp, GUIContent.none);

                EditorGUI.LabelField(spriteLabelRect, "Sprite:");
                EditorGUI.PropertyField(spriteRect, spriteProp, GUIContent.none);
            };

            // 创建 Modify Records 的 ReorderableList
            modifyRecordsList = new ReorderableList(serializedObject, modifyRecordsProp, true, false, true, true);
            modifyRecordsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = modifyRecordsList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 操作按钮
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Copy UI Rule to Clipboard", GUILayout.Height(35)))
            {
                CopyUIRuleToClipboard();
            }
            EditorGUILayout.EndHorizontal();

            // 绘制基本属性
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(linkUrlProp, new GUIContent("Link URL"));
            EditorGUILayout.PropertyField(pictureUrlProp, new GUIContent("Img SaveTo"));
            EditorGUILayout.PropertyField(imageScaleProp, new GUIContent("Image Scale"));
            EditorGUILayout.PropertyField(useExistsComponentsProp, new GUIContent("使用已存在组件", "启用后，将尝试查找并使用场景中已存在的同名组件，而不是创建新组件"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(descriptionsProp, new GUIContent("Descriptions"));

            // 绘制 Node Names 列表
            EditorGUILayout.Space();

            // 自定义折叠标题，包含Clear按钮
            var rect = EditorGUILayout.GetControlRect();
            var foldoutRect = new Rect(rect.x, rect.y, rect.width - 70, rect.height);
            var clearButtonRect = new Rect(rect.x + rect.width - 65, rect.y, 60, rect.height);

            nodeNamesFoldout = EditorGUI.Foldout(foldoutRect, nodeNamesFoldout, $"Node Names Mapping ({nodeNamesProp.arraySize})", true, EditorStyles.foldoutHeader);
            if (GUI.Button(clearButtonRect, "Clear"))
            {
                ClearNodeNames();
            }

            if (nodeNamesFoldout)
            {
                EditorGUI.indentLevel++;
                nodeNamesList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            // 绘制 Node Sprites 列表
            EditorGUILayout.Space();

            // 自定义折叠标题，包含Load All和Clear按钮
            var spritesRect = EditorGUILayout.GetControlRect();
            var spritesFoldoutRect = new Rect(spritesRect.x, spritesRect.y, spritesRect.width - 190, spritesRect.height);
            var loadAllButtonRect = new Rect(spritesRect.x + spritesRect.width - 185, spritesRect.y, 120, spritesRect.height);
            var clearSpritesButtonRect = new Rect(spritesRect.x + spritesRect.width - 65, spritesRect.y, 60, spritesRect.height);

            nodeSpritesFoldout = EditorGUI.Foldout(spritesFoldoutRect, nodeSpritesFoldout, $"Node Sprites Mapping ({nodeSpritesProp.arraySize})", true, EditorStyles.foldoutHeader);
            if (GUI.Button(loadAllButtonRect, "Load All Sprites"))
            {
                LoadAllSprites();
            }
            if (GUI.Button(clearSpritesButtonRect, "Clear"))
            {
                ClearNodeSprites();
            }

            if (nodeSpritesFoldout)
            {
                EditorGUI.indentLevel++;
                nodeSpritesList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            // 绘制 Modify Records 列表
            EditorGUILayout.Space();

            // 自定义折叠标题，包含Clear按钮
            var recordsRect = EditorGUILayout.GetControlRect();
            var recordsFoldoutRect = new Rect(recordsRect.x, recordsRect.y, recordsRect.width - 70, recordsRect.height);
            var clearRecordsButtonRect = new Rect(recordsRect.x + recordsRect.width - 65, recordsRect.y, 60, recordsRect.height);

            modifyRecordsFoldout = EditorGUI.Foldout(recordsFoldoutRect, modifyRecordsFoldout, $"Modification Records ({modifyRecordsProp.arraySize})", true, EditorStyles.foldoutHeader);
            if (GUI.Button(clearRecordsButtonRect, "Clear"))
            {
                ClearModifyRecords();
            }

            if (modifyRecordsFoldout)
            {
                EditorGUI.indentLevel++;
                modifyRecordsList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }


        /// <summary>
        /// 批量载入所有Sprites
        /// </summary>
        private void LoadAllSprites()
        {
            var targetObject = target as UIDefineRuleObject;
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot find UIDefineRuleObject.", "OK");
                return;
            }

            string imgSaveTo = targetObject.img_save_to;
            if (string.IsNullOrEmpty(imgSaveTo))
            {
                EditorUtility.DisplayDialog("Error", "img_save_to path is not set in the rule object.", "OK");
                return;
            }

            int loadedCount = 0;
            int totalCount = nodeSpritesProp.arraySize;

            EditorUtility.DisplayProgressBar("Loading Sprites", "Loading sprites...", 0f);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var element = nodeSpritesProp.GetArrayElementAtIndex(i);
                    var fileNameProp = element.FindPropertyRelative("fileName");
                    var spriteProp = element.FindPropertyRelative("sprite");

                    if (string.IsNullOrEmpty(fileNameProp.stringValue))
                        continue;

                    // 构建完整的文件路径
                    string fullPath = System.IO.Path.Combine(imgSaveTo, fileNameProp.stringValue);

                    // 尝试加载Sprite
                    Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
                    if (loadedSprite != null)
                    {
                        spriteProp.objectReferenceValue = loadedSprite;
                        loadedCount++;
                    }
                    else
                    {
                        // 如果直接加载失败，尝试查找文件
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(fileNameProp.stringValue);
                        string[] foundAssets = AssetDatabase.FindAssets(fileName + " t:Sprite");

                        if (foundAssets.Length > 0)
                        {
                            // 优先选择在指定路径下的文件
                            bool found = false;
                            foreach (string guid in foundAssets)
                            {
                                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                                if (assetPath.StartsWith(imgSaveTo))
                                {
                                    Sprite foundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                                    if (foundSprite != null)
                                    {
                                        spriteProp.objectReferenceValue = foundSprite;
                                        loadedCount++;
                                        found = true;
                                        break;
                                    }
                                }
                            }

                            // 如果在指定路径下没找到，使用第一个找到的
                            if (!found)
                            {
                                string firstAssetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                                Sprite firstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(firstAssetPath);
                                if (firstSprite != null)
                                {
                                    spriteProp.objectReferenceValue = firstSprite;
                                    loadedCount++;
                                }
                            }
                        }
                    }

                    EditorUtility.DisplayProgressBar("Loading Sprites", $"Loading sprites... ({i + 1}/{totalCount})", (float)(i + 1) / totalCount);
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Load Complete", $"Successfully loaded {loadedCount} out of {totalCount} sprites.", "OK");
                Debug.Log($"[UIDefineRuleObjectEditor] Batch loaded {loadedCount} out of {totalCount} sprites from: {imgSaveTo}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 清空Node Names列表
        /// </summary>
        private void ClearNodeNames()
        {
            if (EditorUtility.DisplayDialog("Clear Node Names",
                "Are you sure you want to clear all node names? This action cannot be undone.",
                "Clear", "Cancel"))
            {
                nodeNamesProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[UIDefineRuleObjectEditor] Node names cleared.");
            }
        }

        /// <summary>
        /// 清空Node Sprites列表
        /// </summary>
        private void ClearNodeSprites()
        {
            if (EditorUtility.DisplayDialog("Clear Node Sprites",
                "Are you sure you want to clear all node sprites? This action cannot be undone.",
                "Clear", "Cancel"))
            {
                nodeSpritesProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[UIDefineRuleObjectEditor] Node sprites cleared.");
            }
        }

        /// <summary>
        /// 清空Modify Records列表
        /// </summary>
        private void ClearModifyRecords()
        {
            if (EditorUtility.DisplayDialog("Clear Modify Records",
                "Are you sure you want to clear all modification records? This action cannot be undone.",
                "Clear", "Cancel"))
            {
                modifyRecordsProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[UIDefineRuleObjectEditor] Modification records cleared.");
            }
        }

        /// <summary>
        /// 获取UI规则（公共方法）
        /// </summary>
        public static string GetUIRuleText(UIDefineRuleObject ruleObject)
        {
            Debug.Log($"[UIDefineRuleObjectEditor] Getting UI rule for '{ruleObject.name}'...");

            if (ruleObject == null)
            {
                Debug.LogError("[UIDefineRuleObjectEditor] UIDefineRuleObject is null");
                return null;
            }

            var markdown = new System.Text.StringBuilder();

            // 获取全局UI设置
            var mcpSettings = McpSettings.Instance;
            var uiSettings = mcpSettings?.uiSettings;

            // 标题
            markdown.AppendLine($"# Unity UI规则信息 - {ruleObject.name}");
            markdown.AppendLine();
            markdown.AppendLine("以下是Unity项目中的UI制作规则和配置信息，请基于这些信息基于mcp实现UI界面开发。");
            markdown.AppendLine();

            // 基本信息 - 只显示有值的字段
            bool hasBasicInfo = false;
            var basicInfo = new System.Text.StringBuilder();

            basicInfo.AppendLine("## 基本信息");
            basicInfo.AppendLine();

            // UI名称总是显示
            basicInfo.AppendLine($"- **UI名称**: `{ruleObject.name}`");
            hasBasicInfo = true;

            // 只有非空字段才显示
            if (!string.IsNullOrEmpty(ruleObject.link_url))
            {
                basicInfo.AppendLine($"- **Figma链接**: {ruleObject.link_url}");
                hasBasicInfo = true;
            }

            if (!string.IsNullOrEmpty(ruleObject.optimize_rule_path))
            {
                basicInfo.AppendLine($"- **优化规则路径**: {ruleObject.optimize_rule_path}");
                hasBasicInfo = true;
            }

            if (!string.IsNullOrEmpty(ruleObject.img_save_to))
            {
                basicInfo.AppendLine($"- **图片保存路径**: `{ruleObject.img_save_to}`");
                hasBasicInfo = true;
            }

            // 只有非默认值才显示
            if (ruleObject.image_scale != 3)
            {
                basicInfo.AppendLine($"- **图片缩放比例**: {ruleObject.image_scale}x");
                hasBasicInfo = true;
            }

            // 显示使用已存在组件的设置
            basicInfo.AppendLine($"- **使用已存在组件**: {(ruleObject.use_exists_components ? "是" : "否")}");
            hasBasicInfo = true;

            if (hasBasicInfo)
            {
                basicInfo.AppendLine();
                markdown.Append(basicInfo.ToString());
            }

            // 描述信息 - 只在有内容时显示
            if (!string.IsNullOrEmpty(ruleObject.descriptions))
            {
                markdown.AppendLine("## 描述信息");
                markdown.AppendLine();
                markdown.AppendLine(ruleObject.descriptions);
                markdown.AppendLine();
            }

            // UI构建环境说明 - 只在有数据时显示
            if (uiSettings != null && uiSettings.ui_build_enviroments != null && uiSettings.ui_build_enviroments.Count > 0)
            {
                markdown.AppendLine("## UI构建环境说明");
                markdown.AppendLine();
                markdown.AppendLine($"当前UI类型：**{uiSettings.selectedUIType}**");
                markdown.AppendLine();
                for (int i = 0; i < uiSettings.ui_build_enviroments.Count; i++)
                {
                    if (!string.IsNullOrEmpty(uiSettings.ui_build_enviroments[i]))
                    {
                        markdown.AppendLine($"- {uiSettings.ui_build_enviroments[i]}");
                    }
                }
                markdown.AppendLine();
            }

            // UI构建步骤 - 只在有数据时显示
            if (uiSettings != null && uiSettings.ui_build_steps != null && uiSettings.ui_build_steps.Count > 0)
            {
                markdown.AppendLine("## UI构建步骤");
                markdown.AppendLine();
                markdown.AppendLine($"按照以下步骤进行{uiSettings.selectedUIType} UI开发：");
                markdown.AppendLine();
                for (int i = 0; i < uiSettings.ui_build_steps.Count; i++)
                {
                    if (!string.IsNullOrEmpty(uiSettings.ui_build_steps[i]))
                    {
                        markdown.AppendLine($"{i + 1}. {uiSettings.ui_build_steps[i]}");
                    }
                }
                markdown.AppendLine();
            }

            // 节点名称映射 - 只在有数据时显示
            if (ruleObject.node_names != null && ruleObject.node_names.Count > 0)
            {
                markdown.AppendLine("## 节点名称映射");
                markdown.AppendLine();
                markdown.AppendLine($"总计 {ruleObject.node_names.Count} 个节点重命名规则：");
                markdown.AppendLine();
                markdown.AppendLine("| 节点ID | 新名称 | 原始名称 |");
                markdown.AppendLine("|--------|--------|----------|");
                foreach (var nodeInfo in ruleObject.node_names)
                {
                    string id = !string.IsNullOrEmpty(nodeInfo.id) ? nodeInfo.id : "N/A";
                    string name = !string.IsNullOrEmpty(nodeInfo.name) ? nodeInfo.name : "N/A";
                    string originName = !string.IsNullOrEmpty(nodeInfo.originName) ? nodeInfo.originName : "N/A";
                    markdown.AppendLine($"| `{id}` | `{name}` | {originName} |");
                }
                markdown.AppendLine();
            }

            // 节点Sprite映射 - 只在有数据时显示
            if (ruleObject.node_sprites != null && ruleObject.node_sprites.Count > 0)
            {
                markdown.AppendLine("## 节点Sprite映射");
                markdown.AppendLine();
                markdown.AppendLine($"总计 {ruleObject.node_sprites.Count} 个Sprite资源：");
                markdown.AppendLine();
                markdown.AppendLine("| 节点ID | 文件名 | Sprite状态 |");
                markdown.AppendLine("|--------|--------|-----------|");
                foreach (var spriteInfo in ruleObject.node_sprites)
                {
                    string id = !string.IsNullOrEmpty(spriteInfo.id) ? spriteInfo.id : "N/A";
                    string fileName = !string.IsNullOrEmpty(spriteInfo.fileName) ? spriteInfo.fileName : "N/A";
                    string spriteStatus = spriteInfo.sprite != null ? "✓ 已加载" : "✗ 未加载";
                    markdown.AppendLine($"| `{id}` | `{fileName}` | {spriteStatus} |");
                }
                markdown.AppendLine();
            }

            // 附加说明 - 只在有实际内容时添加
            bool hasContent = (ruleObject.node_names != null && ruleObject.node_names.Count > 0) ||
                             (ruleObject.node_sprites != null && ruleObject.node_sprites.Count > 0);

            if (hasContent)
            {
                markdown.AppendLine("---");
                markdown.AppendLine();
                markdown.AppendLine("### 附加说明");
                markdown.AppendLine();
                if (ruleObject.node_names != null && ruleObject.node_names.Count > 0)
                {
                    markdown.AppendLine("- 使用 **节点名称映射** 来重命名Figma节点到Unity GameObject");
                }
                if (ruleObject.node_sprites != null && ruleObject.node_sprites.Count > 0)
                {
                    markdown.AppendLine("- 使用 **节点Sprite映射** 来关联图片资源");
                }
                markdown.AppendLine();
            }

            string result = markdown.ToString();
            Debug.Log($"[UIDefineRuleObjectEditor] Generated Markdown text with {result.Length} characters");
            return result;
        }
        /// <summary>
        /// 拷贝UI规则到剪贴板
        /// </summary>
        private void CopyUIRuleToClipboard()
        {
            try
            {
                var ruleObject = target as UIDefineRuleObject;
                if (ruleObject == null)
                {
                    Debug.LogError("[UIDefineRuleObjectEditor] Cannot cast target to UIDefineRuleObject");
                    EditorUtility.DisplayDialog("Error", "无法获取UI规则对象", "OK");
                    return;
                }

                string markdownText = GetUIRuleText(ruleObject);
                if (string.IsNullOrEmpty(markdownText))
                {
                    Debug.LogError("[UIDefineRuleObjectEditor] Failed to get UI rule text");
                    EditorUtility.DisplayDialog("Error", "无法生成UI规则文本", "OK");
                    return;
                }

                // 拷贝到剪贴板
                GUIUtility.systemCopyBuffer = markdownText;

                Debug.Log($"[UIDefineRuleObjectEditor] Successfully copied UI rule '{ruleObject.name}' to clipboard ({markdownText.Length} characters)");
                EditorUtility.DisplayDialog("Success", $"UI规则 '{ruleObject.name}' 已复制到剪贴板。\n\n内容长度: {markdownText.Length} 字符", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIDefineRuleObjectEditor] Error copying UI rule to clipboard: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"复制UI规则到剪贴板时出错: {e.Message}", "OK");
            }
        }
    }
}
