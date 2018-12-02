using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeechHand : MonoBehaviour {
    public ParticleSystem smoke;
    public ParticleSystem flakes;
    public ParticleSystem embers;

    public float fireTime = 0.1f;
    public LayerMask hitmask;
    public float damage;
    public float healing;
    public float killHealing;
    public float range;

    private ParticleSystem.EmissionModule smokeEm;
    private ParticleSystem.EmissionModule flakesEm;
    private ParticleSystem.EmissionModule embersEm;
    private Camera cam;

    private ExpirationTimer fireTimer;
    private bool didHit;
    private RaycastHit hit;

	// Use this for initialization
	void Start () {
        smokeEm = smoke.emission;
        flakesEm = flakes.emission;
        embersEm = embers.emission;
        fireTimer = new ExpirationTimer(fireTime);
        cam = GetComponentInParent<Camera>();
    }

    void Fire()
    {
        fireTimer.Set();

        didHit = false;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, range, hitmask))
        {
            var h = hit.transform.root.GetComponent<Health>();
            didHit = hit.transform.CompareTag("Enemy");
            if (h && !h.hasDied)
            {
                var edr = h.GetComponent<EnemyDamageResponse>();
                h.Damage(damage);
                transform.root.Heal(healing);
                HitMarker.inst.Set();
                if (h.hasDied)
                {
                    transform.root.Heal(killHealing);

                    if (edr)
                    {
                        edr.corpse.GetComponent<RagdollDisintegrate>().Die();
                    }
                }
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        smokeEm.enabled = !fireTimer.expired;
        flakesEm.enabled = embersEm.enabled = (!fireTimer.expired &&didHit);
        if (didHit)
        {
            smoke.transform.position = hit.point;
            flakes.transform.position = hit.point;
            var shape = embers.shape;
            shape.position = Vector3.forward * hit.distance;
            var main = embers.main;
            float f = hit.distance;
            main.startLifetime = new ParticleSystem.MinMaxCurve(f * 0.1f, f * 0.24f);
        }
        else
        {
            smoke.transform.position = cam.transform.position + cam.transform.forward * range;
        }
	}
}
