using UnityEngine;
using System.Collections;

namespace SBR {
    public class Magazine {
        private ExpirationTimer reload;
        public int remainingShots { get; private set; }
        public int clipSize { get; private set; }

        public bool canFire {
            get {
                return remainingShots > 0 && reload.expired;
            }
        }

        public Magazine(int size, float reloadTime) {
            reload = new ExpirationTimer(reloadTime);
            remainingShots = size;
            clipSize = size;
        }

        public void Reload() {
            if (remainingShots < clipSize) {
                remainingShots = clipSize;
                reload.Set();
            }
        }

        public bool Reloading() {
            return !reload.expired;
        }

        public bool Fire() {
            if (canFire) {
                remainingShots--;

                if (remainingShots == 0) {
                    Reload();
                }

                return true;
            } else {
                return false;
            }
        }
    }
}