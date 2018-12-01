using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SBR.Geometry;

namespace SBR.Editor {
    [CustomEditor(typeof(Brush))]
    public class BrushInspector : UnityEditor.Editor {
        float size = 0.5f;

        private bool ctrl = false;
        private HashSet<int> selectedFaces = new HashSet<int>();
        private HashSet<int> selectedVertices = new HashSet<int>();
        private HashSet<UnorderedPair<int>> selectedEdges = new HashSet<UnorderedPair<int>>();
        private Vector3 scaleMvt = Vector3.one;
        private bool hadSelected = false;

        private static bool brushMode = true;
        private static bool showCommands = true;
        
        private Brush brush { get { return target as Brush; } }

        public override void OnInspectorGUI() {

            if (GUILayout.Button("Export Mesh")) {
                var path = EditorUtility.SaveFilePanelInProject("Save New Mesh", target.name + ".asset", "asset", "Save new asset to file");
                if (path.Length > 0) {
                    AssetDatabase.CreateAsset(Instantiate(brush.filter.sharedMesh), path);
                    AssetDatabase.Refresh();
                }
            }

            serializedObject.Update();

            if (brush.type == Brush.Type.Box || brush.type == Brush.Type.Slant) {
                DrawPropertiesExcluding(serializedObject, "complexity", "smooth", "mesh", "stepSeparation", "stepHeight", "addBottomStep");
            } else if (brush.type == Brush.Type.Cyllinder) {
                DrawPropertiesExcluding(serializedObject, "stepSeparation", "stepHeight", "addBottomStep", "mesh");
            } else if (brush.type == Brush.Type.BlockStair || brush.type == Brush.Type.SlantStair) {
                DrawPropertiesExcluding(serializedObject, "complexity", "smooth", "mesh", "stepHeight");
            } else if (brush.type == Brush.Type.SeparateStair) {
                DrawPropertiesExcluding(serializedObject, "complexity", "smooth", "mesh");
            } else {
                DrawPropertiesExcluding(serializedObject, "separateMaterials", "size", "textureWorldScale", "textureLocalScale", "complexity", "smooth", "stepSeparation", "stepHeight", "addBottomStep", "mesh");
            }

            serializedObject.ApplyModifiedProperties();

            if (brush.type != Brush.Type.Freeform) {
                selectedFaces.Clear();
                selectedVertices.Clear();
                selectedEdges.Clear();
            }

            if (selectedFaces.Count > 0 || selectedVertices.Count > 0 || selectedEdges.Count > 0) {
                selectedFaces.RemoveWhere(i => i < 0 || i >= brush.mesh.faces.Count);
                selectedVertices.RemoveWhere(i => i < 0 || i >= brush.mesh.vertices.Count);
                selectedEdges.RemoveWhere(e => e.t1 < 0 || e.t1 >= brush.mesh.vertices.Count || e.t2 < 0 || e.t2 >= brush.mesh.vertices.Count);
                
                HashSet<int> vertices;

                if (selectedFaces.Count > 0) {
                    vertices = brush.mesh.GetUniqueVertexList(selectedFaces);
                } else if (selectedVertices.Count > 0) {
                    vertices = selectedVertices;
                } else {
                    vertices = brush.mesh.GetUniqueVertexList(selectedEdges);
                }
                Vector3 vertexCenter = brush.mesh.GetVerticesCenter(vertices);

                if (selectedFaces.Count > 0) {
                    EditorGUILayout.LabelField("Selected Faces", EditorStyles.boldLabel);
                } else if (selectedEdges.Count > 0) {
                    EditorGUILayout.LabelField("Selected Edges", EditorStyles.boldLabel);
                } else {
                    EditorGUILayout.LabelField("Selected Vertices", EditorStyles.boldLabel);
                }
                GUILayout.BeginHorizontal();

                if (selectedFaces.Count > 0) {
                    GUILayout.BeginVertical();
                    if (GUILayout.Button("Extrude")) {
                        Extrude();
                    }

                    if (GUILayout.Button("Delete")) {
                        DeleteFaces();
                    }
                    GUILayout.EndVertical();
                } else if (selectedEdges.Count > 0) {
                    GUILayout.BeginVertical();
                    if (GUILayout.Button("Delete")) {
                        DeleteEdges();
                    }

                    if (GUILayout.Button("Create Face")) {
                        CreateFace();
                    }

                    if (GUILayout.Button("Loop Cut")) {
                        LoopCut();
                    }
                    GUILayout.EndVertical();
                } else if (selectedVertices.Count > 0) {
                    if (GUILayout.Button("Weld")) {
                        Weld();
                    }
                }
                
                GUILayout.Label("Align: ");
                GUILayout.BeginHorizontal();

                if (selectedFaces.Count > 0 && GUILayout.Button("Normal")) {
                    AlignVertices(vertices, brush.mesh.GetFacesNormal(selectedFaces));
                }

                if (GUILayout.Button("X")) {
                    AlignVertices(vertices, Vector3.right);
                }

                if (GUILayout.Button("Y")) {
                    AlignVertices(vertices, Vector3.up);
                }

                if (GUILayout.Button("Z")) {
                    AlignVertices(vertices, Vector3.forward);
                }

                GUILayout.EndHorizontal();

                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                
                Vector3 newPos = EditorGUILayout.Vector3Field("Center", vertexCenter);

                if (vertexCenter != newPos) {
                    brush.undone = true;
                    Undo.RecordObject(brush, "Brush Edit");

                    Vector3 off = newPos - vertexCenter;
                    foreach (int v in vertices) {
                        brush.mesh.vertices[v].position += off;
                    }
                    
                    brush.undone = false;
                    brush.dirty = true;
                    brush.Update();
                }

                GUILayout.Space(10);
            }

            if (!brushMode) {
                EditorGUILayout.LabelField("Brush mode is disabled! (press tab to re-enable)", EditorStyles.boldLabel);
            }

            showCommands = EditorGUILayout.Foldout(showCommands, "Keyboard Commands", true);
            if (showCommands) {
                GUILayout.Label("Toggle Brush Editor: Tab");
                GUILayout.Label("Extrude Faces: G");
                GUILayout.Label("Delete Edges/Faces: Delete");
                GUILayout.Label("Weld Vertices: Y");
                GUILayout.Label("Create Face: B");
                GUILayout.Label("Loop Cut: C");
                GUILayout.Label("Select: Left Click");
                GUILayout.Label("Select Multiple: Ctrl + Click");
                GUILayout.Label("Select Edge Loop: Double Click");
            }
        }

