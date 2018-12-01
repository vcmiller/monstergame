using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SBR.Editor {
    public abstract class Operation {
        public bool _repaint;

        public bool done { get; protected set; }
        public bool repaint {
            get {
                bool b = _repaint;
                _repaint = false;
                return b;
            }

            protected set {
                _repaint = value;
            }
        }
        public StateMachineDefinition definition { get; private set; }
        public StateMachineEditorWindow window { get; private set; }
        public StateMachineDefinition.State state { get; private set; }
        public bool showBaseGUI { get; protected set; }

        public Operation(StateMachineDefinition definition, StateMachineEditorWindow window, StateMachineDefinition.State state) {
            this.definition = definition;
            this.state = state;
            this.window = window;
        }

        public abstract void Update();
        public abstract void Cancel();
        public abstract void Confirm();
        public abstract void OnGUI();
    }
}
