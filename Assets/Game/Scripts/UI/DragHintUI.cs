using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// Displays a floating hint label at the top-centre of the screen while dragging a skill.
    /// Created / destroyed by DragVisualManager each drag session.
    /// </summary>
    public class DragHintUI
    {
        private GameObject root;

        public void Show(string text, Canvas canvas)
        {
            if (canvas == null || string.IsNullOrEmpty(text)) return;

            root = new GameObject("DragHint");
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();

            // Background
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(300f, 40f);

            // Text
            GameObject textObj = new GameObject("HintText");
            textObj.transform.SetParent(root.transform, false);

            Text t = textObj.AddComponent<Text>();
            t.text = text;
            t.font = GameHelper.LoadFont();
            t.fontSize = 22;
            t.color = new Color(1f, 1f, 0.8f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        public void Hide()
        {
            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }
        }
    }
}
