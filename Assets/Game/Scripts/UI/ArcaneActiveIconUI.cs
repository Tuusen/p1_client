using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class ArcaneActiveIconUI : MonoBehaviour
    {
        [SerializeField] private RectTransform container;

        private ArcaneManager arcaneManager;
        private List<GameObject> iconObjects = new List<GameObject>();

        public void SetArcaneManager(ArcaneManager manager)
        {
            arcaneManager = manager;
            if (arcaneManager != null)
                arcaneManager.OnArcanePlaced += OnArcanePlaced;
        }

        private void OnDestroy()
        {
            if (arcaneManager != null)
                arcaneManager.OnArcanePlaced -= OnArcanePlaced;
        }

        private void OnArcanePlaced(int slotIndex)
        {
            RebuildIcons();
        }

        private void RebuildIcons()
        {
            // Clear existing icons
            foreach (var obj in iconObjects)
            {
                if (obj != null) Destroy(obj);
            }
            iconObjects.Clear();

            if (arcaneManager == null) return;

            var actives = arcaneManager.GetActiveArcanes();
            RectTransform parent = container != null ? container : GetComponent<RectTransform>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            for (int i = 0; i < actives.Count; i++)
            {
                var a = actives[i];
                var config = a.config;

                GameObject iconObj = new GameObject($"ActiveArcane_{i}");
                iconObj.transform.SetParent(parent, false);

                RectTransform rt = iconObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(40, 40);
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(i * 48 + 24, 0);

                Image bg = iconObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.3f, 0.8f);
                bg.raycastTarget = false;

                // Timer text
                GameObject textObj = new GameObject("Timer");
                textObj.transform.SetParent(iconObj.transform, false);
                Text timerText = textObj.AddComponent<Text>();
                timerText.font = font;
                timerText.fontSize = 12;
                timerText.alignment = TextAnchor.MiddleCenter;
                timerText.color = Color.white;
                timerText.raycastTarget = false;
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                iconObjects.Add(iconObj);
            }
        }

        private void Update()
        {
            if (arcaneManager == null) return;

            var actives = arcaneManager.GetActiveArcanes();
            if (iconObjects.Count != actives.Count)
            {
                RebuildIcons();
                return;
            }

            for (int i = 0; i < actives.Count && i < iconObjects.Count; i++)
            {
                if (iconObjects[i] == null) continue;
                var timerText = iconObjects[i].GetComponentInChildren<Text>();
                if (timerText != null)
                {
                    float remaining = actives[i].tickTimer;
                    timerText.text = $"{remaining:F1}";
                }
            }
        }
    }
}
