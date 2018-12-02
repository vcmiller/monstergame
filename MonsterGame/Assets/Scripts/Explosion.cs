using SBR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour {
    public float damage;
    public LayerMask hitMask;
    public float radius;
    public AudioClip explosionSound;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    // Use this for initialization
    void Start () {
        source.PlayOneShot(explosionSound);
        foreach (var obj in Physics.OverlapSphere(transform.position, radius, hitMask))
        {
            obj.Damage(damage, transform.position, (obj.transform.position - transform.position).normalized * 0.1f);
        }
	}
}
