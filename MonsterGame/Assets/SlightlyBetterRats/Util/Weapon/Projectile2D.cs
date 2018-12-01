using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Projectile2D : Projectile {
        public override bool hitsTriggers {
            get {
                return triggerInteraction == QueryTriggerInteraction.Collide ||
                    (triggerInteraction == QueryTriggerInteraction.UseGlobal && Physics2D.queriesHitTriggers);
            }
        }

        protected virtual void OnHitCollider2D(Collider2D col, Vector2 position, Vector2 normal) {
            if (fired || hitsIfNotFired) {
                if (hitsTriggers || !col.isTrigger) {
                    OnHitObject(col.transform, position, normal);
                }
            }
        }

        public override Vector3 gravityVector {
            get {
                return gravity * Physics2D.gravity;
            }
        }
    }
}