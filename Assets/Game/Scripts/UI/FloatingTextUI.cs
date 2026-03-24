using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class FloatingTextUI : MonoBehaviour
    {
        public void Show(string text, Vector3 worldPos, Color color)
        {
            StartCoroutine(FloatText(text, worldPos, color));
        }

        private IEnumerator FloatText(string text, Vector3 worldPos, Color color)
        {
            GameObject go = new GameObject("FloatingText");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 50);
            rt.localScale = Vector3.one * 0.01f;
            go.transform.position = worldPos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.text = text;
            t.fontSize = 42;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            float duration = 1.2f;
            float elapsed = 0f;
            Vector3 startPos = worldPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                go.transform.position = startPos + Vector3.up * progress * 1.5f;
                t.color = new Color(color.r, color.g, color.b, 1f - progress);
                yield return null;
            }

            Destroy(go);
        }
    }
}
