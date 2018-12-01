using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace SBR.Editor {
    [CustomEditor(typeof(Brain))]
    public class BrainEditor : UnityEditor.Editor {

        public override void OnInspectorGUI() {
            Brain brain = target as Brain;

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "controllerPrefabs", "script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField("Detected Controller Components", EditorStyles.boldLabel);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            var ctrls = brain.GetComponents<Controller>();
            if (ctrls.Length > 0) {
                for (int i = 0; i < ctrls.Length; i++) {
                    EditorGUILayout.LabelField(ctrls[i].GetType().Name + (i == brain.defaultController ? " (default)" : ""));
                }
            } else {
                EditorGUILayout.LabelField("Add your Controller / StateMachine as a MonoBehaviour!");
            }
            

            EditorGUI.indentLevel = indent;
            
            EditorGUILayout.LabelField("Detected Motor Components", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var motor in brain.GetComponentsInChildren<Motor>()) {
                EditorGUILayout.LabelField(motor.GetType().Name + " at " + motor.name);
            }

            EditorGUI.indentLevel = indent;
        }
    }
}
