using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace SBR.Editor {
    [CustomPropertyDrawer(typeof(TypeSelectAttribute))]
    public class TypeSelectDrawer : PropertyDrawer {
        private string[] types;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // First get the attribute since it contains the range for the slider
            TypeSelectAttribute attr = attribute as TypeSelectAttribute;

            // Now draw the property as a Slider or an IntSlider based on whether it's a float or integer.
            if (property.propertyType == SerializedPropertyType.String) {
                if (types == null) {
                    types = typeof(Channels).Assembly.GetTypes()
                        .Where(p => !p.IsGenericType && (attr.allowAbstract || !p.IsAbstract) && attr.baseClass.IsAssignableFrom(p))
                        .Select(t => t.FullName).ToArray();
                }

                int index = Array.IndexOf(types, property.stringValue);
                if (index < 0) {
                    index = Mathf.Max(Array.IndexOf(types, attr.baseClass.FullName), 0);
                }

                EditorGUI.LabelField(new Rect(position.x, position.y, position.width / 2, position.height), property.displayName);
                index = EditorGUI.Popup(new Rect(position.x + position.width / 2, position.y, position.width / 2, position.height), index, types);
                property.stringValue = types[index];
            } else {
                EditorGUI.LabelField(position, label.text, "Use TypeSelect with string.");
            }
        }
    }
}