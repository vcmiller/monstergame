using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SBR.Editor {
    [CustomPropertyDrawer(typeof(ConditionalAttribute))]
    public class ConditionalAttributeDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (ShouldDraw(property)) {
                return EditorGUI.GetPropertyHeight(property, label, true);
            } else {
                return 0;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (ShouldDraw(property)) {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool ShouldDraw(SerializedProperty property) {
            ConditionalAttribute attr = attribute as ConditionalAttribute;
            var obj = property.serializedObject.targetObject;

            var func = obj.GetType().GetMethod(attr.condition);
            var prop = obj.GetType().GetProperty(attr.condition);
            var field = obj.GetType().GetField(attr.condition);

            bool draw = true;

            if (func != null) {
                draw = (bool)func.Invoke(obj, null);
            } else if (prop != null) {
                draw = (bool)prop.GetValue(obj, null);
            } else if (field != null) {
                draw = (bool)field.GetValue(obj);
            } else {
                Debug.LogError(string.Format("Could not find method, property, or field {0} on type {1}.", attr.condition, obj.GetType()));
            }

            return draw;
        }
    }
}
