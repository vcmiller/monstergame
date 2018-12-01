using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SBR.Editor {
    public static class CreateHealthbar {
        [MenuItem("GameObject/UI/Healthbar")]
        public static void Create() {
            Canvas canvas = Object.FindObjectOfType<Canvas>();

            if (!canvas) {
                GameObject canvasObj = new GameObject("Canvas");
                canvasObj.layer = LayerMask.NameToLayer("UI");

                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            GameObject healthObj = new GameObject("Healthbar");
            Selection.activeGameObject = healthObj;

            RectTransform healthRect = healthObj.AddComponent<RectTransform>();
            healthRect.SetParent(canvas.transform, false);

            healthRect.sizeDelta = new Vector2(100, 10);

            Image healthBG = healthObj.AddComponent<Image>();
            healthBG.color = new Color(0.5f, 0.5f, 0.5f);
            healthBG.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            healthBG.type = Image.Type.Sliced;

            GameObject healthFill = new GameObject("Fill");
            RectTransform fillRect = healthFill.AddComponent<RectTransform>();
            fillRect.SetParent(healthRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;

            Image healthFG = healthFill.AddComponent<Image>();
            healthFG.color = new Color(0.75f, 0.0f, 0.0f);
            healthFG.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            healthFG.type = Image.Type.Sliced;

            Healthbar health = healthObj.AddComponent<Healthbar>();
            health.fillImage = healthFG;
            health.backImage = healthBG;
        }
    }
}

