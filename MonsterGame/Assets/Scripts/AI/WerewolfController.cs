using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SBR;

public class WerewolfController : WerewolfSM<EnemyChannels> {
    public Transform player;
    public float attackRadius = 1;
    public int attacks = 2;
    public float attackCooldown = 1;

    private CooldownTimer attackTimer;

    public override void Initialize()
    {
        base.Initialize();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        attackTimer = new CooldownTimer(attackCooldown);
    }

    protected override void State_Chase()
    {
        MoveTo(player.position);

        if (Vector3.Distance(transform.position, player.position) < attackRadius && attackTimer.Use())
        {
            channels.attack = Random.Range(1, attacks + 1);
            print("attacking " + channels.attack);
        }
    }
}
