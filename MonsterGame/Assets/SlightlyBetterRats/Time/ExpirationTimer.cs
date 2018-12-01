
using UnityEngine;

namespace SBR {
    public class ExpirationTimer {
        public float expiration { get; set; }
        public float lastSet { get; set; }
        public bool unscaled { get; set; }

        private float curTime {
            get {
                return unscaled ? Time.unscaledTime : Time.time;
            }
        }

        private float deltaTime {
            get {
                return unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
        }

        public bool expired {
            get {
                return curTime >= lastSet + expiration;
            }
        }

        public bool expiredThisFrame {
            get {
                return expired && curTime - deltaTime <= lastSet + expiration;
            }
        }

        public float remaining {
            get {
                return Mathf.Max(0, expiration - (curTime - lastSet));
            }
        }

        public float remainingRatio {
            get {
                if (expired) {
                    return 0;
                } else {
                    return 1 - ((curTime - lastSet) / expiration);
                }
            }
        }

        public ExpirationTimer(float expiration) {
            this.expiration = expiration;
            Clear();
        }

        public void Set() {
            lastSet = curTime;
        }

        public void Clear() {
            lastSet = curTime - expiration;
        }
    }
}