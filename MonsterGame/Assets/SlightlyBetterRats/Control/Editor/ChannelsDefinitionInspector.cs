using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using UnityEditorInternal;

namespace SBR.Editor {
    [CustomEditor(typeof(ChannelsDefinition))]
    public class ChannelsDefinitionInspector : UnityEditor.Editor {
        private ReorderableList channelList;

        private void OnEnable() {
            channelList = new ReorderableList(serializedObject, serializedObject.FindProperty("channels"), true, true, true, true);
            channelList.drawElementCallback = DrawListElement;
            channelList.elementHeight *= 4;
            channelList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Channel List");
            };
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused) {
            var element = channelList.serializedProperty.GetArrayElementAtIndex(index);

            rect.yMin += 4;
            EditorGUI.PropertyField(rect, element);
        }

        public override void OnInspectorGUI() {
            if (GUILayout.Button("Generate Class")) {
                var path = AssetDatabase.GetAssetPath(target);
                if (path.Length > 0) {
                    ChannelsClassGenerator.GenerateClass(target as ChannelsDefinition);
                }
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "channels", "m_Script");
            channelList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}