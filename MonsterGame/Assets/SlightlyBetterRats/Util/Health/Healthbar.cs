using UnityEngine;
using UnityEngine.UI;

namespace SBR {
    [ExecuteInEditMode]
    public class Healthbar : MonoBehaviour {
        public Health target;
        public bool destroyWithTarget = true;
        public bool trackOnScreen = false;
        public Vector3 trackWorldOffset = new Vector3(0, 1, 0);

        public Image fillImage;
        public Image backImage;

        private RectTransform fillRect { get { return fillImage.GetComponent<RectTransform>(); } }
        private RectTransform rect { get; set; }

        public bool visible { get; set; }

        private void Awake() {
            visible = false;
        }

        private void Start() {
            visible = true;
            rect = GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update() {
            if (Application.isPlaying) {
                fillImage.enabled = visible && target;
                backImage.enabled = visible && target;
            }

            if (fillImage && target) {
                if (fillImage.type == Image.Type.Filled) {
                    fillImage.fillAmount = target.health / target.maxHealth;
                    fillRect.anchorMax = Vector2.one;
                } else {
                    fillImage.fillAmount = 1.0f;
                    fillRect.anchorMax = new Vector2(target.health / target.maxHealth, 1);
                }
            }

            if (Application.isPlaying && target && trackOnScreen) {
                Vector3 wpos = target.transform.position + trackWorldOffset;
                Vector3 spos = Camera.main.WorldToScreenPoint(wpos);

                fillImage.enabled &= spos.z > 0;
                backImage.enabled &= spos.z > 0;

                spos.z = 0;

                transform.position = spos;
            }

            if (Application.isPlaying && !target && destroyWithTarget) {
                Destroy(gameObject);
            }
        }
    }
}