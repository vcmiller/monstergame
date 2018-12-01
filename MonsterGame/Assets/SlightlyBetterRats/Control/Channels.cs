using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Channels {
        private Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        private class Channel {
            public Channel(object def, bool clears) {
                value = defaultValue = def;
                this.clears = clears;
            }

            public void Set(object val) {
                value = val;
            }

            public object Get() {
                return value;
            }

            public void Clear() {
                if (clears) {
                    value = defaultValue;
                }
            }

            private object defaultValue;
            private object value;
            private bool clears;
        }

        protected void RegisterInputChannel(string name, object defaultValue, bool clears) {
            channels[name] = new Channel(defaultValue, clears);
        }

        public void SetInput(string name, object value) {
            channels[name].Set(value);
        }

        public T GetInput<T>(string name) {
            return (T)channels[name].Get();
        }

        public void SetFloat(string name, float value, float min = Mathf.NegativeInfinity, float max = Mathf.Infinity) {
            SetInput(name, Mathf.Clamp(value, min, max));
        }

        public float GetFloat(string name) {
            return GetInput<float>(name);
        }

        public void SetVector(string name, Vector3 value, float maxLength = Mathf.Infinity) {
            if (value.sqrMagnitude > maxLength * maxLength) {
                value = value.normalized * maxLength;
            }

            SetInput(name, value);
        }

        public Vector3 GetVector(string name) {
            return GetInput<Vector3>(name);
        }

        public void SetBool(string name, bool value) {
            SetInput(name, value);
        }

        public bool GetBool(string name) {
            return GetInput<bool>(name);
        }

        public void SetInt(string name, int value, int min = int.MinValue, int max = int.MaxValue) {
            SetInput(name, Mathf.Clamp(value, min, max));
        }

        public int GetInt(string name) {
            return GetInput<int>(name);
        }

        public void SetQuaternion(string name, Quaternion value) {
            SetInput(name, value);
        }

        public Quaternion GetQuaternion(string name) {
            return GetInput<Quaternion>(name);
        }

        public void ClearInput() {
            foreach (var input in channels) {
                input.Value.Clear();
            }
        }
    }
}