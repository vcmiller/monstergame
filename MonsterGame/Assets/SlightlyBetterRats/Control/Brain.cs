using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SBR {
    [DisallowMultipleComponent]
    public class Brain : MonoBehaviour {
        [TypeSelect(typeof(Channels))]
        public string channelsType;

        public Channels channels { get; private set; }

        private Controller[] controllers;
        private Motor[] motors;

        private int _activeIndex = -1;
        public int defaultController = 0;

        public int activeControllerIndex {
            get {
                return _activeIndex;
            }

            set {
                if (value != _activeIndex && value < controllers.Length) {
                    if (_activeIndex >= 0) {
                        try {
                            activeController.enabled = false;
                        } catch (Exception ex) {
                            Debug.LogException(ex);
                        }
                    }

                    _activeIndex = value;

                    if (value >= 0) {
                        try {
                            activeController.enabled = true;
                        } catch (Exception ex) {
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        public Controller activeController {
            get {
                return _activeIndex >= 0 ? controllers[_activeIndex] : null;
            }
        }

        private void Awake() {
            Type t = Type.GetType(channelsType);

            if (typeof(Channels).IsAssignableFrom(t)) {
                channels = (Channels)t.GetConstructor(new Type[0]).Invoke(new object[0]);
            } else {
                Debug.LogError("Error: invalid channel type!");
            }

            UpdateMotors();

            controllers = GetComponents<Controller>();
            foreach (var ctrl in controllers) {
                try {
                    ctrl.Initialize();
                } catch (Exception ex) {
                    Debug.LogException(ex);
                }
            }

            activeControllerIndex = defaultController;
        }

        public void UpdateMotors() {
            motors = GetComponentsInChildren<Motor>();
        }

        private void Update() {
            var c = activeController;

            if (c && c.enabled) {
                try {
                    c.GetInput();
                } catch (Exception ex) {
                    Debug.LogException(ex);
                }
            }

            foreach (var motor in motors) {
                if (motor.enabled) {
                    try {
                        motor.TakeInput();
                    } catch (Exception ex) {
                        Debug.LogException(ex);
                    }

                    try {
                        motor.UpdateAfterInput();
                    } catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                }
            }

            channels.ClearInput();
        }
    }
}
