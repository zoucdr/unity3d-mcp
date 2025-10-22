using UnityEngine;
using UnityEditor;
using Unity.Mcp.Models;

namespace Unity.Mcp.Gui
{
    /// <summary>
    /// NodeSpriteInfo的自定义PropertyDrawer，让id、fileName和sprite显示在同一行，并支持载入图片按钮
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeSpriteInfo))]
    public class NodeSpriteInfoDrawer : PropertyDrawer
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
            var fieldWidth = remainingWidth / 3f; // 每个字段占1/3宽度
            var idWidth = fieldWidth;
            var fileNameWidth = fieldWidth;
            var spriteFieldWidth = fieldWidth;

            var currentX = position.x;

            // Node ID
            var idLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var idRect = new Rect(currentX, position.y, idWidth, position.height);
            currentX += idWidth + spacing;

            // File Name
            var fileNameLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var fileNameRect = new Rect(currentX, position.y, fileNameWidth, position.height);
            currentX += fileNameWidth + spacing;

            // Sprite
            var spriteLabelRect = new Rect(currentX, position.y, labelWidth, position.height);
            currentX += labelWidth;
            var spriteRect = new Rect(currentX, position.y, spriteFieldWidth, position.height);

            // 绘制标签和字段
            var idProp = property.FindPropertyRelative("id");
            var fileNameProp = property.FindPropertyRelative("fileName");
            var spriteProp = property.FindPropertyRelative("sprite");

            EditorGUI.LabelField(idLabelRect, "ID:");
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

            EditorGUI.LabelField(fileNameLabelRect, "File:");
            EditorGUI.PropertyField(fileNameRect, fileNameProp, GUIContent.none);

            EditorGUI.LabelField(spriteLabelRect, "Sprite:");
            EditorGUI.PropertyField(spriteRect, spriteProp, GUIContent.none);

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

