using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class CameraArm : Motor<CharacterChannels> {
        public bool useControlRotationX = true;
        public bool useControlRotationY = true;

        public LayerMask blocking = 1;
        public float targetLength = 6;

        private float lastX;
        private float lastY;

        private Quaternion rot;
        private Camera cam;

        protected override void Start() {
            base.Start();

            Vector3 v = transform.eulerAngles;
            lastX = v.x;
            lastY = v.y;

            cam = GetComponentInChildren<Camera>();
        }

        private void LateUpdate() {
            if (channels != null) {
                Vector3 v = transform.eulerAngles;
                Vector3 r = rot.eulerAngles;

                if (useControlRotationX) {
                    v.x = r.x;
                } else {
                    v.x = lastX;
                }

                if (useControlRotationY) {
                    v.y = r.y;
                } else {
                    v.y = lastY;
                }

                v.z = 0;

                transform.eulerAngles = v;
                lastX = v.x;
                lastY = v.y;

                if (cam && blocking != 0) {
                    RaycastHit hit;

                    if (Physics.SphereCast(transform.position, cam.nearClipPlane, -transform.forward, out hit, targetLength + cam.nearClipPlane, blocking)) {
                        cam.transform.localPosition = new Vector3(0, 0, -hit.distance);
                    } else {
                        cam.transform.localPosition = new Vector3(0, 0, -targetLength);
                    }
                }
            }
        }

        public override void TakeInput() {
            rot = channels.rotation;
        }
    }
}