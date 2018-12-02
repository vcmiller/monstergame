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
    public AudioClip attackSound;
    public AudioClip idleSound;

    private CooldownTimer attackTimer;
    private AudioSource attackSource;
    private AudioSource idleSource;

    public void Awake()
    {
        attackSource = GetComponent<AudioSource>();
        idleSource = GetComponent<AudioSource>();
    }

    public override void Initialize()
    {
        base.Initialize();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        attackTimer = new CooldownTimer(attackCooldown);
    }

    protected override void State_Chase()
    {
        MoveTo(player.position);
        idleSource.PlayOneShot(idleSound);
        if (Vector3.Distance(transform.position, player.position) < attackRadius && attackTimer.Use())
        {
            attackSource.PlayOneShot(attackSound);
            channels.attack = Random.Range(1, attacks + 1);
            print("attacking " + channels.attack);
        }
    }
}
