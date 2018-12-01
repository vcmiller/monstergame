using UnityEngine;

namespace SBR {
    class InstantProjectile2D : Projectile2D {
        public LayerMask hitMask = 1;
        public float range = Mathf.Infinity;

        public override void Fire(Vector3 direction, bool align = true) {
            base.Fire(direction, align);
            RaycastHit2D hit;

            bool trig = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = hitsTriggers;

            if (hit = Physics2D.Raycast(transform.position, direction, range, hitMask)) {
                OnHitCollider2D(hit.collider, hit.point, hit.normal);
            } else {
                Destroy(gameObject, linger);
            }

            Physics2D.queriesHitTriggers = trig;
        }
    }
}

