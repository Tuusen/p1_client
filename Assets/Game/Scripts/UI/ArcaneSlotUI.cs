using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GeometryTD
{
    public class ArcaneSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private CanvasGroup slotCanvasGroup;

        private int slotIndex;
        private ArcaneManager arcaneManager;
        private Canvas rootCanvas;
        private RectTransform barRect;

        private GameObject dragGhost;
        private bool isDragging;

        // Range circle shown during drag
        private GameObject rangeCircle;
        private float arcaneRadius;

        private static GameObject activeTooltip;

        public void Init(int index, ArcaneManager manager)
        {
            slotIndex = index;
            arcaneManager = manager;

            rootCanvas = GetComponentInParent<Canvas>();
            while (rootCanvas != null && rootCanvas.transform.parent != null)
            {
                var parentCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
                if (parentCanvas != null) rootCanvas = parentCanvas;
                else break;
            }

            barRect = transform.parent != null
                ? transform.parent.GetComponent<RectTransform>()
                : null;
        }

        public void UpdateSlot(ArcaneSlotState state)
        {
            if (state == null) return;

            var config = ConfigManager.Instance.GetArcaneConfig(state.arcaneId);

            // Load icon from config
            if (config != null && !string.IsNullOrEmpty(config.icon) && iconImage != null && iconImage.sprite == null)
            {
                var sprite = GameHelper.LoadSprite(config.icon);
                if (sprite != null)
                    iconImage.sprite = sprite;
            }

            bool canCast = arcaneManager != null && arcaneManager.CanCast(slotIndex);
            bool inCooldown = state.cooldownRemaining > 0f && state.maxCooldown > 0f;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(inCooldown);
                if (inCooldown)
                    cooldownOverlay.fillAmount = state.cooldownRemaining / state.maxCooldown;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(inCooldown);
                if (inCooldown)
                    cooldownText.text = $"{state.cooldownRemaining:F1}s";
            }

            // Update cost text color based on affordability
            if (costText != null && config != null)
            {
                int runeIdx = config.runeType - 1;
                int currentRunes = arcaneManager != null ? arcaneManager.GetRune(config.runeType) : 0;
                costText.color = currentRunes >= config.runeCost
                    ? new Color(0.2f, 0.9f, 0.2f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            if (!isDragging)
            {
                if (slotCanvasGroup != null)
                    slotCanvasGroup.alpha = canCast ? 1.0f : 0.5f;
                if (iconImage != null)
                    iconImage.color = canCast ? Color.white : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // ===== Drag =====
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (arcaneManager == null || !arcaneManager.CanCast(slotIndex))
            {
                eventData.pointerDrag = null;
                return;
            }

            isDragging = true;

            var state = arcaneManager.GetSlot(slotIndex);
            var config = ConfigManager.Instance.GetArcaneConfig(state.arcaneId);
            arcaneRadius = config != null ? config.radius : 3f;

            CreateDragGhost(eventData.position);

            if (arcaneRadius > 0)
                CreateRangeCircle();

            if (iconImage != null)
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || dragGhost == null) return;
            dragGhost.transform.position = eventData.position;

            if (rangeCircle != null)
            {
                Vector3 worldPos = ScreenToWorldZ0(eventData.position);
                rangeCircle.transform.position = worldPos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            DestroyDragGhost();
            DestroyRangeCircle();

            bool insideBar = barRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(barRect, eventData.position, null);

            if (!insideBar)
            {
                Vector3 worldPos = ScreenToWorldZ0(eventData.position);
                arcaneManager.TryCastArcane(slotIndex, worldPos);
            }

            var curState = arcaneManager.GetSlot(slotIndex);
            if (curState != null) UpdateSlot(curState);
        }

        private Vector3 ScreenToWorldZ0(Vector2 screenPos)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Plane zPlane = new Plane(Vector3.forward, Vector3.zero);
            if (zPlane.Raycast(ray, out float dist))
                return ray.GetPoint(dist);
            return Vector3.zero;
        }

        private void CreateDragGhost(Vector2 screenPos)
        {
            if (rootCanvas == null || iconImage == null) return;

            dragGhost = new GameObject("ArcaneDragGhost");
            dragGhost.transform.SetParent(rootCanvas.transform, false);
            dragGhost.transform.SetAsLastSibling();

            RectTransform rt = dragGhost.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);

            Image ghostImg = dragGhost.AddComponent<Image>();
            ghostImg.sprite = iconImage.sprite;
            ghostImg.color = new Color(1f, 1f, 1f, 0.7f);
            ghostImg.raycastTarget = false;

            dragGhost.transform.position = screenPos;
        }

        private void DestroyDragGhost()
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }
        }

        private void CreateRangeCircle()
        {
            if (arcaneRadius <= 0) return;

            rangeCircle = new GameObject("ArcaneRangeCircle");
            var sr = rangeCircle.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 90;
            sr.color = new Color(0.3f, 0.6f, 1f, 0.25f);

            // Create circle texture
            int texSize = 64;
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            float center = texSize / 2f;
            float rSq = center * center;
            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distSq = dx * dx + dy * dy;
                    tex.SetPixel(x, y, distSq <= rSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), texSize / (arcaneRadius * 2f));
        }

        private void DestroyRangeCircle()
        {
            if (rangeCircle != null)
            {
                Destroy(rangeCircle);
                rangeCircle = null;
            }
        }

        // ===== Tooltip =====
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            if (activeTooltip != null)
            {
                Destroy(activeTooltip);
                activeTooltip = null;
            }

            if (arcaneManager == null || rootCanvas == null) return;
            var state = arcaneManager.GetSlot(slotIndex);
            if (state == null) return;

            var config = ConfigManager.Instance.GetArcaneConfig(state.arcaneId);
            if (config == null || config.desList == null) return;

            string[] runeNames = { "火", "冰", "雷", "风" };
            string runeTypeName = config.runeType >= 1 && config.runeType <= 4
                ? runeNames[config.runeType - 1] : "?";

            activeTooltip = new GameObject("ArcaneTooltip");
            activeTooltip.transform.SetParent(rootCanvas.transform, false);
            activeTooltip.transform.SetAsLastSibling();

            RectTransform tooltipRT = activeTooltip.AddComponent<RectTransform>();
            Image bgImg = activeTooltip.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);

            float lineHeight = 22f;
            float padding = 10f;
            int lineCount = config.desList.Length + 3; // name + desList + cost + cd
            float totalHeight = lineCount * lineHeight + padding * 2;
            float tooltipWidth = 220f;

            tooltipRT.sizeDelta = new Vector2(tooltipWidth, totalHeight);

            RectTransform slotRT = GetComponent<RectTransform>();
            Vector3 tooltipPos = slotRT.position + new Vector3(0, slotRT.rect.height / 2f + totalHeight / 2f + 10f, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(),
                RectTransformUtility.WorldToScreenPoint(null, tooltipPos),
                null, out Vector2 localPos);
            tooltipRT.anchoredPosition = localPos;

            Font font = GameHelper.LoadFont();

            float yOffset = totalHeight / 2f - padding;

            CreateTooltipLine(activeTooltip, config.name, font, 16, FontStyle.Bold,
                Color.white, tooltipWidth, ref yOffset, lineHeight);

            for (int i = 0; i < config.desList.Length; i++)
            {
                CreateTooltipLine(activeTooltip, config.desList[i], font, 13, FontStyle.Normal,
                    Color.white, tooltipWidth, ref yOffset, lineHeight);
            }

            CreateTooltipLine(activeTooltip, $"消耗: {config.runeCost} {runeTypeName}符能",
                font, 13, FontStyle.Normal, new Color(0.6f, 0.8f, 1f),
                tooltipWidth, ref yOffset, lineHeight);

            CreateTooltipLine(activeTooltip, $"冷却: {config.cd:F1}s",
                font, 13, FontStyle.Normal, new Color(0.7f, 0.7f, 0.9f),
                tooltipWidth, ref yOffset, lineHeight);

            StartCoroutine(DestroyTooltipAfter(5f));
        }

        private void CreateTooltipLine(GameObject parent, string content, Font font,
            int fontSize, FontStyle style, Color color, float width, ref float yOffset, float lineHeight)
        {
            GameObject textObj = new GameObject("Line");
            textObj.transform.SetParent(parent.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width - 16f, lineHeight);
            rt.anchoredPosition = new Vector2(0, yOffset - lineHeight / 2f);
            yOffset -= lineHeight;
        }

        private System.Collections.IEnumerator DestroyTooltipAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (activeTooltip != null)
            {
                Destroy(activeTooltip);
                activeTooltip = null;
            }
        }

        public Vector3 GetIconWorldPos()
        {
            if (iconImage == null) return transform.position;
            return iconImage.transform.position;
        }
    }
}