        private void LoopCut() {
            brush.undone = true;
            Undo.RecordObject(brush, "Loop Cut");
            if (brush.mesh.InsertLoop(selectedEdges)) {
                selectedEdges.Clear();
                brush.dirty = true;
                brush.Update();
            }
            brush.undone = false;
        }

        private void DeleteEdges() {
            brush.undone = true;
            Undo.RecordObject(brush, "Edge Deletion");
            brush.mesh.DeleteEdges(selectedEdges);
            brush.undone = false;
            brush.dirty = true;
            brush.Update();

            selectedEdges.Clear();
        }

        private void DeleteFaces() {
            brush.undone = true;
            Undo.RecordObject(brush, "Face Deletion");
            brush.mesh.DeleteFaces(selectedFaces);
            brush.undone = false;
            brush.dirty = true;
            brush.Update();

            selectedFaces.Clear();
        }

        private void Weld() {
            brush.undone = true;
            Undo.RecordObject(brush, "Vertex Weld");
            int nv = brush.mesh.WeldVertices(selectedVertices);
            selectedVertices.Clear();
            selectedVertices.Add(nv);
            brush.undone = false;
            brush.dirty = true;
            brush.Update();
        }

        private void CreateFace() {
            brush.undone = true;
            Undo.RecordObject(brush, "New Face");
            if (brush.mesh.AddFace(selectedEdges)) {
                selectedEdges.Clear();
                selectedFaces.Add(brush.mesh.faces.Count - 1);
                brush.dirty = true;
                brush.Update();
            } else {
                Debug.LogError("Create face requires a contiguous loop of edges, or exactly two edges.");
            }

            brush.undone = false;
        }

