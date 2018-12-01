using UnityEngine;
using System.Collections;

namespace SBR {
    #pragma warning disable 618
    public class SpaceParticles : MonoBehaviour {
        private ParticleSystem.Particle[] particles;
        private ParticleSystem system;

        public int count = 100;
        public float size = .2f;
        public float distance = 30.0f;
        public float clip = 2.0f;

        // Use this for initialization
        void Start() {
            system = GetComponent<ParticleSystem>();
        }

        private void createParticles() {
            particles = new ParticleSystem.Particle[count];

            for (int i = 0; i < count; i++) {
                particles[i].position = Random.insideUnitSphere * distance + transform.position;
                particles[i].size = size;
                particles[i].color = Color.white;
            }
        }

        // Update is called once per frame
        void Update() {
            if (Camera.main) {
                transform.position = Camera.main.transform.position;
            }

            if (particles == null) {
                createParticles();
            }

            for (int i = 0; i < count; i++) {
                float f = Vector3.SqrMagnitude(transform.position - particles[i].position);
                if (f > distance * distance || f < clip * clip) {
                    particles[i].position = Random.insideUnitSphere * distance + transform.position;
                }
            }

            system.SetParticles(particles, count);

        }
    }
    #pragma warning restore 618
}