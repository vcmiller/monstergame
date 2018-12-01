using UnityEngine;

namespace SBR {
    public struct Damage {
        public Damage(float amount, Vector3 point, Vector3 dir) {
            this.amount = amount;
            this.point = point;
            this.dir = dir;
        }

        public float amount;
        public Vector3 point;
        public Vector3 dir;
    }
}