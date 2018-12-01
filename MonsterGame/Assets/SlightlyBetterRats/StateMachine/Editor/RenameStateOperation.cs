using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SBR.Editor {
    public class RenameStateOperation : Operation {
        private string name;

        public RenameStateOperation(StateMachineDefinition def, StateMachineEditorWindow window, StateMachineDefinition.State state) : base(def, window, state) {
            name = state.name;
            showBaseGUI = false;
        }

        public override void Update() {
            var evt = Event.current;
            if (evt.type == EventType.KeyDown) {
                if (evt.keyCode == KeyCode.Return) {
                    Confirm();
                    done = true;
                } else if (evt.keyCode == KeyCode.Escape) {
                    Cancel();
                    done = true;
                }
            } else if (evt.type == EventType.MouseDown) {
                if (definition.SelectState(window.ToWorld(evt.mousePosition)) != state) {
                    Confirm();
                    done = true;
                }
            }
        }

        private void ensureNotEmpty() {
            if (name == null || name.Length == 0) {
                name = "Default";
            }
        }

        public override void Cancel() {
            if (state.name == null || state.name.Length == 0) {
                Confirm();
            } else {
                GUI.FocusControl("StateButton");
                GUIUtility.keyboardControl = 0;
            }
        }

        public override void Confirm() {
            Undo.RecordObject(definition, "Rename State");
            ensureNotEmpty();
            definition.RenameState(state, name != null ? name.Replace(" ", "") : name);
        }

        public override void OnGUI() {
            Rect rect = state.rect;
            rect.position = window.ToScreen(rect.position);
            name = GUI.TextField(rect, name);
        }
    }
}
