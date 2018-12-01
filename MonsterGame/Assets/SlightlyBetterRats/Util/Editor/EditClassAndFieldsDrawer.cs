using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SBR.Editor {
    [CustomPropertyDrawer(typeof(EditClassAndFieldsAttribute))]
    public class EditClassAndFieldsDrawer : PropertyDrawer {
        private UnityEditor.Editor editor;

        private Type[] typesArr;
        private string[] typeNames;

        private static Dictionary<SerializedProperty, int> selClasses = new Dictionary<SerializedProperty, int>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0;

            if (property.propertyType == SerializedPropertyType.ObjectReference) {
                height += GetEditorHeight(property.objectReferenceValue, label);
            } else {
                height = 20;
            }

            return height;
        }

        public float GetEditorHeight(UnityEngine.Object obj, GUIContent label) {
            float height = 20;

            var e = UnityEditor.Editor.CreateEditor(obj);
            if (e != null) {
                var so = e.serializedObject;
                var prop = so.GetIterator();
                prop.NextVisible(true);
                while (prop.NextVisible(prop.isExpanded)) height += GetPropertyHeight(prop, label);
            }

            return height;
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Type type = fieldInfo.FieldType;
            if (type.HasElementType) {
                type = type.GetElementType();
            }

            bool b = false;
            property.objectReferenceValue = Show(position, property, property.objectReferenceValue, label, type, ref b);
        }

        public UnityEngine.Object Show(Rect position, SerializedProperty property, UnityEngine.Object value, GUIContent label, Type type, ref bool dirty) {
            bool compatibleType = typeof(ScriptableObject).IsAssignableFrom(type);

            if (compatibleType) {
                var curType = value != null ? value.GetType() : null;

                if (typesArr == null || typeNames == null) {
                    var types = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(s => s.GetTypes())
                        .Where(p => !p.IsAbstract && !p.IsGenericType && type.IsAssignableFrom(p));

                    typesArr = types.ToArray();
                    typeNames = types.Select(i => i.Name).ToArray();
                    var temp = new string[typeNames.Length + 1];
                    Array.Copy(typeNames, 0, temp, 1, typeNames.Length);
                    typeNames = temp;
                    temp[0] = "Select Class";
                }

                int curIndex;
                if (curType != null) {
                    curIndex = Array.IndexOf(typesArr, curType);
                } else {
                    if (selClasses.ContainsKey(property)) {
                        curIndex = selClasses[property];
                    } else {
                        curIndex = -1;
                    }
                }

                var textDimensions = GUI.skin.label.CalcSize(label);
                EditorGUI.LabelField(new Rect(position.x, position.y, textDimensions.x, 20), label);
                float w = position.width - textDimensions.x - 15;
                selClasses[property] = EditorGUI.Popup(new Rect(position.x + textDimensions.x + 15, position.y, w / 2, 20), curIndex + 1, typeNames) - 1;

                int selClass = selClasses[property];
                if (selClass != curIndex) {
                    dirty = true;
                }

                if (selClass >= 0) {
                    if (curType != null && typesArr[selClass] != curType) {
                        value = null;
                    }

                    var guids = AssetDatabase.FindAssets("t:" + typesArr[selClass]);
                    var paths = guids.Select(i => AssetDatabase.GUIDToAssetPath(i)).ToArray();
                    var options = new string[paths.Length + 2];
                    Array.Copy(paths, 0, options, 2, paths.Length);
                    options[0] = "Single-Use Instance";
                    options[1] = "Create Asset...";

                    int curAssetIndex = -1;

                    bool wasSingleUse = false;

                    if (value) {
                        curAssetIndex = Array.IndexOf(paths, AssetDatabase.GetAssetPath(value));
                        wasSingleUse = curAssetIndex < 0;
                    }

                    if (curAssetIndex < 0) {
                        curAssetIndex = -2;
                    }

                    var newAssetIndex = EditorGUI.Popup(new Rect(position.x + textDimensions.x + w / 2 + 20, position.y, w / 2 - 20, 20), curAssetIndex + 2, options) - 2;

                    if (newAssetIndex != curAssetIndex) {
                        dirty = true;
                    }

                    curAssetIndex = newAssetIndex;

                    if (curAssetIndex >= 0) {
                        value = AssetDatabase.LoadAssetAtPath(paths[curAssetIndex], typesArr[selClass]);
                    } else if (curAssetIndex == -1) {
                        var path = EditorUtility.SaveFilePanelInProject("Save New " + typesArr[selClass].Name, typesArr[selClass].Name + ".asset", "asset", "Save new asset to file");
                        if (path.Length > 0) {
                            UnityEngine.Object obj;
                            if (value) {
                                obj = UnityEngine.Object.Instantiate(value);
                            } else {
                                obj = ScriptableObject.CreateInstance(typesArr[selClass]);
                            }
                            AssetDatabase.CreateAsset(obj, path);
                            AssetDatabase.Refresh();
                            value = obj;
                        }
                    } else if (curAssetIndex == -2 && !wasSingleUse) {
                        if (value) {
                            value = UnityEngine.Object.Instantiate(value);
                        } else {
                            value = ScriptableObject.CreateInstance(typesArr[selClass]);
                        }
                    }
                } else {
                    value = null;
                }


                if (value != null) {
                    UnityEditor.Editor.CreateCachedEditor(value, null, ref editor);

                    position.height = 16;

                    position.y += 20;

                    if (editor != null) {
                        position.x += 20;
                        position.width -= 40;
                        var so = editor.serializedObject;
                        so.Update();

                        var prop = so.GetIterator();
                        prop.NextVisible(true);
                        while (prop.NextVisible(prop.isExpanded)) {
                            position.height = 16;
                            EditorGUI.PropertyField(position, prop);
                            position.y += 20;
                        }
                        if (GUI.changed) {
                            so.ApplyModifiedProperties();
                            dirty = true;
                        }
                    }
                }

            } else {
                EditorGUI.LabelField(position, label.text, "Only use [EditClassAndFields] on a field that is a subclass of ScriptableObject.");
            }

            return value;
        }
    }
}