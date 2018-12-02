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
    public float detectionRadius = 30;
    public int maxChasing = 3;
    public float chaseRadius;
    public float fleeHealth = 25;
    public float rotateSpeed = 400;

    public AudioClip attackSound;
    public AudioClip idleSound;

    private CooldownTimer attackTimer;
    private AudioSource attackSource;
    private AudioSource idleSource;
    private GameObject[] wanderPoints;
    private static HashSet<WerewolfSM> chasingPlayer = new HashSet<WerewolfSM>();
    private Health health;

    public void Awake()
    {
        attackSource = GetComponent<AudioSource>();
        idleSource = GetComponent<AudioSource>();
        health = GetComponent<Health>();
    }

    void OnDamage(Damage dmg)
    {
        //state = StateID.Chase;
    }

    public override void Initialize()
    {
        base.Initialize();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        attackTimer = new CooldownTimer(attackCooldown);
        wanderPoints = GameObject.FindGameObjectsWithTag("WanderNode");
    }

    protected override void State_Chase()
    {
        if (Vector3.Distance(transform.position, player.position) > chaseRadius)
            MoveTo(player.position);
        else
        {
            Stop();
            Quaternion q = Quaternion.LookRotation(player.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, Time.deltaTime * rotateSpeed);
        }
        if (Vector3.Distance(transform.position, player.position) < attackRadius && attackTimer.Use())
        {
            channels.attack = Random.Range(1, attacks + 1);
        }
    }

    protected override void State_Wander()
    {
        if (arrived)
        {
            MoveTo(wanderPoints[Random.Range(0, wanderPoints.Length)].transform.position);
        }
    }

    protected override bool TransitionCond_Wander_Chase()
    {
        chasingPlayer.RemoveWhere(sm => !sm);

        return chasingPlayer.Count < maxChasing && Vector3.Distance(transform.position, player.position) < detectionRadius;
    }

    protected override void TransitionNotify_Wander_Chase()
    {
        chasingPlayer.Add(this);
    }

    protected override void State_RunAway()
    {
        if (arrived)
        {
            float bestDist = 0;
            Vector3 bestPos = Vector3.zero;
            foreach (var point in wanderPoints)
            {
                float d = Vector3.SqrMagnitude(point.transform.position - player.position);
                if (d > bestDist)
                {
                    bestDist = d;
                    bestPos = point.transform.position;
                }
            }
            MoveTo(bestPos);
        }
    }

    protected override bool TransitionCond_Chase_RunAway()
    {
        return health.health < fleeHealth;
    }

    protected override void TransitionNotify_Chase_RunAway()
    {
        Stop();
        chasingPlayer.Remove(this);
    }
}
