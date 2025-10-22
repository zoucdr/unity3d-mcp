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
        private SerializedProperty prototypePicProp;
        private SerializedProperty imageScaleProp;
        private SerializedProperty descriptionsProp;
        private SerializedProperty modifyRecordsProp;
        private SerializedProperty nodeNamesProp;
        private SerializedProperty nodeSpritesProp;

        // 折叠状态
        private bool nodeNamesFoldout = true;
        private bool nodeSpritesFoldout = true;
        private bool modifyRecordsFoldout = true;

        void OnEnable()
        {
            // 获取序列化属性
            linkUrlProp = serializedObject.FindProperty("link_url");
            pictureUrlProp = serializedObject.FindProperty("img_save_to");
            prototypePicProp = serializedObject.FindProperty("prototype_pic");
            imageScaleProp = serializedObject.FindProperty("image_scale");
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
            EditorGUILayout.PropertyField(linkUrlProp, new GUIContent("Figma Link URL"));
            EditorGUILayout.PropertyField(pictureUrlProp, new GUIContent("Img SaveTo"));
            EditorGUILayout.PropertyField(prototypePicProp, new GUIContent("Prototype Pic"));
            EditorGUILayout.PropertyField(imageScaleProp, new GUIContent("Image Scale"));

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
        /// 验证并获取UI名称
        /// </summary>
        private string ValidateAndGetUIName()
        {
            var targetObject = target as UIDefineRuleObject;
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Cannot find UIDefineRuleObject.", "OK");
                return null;
            }

            string uiName = targetObject.name;
            if (string.IsNullOrEmpty(uiName))
            {
                EditorUtility.DisplayDialog("Error", "UI name is empty. Please set a name for this rule object.", "OK");
                return null;
            }

            return uiName;
        }

        /// <summary>
        /// 获取UI规则（公共方法）
        /// </summary>
        private void GetUIRule(string uiName, System.Action<JsonNode> onComplete)
        {
            Debug.Log($"[UIDefineRuleObjectEditor] Getting UI rule for '{uiName}'...");

            // 创建UIRuleManage实例
            var uiRuleManage = new UIRuleManage();

            // 创建参数
            var args = new JsonClass();
            args["action"] = "get_rule";
            args["name"] = uiName;

            // 使用StateTreeContext调用ExecuteMethod
            var context = new Unity.Mcp.StateTreeContext(args);
            bool resultReceived = false;
            JsonNode result = null;

            // 注册完成回调
            context.RegistComplete((res) =>
            {
                result = res;
                resultReceived = true;
            });

            try
            {
                uiRuleManage.ExecuteMethod(context);
                context.RegistComplete((System.Action<JsonNode>)(x =>
                {
                    // 如果立即有结果，直接使用
                    if (x != null)
                    {
                        result = x;
                        onComplete?.Invoke(result);
                    }
                    else
                    {
                        if (!resultReceived)
                        {
                            Debug.LogError("[UIDefineRuleObjectEditor] ExecuteMethod timeout");
                            EditorUtility.DisplayDialog("Error", "ExecuteMethod timeout", "OK");
                            onComplete?.Invoke(null);
                            return;
                        }
                    }
                }));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIDefineRuleObjectEditor] Error getting UI rule: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Error getting UI rule: {e.Message}", "OK");
                onComplete?.Invoke(null);
            }
        }
        /// <summary>
        /// 构建发送到Cursor的消息
        /// </summary>
        private string BuildCursorMessage(JsonNode result, string uiName)
        {
            try
            {
                // 将结果转换为JSON字符串以便解析
                string resultJson = Json.FromObject(result);

                var message = new System.Text.StringBuilder();
                message.AppendLine($"# Unity UI规则信息 - {uiName}");
                message.AppendLine();
                message.AppendLine("以下是Unity项目中的UI制作规则和配置信息，请基于这些信息基于mcp实现UI界面开发：");
                message.AppendLine();
                foreach (KeyValuePair<string, JsonNode> kv in result.AsObject)
                {
                    if (kv.Value.IsNull())
                        continue;
                    message.AppendLine($"- {kv.Key}: {kv.Value}");
                }
                return message.ToString();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIDefineRuleObjectEditor] Error building Cursor message: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 拷贝UI规则到剪贴板
        /// </summary>
        private void CopyUIRuleToClipboard()
        {
            string uiName = ValidateAndGetUIName();
            if (string.IsNullOrEmpty(uiName))
                return;

            Debug.Log($"[UIDefineRuleObjectEditor] Starting to copy UI rule '{uiName}' to clipboard...");

            try
            {
                GetUIRule(uiName, (result) =>
                {
                    if (result != null && result["success"].AsBoolDefault(false))
                    {
                        CopyToClipboard(result["data"]["rule"], uiName);
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIDefineRuleObjectEditor] Error copying UI rule to clipboard: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Error copying UI rule to clipboard: {e.Message}", "OK");
            }
        }

        /// <summary>
        /// 拷贝内容到剪贴板
        /// </summary>
        private void CopyToClipboard(JsonNode result, string uiName)
        {
            if (result == null)
            {
                Debug.LogError("[UIDefineRuleObjectEditor] Failed to get UI rule result");
                EditorUtility.DisplayDialog("Error", "Failed to get UI rule result", "OK");
                return;
            }

            // 解析结果并构建消息
            string message = BuildCursorMessage(result, uiName);

            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("[UIDefineRuleObjectEditor] Failed to build message");
                EditorUtility.DisplayDialog("Error", "Failed to build message for clipboard", "OK");
                return;
            }

            Debug.Log($"[UIDefineRuleObjectEditor] Copying UI rule to clipboard: {message.Length} characters");

            // 拷贝到剪贴板
            GUIUtility.systemCopyBuffer = message;

            Debug.Log($"[UIDefineRuleObjectEditor] Successfully copied UI rule '{uiName}' to clipboard");
            EditorUtility.DisplayDialog("Success", $"UI rule '{uiName}' has been copied to clipboard.\n\nMessage length: {message.Length} characters", "OK");
        }
    }
}
