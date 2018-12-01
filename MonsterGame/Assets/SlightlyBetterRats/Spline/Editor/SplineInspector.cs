using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace SBR.Editor {
    [CustomEditor(typeof(Spline))]
    public class SplineInspector : UnityEditor.Editor {
        private Spline spline { get { return target as Spline; } }
        private SplineData data { get { return spline.spline; } }
        private bool meshNeedsUpdate;

        private static Vector3[] samples = new Vector3[100];

        private ReorderableList pointList;

        private void OnEnable() {
            pointList = null;
        }

        private static string[] excludeFields;
        private static bool pointsExpanded;
        private static bool preferHandleSelection = true;
        private static bool updateMeshConstantly = false;

        private static readonly Color pathColorSelected = Color.white;
        private static readonly Color pathColorDeselected = new Color(1, 1, 1, 0.4f);
        private static readonly Color pointColor = Color.white;
        private static readonly Color tangentColor = Color.yellow;

        public override void OnInspectorGUI() {
            if (pointList == null)
                SetupPointList();

            if (pointList.index >= pointList.count)
                pointList.index = pointList.count - 1;

            // Ordinary properties
            if (excludeFields == null) {
                excludeFields = new string[] {
                    "m_Script",
                    "spline"
                };
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, excludeFields);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("spline.closed"));

            GUILayout.Label(new GUIContent("Selected Waypoint:"));
            EditorGUILayout.BeginVertical(GUI.skin.box);
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 3 + 10);
            if (pointList.index >= 0) {
                DrawWaypointEditor(rect, pointList.index);
                serializedObject.ApplyModifiedProperties();
            } else {
                if (data.points.Length > 0) {
                    EditorGUI.HelpBox(rect,
                        "Click on a waypoint in the scene view\nor in the Path Details list",
                        MessageType.Info);
                } else if (GUI.Button(rect, new GUIContent("Add a waypoint to the path"))) {
                    InsertWaypointAtIndex(pointList.index);
                    pointList.index = 0;
                }
            }
            EditorGUILayout.EndVertical();

            preferHandleSelection = EditorGUILayout.Toggle(
                    new GUIContent("Prefer Tangent Drag",
                        "When editing the path, if waypoint position and tangent coincide, dragging will apply preferentially to the tangent."),
                    preferHandleSelection);


            updateMeshConstantly = EditorGUILayout.Toggle(
                    new GUIContent("Update Mesh Constantly",
                        "If false, spline meshes will be updated at all times when the path is dragged. This can lead to poor performance while dragging."),
                    updateMeshConstantly);

            pointsExpanded = EditorGUILayout.Foldout(pointsExpanded, "Path Details");
            if (pointsExpanded) {
                EditorGUI.BeginChangeCheck();
                pointList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
        }

        private void SetupPointList() {
            pointList = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("spline.points"), true, true, true, true);
            pointList.elementHeight *= 3;

            pointList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Waypoints");
            };

            pointList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                DrawWaypointEditor(rect, index);
            };

            pointList.onAddCallback = (ReorderableList l) => {
                InsertWaypointAtIndex(l.index);
            };
        }

        private void DrawWaypointEditor(Rect rect, int index) {
            Vector2 numberDimension = GUI.skin.button.CalcSize(new GUIContent("999"));
            Vector2 labelDimension = GUI.skin.label.CalcSize(new GUIContent("Position"));
            Vector3 addButtonDimension = GUI.skin.button.CalcSize(new GUIContent("+"));
            addButtonDimension.y = labelDimension.y;
            float vSpace = 2;
            float hSpace = 3;

            SerializedProperty element = pointList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += vSpace / 2;

            Rect r = new Rect(rect.position, numberDimension);
            r.y += numberDimension.y - r.height / 2;
            Color color = GUI.color;
            // GUI.color = spline.m_Appearance.pathColor;
            if (GUI.Button(r, new GUIContent(index.ToString(), "Go to the waypoint in the scene view"))) {
                pointList.index = index;
                SceneView.lastActiveSceneView.pivot = spline.transform.TransformPoint(data.points[index].position);
                SceneView.lastActiveSceneView.size = 3;
                SceneView.lastActiveSceneView.Repaint();
            }
            GUI.color = color;

            r = new Rect(rect.position, labelDimension);
            r.x += hSpace + numberDimension.x;
            EditorGUI.LabelField(r, "Position");
            r.x += hSpace + r.width;
            r.width = rect.width - (numberDimension.x + hSpace + r.width + hSpace + addButtonDimension.x + hSpace);
            EditorGUI.PropertyField(r, element.FindPropertyRelative("position"), GUIContent.none);
            r.x += r.width + hSpace;
            r.size = addButtonDimension;
            if (GUI.Button(r, new GUIContent("-", "Remove this waypoint"))) {
                Undo.RecordObject(spline, "Delete waypoint");
                ArrayUtility.RemoveAt(ref data.points, index);
                if (index == data.points.Length) {
                    pointList.index = index - 1;
                }
            }

            r = new Rect(rect.position, labelDimension);
            r.y += numberDimension.y + vSpace;
            r.x += hSpace + numberDimension.x; r.width = labelDimension.x;
            EditorGUI.LabelField(r, "Tangent");
            r.x += hSpace + r.width;
            r.width = rect.width - (numberDimension.x + hSpace + r.width + hSpace + addButtonDimension.x + hSpace);
            EditorGUI.PropertyField(r, element.FindPropertyRelative("tangent"), GUIContent.none);
            r.x += r.width + hSpace;
            r.size = addButtonDimension;
            if (GUI.Button(r, new GUIContent("+", "Add a new waypoint after this one"))) {
                pointList.index = index;
                InsertWaypointAtIndex(index);
            }

            r = new Rect(rect.position, labelDimension);
            r.y += 2 * (numberDimension.y + vSpace);
            r.x += hSpace + numberDimension.x; r.width = labelDimension.x;
            EditorGUI.LabelField(r, "Roll");
            r.x += hSpace + labelDimension.x;
            r.width = rect.width
                - (numberDimension.x + hSpace)
                - (labelDimension.x + hSpace)
                - (addButtonDimension.x + hSpace);
            r.width /= 3;
            EditorGUI.MultiPropertyField(r, new GUIContent[] { new GUIContent(" ") },
                element.FindPropertyRelative("roll"));
        }

        void InsertWaypointAtIndex(int indexA) {
            Vector3 pos = Vector3.forward;
            Vector3 tangent = Vector3.right;
            float roll = 0;
            
            int numWaypoints = data.points.Length;
            if (indexA < 0)
                indexA = numWaypoints - 1;
            if (indexA >= 0) {
                int indexB = indexA + 1;
                if (data.closed && indexB >= numWaypoints) {
                    indexB = 0;
                }

                if (indexB >= numWaypoints) {
                    if (data.points[indexA].tangent.sqrMagnitude >= 0.0001f)
                        tangent = data.points[indexA].tangent;
                    pos = data.points[indexA].position + tangent;
                    roll = data.points[indexA].roll;
                } else {
                    float interp = (0.5f + indexA) / data.max;
                    pos = data.GetPointNonUniform(interp);
                    tangent = data.GetTangent(interp);
                    roll = data.GetRoll(interp);
                }
            }
            Undo.RecordObject(spline, "Add waypoint");
            var wp = new SplineData.Point();
            wp.position = pos;
            wp.tangent = tangent;
            wp.roll = roll;
            ArrayUtility.Insert(ref data.points, indexA + 1, wp);
            pointList.index = indexA + 1;
        }

        void OnSceneGUI() {
            if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.Ignore)
                && meshNeedsUpdate) {
                meshNeedsUpdate = false;
                spline.OnChanged(true);
            }

            if (pointList == null)
                SetupPointList();
            
            Matrix4x4 mOld = Handles.matrix;
            Color colorOld = Handles.color;

            Handles.matrix = spline.transform.localToWorldMatrix;
            for (int i = 0; i < data.points.Length; ++i) {
                DrawSelectionHandle(i);
                if (pointList.index == i) {
                    // Waypoint is selected
                    if (preferHandleSelection) {
                        DrawPositionControl(i);
                        DrawTangentControl(i);
                    } else {
                        DrawTangentControl(i);
                        DrawPositionControl(i);
                    }
                }
            }
            Handles.color = colorOld;
            Handles.matrix = mOld;
        }

        void DrawSelectionHandle(int i) {
            if (Event.current.button != 1) {
                Vector3 pos = data.points[i].position;
                float size = HandleUtility.GetHandleSize(pos) * 0.2f;
                Handles.color = Color.white;
                if (Handles.Button(pos, Quaternion.identity, size, size, Handles.SphereHandleCap)
                    && pointList.index != i) {
                    pointList.index = i;
                    InternalEditorUtility.RepaintAllViews();
                }
                // Label it
                Handles.BeginGUI();
                Vector2 labelSize = new Vector2(
                        EditorGUIUtility.singleLineHeight * 2, EditorGUIUtility.singleLineHeight);
                Vector2 labelPos = HandleUtility.WorldToGUIPoint(pos);
                labelPos.y -= labelSize.y / 2;
                labelPos.x -= labelSize.x / 2;
                GUILayout.BeginArea(new Rect(labelPos, labelSize));
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.black;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(new GUIContent(i.ToString(), "Waypoint " + i), style);
                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        void DrawTangentControl(int i) {
            SplineData.Point wp = data.points[i];
            Vector3 hPos = wp.position + wp.tangent;

            Handles.color = tangentColor;
            Handles.DrawLine(wp.position, hPos);

            Quaternion rotation;
            
            if (Tools.pivotRotation == PivotRotation.Local) {
                rotation = Quaternion.identity;
            } else {
                rotation = Quaternion.Inverse(spline.transform.rotation);
            }

            float size = HandleUtility.GetHandleSize(hPos) * 0.1f;
            Handles.SphereHandleCap(0, hPos, rotation, size, EventType.Repaint);
            Vector3 newPos = Vector3.zero;

            if (Tools.current == Tool.Move) {
                EditorGUI.BeginChangeCheck();

                newPos = Handles.PositionHandle(hPos, rotation);

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, "Edit Waypoint Tangent");
                    wp.tangent = newPos - wp.position;
                    data.points[i] = wp;
                    if (updateMeshConstantly) {
                        spline.OnChanged(true);
                    } else {
                        meshNeedsUpdate = true;
                    }
                }
            }
        }

        void DrawPositionControl(int i) {
            SplineData.Point wp = data.points[i];
            Handles.color = pointColor;
            Quaternion rotation;
            if (Tools.current == Tool.Rotate || Tools.current == Tool.Scale) {
                rotation = data.GetPointRotation(i) * Quaternion.AngleAxis(data.points[i].roll, Vector3.forward);
            } else if (Tools.pivotRotation == PivotRotation.Local) {
                rotation = Quaternion.identity;
            } else {
                rotation = Quaternion.Inverse(spline.transform.rotation);
            }
            float size = HandleUtility.GetHandleSize(wp.position) * 0.1f;
            Handles.SphereHandleCap(0, wp.position, rotation, size, EventType.Repaint);
            
            if (Tools.current == Tool.Move) {
                EditorGUI.BeginChangeCheck();
                Vector3 pos = Handles.PositionHandle(wp.position, rotation);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, "Move Waypoint");
                    wp.position = pos;
                    data.points[i] = wp;
                    if (updateMeshConstantly) {
                        spline.OnChanged(true);
                    } else {
                        meshNeedsUpdate = true;
                    }
                }
            } else if (Tools.current == Tool.Rotate) {
                EditorGUI.BeginChangeCheck();
                Quaternion rot = Handles.RotationHandle(rotation, wp.position);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, "Edit Waypoint Tangent");
                    float f = wp.tangent.magnitude;
                    wp.tangent = rot * Vector3.forward * f;
                    float rollMod = (Quaternion.Inverse(rotation) * rot).eulerAngles.z;
                    rollMod = ((rollMod % 360) + 360) % 360;
                    if (rollMod > 180) {
                        rollMod -= 360;
                    }
                    wp.roll += rollMod;
                    data.points[i] = wp;
                    if (updateMeshConstantly) {
                        spline.OnChanged(true);
                    } else {
                        meshNeedsUpdate = true;
                    }
                }
            } else if (Tools.current == Tool.Scale) {
                EditorGUI.BeginChangeCheck();
                float scale = Handles.ScaleSlider(wp.tangent.magnitude, wp.position, wp.tangent.normalized, Quaternion.identity, size * 10, 0);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, "Edit Waypoint Tangent");
                    float f = wp.tangent.magnitude;
                    wp.tangent = wp.tangent.normalized * scale;
                    data.points[i] = wp;
                    if (updateMeshConstantly) {
                        spline.OnChanged(true);
                    } else {
                        meshNeedsUpdate = true;
                    }
                }
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy
             | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(Spline))]
        internal static void DrawPathGizmos(Spline spline, GizmoType selectionType) {
            // Draw the path
            Color colorOld = Gizmos.color;
            Gizmos.color = (Selection.activeGameObject == spline.gameObject)
                ? pathColorSelected : pathColorDeselected;

            spline.GetWorldPoints(samples);
            for (int i = 0; i < samples.Length - 1; i++) {
                Gizmos.DrawLine(samples[i], samples[i + 1]);
            }

            Gizmos.color = colorOld;
        }
    }

}