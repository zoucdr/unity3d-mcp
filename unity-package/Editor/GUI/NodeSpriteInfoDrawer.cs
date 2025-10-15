using UnityEngine;
using UnityEditor;
using UnityMcp.Models;

namespace UnityMcp.Gui
{
    /// <summary>
    /// NodeSpriteInfoCustomized ofPropertyDrawer，Letid、fileNameAndspriteShow in one line，And support load picture button
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeSpriteInfo))]
    public class NodeSpriteInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Don't indent child property
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate field rectangle area
            var labelWidth = 45f; // Label width
            var spacing = 3f; // Spacing

            var totalLabelWidth = labelWidth * 3; // Total width of three labels
            var remainingWidth = position.width - totalLabelWidth - spacing * 4;
            var fieldWidth = remainingWidth / 3f; // Each field occupies1/3Width
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

            // Draw label and field
            var idProp = property.FindPropertyRelative("id");
            var fileNameProp = property.FindPropertyRelative("fileName");
            var spriteProp = property.FindPropertyRelative("sprite");

            EditorGUI.LabelField(idLabelRect, "ID:");
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

            EditorGUI.LabelField(fileNameLabelRect, "File:");
            EditorGUI.PropertyField(fileNameRect, fileNameProp, GUIContent.none);

            EditorGUI.LabelField(spriteLabelRect, "Sprite:");
            EditorGUI.PropertyField(spriteRect, spriteProp, GUIContent.none);

            // Restore indent
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

