using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnim : Motor<EnemyChannels> {
    private Animator anim;
    private CharacterMotor cm;
    private bool attacking;

    public Projectile projectile;

    protected override void Start()
    {
        base.Start();
        anim = GetComponent<Animator>();
        cm = GetComponent<CharacterMotor>();
    }

    public override void TakeInput()
    {
        if (channels.attack != 0)
        {
            anim.Play("Attack" + channels.attack);
            attacking = true;
        }

        cm.enableInput = !attacking;
    }

    void AttackEnd()
    {
        channels.attack = 0;
        attacking = false;
    }

    void AttackDamage()
    {
        Instantiate(projectile, transform.position, transform.rotation).Fire();
    }
}