        private void Extrude() {
            brush.undone = true;
            Undo.RecordObject(brush, "Brush Extrude");
            brush.mesh.ExtrudeFaces(selectedFaces);
            brush.undone = false;
            brush.dirty = true;
            brush.Update();
        }

        private void AlignVertices(HashSet<int> vertices, Vector3 axis) {

            Vector3 center = brush.mesh.GetVerticesCenter(vertices);

            brush.undone = true;
            Undo.RecordObject(brush, "Brush Align Face");

            foreach (int i in vertices) {
                Vertex vert = brush.mesh.vertices[i];
                Vector3 off = vert.position - center;
                off = Vector3.ProjectOnPlane(off, axis);

                vert.position = center + off;
            }

            brush.undone = false;
            brush.dirty = true;
            brush.Update();
        }

        protected virtual void OnSceneGUI() {
            if (Event.current.type == EventType.MouseDown) {
                scaleMvt = Vector3.one;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftControl) {
                ctrl = true;
            } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftControl) {
                ctrl = false;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab) {
                brushMode = !brushMode;
                Event.current.Use();
                Repaint();
            }

            if (!brushMode) {
                selectedVertices.Clear();
                selectedEdges.Clear();
                selectedFaces.Clear();
            }

            if (brushMode) {
                if (brush.type != Brush.Type.Freeform) {
                    SizeHandle(Vector3.right, Vector3.right, Handles.xAxisColor);
                    SizeHandle(Vector3.up, Vector3.up, Handles.yAxisColor);
                    SizeHandle(Vector3.forward, Vector3.forward, Handles.zAxisColor);

                    SizeHandle(Vector3.right, Vector3.left, Handles.xAxisColor);
                    SizeHandle(Vector3.up, Vector3.down, Handles.yAxisColor);
                    SizeHandle(Vector3.forward, Vector3.back, Handles.zAxisColor);
                } else {
                    UpdateFreeform();
                }
            }
        }

