using UnityEngine;

namespace SBR {
    class InstantProjectile : Projectile {
        public LayerMask hitMask = 1;
        public float range = Mathf.Infinity;

        public override void Fire(Vector3 direction, bool align = true) {
            base.Fire(direction, align);
            RaycastHit hit;

            if (Physics.Raycast(transform.position, direction, out hit, range, hitMask, triggerInteraction)) {
                OnHitCollider(hit.collider, hit.point, hit.normal);
            } else {
                Destroy(gameObject, linger);
            }
        }
    }
}

