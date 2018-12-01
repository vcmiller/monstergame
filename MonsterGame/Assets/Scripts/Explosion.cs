using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour {
    public float damage;
    public LayerMask hitMask;
    public float radius;

	// Use this for initialization
	void Start () {
        foreach (var obj in Physics.OverlapSphere(transform.position, radius, hitMask))
        {
            obj.Damage(damage, transform.position, (obj.transform.position - transform.position).normalized * 0.1f);
        }
	}
}
