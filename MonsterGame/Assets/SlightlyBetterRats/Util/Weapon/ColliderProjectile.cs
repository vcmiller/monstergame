using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class ColliderProjectile : Projectile {
        protected virtual void Update() {
            transform.position += velocity * Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision) {
            OnHitCollider(collision.collider, transform.position, velocity);
        }

        private void OnTriggerEnter(Collider other) {
            OnHitCollider(other, transform.position, velocity);
        }
    }

}