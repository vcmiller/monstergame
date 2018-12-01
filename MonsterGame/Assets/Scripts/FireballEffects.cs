using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballEffects : MonoBehaviour {
    private Animation anim;
    public ParticleSystem flame;
    public float offMin;
    public float offMax;

    private void Start()
    {
        anim = GetComponent<Animation>();
    }

    void Update()
    {
        var em = flame.emission;
        float t = anim[anim.clip.name].normalizedTime;
        em.enabled = t < offMin || t > offMax;
        if (!em.enabled)
        {
            flame.Clear();
        }
    }
    
}
