using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDamageResponse : MonoBehaviour {
    public float hitForce = 10;

    private NavMeshAgent agent;

    private void Start() {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnDamage(Damage dmg) {
        agent.velocity = dmg.dir * hitForce;
    }
}
