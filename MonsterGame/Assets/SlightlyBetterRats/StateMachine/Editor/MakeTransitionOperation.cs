using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SBR.Editor {
    public class MakeTransitionOperation : Operation {
        private Vector2 target;

        public MakeTransitionOperation(StateMachineDefinition def, StateMachineEditorWindow window, StateMachineDefinition.State state) : base(def, window, state) {
            showBaseGUI = true;
        }

        public override void Update() {
            var evt = Event.current;
            if (evt.type == EventType.MouseMove) {
                target = evt.mousePosition;
                repaint = true;
            } else if (evt.type == EventType.MouseDown) {
                var targ = definition.SelectState(window.ToWorld(target));

                if (evt.button == 0 && targ != null && targ != state) {
                    done = true;
                    Confirm();
                } else {
                    done = true;
                    Cancel();
                }
            }
        }

        public override void Cancel() {
        }

        public override void Confirm() {
            Undo.RecordObject(definition, "Add Transition");
            var targ = definition.SelectState(window.ToWorld(target));

            if (definition.TransitionValid(state, targ)) {
                state.AddTransition(targ);
            }
        }

        public override void OnGUI() {
            Handles.BeginGUI();

            Vector2 src = window.ToScreen(state.center);

            var targ = definition.SelectState(window.ToWorld(target));

            if (targ == null) {
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(3, src, target);
            } else if (targ != state) {
                if (definition.TransitionValid(state, targ)) {
                    Handles.color = Color.black;
                } else {
                    Handles.color = Color.red;
                }

                var fake = new StateMachineDefinition.Transition();
                fake.to = targ.name;
                var line = definition.GetTransitionPoints(state, fake);

                Handles.DrawAAPolyLine(3, window.ToScreen(line.t1), window.ToScreen(line.t2));
            }

            Handles.EndGUI();
        }
    }
}