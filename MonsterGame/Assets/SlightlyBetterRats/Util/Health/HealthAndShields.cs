using UnityEngine;

namespace SBR {
    class HealthAndShields : Health {
        public float shields { get; private set; }
        public float maxShields = 100;

        public float shieldRegenRate = 1;
        public float shieldRegenDelay = 1;

        public ExpirationTimer shieldRegenTimer { get; private set; }
        
        [Tooltip("If more damage is dealt than remaining shields, whether shield absorbs full damage or passes remaining damage to health.")]
        public bool shieldAbsorbsLastHit = true;

        protected override void Start() {
            base.Start();

            shields = maxShields;

            shieldRegenTimer = new ExpirationTimer(shieldRegenDelay);
        }

        public override void Update() {
            base.Update();

            if (shieldRegenTimer.expired && !hasDied) {
                Recharge(shieldRegenRate * Time.deltaTime);
            }
        }

        public override void Damage(Damage dmg) {
            if (enabled) {
                float absorbed = Mathf.Min(shields, dmg.amount);

                if (absorbed > 0) {
                    shields -= dmg.amount;
                    shields = Mathf.Max(shields, 0);
                    shieldRegenTimer.Set();
                    SendMessage("OnShieldDamage", dmg, SendMessageOptions.DontRequireReceiver);

                    if (shields == 0) {
                        SendMessage("OnZeroShields", SendMessageOptions.DontRequireReceiver);
                    }
                }

                if (absorbed == 0 || (absorbed < dmg.amount && !shieldAbsorbsLastHit)) {
                    base.Damage(new Damage(dmg.amount - absorbed, dmg.point, dmg.dir));
                }
            }
        }

        public void Recharge(float amount) {
            shields += amount;
            shields = Mathf.Min(shields, maxShields);
        }
    }
}
