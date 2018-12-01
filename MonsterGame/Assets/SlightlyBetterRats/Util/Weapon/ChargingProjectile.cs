using SBR;
using UnityEngine;

public class ChargingProjectile : Projectile {
    public float delay;
    public Projectile subProjectile;
    public bool destroyAfterFiring = true;
    public AudioParameters shootSound;
    public bool parent = true;
    public bool fireAtTarget = true;

    private ExpirationTimer delayTimer;

    private Vector3 targetPoint;

    public override void Fire(Vector3 direction, bool align = true) {
        base.Fire(direction, align);

        delayTimer = new ExpirationTimer(delay);
        delayTimer.Set();

        targetPoint = transform.position + direction;
    }

    private void Update() {
        transform.forward = targetPoint - transform.position;
        if (fired && delayTimer.expired) {
            delayTimer = null;
            fired = false;

            var proj = Instantiate(subProjectile, transform.position, subProjectile.transform.rotation);
            if (parent) {
                proj.transform.parent = transform;
            }

            if (fireAtTarget) {
                proj.Fire(targetPoint - transform.position);
            } else {
                proj.Fire();
            }

            if (shootSound) {
                shootSound.PlayAtPoint(transform.position);
            }

            if (destroyAfterFiring) {
                Destroy(gameObject, linger);
            }
        }
    }
}
