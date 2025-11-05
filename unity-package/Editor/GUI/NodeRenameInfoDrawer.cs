using UnityEngine;
using UnityEditor;
using UniMcp.Models;

namespace UniMcp.Gui
{
    /// <summary>
    /// NodeRenameInfo的自定义PropertyDrawer，让id、name和originName显示在同一行
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeRenameInfo))]
    public class NodeRenameInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 不缩进子属性
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // 计算字段的矩形区域
            var labelWidth = 45f; // 标签宽度
            var spacing = 3f; // 间距

            var totalLabelWidth = labelWidth * 3; // 三个标签的总宽度
            var remainingWidth = position.width - totalLabelWidth - spacing * 4;
            var idWidth = remainingWidth * 0.35f; // ID占35%剩余宽度
            var nameWidth = remainingWidth * 0.35f; // Name占35%剩余宽度
            var originNameWidth = remainingWidth * 0.3f; // OriginName占30%剩余宽度

            var currentX = position.x;

            // Node ID
            var idLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var idRect = new Rect(currentX, position.y, idWidth, position.height);
            currentX += idWidth + spacing;

            // Name
            var nameLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var nameRect = new Rect(currentX, position.y, nameWidth, position.height);
            currentX += nameWidth + spacing;

            // Origin Name
            var originNameLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var originNameRect = new Rect(currentX, position.y, originNameWidth, position.height);

            // 绘制标签和字段
            var idProp = property.FindPropertyRelative("id");
            var nameProp = property.FindPropertyRelative("name");
            var originNameProp = property.FindPropertyRelative("originName");

            EditorGUI.LabelField(idLabelRect, "ID:");
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

            EditorGUI.LabelField(nameLabelRect, "Name:");
            EditorGUI.PropertyField(nameRect, nameProp, GUIContent.none);

            EditorGUI.LabelField(originNameLabelRect, "Origin:");
            EditorGUI.PropertyField(originNameRect, originNameProp, GUIContent.none);

            // 恢复缩进
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

}
