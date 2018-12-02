using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollDisintegrate : MonoBehaviour {
    public float deathTime;
    public ParticleSystem ps;

	// Use this for initialization
	void Start () {
        Invoke("Die", deathTime);
	}
	
	public void Die()
    {
        CancelInvoke("Die");
        ps.Play();
        GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        Destroy(gameObject, ps.main.duration);
    }
}
