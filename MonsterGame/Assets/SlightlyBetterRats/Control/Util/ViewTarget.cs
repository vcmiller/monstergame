using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class ViewTarget : MonoBehaviour {
        new public Camera camera { get; private set; }
        public AudioListener listener { get; private set; }

        private void Awake() {
            camera = GetComponent<Camera>();
            listener = GetComponent<AudioListener>();
        }

        private void OnEnable() {
            if (camera) {
                camera.enabled = true;
            }

            if (listener) {
                listener.enabled = true;
            }
        }

        private void OnDisable() {
            if (camera) {
                camera.enabled = false;
            }

            if (listener) {
                listener.enabled = false;
            }
        }
    }
}
