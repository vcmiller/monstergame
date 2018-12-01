
using System;
using UnityEngine;

namespace SBR {
    public class CooldownTimer {
        public float cooldown { get; set; }
        public float lastUse { get; set; }
        public bool unscaled { get; private set; }

        private float curTime {
            get {
                return unscaled ? Time.unscaledTime : Time.time;
            }
        }

        public float chargeRatio {
            get {
                if (curTime - lastUse > cooldown) {
                    return 1.0f;
                } else {
                    return (curTime - lastUse) / cooldown;
                }
            }
        }

        public bool canUse {
            get {
                return curTime - lastUse > cooldown;
            }
        }

        public CooldownTimer(float cooldown) {
            this.cooldown = cooldown;
            this.lastUse = curTime;
        }

        public CooldownTimer(float cooldown, float initial) {
            this.cooldown = cooldown;
            this.lastUse = curTime - cooldown + initial;
        }

        public bool Use() {
            if (curTime - lastUse > cooldown) {
                lastUse = curTime;
                return true;
            } else {
                return false;
            }
        }

        public void Clear() {
            lastUse = curTime - cooldown;
        }

        public void Reset() {
            lastUse = curTime;
        }
    }
}

