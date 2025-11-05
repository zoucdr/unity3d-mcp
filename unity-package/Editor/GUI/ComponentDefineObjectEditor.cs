using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UniMcp.Models;
using System.Collections.Generic;

namespace UniMcp.Gui
{
    [CustomEditor(typeof(ComponentDefineObject))]
    public class ComponentDefineObjectEditor : Editor
    {
        // 序列化属性
        private SerializedProperty libraryNameProp;
        private SerializedProperty componentMappingsProp;

        // 可重排列表
        private ReorderableList mappingsList;

        // 搜索过滤
        private string searchFilter = "";
        private bool showSearchBar = false;

        // 展开状态字典
        private Dictionary<int, bool> expandedStates = new Dictionary<int, bool>();

        private void OnEnable()
        {
            // 获取序列化属性
            libraryNameProp = serializedObject.FindProperty("libraryName");
            componentMappingsProp = serializedObject.FindProperty("componentMappings");

            // 初始化可重排列表
            InitializeMappingsList();
        }

        private void InitializeMappingsList()
        {
            mappingsList = new ReorderableList(
                serializedObject,
                componentMappingsProp,
                true, // 可拖拽
                true, // 显示标题
                true, // 显示添加按钮
                true  // 显示删除按钮
            );

            // 设置标题
            mappingsList.drawHeaderCallback = (Rect rect) =>
            {
                float idWidth = rect.width * 0.4f;
                float prefabWidth = rect.width * 0.6f - 20f;

                EditorGUI.LabelField(new Rect(rect.x, rect.y, idWidth, rect.height), "组件ID");
                EditorGUI.LabelField(new Rect(rect.x + idWidth, rect.y, prefabWidth, rect.height), "预制件引用");

                // 搜索按钮
                if (GUI.Button(new Rect(rect.x + rect.width - 20f, rect.y, 20f, rect.height), EditorGUIUtility.IconContent("d_Search Icon")))
                {
                    showSearchBar = !showSearchBar;
                    if (!showSearchBar)
                        searchFilter = "";
                }
            };

            // 设置元素高度
            mappingsList.elementHeightCallback = (int index) =>
            {
                if (index < 0 || index >= componentMappingsProp.arraySize)
                    return EditorGUIUtility.singleLineHeight + 4;

                bool isExpanded = false;
                expandedStates.TryGetValue(index, out isExpanded);

                if (isExpanded)
                    return (EditorGUIUtility.singleLineHeight + 2) * 2 + 4; // 展开时两行高度
                else
                    return EditorGUIUtility.singleLineHeight + 4; // 收起时一行高度
            };

            // 绘制每个元素
            mappingsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= componentMappingsProp.arraySize)
                    return;

                SerializedProperty element = componentMappingsProp.GetArrayElementAtIndex(index);
                SerializedProperty idProp = element.FindPropertyRelative("id");
                SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
                SerializedProperty urlProp = element.FindPropertyRelative("url");

                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                float idWidth = rect.width * 0.4f;
                float prefabWidth = rect.width * 0.5f;
                float expandButtonWidth = rect.width * 0.1f;

                // 获取当前元素的展开状态
                bool isExpanded = false;
                expandedStates.TryGetValue(index, out isExpanded);

                // 第一行
                // ID字段
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, idWidth - 5f, rect.height),
                    idProp,
                    GUIContent.none
                );

                // 预制件字段
                EditorGUI.PropertyField(
                    new Rect(rect.x + idWidth, rect.y, prefabWidth, rect.height),
                    prefabProp,
                    GUIContent.none
                );

                // 展开/收起按钮
                if (GUI.Button(
                    new Rect(rect.x + idWidth + prefabWidth, rect.y, expandButtonWidth, rect.height),
                    isExpanded ? "▲" : "▼", EditorStyles.miniButton))
                {
                    isExpanded = !isExpanded;
                    expandedStates[index] = isExpanded;
                }

                // 如果展开，显示第二行
                if (isExpanded)
                {
                    // URL字段
                    EditorGUI.LabelField(
                        new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, 30, rect.height),
                        "URL:"
                    );

                    EditorGUI.PropertyField(
                        new Rect(rect.x + 30, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width - 30, rect.height),
                        urlProp,
                        GUIContent.none
                    );
                }
            };

            // 添加元素回调
            mappingsList.onAddCallback = (ReorderableList list) =>
            {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;

                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("id").stringValue = "";
                element.FindPropertyRelative("prefab").objectReferenceValue = null;

                serializedObject.ApplyModifiedProperties();
            };

            // 选择元素回调
            mappingsList.onSelectCallback = (ReorderableList list) =>
            {
                // 可以在这里添加选择元素时的逻辑
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制基本属性
            EditorGUILayout.PropertyField(libraryNameProp, new GUIContent("组件库名称"));

            // 添加一个分隔线
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            // 显示搜索栏
            if (showSearchBar)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("搜索:", GUILayout.Width(40));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                EditorGUILayout.EndHorizontal();
            }

            // 过滤列表
            if (!string.IsNullOrEmpty(searchFilter))
            {
                FilterMappingsList();
            }
            else
            {
                // 绘制组件映射列表
                mappingsList.DoLayoutList();
            }


            serializedObject.ApplyModifiedProperties();
        }

        private void FilterMappingsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 显示过滤后的结果
            bool foundAny = false;
            string searchLower = searchFilter.ToLower();

            for (int i = 0; i < componentMappingsProp.arraySize; i++)
            {
                SerializedProperty element = componentMappingsProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp = element.FindPropertyRelative("id");
                SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
                SerializedProperty urlProp = element.FindPropertyRelative("url");

                string id = idProp.stringValue.ToLower();
                string prefabName = prefabProp.objectReferenceValue != null ? prefabProp.objectReferenceValue.name.ToLower() : "";
                string url = urlProp.stringValue.ToLower();

                if (id.Contains(searchLower) || prefabName.Contains(searchLower) || url.Contains(searchLower))
                {
                    foundAny = true;

                    EditorGUILayout.BeginHorizontal();

                    // ID字段
                    EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.4f));

                    // 预制件字段
                    EditorGUILayout.PropertyField(prefabProp, GUIContent.none);

                    // 删除按钮
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        componentMappingsProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return; // 退出循环，避免索引问题
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (!foundAny)
            {
                EditorGUILayout.HelpBox("没有匹配的结果", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

    }
}
