using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class PointProjectile2D : Projectile2D {
        [Tooltip("Layers that the projectile collides with.")]
        public LayerMask hitMask = 1;

        [Tooltip("Extra length to raycast each frame, to compensate for other objects' velocity.")]
        public float offset = 0;

        private void Update() {
            if (velocity.sqrMagnitude > 0) {
                Vector3 oldPosition = transform.position;
                transform.position += velocity * Time.deltaTime;

                RaycastHit2D hit;

                bool trig = Physics2D.queriesHitTriggers;
                Physics2D.queriesHitTriggers = hitsTriggers;

                if (hit = Physics2D.Linecast(oldPosition - velocity.normalized * offset, transform.position, hitMask)) {
                    OnHitCollider2D(hit.collider, hit.point, hit.normal);
                }

                Physics2D.queriesHitTriggers = trig;
            }
        }
    }
}