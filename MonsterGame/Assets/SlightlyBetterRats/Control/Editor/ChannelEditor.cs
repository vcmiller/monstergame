using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace SBR.Editor {
    [CustomPropertyDrawer(typeof(ChannelsDefinition.Channel))]
    public class ChannelEditor : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float f = base.GetPropertyHeight(property, label);
            float extraLine = 20;
            float spacing = 20;
            ChannelsDefinition.ChannelType type = (ChannelsDefinition.ChannelType)property.FindPropertyRelative("type").enumValueIndex;

            if (type == ChannelsDefinition.ChannelType.Bool || type == ChannelsDefinition.ChannelType.Quaternion) {
                return f + extraLine * 2 + spacing;
            } else {
                return f + extraLine * 3 + spacing;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;

            //EditorGUI.indentLevel = 0;
            position.height = 16;
            position.width -= 10;

            // Draw fields - passs GUIContent.none to each so they are drawn without labels
            EditorGUI.MultiPropertyField(position, new GUIContent[] { GUIContent.none, GUIContent.none }, property.FindPropertyRelative("name"), GUIContent.none);

            position.y += 20;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("clears"), new GUIContent("Reset Every Frame"));
            position.y += 20;

            ChannelsDefinition.ChannelType type = (ChannelsDefinition.ChannelType)property.FindPropertyRelative("type").enumValueIndex;

            EditorGUI.indentLevel++;
            if (type == ChannelsDefinition.ChannelType.Bool) {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("defaultBool"), new GUIContent("Default Value"));
            } else if (type == ChannelsDefinition.ChannelType.Int) {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("defaultInt"), new GUIContent("Default Value"));
                position.y += 20;
                Rect r = position;
                r.width = (r.width - 10) / 2;
                EditorGUI.PropertyField(r, property.FindPropertyRelative("intHasRange"), new GUIContent("Has Range"));

                r.x += r.width - 16;

                if (property.FindPropertyRelative("intHasRange").boolValue) {
                    EditorGUI.LabelField(r, new GUIContent("Min"));
                    r.x += 30;
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width * 0.4f, r.height), property.FindPropertyRelative("intMin"), GUIContent.none);
                    r.x += r.width * 0.4f;
                    EditorGUI.LabelField(r, new GUIContent("Max"));
                    r.x += 30;
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width * 0.4f, r.height), property.FindPropertyRelative("intMax"), GUIContent.none);
                }
            } else if (type == ChannelsDefinition.ChannelType.Float) {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("defaultFloat"), new GUIContent("Default Value"));
                position.y += 20;
                Rect r = position;
                r.width = (r.width - 10) / 2;
                EditorGUI.PropertyField(r, property.FindPropertyRelative("floatHasRange"), new GUIContent("Has Range"));

                r.x += r.width - 16;

                if (property.FindPropertyRelative("floatHasRange").boolValue) {
                    EditorGUI.LabelField(r, new GUIContent("Min"));
                    r.x += 30;
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width * 0.4f, r.height), property.FindPropertyRelative("floatMin"), GUIContent.none);
                    r.x += r.width * 0.4f;
                    EditorGUI.LabelField(r, new GUIContent("Max"));
                    r.x += 30;
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width * 0.4f, r.height), property.FindPropertyRelative("floatMax"), GUIContent.none);
                }
            } else if (type == ChannelsDefinition.ChannelType.Object) {
                var prop = property.FindPropertyRelative("objectType");

                EditorGUI.PropertyField(position, prop, new GUIContent("Object Type"));
            } else if (type == ChannelsDefinition.ChannelType.Vector) {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("defaultVector"), new GUIContent("Default Value"));

                position.y += 20;
                Rect r = position;
                r.width = (r.width - 10) / 2;
                EditorGUI.PropertyField(r, property.FindPropertyRelative("vectorHasMax"), new GUIContent("Has Max Length"));

                r.x += r.width - 16;

                if (property.FindPropertyRelative("vectorHasMax").boolValue) {
                    EditorGUI.LabelField(r, new GUIContent("Max Length"));
                    r.x += 75;
                    EditorGUI.PropertyField(new Rect(r.x, r.y, r.width - 50, r.height), property.FindPropertyRelative("vectorMax"), GUIContent.none);
                }
            } else if (type == ChannelsDefinition.ChannelType.Quaternion) {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("defaultRotation"), new GUIContent("Default Value (Euler)"));
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}