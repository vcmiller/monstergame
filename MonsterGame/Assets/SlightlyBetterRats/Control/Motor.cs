using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public abstract class Motor : MonoBehaviour {
        public bool enableInput { get; set; }

        protected virtual void Start() {
            enableInput = true;
        }

        public abstract void TakeInput();

        public virtual void UpdateAfterInput() { }
    }

    public abstract class Motor<T> : Motor where T : Channels {
        public T channels { get; private set; }

        protected override void Start() {
            base.Start();
            Brain b = GetComponentInParent<Brain>();
            if (b) {
                channels = b.channels as T;
            }
        }
    }
}