        private void UpdateFreeform() {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            
            selectedFaces.RemoveWhere(i => i < 0 || i >= brush.mesh.faces.Count);
            selectedVertices.RemoveWhere(i => i < 0 || i >= brush.mesh.vertices.Count);
            selectedEdges.RemoveWhere(e => e.t1 < 0 || e.t1 >= brush.mesh.vertices.Count || e.t2 < 0 || e.t2 >= brush.mesh.vertices.Count);

            bool moved = false;

            if (selectedFaces.Count > 0 || selectedEdges.Count > 0 || selectedVertices.Count > 0) {

                Vector3 normalAxis, rightAxis, upAxis;
                HashSet<int> vertices;

                if (selectedFaces.Count > 0) {
                    vertices = brush.mesh.GetUniqueVertexList(selectedFaces);
                } else if (selectedVertices.Count > 0) {
                    vertices = selectedVertices;
                } else {
                    vertices = brush.mesh.GetUniqueVertexList(selectedEdges);
                }
                Vector3 vertexCenter = brush.mesh.GetVerticesCenter(vertices);

                vertexCenter = brush.transform.TransformPoint(vertexCenter);

                if (Tools.pivotRotation == PivotRotation.Local && selectedFaces.Count > 0) {
                    Vector3 normal = brush.mesh.GetFacesNormal(selectedFaces);
                    normalAxis = brush.transform.TransformDirection(normal);

                    rightAxis = Vector3.Cross(Vector3.up, normalAxis);
                    if (rightAxis.sqrMagnitude < 0.01f) {
                        rightAxis = Vector3.right;
                    } else {
                        rightAxis = rightAxis.normalized;
                    }

                    upAxis = Vector3.Cross(normalAxis, rightAxis).normalized;
                } else {
                    normalAxis = Vector3.forward;
                    rightAxis = Vector3.right;
                    upAxis = Vector3.up;
                }
                

                float hs = HandleUtility.GetHandleSize(vertexCenter);

                if (Tools.current == Tool.Move) {
                    MoveHandle(vertices, vertexCenter, Quaternion.LookRotation(normalAxis, upAxis), ref moved);
                } else if (Tools.current == Tool.Rotate) {
                    RotateHandle(vertices, vertexCenter, Quaternion.LookRotation(normalAxis, upAxis), ref moved);
                } else if (Tools.current == Tool.Scale) {
                    ScaleHandle(vertices, vertexCenter, Quaternion.LookRotation(normalAxis, upAxis), ref moved);
                }
            }
            
            if (!moved) {
                switch (Event.current.GetTypeForControl(id)) {
                    case EventType.KeyDown:
                        if (Event.current.keyCode == KeyCode.Delete) {
                            if (selectedFaces.Count > 0) {
                                DeleteFaces();
                                Event.current.Use();
                            } else if (selectedEdges.Count > 0) {
                                DeleteEdges();
                                Event.current.Use();
                            } else if (selectedVertices.Count > 0) {
                                Event.current.Use();
                            }
                        } else if (Event.current.keyCode == KeyCode.B) {
                            if (selectedEdges.Count > 0) {
                                CreateFace();
                                Event.current.Use();
                            }
                        } else if (Event.current.keyCode == KeyCode.C) {
                            if (selectedEdges.Count > 0) {
                                LoopCut();
                                Event.current.Use();
                            }
                        } else if (Event.current.keyCode == KeyCode.G) {
                            if (selectedFaces.Count > 0) {
                                Extrude();
                                Event.current.Use();
                            }
                        } else if (Event.current.keyCode == KeyCode.Y) {
                            if (selectedVertices.Count > 0) {
                                Weld();
                                Event.current.Use();
                            }
                        }
                        break;
                    case EventType.MouseDown:
                        if (Event.current.button == 0) {

                            Vector2 v = Event.current.mousePosition;
                            v.y = Camera.current.pixelHeight - v.y;

                            int selVertex, selFace;
                            UnorderedPair<int> selEdge;

                            brush.mesh.Select(brush.transform, v, Camera.current, out selVertex, out selFace, out selEdge);
                            hadSelected = selectedVertices.Count > 0 || selectedEdges.Count > 0 || selectedFaces.Count > 0;

                            if (selEdge.t1 >= 0 && Event.current.clickCount == 2 && selectedEdges.Count > 0) {
                                brush.mesh.SelectEdgeLoop(selectedEdges,selEdge);
                            } else {
                                if (selVertex >= 0) {
                                    selectedFaces.Clear();
                                    selectedEdges.Clear();
                                    if (!ctrl) {
                                        selectedVertices.Clear();
                                    }

                                    if (selectedVertices.Contains(selVertex)) {
                                        selectedVertices.Remove(selVertex);
                                    } else {
                                        selectedVertices.Add(selVertex);
                                    }
                                } else {
                                    selectedVertices.Clear();

                                    if (selEdge.t1 >= 0) {
                                        selectedFaces.Clear();
                                        if (!ctrl) {
                                            selectedEdges.Clear();
                                        }

                                        if (selectedEdges.Contains(selEdge)) {
                                            if (selectedEdges.Count > 1) {
                                                selectedEdges.Remove(selEdge);
                                            }
                                        } else {
                                            selectedEdges.Add(selEdge);
                                        }
                                    } else {
                                        selectedEdges.Clear();

                                        if (selFace >= 0) {
                                            selectedEdges.Clear();

                                            if (!ctrl) {
                                                selectedFaces.Clear();
                                            }

                                            if (selectedFaces.Contains(selFace)) {
                                                if (selectedFaces.Count > 1) {
                                                    selectedFaces.Remove(selFace);
                                                }
                                            } else {
                                                selectedFaces.Add(selFace);
                                            }
                                        } else {
                                            selectedFaces.Clear();
                                        }
                                    }
                                }
                            }
                            
                            if (selectedFaces.Count > 0 || selectedEdges.Count > 0 || selectedVertices.Count > 0 || hadSelected) {
                                GUIUtility.hotControl = id;
                                Event.current.Use();
                                EditorGUIUtility.SetWantsMouseJumping(1);

                                Repaint();
                            }
                        }
                        break;

                    case EventType.MouseUp:
                        if (Event.current.button == 0) {
                            if (selectedFaces.Count > 0 || selectedEdges.Count > 0 || selectedVertices.Count > 0 || hadSelected) {
                                GUIUtility.hotControl = 0;
                                Event.current.Use();
                                EditorGUIUtility.SetWantsMouseJumping(0);
                                hadSelected = false;

                                Repaint();
                            }
                        }
                        break;

                    case EventType.Repaint:
                        foreach (var edge in brush.mesh.GetEdges()) {

                            Vector3 v1 = brush.mesh.vertices[edge.t1].position;
                            Vector3 v2 = brush.mesh.vertices[edge.t2].position;

                            v1 = brush.transform.TransformPoint(v1);
                            v2 = brush.transform.TransformPoint(v2);

                            Handles.color = new Color(0, 0, 0, 0.1f);
                            Handles.DrawAAPolyLine(2, v1, v2);

                            var c = Handles.zTest;
                            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                            Handles.color = Color.black;
                            Handles.DrawAAPolyLine(2, v1, v2);
                            Handles.zTest = c;
                        }

                        foreach (var vert in brush.mesh.vertices) {
                            Handles.color = new Color(0, 0, 0, 0.5f);
                            Vector3 v = brush.transform.TransformPoint(vert.position);
                            Handles.DotHandleCap(0, v, Quaternion.identity, HandleUtility.GetHandleSize(v) * 0.02f, EventType.Repaint);
                        }

                        foreach (var selectedEdge in selectedEdges) {
                            Vector3 v1 = brush.mesh.vertices[selectedEdge.t1].position;
                            Vector3 v2 = brush.mesh.vertices[selectedEdge.t2].position;

                            v1 = brush.transform.TransformPoint(v1);
                            v2 = brush.transform.TransformPoint(v2);

                            Handles.color = Color.blue;
                            Handles.DrawAAPolyLine(4, v1, v2);
                        }

                        foreach (var selectedVertex in selectedVertices) {
                            Vector3 v = brush.mesh.vertices[selectedVertex].position;
                            v = brush.transform.TransformPoint(v);

                            Handles.color = Color.blue;
                            Handles.DotHandleCap(0, v, Quaternion.identity, HandleUtility.GetHandleSize(v) * 0.05f, EventType.Repaint);
                        }

                        foreach (int selectedFace in selectedFaces) {
                            Face f = brush.mesh.faces[selectedFace];

                            var points = f.GetPointsArray(brush.transform, brush.mesh);

                            Handles.color = new Color(0, 0, 1, 0.25f);
                            Handles.DrawAAConvexPolygon(points);
                            Handles.color = Color.blue;
                            Handles.DrawAAPolyLine(3, points);
                            Handles.DrawAAPolyLine(3, points[points.Length - 1], points[0]);
                        }

                        break;
                }
            }
        }

