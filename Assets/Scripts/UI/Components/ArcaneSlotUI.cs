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
        private DragVisualManager dragVisualManager;

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

            dragVisualManager = Object.FindObjectOfType<DragVisualManager>();
        }

        public void UpdateSlot(ArcaneSlotState state)
        {
            if (state == null) return;

            var config = Cfg.Arcane.Get(state.arcaneId);

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
            var config = Cfg.Arcane.Get(state.arcaneId);
            arcaneRadius = config != null ? config.radius : 3f;

            // Get drag hint from config
            string hintText = config != null ? config.dragHint : "";

            // Start drag visual effects with slow-motion
            if (dragVisualManager != null)
                dragVisualManager.BeginDrag(SkillCategory.Aoe, hintText);

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

            // Update drag visuals
            if (dragVisualManager != null)
                dragVisualManager.UpdateDrag(SkillCategory.Aoe);

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

            // End drag visual effects and restore time scale
            if (dragVisualManager != null)
                dragVisualManager.EndDrag();

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
            // 销毁已有tooltip（如果有）
            if (activeTooltip != null)
            {
                Destroy(activeTooltip);
                activeTooltip = null;
            }

            if (arcaneManager == null) return;
            var state = arcaneManager.GetSlot(slotIndex);
            if (state == null) return;

            var config = Cfg.Arcane.Get(state.arcaneId);
            if (config == null) return;

            // 打开详情窗口
            var param = new SkillArcaneDetailWinParam
            {
                id = state.arcaneId,
                isSkill = false,
                currentLevel = 0
            };
            GameHelper.OpenWin<SkillArcaneDetailWin>(param: param);
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
