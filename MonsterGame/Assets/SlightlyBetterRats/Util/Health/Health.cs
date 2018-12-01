using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Health : MonoBehaviour {
        public float health { get; private set; }
        public bool hasDied { get; private set; }

        public float maxHealth = 100;
        public float healthRegenRate = 0;
        public float healthRegenDelay = 0;
        public GameObject healthbarPrefab;
        
        public ExpirationTimer healthRegenTimer { get; private set; }
        public Healthbar healthbar { get; private set; }

        private void Awake() {
            health = maxHealth;

            healthRegenTimer = new ExpirationTimer(healthRegenDelay);
        }

        protected virtual void Start() {
            if (healthbarPrefab) {
                healthbar = Instantiate(healthbarPrefab).GetComponent<Healthbar>();
                healthbar.target = this;

                healthbar.transform.SetParent(FindObjectOfType<Canvas>().transform, false);
            }
        }

        public void Damage(float amount, Vector3 position = default(Vector3), Vector3 dir = default(Vector3)) {
            Damage(new Damage(amount, position, dir));
        }

        public virtual void Damage(Damage dmg) {
            if (enabled) {
                health -= dmg.amount;
                health = Mathf.Max(health, 0);
                healthRegenTimer.Set();
                SendMessage("OnDamage", dmg, SendMessageOptions.DontRequireReceiver);

                if (health == 0 && !hasDied) {
                    hasDied = true;
                    SendMessage("OnZeroHealth", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        public virtual void Update() {
            if (healthRegenTimer.expired && !hasDied) {
                Heal(healthRegenRate * Time.deltaTime);
            }
        }

        public virtual void Heal(float amount) {
            if (enabled) {
                hasDied = false;
                health += amount;
                health = Mathf.Min(health, maxHealth);
            }
        }
    }

    public static class HealthExt {
        public static void Damage(this GameObject obj, Damage dmg) {
            Health h = obj.GetComponentInParent<Health>();
            if (h) {
                h.Damage(dmg);
            }
        }

        public static void Damage(this GameObject obj, float amount, Vector3 position = default(Vector3), Vector3 dir = default(Vector3)) {
            obj.Damage(new Damage(amount, position, dir));
        }

        public static void Damage(this Component cmp, Damage dmg) {
            cmp.gameObject.Damage(dmg);
        }
        
        public static void Damage(this Component cmp, float amount, Vector3 position = default(Vector3), Vector3 dir = default(Vector3)) {
            cmp.gameObject.Damage(amount, position, dir);
        }

        public static void Heal(this GameObject obj, float amount) {
            Health h = obj.GetComponentInParent<Health>();
            if (h) {
                h.Heal(amount);
            }
        }

        public static void Heal(this Component cmp, float amount) {
            cmp.gameObject.Heal(amount);
        }
    }
}