        private void MoveHandle(HashSet<int> vertices, Vector3 position, Quaternion rotation, ref bool moved) {
            Vector3 dragPos = Handles.PositionHandle(position, rotation);

            moved = dragPos != position;
            if (moved) {
                brush.undone = true;
                Undo.RecordObject(brush, "Brush Edit");
                Vector3 offset = dragPos - position;

                foreach (var vert in vertices) {
                    brush.mesh.vertices[vert].position += offset;
                }

                brush.undone = false;

                brush.dirty = true;
                brush.Update();
            }
        }
        
        private void RotateHandle(HashSet<int> vertices, Vector3 position, Quaternion rotation, ref bool moved) {
            Quaternion dragRot = Handles.RotationHandle(rotation, position);
            
            moved = dragRot != rotation;
            if (moved) {
                brush.undone = true;
                Undo.RecordObject(brush, "Brush Edit");
                Quaternion delta = dragRot * Quaternion.Inverse(rotation);

                Vector3 c = brush.mesh.GetVerticesCenter(vertices);
                foreach (var vert in vertices) {
                    Vector3 v = brush.mesh.vertices[vert].position;
                    v -= c;
                    v = delta * v;
                    v += c;
                    brush.mesh.vertices[vert].position = v;
                }

                brush.undone = false;

                brush.dirty = true;
                brush.Update();
            }
        }

