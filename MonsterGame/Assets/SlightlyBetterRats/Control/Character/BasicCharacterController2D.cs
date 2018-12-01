using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SBR {
    public class BasicCharacterController2D : PlayerController {
        public CharacterChannels character { get; private set; }

        public override void Initialize() {
            base.Initialize();

            character = channels as CharacterChannels;
        }

        public void Axis_Horizontal(float value) {
            Vector3 right = viewTarget.transform.right;
            right.y = 0;
            right = right.normalized;

            character.movement += right * value;
        }

        public void ButtonDown_Jump() {
            character.jump = true;
        }

        public void ButtonUp_Jump() {
            character.jump = false;
        }
    }
}
