using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace SBR.Editor {
    public class StateMachineEditorWindow : EditorWindow {
        private Operation op;
        private bool dirty = false;
        private float _sidePanelWidth;
        private bool resize = false;
        private readonly Color bgColor = new Color32(93, 93, 93, 255);
        private readonly Color lineColor = new Color32(70, 70, 70, 255);
        private readonly Color panelColor = new Color32(194, 194, 194, 255);
        private const float editorWindowTabHeight = 21.0f;
        
        private StateMachine observing;

        [NonSerialized]
        private bool showSide = true;

        [NonSerialized]
        private string[] types;
        
        public static StateMachineDefinition def;

        [NonSerialized]
        private StateMachineDefinition.Transition lastSelectedTr;
        [NonSerialized]
        private StateMachineDefinition.State lastSelectedState;

        [NonSerialized]
        private StateMachineDefinition.State editingState;
        [NonSerialized]
        private Pair<StateMachineDefinition.State, StateMachineDefinition.Transition> editingTransition;

        public Vector2 scroll { get; private set; }
        public float zoom { get; private set; }

        public const float zoomMin = 0.5f;
        public const float zoomMax = 2f;

        private float sideWidth {
            get {
                if (Application.isPlaying) {
                    return 0;
                } else {
                    return _sidePanelWidth;
                }
            }
        }

        public StateMachineEditorWindow() {
            wantsMouseMove = true;
            editingState = null;
            editingTransition = new Pair<StateMachineDefinition.State, StateMachineDefinition.Transition>(null, null);
            zoom = 1.0f;
        }

        [MenuItem("Window/State Machine Editor")]
        public static void ShowWindow() {
            GetWindow(typeof(StateMachineEditorWindow));
        }

        void UpdateSelection() {
            Repaint();
        }

        public Vector2 ToWorld(Vector2 screen) {
            return screen + scroll / zoom;
        }

        public Vector2 ToScreen(Vector2 world) {
            return world - scroll / zoom;
        }

        private void Update() {
            if (Application.isPlaying && def) {
                Repaint();
            }
        }

        void OnGUI() {
            foreach (var obj in Selection.objects) {
                if (obj is StateMachineDefinition) {
                    def = (StateMachineDefinition)obj;
                    break;
                }
            }

            if (!Application.isPlaying) {
                ShowSidebar();
            }

            Rect viewportRect = new Rect(0, 0, position.width - sideWidth, position.height);
            EditorGUI.DrawRect(viewportRect, bgColor);

            GUI.EndGroup();
            
            Rect clippedArea = new Rect(0, 0, position.width - sideWidth, position.height);
            clippedArea.size /= zoom;
            clippedArea.y += editorWindowTabHeight / zoom;
            GUI.BeginGroup(clippedArea);

            bool repaint = false;

            if (def != null) {
                Event cur = Event.current;
                Matrix4x4 gm = GUI.matrix;
                GUI.matrix = Matrix4x4.Scale(new Vector3(zoom, zoom, 1));

                UpdateView(ref repaint);

                if (!Application.isPlaying) {
                    if (op != null) {
                        if (def != op.definition) {
                            op.Cancel();
                            op = null;
                        } else {
                            op.Update();

                            if (op.done) {
                                op = null;
                                repaint = true;
                                EditorUtility.SetDirty(def);
                                dirty = true;
                            }
                        }
                    } else {
                        var selected = def.SelectState(ToWorld(cur.mousePosition));
                        var selectedTr = def.SelectTransition(ToWorld(cur.mousePosition));

                        if (selectedTr.t1 == null) {
                            if (selected != lastSelectedState) {
                                repaint = true;
                                lastSelectedState = selected;
                            }

                            if (lastSelectedTr != null) {
                                lastSelectedTr = null;
                                repaint = true;
                            }
                        } else {
                            if (selectedTr.t2 != lastSelectedTr) {
                                repaint = true;
                                lastSelectedTr = selectedTr.t2;
                            }

                            if (lastSelectedState != null) {
                                lastSelectedState = null;
                                repaint = true;
                            }
                        }

                        if (cur.type == EventType.MouseDown) {
                            if (cur.button == 0) {
                                if (selectedTr.t1 != null) {
                                    editingTransition = selectedTr;
                                    editingState = null;
                                    repaint = true;
                                } else if (selected != null) {
                                    editingState = selected;
                                    editingTransition.t1 = null;
                                    editingTransition.t2 = null;
                                    repaint = true;

                                    if (cur.clickCount == 1) {
                                        op = new MoveStateOperation(def, this, selected);
                                    } else if (cur.clickCount == 2) {
                                        op = new RenameStateOperation(def, this, selected);
                                    }
                                } else {
                                    editingState = null;
                                    editingTransition.t1 = null;
                                    editingTransition.t2 = null;
                                    repaint = true;
                                }
                            } else if (cur.button == 1) {
                                if (selected == null && lastSelectedTr == null) {
                                    GenericMenu menu = new GenericMenu();

                                    menu.AddItem(new GUIContent("Create State"), false, () => {
                                        Undo.RecordObject(def, "Create State");
                                        var s = def.AddState();
                                        s.position = ToWorld((cur.mousePosition - Vector2.up * editorWindowTabHeight) / zoom);
                                        MoveStateOperation.Snap(ref s.position);
                                        op = new RenameStateOperation(def, this, s);
                                        EditorUtility.SetDirty(def);
                                        dirty = true;
                                    });

                                    GUI.EndGroup();
                                    menu.ShowAsContext();
                                    GUI.BeginGroup(clippedArea);
                                } else if (selectedTr.t1 != null) {
                                    GenericMenu menu = new GenericMenu();

                                    menu.AddItem(new GUIContent("Remove Transition"), false, () => {
                                        Undo.RecordObject(def, "Remove Transition");
                                        selectedTr.t1.RemoveTransition(selectedTr.t2);
                                        EditorUtility.SetDirty(def);
                                        dirty = true;
                                        repaint = true;
                                    });

                                    GUI.EndGroup();
                                    menu.ShowAsContext();
                                    GUI.BeginGroup(clippedArea);
                                } else if (selected != null) {
                                    GenericMenu menu = new GenericMenu();

                                    menu.AddItem(new GUIContent("Delete State"), false, () => {
                                        Undo.RecordObject(def, "Delete State");
                                        def.RemoveState(selected);
                                        EditorUtility.SetDirty(def);
                                        dirty = true;
                                        repaint = true;
                                    });

                                    menu.AddItem(new GUIContent("Rename State"), false, () => {
                                        op = new RenameStateOperation(def, this, selected);
                                    });

                                    menu.AddItem(new GUIContent("Add Transition"), false, () => {
                                        op = new MakeTransitionOperation(def, this, selected);
                                    });

                                    StateMachineDefinition.State parent = def.GetState(selected.parent);

                                    if (parent == null) {
                                        if (selected.name != def.defaultState) {
                                            menu.AddItem(new GUIContent("Make Default State"), false, () => {
                                                Undo.RecordObject(def, "Set Default State");
                                                def.defaultState = selected.name;
                                                dirty = true;
                                                repaint = true;
                                            });
                                        }
                                    } else {
                                        if (selected.name != parent.localDefault) {
                                            menu.AddItem(new GUIContent("Make Local Default"), false, () => {
                                                Undo.RecordObject(def, "Set Local Default");
                                                parent.localDefault = selected.name;
                                                dirty = true;
                                                repaint = true;
                                            });
                                        }
                                    }

                                    if (selected.hasChildren) {
                                        menu.AddItem(new GUIContent("Remove Sub-Machine"), false, () => {
                                            Undo.RecordObject(def, "Remove Sub-Machine");
                                            def.RemoveSub(selected, MoveStateOperation.snap * 2);
                                        });

                                        menu.AddItem(new GUIContent("Create Child State"), false, () => {
                                            Undo.RecordObject(def, "Create Child State");
                                            var s = def.AddState();
                                            s.position = ToWorld((cur.mousePosition - Vector2.up * editorWindowTabHeight) / zoom);
                                            def.SetStateParent(s, selected, MoveStateOperation.snap);
                                            MoveStateOperation.Snap(ref s.position);
                                            op = new RenameStateOperation(def, this, s);
                                            EditorUtility.SetDirty(def);
                                            dirty = true;
                                        });
                                    } else {
                                        menu.AddItem(new GUIContent("Make Sub-Machine"), false, () => {
                                            Undo.RecordObject(def, "Make Sub-Machine");
                                            def.CreateSub(selected, MoveStateOperation.snap * 2);
                                        });
                                    }

                                    GUI.EndGroup();
                                    menu.ShowAsContext();
                                    GUI.BeginGroup(clippedArea);
                                }
                            }
                        } else if (cur.type == EventType.KeyDown) {
                            if (cur.keyCode == KeyCode.Delete) {
                                if (selected != null) {
                                    Undo.RecordObject(def, "Delete State");
                                    def.RemoveState(selected);
                                    EditorUtility.SetDirty(def);
                                    dirty = true;
                                    repaint = true;
                                } else if (selectedTr.t1 != null) {
                                    Undo.RecordObject(def, "Remove Transition");
                                    selectedTr.t1.RemoveTransition(selectedTr.t2);
                                    EditorUtility.SetDirty(def);
                                    dirty = true;
                                    repaint = true;
                                }
                            }
                        } else if (cur.type == EventType.ValidateCommand) {
                            editingState = selected = lastSelectedState = null;
                            editingTransition = selectedTr = new Pair<StateMachineDefinition.State, StateMachineDefinition.Transition>();
                            lastSelectedTr = null;
                            dirty = true;
                            repaint = true;
                        }
                    }
                }

                if (Event.current.type != EventType.Repaint && (repaint || (op != null && op.repaint))) {
                    Repaint();
                }

                Handles.BeginGUI();

                Handles.color = lineColor;
                Vector2 sz = scroll / zoom;

                for (float x = -sz.x % MoveStateOperation.snap; x < clippedArea.width; x += MoveStateOperation.snap) {
                    Handles.DrawLine(new Vector3(x, 0), new Vector3(x, clippedArea.height));
                }

                for (float y = -sz.y % MoveStateOperation.snap; y < clippedArea.height; y += MoveStateOperation.snap) {
                    Handles.DrawLine(new Vector3(0, y), new Vector3(clippedArea.width, y));
                }

                if (Application.isPlaying) {
                    if (observing == null || observing.gameObject != Selection.activeGameObject) {
                        if (Selection.activeGameObject) {
                            observing = Selection.activeGameObject.GetComponent<StateMachine>();
                        }

                        if (observing == null) {
                            observing = (StateMachine)FindObjectOfType(typeof(StateMachine).Assembly.GetType(def.name));
                        }
                    }
                } else if (!Application.isPlaying) {
                    observing = null;
                }
                Color oldColor = GUI.color;

                if (def.states != null) {
                    EditorGUI.BeginDisabledGroup(op != null && op is RenameStateOperation);
                    foreach (var state in def.states) {
                        if (op == null || op.state != state || op.showBaseGUI) {
                            string s = state.name;
                            var parent = def.GetState(state.parent);
                            if (parent == null && def.defaultState == s) {
                                s += "\n<default state>";
                            } else if (parent != null && parent.localDefault == s) {
                                s += "\n<local default>";
                            }

                            Rect rect = state.rect;
                            rect.position = ToScreen(rect.position);

                            GUI.SetNextControlName("StateButton");
                            
                            if (observing && cur.type == EventType.Repaint) {
                                if (observing.IsStateActive(state.name)) {
                                    GUI.color = new Color(0.5f, 0.7f, 1.0f);
                                } else if (observing.IsStateRemembered(state.name)) {
                                    GUI.color = new Color(0.9f, 0.8f, 1.0f);
                                } else {
                                    GUI.color = oldColor;
                                }
                            }

                            if (!state.hasChildren) {
                                if (state != editingState) {
                                    GUI.Button(rect, s);
                                } else {
                                    GUI.Button(rect, "");

                                    var style = new GUIStyle(GUI.skin.label);
                                    style.alignment = TextAnchor.MiddleCenter;
                                    style.normal.textColor = Color.blue;
                                    style.fontStyle = FontStyle.Bold;

                                    GUI.Label(rect, s, style);
                                }
                            } else {
                                GUI.Button(rect, "");

                                var style = new GUIStyle(GUI.skin.label);
                                style.alignment = TextAnchor.UpperCenter;

                                if (state == editingState) {
                                    style.normal.textColor = Color.blue;
                                    style.fontStyle = FontStyle.Bold;
                                }

                                GUI.Label(rect, s, style);

                                Rect innerRect = rect;

                                innerRect.xMin += 4;
                                innerRect.xMax -= 4;

                                innerRect.yMin += 32;
                                innerRect.yMax -= 4;

                                GUI.color = oldColor;
                                EditorGUI.DrawRect(innerRect, bgColor);

                                Handles.color = lineColor;
                                for (float x = rect.xMin + MoveStateOperation.snap; x < rect.xMax; x += MoveStateOperation.snap) {
                                    Handles.DrawLine(new Vector3(x, innerRect.yMin), new Vector3(x, innerRect.yMax));
                                }

                                for (float y = rect.yMin + MoveStateOperation.snap * 2; y < rect.yMax; y += MoveStateOperation.snap) {
                                    Handles.DrawLine(new Vector3(innerRect.xMin, y), new Vector3(innerRect.xMax, y));
                                }
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    if (op != null) {
                        op.OnGUI();
                    }

                    foreach (var from in def.states) {
                        if (from.transitions != null) {
                            foreach (var tr in from.transitions) {
                                if (Application.isPlaying) {
                                    Handles.color = Color.black;
                                    if (observing) {
                                        float t = Time.unscaledTime - observing.TransitionLastTime(from.name, tr.to);
                                        if (t < 0.5f) {
                                            Handles.color = Color.Lerp(Color.black, Color.green, 1.0f - t * 2);
                                        }
                                    }
                                } else {
                                    if (tr != lastSelectedTr && tr != editingTransition.t2) {
                                        Handles.color = Color.black;
                                    } else {
                                        Handles.color = Color.blue;
                                    }
                                }

                                if (def.GetState(tr.to) != null) {
                                    var line = def.GetTransitionPoints(from, tr);
                                    Vector2 src = ToScreen(line.t1);
                                    Vector2 dest = ToScreen(line.t2);

                                    Vector2 v = (dest - src).normalized;
                                    Vector2 ortho = new Vector2(v.y, -v.x);

                                    Vector2 arrow = ortho - v;
                                    Vector2 mid = (src + dest) / 2;

                                    Handles.DrawAAPolyLine(3, src, dest);
                                    Handles.DrawAAPolyLine(3, mid + v * 5, mid + arrow * 6);
                                }
                            }
                        }
                    }
                }
                
                Handles.EndGUI();

                GUI.matrix = gm;
            } else if (op != null) {
                op.Cancel();
                op = null;
            }
        }

        private void UpdateView(ref bool repaint) {
            Rect viewportRect = new Rect(0, 0, position.width - sideWidth, position.height);
            var cur = Event.current;
            if (cur.type == EventType.MouseDrag && cur.button == 2) {
                scroll -= cur.delta * zoom;
                repaint = true;
            } else if (cur.type == EventType.KeyDown && cur.keyCode == KeyCode.F && !(op is RenameStateOperation)) {
                var state = def.GetState(def.defaultState);
                if (state == null && def.states.Count > 0) {
                    state = def.states[0];
                }

                if (state != null) {
                    scroll = state.position - viewportRect.size / 2 + state.size / 2;
                    repaint = true;
                    zoom = 1;
                } else {
                    scroll = Vector2.zero;
                    repaint = true;
                    zoom = 1;
                }
            } else if (cur.isScrollWheel) {
                Vector2 mw = ToWorld(cur.mousePosition);
                Vector2 swo = scroll / zoom;
                Vector2 wo = swo - mw;

                float oz = zoom;
                zoom *= Mathf.Pow(1.1f, -cur.delta.y);
                zoom = Mathf.Clamp(zoom, zoomMin, zoomMax);

                wo *= (oz / zoom);

                scroll = (wo + mw) * zoom;

                repaint = true;
            }
        }

        private void ShowSidebar() {
            ResizeScrollView();

            if (def && showSide) {
                float padding = 20;
                EditorGUI.DrawRect(new Rect(position.width - _sidePanelWidth, 0, _sidePanelWidth, position.height), panelColor);
                GUILayout.BeginArea(new Rect(position.width - _sidePanelWidth + padding, padding, _sidePanelWidth - padding * 2, position.height - padding * 2));
                EditorGUILayout.BeginVertical();
                float w = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 1;

                if (GUILayout.Button(dirty ? "Save *" : "Save")) {
                    AssetDatabase.SaveAssets();
                    dirty = false;
                    StateMachineClassGenerator.GenerateAbstractClass(def);
                }

                if (GUILayout.Button("New Impl Class...")) {
                    var path = EditorUtility.SaveFilePanelInProject("Save new class", def.name + "Impl.cs", "cs", "Enter a name for the new impl class");
                    if (path.Length > 0) {
                        StateMachineClassGenerator.GenerateImplClass(def, path);
                    }
                }

                if (types == null) {
                    types = typeof(StateMachine).Assembly.GetTypes()
                        .Where(p => !p.IsGenericType && typeof(StateMachine).IsAssignableFrom(p))
                        .Select(t => t.FullName).ToArray();
                }

                int index = Array.IndexOf(types, def.baseClass);
                if (index < 0) {
                    index = Array.IndexOf(types, typeof(StateMachine).FullName);
                }
                int prev = index;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Base Class");
                index = EditorGUILayout.Popup(index, types);

                if (prev != index) {
                    dirty = true;
                }

                EditorGUILayout.EndHorizontal();
                def.baseClass = types[index];

                if (editingState != null) {
                    EditorGUILayout.LabelField("State " + editingState.name);
                    EditorGUILayout.LabelField("State Events", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Enter: ");
                    bool enter = EditorGUILayout.Toggle(editingState.hasEnter);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("During: ");
                    bool during = EditorGUILayout.Toggle(editingState.hasDuring);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Exit: ");
                    bool exit = EditorGUILayout.Toggle(editingState.hasExit);
                    EditorGUILayout.EndHorizontal();


                    if (enter != editingState.hasEnter || during != editingState.hasDuring || exit != editingState.hasExit) {
                        Undo.RecordObject(def, "Edit State");
                        editingState.hasEnter = enter;
                        editingState.hasDuring = during;
                        editingState.hasExit = exit;

                        dirty = true;
                        EditorUtility.SetDirty(def);
                    }

                } else if (editingTransition.t1 != null) {
                    EditorGUILayout.LabelField("Transition From " + editingTransition.t1.name + " To " + editingTransition.t2.to);
                    EditorGUILayout.LabelField("Transition Events", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Notify: ");
                    bool notify = EditorGUILayout.Toggle("Notify: ", editingTransition.t2.hasNotify);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Exit Time: ");
                    float exitTime = EditorGUILayout.FloatField(editingTransition.t2.exitTime);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Mode: ");
                    var mode = (StateMachineDefinition.TransitionMode)EditorGUILayout.EnumPopup(editingTransition.t2.mode);
                    EditorGUILayout.EndHorizontal();

                    if (notify != editingTransition.t2.hasNotify || exitTime != editingTransition.t2.exitTime || mode != editingTransition.t2.mode) {
                        Undo.RecordObject(def, "Edit Transition");
                        editingTransition.t2.hasNotify = notify;
                        editingTransition.t2.exitTime = exitTime;
                        editingTransition.t2.mode = mode;

                        dirty = true;
                        EditorUtility.SetDirty(def);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUIUtility.labelWidth = w;

                GUILayout.EndArea();
            }
        }

        private void OnEnable() {
            _sidePanelWidth = 200;

            Selection.selectionChanged += UpdateSelection;
            titleContent = new GUIContent("SM Editor", Resources.Load<Texture2D>("StateMachine"));
        }

        private void OnDisable() {
            Selection.selectionChanged -= UpdateSelection;
        }

        private void ResizeScrollView() {
            Rect cursorChangeRect = new Rect(position.width - _sidePanelWidth - 5.0f, 0, 10f, position.height);

            EditorGUIUtility.AddCursorRect(cursorChangeRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && cursorChangeRect.Contains(Event.current.mousePosition)) {
                resize = true;
            }
            if (resize) {
                _sidePanelWidth = position.width - Event.current.mousePosition.x;
                Repaint();
            }
            if (Event.current.type == EventType.MouseUp)
                resize = false;
        }
    }
}