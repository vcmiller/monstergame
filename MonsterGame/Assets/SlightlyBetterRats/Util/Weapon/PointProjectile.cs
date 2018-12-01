using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class PointProjectile : Projectile {
        [Tooltip("Layers that the projectile collides with.")]
        public LayerMask hitMask = 1;

        [Tooltip("Extra length to raycast each frame, to compensate for other objects' velocity.")]
        public float offset = 0;

        private void Update() {
            velocity += gravityVector * Time.deltaTime;

            if (velocity.sqrMagnitude > 0) {
                Vector3 oldPosition = transform.position;
                transform.position += velocity * Time.deltaTime;

                RaycastHit hit;

                if (Physics.Linecast(oldPosition - velocity.normalized * offset, transform.position, out hit, hitMask, triggerInteraction)) {
                    OnHitCollider(hit.collider, hit.point, velocity.normalized);
                }
            }
        }
    }
}