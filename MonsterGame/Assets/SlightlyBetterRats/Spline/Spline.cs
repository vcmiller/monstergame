using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Spline : MonoBehaviour {
        public SplineData spline = new SplineData();
        public event Action<bool> PropertyChanged;

        public Vector3 GetWorldPoint(float pos) {
            return transform.TransformPoint(spline.GetPoint(pos));
        }

        public Vector3 GetWoldTangent(float pos) {
            return transform.TransformVector(spline.GetTangent(pos));
        }

        public void GetWorldPoints(Vector3[] samples) {
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = GetWorldPoint(i / (samples.Length - 1.0f));
            }
        }

        private void OnValidate() {
            OnChanged(false);
        }

        public void OnChanged(bool update) {
            if (spline == null) {
                return;
            }

            spline.InvalidateSamples();

            if (PropertyChanged != null) {
                PropertyChanged(update);
            }
        }
    }

}