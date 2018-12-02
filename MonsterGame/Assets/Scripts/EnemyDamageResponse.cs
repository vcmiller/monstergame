using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDamageResponse : MonoBehaviour {
    public float hitForce = 10;

    private CharacterMotor cm;
    public Transform model;
    public GameObject ragdoll;
    public float deathVelMultiplier = 0.2f;
    public GameObject corpse { get; private set; }

    private void Start() {
        cm = GetComponent<CharacterMotor>();
    }

    void OnDamage(Damage dmg) {
        cm.velocity = dmg.dir * hitForce;
    }

    void OnZeroHealth()
    {
        corpse = Instantiate(ragdoll, model.position, model.rotation);
        foreach (var rb in corpse.GetComponentsInChildren<Rigidbody>())
        {
            rb.velocity = cm.velocity * deathVelMultiplier;
        }

        Destroy(gameObject);
    }
}
