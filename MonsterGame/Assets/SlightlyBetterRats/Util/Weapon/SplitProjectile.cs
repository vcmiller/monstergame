using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SBR;

namespace SBR {
    public class SplitProjectile : ColliderProjectile {
        public float splitTime;
        public Projectile subProjectile;
        public int spawnCount;
        public float startAngle;
        public AudioParameters sound;
        public bool startAngleRelative;

        private ExpirationTimer timer;

        // Use this for initialization
        void Start() {
            timer = new ExpirationTimer(splitTime);
            timer.Set();
        }

        public override void Fire(Vector3 forwad, bool align = true) {
            base.Fire(forwad, align);
        }

        // Update is called once per frame
        protected override void Update() {
            base.Update();

            float start = startAngle;
            if (startAngleRelative) {
                start += transform.eulerAngles.y;
            }

            if (timer.expired && fired) {
                float angleStep = 360.0f / spawnCount;
                for (int i = 0; i < spawnCount; i++) {
                    Quaternion rot = Quaternion.Euler(0, start + angleStep * i, 0);
                    Instantiate(subProjectile, transform.position, rot).Fire();
                    if (sound) {
                        sound.PlayAtPoint(transform.position);
                    }
                }
                Destroy(gameObject);
            }
        }
    }

}