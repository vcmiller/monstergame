using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class ColliderProjectile : Projectile {
        protected virtual void Update() {
            velocity += gravityVector * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision) {
            OnHitCollider(collision.collider, transform.position, velocity.normalized);
        }

        private void OnTriggerEnter(Collider other) {
            OnHitCollider(other, transform.position, velocity.normalized);
        }
    }

}