        private void ScaleHandle(HashSet<int> vertices, Vector3 position, Quaternion rotation, ref bool moved) {
            float handleSize = HandleUtility.GetHandleSize(position);
            CustomHandles.HandleResult result = CustomHandles.HandleResult.None;

            Vector3 dragScale = Handles.ScaleHandle(scaleMvt, position, rotation, handleSize);
            Vector3 delta = new Vector3(dragScale.x / scaleMvt.x, dragScale.y / scaleMvt.y, dragScale.z / scaleMvt.z);
            scaleMvt = dragScale;

            moved = delta != Vector3.one;
            if (moved) {
                brush.undone = true;
                Undo.RecordObject(brush, "Brush Edit");

                Vector3 x = rotation * Vector3.right;
                Vector3 y = rotation * Vector3.up;
                Vector3 z = rotation * Vector3.forward;

                Vector3 c = brush.mesh.GetVerticesCenter(vertices);

                foreach (var vert in vertices) {
                    Vector3 v = brush.mesh.vertices[vert].position;
                    v -= c;
                    Vector3 onX = Vector3.Project(v, x);
                    Vector3 onY = Vector3.Project(v, y);
                    Vector3 onZ = Vector3.Project(v, z);

                    onX *= delta.x;
                    onY *= delta.y;
                    onZ *= delta.z;

                    v = c + onX + onY + onZ;
                    brush.mesh.vertices[vert].position = v;
                }

                brush.undone = false;

                brush.dirty = true;
                brush.Update();
            }
        }

        private void SizeHandle(Vector3 axis, Vector3 axisN, Color color) {
            Transform transform = brush.transform;
            CustomHandles.HandleResult result;

            Vector3 worldAxis = transform.TransformDirection(axisN);

            Vector3 center = transform.rotation * (brush.size / 2);

            Vector3 handlePos = transform.position + center + Vector3.Dot(axis, brush.size / 2) * worldAxis;
            float handleSize = HandleUtility.GetHandleSize(handlePos) * size;

            Handles.color = color;

            Vector3 dragPos = CustomHandles.DragHandle(handlePos, worldAxis,
                handleSize, Handles.ConeHandleCap, out result, ctrl);

            float drag = Vector3.Dot(dragPos - handlePos, worldAxis);

            float diff = -Vector3.Dot(axis, axisN);
            diff = (1.0f + diff) / 2.0f;

            if (result == CustomHandles.HandleResult.Drag) {
                brush.undone = true;
                Undo.RecordObject(brush, "Brush Edit");
                Undo.RecordObject(brush.transform, "Brush Edit");
                
                brush.transform.position += worldAxis * drag * diff;
                
                brush.size += axis * drag;
                brush.undone = false;

                if (drag != 0) {
                    brush.Update();
                }
            }
        }
    }
}
