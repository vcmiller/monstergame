using UnityEngine;
using UnityEditor;

namespace SBR.Editor {
    public class CustomHandles {
        // internal state for DragHandle()
        static int hash = "DragHandleHash".GetHashCode();
        static Vector3 mouseDragStart;
        static Vector3 handleWorldStart;
        static bool handleHasMoved;
        static int hovered = 0;

        public enum HandleResult {
            None = 0,

            Press,
            Drag,
            Release
        };

        public static Vector3 DragHandle(Vector3 position, Vector3 axis, float handleSize, Handles.CapFunction capFunc, out HandleResult result, bool snap = false, float drawOffset = 0) {
            int id = GUIUtility.GetControlID(hash, FocusType.Passive);
            
            result = HandleResult.None;

            switch (Event.current.GetTypeForControl(id)) {
                case EventType.MouseMove:
                    hovered = HandleUtility.nearestControl;
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && (Event.current.button == 0)) {
                        GUIUtility.hotControl = id;
                        mouseDragStart = MousePos(position, axis);
                        handleWorldStart = position;
                        handleHasMoved = false;

                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        
                        result = HandleResult.Press;
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 1)) {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        
                        result = HandleResult.Release;
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        
                        if (!Mathf.Approximately(Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, axis)), 1)) {
                            Vector3 mouseDragCurrent = MousePos(handleWorldStart, axis);

                            position = handleWorldStart + (mouseDragCurrent - mouseDragStart);

                            Vector3 translation = position - handleWorldStart;

                            float amount = Vector3.Dot(translation, axis);

                            if (snap) {
                                amount = Mathf.Round(amount);
                            }

                            position = axis * amount + handleWorldStart;
                        }
                        
                        result = HandleResult.Drag;

                        handleHasMoved = true;

                        GUI.changed = true;
                        Event.current.Use();
                    }
                    break;

                case EventType.Repaint:
                    Color currentColour = Handles.color;
                    if (id == GUIUtility.hotControl && handleHasMoved) {
                        Handles.color = Handles.selectedColor;
                    } else if (hovered == id) {
                        Handles.color = Handles.preselectionColor;
                    }
                    
                    capFunc(id, position + axis * drawOffset, Quaternion.LookRotation(axis), handleSize, EventType.Repaint);

                    Handles.color = currentColour;
                    break;

                case EventType.Layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + axis * drawOffset, handleSize / 2));
                    break;
            }

            return position;
        }

        private static Vector3 MousePos(Vector3 point, Vector3 axis) {
            Vector3 c = Vector3.Cross(Camera.current.transform.forward, axis);
            Plane plane = new Plane(Vector3.Cross(c, axis), point);

            Vector2 v = Event.current.mousePosition;
            v.y = Camera.current.pixelHeight - v.y;

            Ray ray = Camera.current.ScreenPointToRay(v);
            float dist;

            if (plane.Raycast(ray, out dist)) {
                return ray.GetPoint(dist);
            } else {
                return Vector3.zero;
            }
        }
    }
}