using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Projectile : MonoBehaviour {
        [Tooltip("Speed that the projectile is fired at.")]
        public float launchSpeed;

        [Tooltip("How much damage to do if the projectile hits an object with a Health component.")]
        public float damage;

        [Tooltip("Whether the projectile should hit triggers.")]
        public QueryTriggerInteraction triggerInteraction;

        [Tooltip("Whether the projectile should be able to hit objects before it is fired.")]
        public bool hitsIfNotFired;

        [Tooltip("How long the projectile should last after it hits, if destroyOnHit is true.")]
        public float linger;

        [Tooltip("Multiplier for Physics.gravity or Physics2D.gravity.")]
        public float gravity;

        [Tooltip("Whether the projectile should destroy its GameObject on impact.")]
        public bool destroyOnHit = true;

        [Tooltip("Prefab to spawn on impact, such as an explosion.")]
        public GameObject impactPrefab;

        [Tooltip("Sound to play on impact.")]
        public AudioParameters impactSound;

        public Vector3 velocity { get; set; }
        public bool fired { get; protected set; }

        public virtual bool hitsTriggers {
            get {
                return triggerInteraction == QueryTriggerInteraction.Collide || 
                    (triggerInteraction == QueryTriggerInteraction.UseGlobal && Physics.queriesHitTriggers);
            }
        }

        public virtual Vector3 gravityVector {
            get {
                return gravity * Physics.gravity;
            }
        }

        public virtual void Fire() {
            Fire(transform.forward, false);
        }

        public virtual void Fire(Vector3 direction, bool align = true) {
            velocity = direction.normalized * launchSpeed;
            if (align) {
                transform.forward = direction;
            }
            fired = true;
        }

        protected virtual void OnHitCollider(Collider col, Vector3 position, Vector3 normal) {
            if (fired || hitsIfNotFired) {
                if (hitsTriggers || !col.isTrigger) {
                    OnHitObject(col.transform, position, normal);
                }
            }
        }

        protected virtual void OnHitObject(Transform col, Vector3 position, Vector3 normal) {
            col.Damage(damage, position, normal);

            velocity = Vector3.zero;
            transform.position = position;

            if (destroyOnHit) {
                Destroy(gameObject, linger);
            }

            if (impactSound) {
                impactSound.PlayAtPoint(transform.position);
            }
            if (impactPrefab) {
                Instantiate(impactPrefab, position, transform.rotation);
            }
        }
    }
}