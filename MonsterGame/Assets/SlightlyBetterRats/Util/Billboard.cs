using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class Billboard : MonoBehaviour {
        public Mode mode;
        public TargetMode targetMode;

        [Conditional("UseTargetObject")]
        public Transform targetObject;
        
        void LateUpdate() {
            Transform target;
            if (targetMode == TargetMode.MainCamera) {
                target = Camera.main.transform;
            } else {
                target = targetObject;
            }

            if (mode == Mode.CopyRotation) {
                transform.rotation = target.rotation;
            } else if (mode == Mode.LookAt) {
                transform.rotation = Quaternion.LookRotation(target.position - transform.position);
            } else {
                Vector3 right = Vector3.Cross(Vector3.up, target.position - transform.position);
                Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
                transform.rotation = Quaternion.LookRotation(fwd);
            }
        }

        public bool UseTargetObject() {
            return targetMode == TargetMode.TargetObject;
        }

        public enum Mode {
            CopyRotation, LookAt, LookAtYaw
        }

        public enum TargetMode {
            MainCamera, TargetObject
        }
    }
}