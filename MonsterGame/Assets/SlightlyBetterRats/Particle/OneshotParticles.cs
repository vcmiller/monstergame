using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class OneshotParticles : MonoBehaviour {

        // Use this for initialization
        void Start() {
            Destroy(gameObject, GetComponent<ParticleSystem>().main.duration);
        }
    }
}
