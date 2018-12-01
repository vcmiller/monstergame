using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SBR {
    public abstract class Controller : MonoBehaviour, IEventSystemHandler {
        public Channels channels { get; private set; }

        private bool isEnabled;
        
        public virtual void Initialize() {
            var brain = GetComponent<Brain>();
            if (brain) {
                channels = brain.channels;
            }
            enabled = false;
        }

        public virtual void GetInput() { }
    }

    public abstract class Controller<T> : Controller where T : Channels{
        public new T channels { get; private set; }

        public override void Initialize() {
            base.Initialize();
            channels = base.channels as T;
        }
    }
}
