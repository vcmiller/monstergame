using UnityEngine;
using System.Collections;
using UnityEditor;

namespace SBR.Editor {
    [CustomEditor(typeof(StateMachineDefinition))]
    public class StateMachineInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            StateMachineDefinition myTarget = (StateMachineDefinition)target;

            if (GUILayout.Button("Open State Machine Editor")) {
                StateMachineEditorWindow.def = myTarget;
                StateMachineEditorWindow.ShowWindow();
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "baseClass");
            serializedObject.ApplyModifiedProperties();
        }
    }
}