using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GeometryTD
{
    public class SkillSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text levelText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Slider xpSlider;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Button slotButton;
        [SerializeField] private CanvasGroup slotCanvasGroup;

        private int slotIndex;
        private SkillManager skillManager;
        private FloatingTextUI floatingTextUI;
        private Canvas rootCanvas;
        private RectTransform skillBarRect;

        // 拖拽
        private GameObject dragGhost;
        private bool isDragging;
        private DragVisualManager dragVisualManager;
        private SkillCategory currentDragCategory;

        // Tooltip
        private static GameObject activeTooltip;

        public void Init(int index, SkillManager manager)
        {
            slotIndex = index;
            skillManager = manager;

            if (skillManager != null)
            {
                var state = skillManager.GetSlot(index);
                if (state != null)
                {
                    // Load name and icon from skill pool config
                    var poolConfig = ConfigManager.Instance.GetSkillPoolConfig(state.skillPoolId);
                    if (poolConfig != null)
                    {
                        if (nameText != null)
                            nameText.text = poolConfig.name;

                        if (!string.IsNullOrEmpty(poolConfig.icon) && iconImage != null)
                        {
                            var sprite = GameHelper.LoadSprite(poolConfig.icon);
                            if (sprite != null)
                                iconImage.sprite = sprite;
                        }
                    }
                }
            }

            // 缓存引用
            rootCanvas = GetComponentInParent<Canvas>();
            while (rootCanvas != null && rootCanvas.transform.parent != null)
            {
                var parentCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
                if (parentCanvas != null) rootCanvas = parentCanvas;
                else break;
            }

            skillBarRect = transform.parent != null
                ? transform.parent.GetComponent<RectTransform>()
                : null;

            floatingTextUI = Object.FindObjectOfType<FloatingTextUI>();
            dragVisualManager = Object.FindObjectOfType<DragVisualManager>();
            // Debug.Log($"[SkillSlotUI] Init slot={index}, dragVisualManager found={dragVisualManager != null}");
        }

        public RectTransform GetIconRect()
        {
            return iconImage != null ? iconImage.GetComponent<RectTransform>() : null;
        }

        public void UpdateSlot(SkillSlotState state)
        {
            if (state == null) return;

            if (levelText != null)
                levelText.text = $"Lv.{state.level}";

            if (xpSlider != null)
            {
                xpSlider.maxValue = 10;
                xpSlider.value = state.level >= 10 ? 10 : state.xp;
            }

            bool inCooldown = state.cooldownRemaining > 0f && state.maxCooldown > 0f;
            bool isUnusable = state.level <= 0;

            // 冷却遮罩
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(inCooldown);
                if (inCooldown)
                    cooldownOverlay.fillAmount = state.cooldownRemaining / state.maxCooldown;
            }

            // 冷却文本
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(inCooldown);
                if (inCooldown)
                    cooldownText.text = $"{state.cooldownRemaining:F1}s";
            }

            // 压暗逻辑
            if (!isDragging)
            {
                if (slotCanvasGroup != null)
                    slotCanvasGroup.alpha = isUnusable ? 0.4f : 1.0f;
                if (iconImage != null)
                    iconImage.color = isUnusable ? new Color(0.3f, 0.3f, 0.3f) : Color.white;
            }

            if (slotButton != null)
                slotButton.interactable = !isUnusable && !inCooldown;
        }

        // ===== 拖拽 =====
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (skillManager == null) return;
            var state = skillManager.GetSlot(slotIndex);
            if (state == null || state.level <= 0 || state.cooldownRemaining > 0f)
            {
                eventData.pointerDrag = null;
                return;
            }

            isDragging = true;

            // 分类技能并启动视觉效果
            var config = ConfigManager.Instance.GetSkillConfigByPool(state.skillPoolId, Mathf.Max(state.level, 1));
            currentDragCategory = SkillManager.ClassifySkill(config);

            // Read dragHint from skill pool config
            var poolConfig = ConfigManager.Instance.GetSkillPoolConfig(state.skillPoolId);
            string hintText = poolConfig != null ? poolConfig.dragHint : "";

            if (dragVisualManager != null)
                dragVisualManager.BeginDrag(currentDragCategory, hintText);

            CreateDragGhost(eventData.position);

            // 原图标变暗
            if (iconImage != null)
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || dragGhost == null) return;
            dragGhost.transform.position = eventData.position;

            if (dragVisualManager != null)
                dragVisualManager.UpdateDrag(currentDragCategory);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (dragVisualManager != null)
                dragVisualManager.EndDrag();

            DestroyDragGhost();

            bool insideBar = skillBarRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(skillBarRect, eventData.position, null);

            if (!insideBar)
            {
                // 计算释放位置的世界坐标
                Vector3 worldPos = Vector3.zero;
                Ray ray = Camera.main.ScreenPointToRay(eventData.position);
                Plane zPlane = new Plane(Vector3.forward, Vector3.zero);
                if (zPlane.Raycast(ray, out float dist))
                    worldPos = ray.GetPoint(dist);

                var result = skillManager.TryUseSkill(slotIndex, worldPos);
                if (result.result != SkillUseResult.Success)
                    ShowUseFeedback(result);
            }

            // 恢复图标
            var curState = skillManager.GetSlot(slotIndex);
            if (curState != null) UpdateSlot(curState);
        }

        private void CreateDragGhost(Vector2 screenPos)
        {
            if (rootCanvas == null || iconImage == null) return;

            dragGhost = new GameObject("DragGhost");
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

        // ===== 点击 → 提示框 =====
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            // 销毁已有
            if (activeTooltip != null)
            {
                Destroy(activeTooltip);
                activeTooltip = null;
            }

            if (skillManager == null || rootCanvas == null) return;
            var state = skillManager.GetSlot(slotIndex);
            if (state == null) return;

            // 获取 desList（从 skill pool 配置）
            var poolConfig = ConfigManager.Instance.GetSkillPoolConfig(state.skillPoolId);
            if (poolConfig == null || poolConfig.desList == null) return;

            int currentLevel = state.level;
            float cd = 0f;
            var lvConfig = currentLevel > 0
                ? ConfigManager.Instance.GetSkillConfigByPool(state.skillPoolId, currentLevel)
                : null;
            if (lvConfig != null) cd = lvConfig.cd;

            // 创建 tooltip
            activeTooltip = new GameObject("SkillTooltip");
            activeTooltip.transform.SetParent(rootCanvas.transform, false);
            activeTooltip.transform.SetAsLastSibling();

            RectTransform tooltipRT = activeTooltip.AddComponent<RectTransform>();
            Image bgImg = activeTooltip.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);

            // 内容容器
            float lineHeight = 22f;
            float padding = 10f;
            int lineCount = poolConfig.desList.Length + 2; // name + desList + cd
            float totalHeight = lineCount * lineHeight + padding * 2;
            float tooltipWidth = 220f;

            tooltipRT.sizeDelta = new Vector2(tooltipWidth, totalHeight);

            // 定位到槽位上方
            RectTransform slotRT = GetComponent<RectTransform>();
            Vector3 tooltipPos = slotRT.position + new Vector3(0, slotRT.rect.height / 2f + totalHeight / 2f + 10f, 0);
            // 转到Canvas坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(),
                RectTransformUtility.WorldToScreenPoint(null, tooltipPos),
                null, out Vector2 localPos);
            tooltipRT.anchoredPosition = localPos;

            Font font = GameHelper.LoadFont();

            float yOffset = totalHeight / 2f - padding;

            // 技能名
            CreateTooltipText(activeTooltip, poolConfig.name, font, 16, FontStyle.Bold,
                Color.white, tooltipWidth, ref yOffset, lineHeight);

            // desList
            for (int i = 0; i < poolConfig.desList.Length; i++)
            {
                Color textColor = Color.white;
                if (i == 2)
                    textColor = currentLevel >= 6
                        ? new Color(0.2f, 0.9f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
                else if (i == 3)
                    textColor = currentLevel >= 10
                        ? new Color(0.2f, 0.9f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);

                CreateTooltipText(activeTooltip, poolConfig.desList[i], font, 13, FontStyle.Normal,
                    textColor, tooltipWidth, ref yOffset, lineHeight);
            }

            // 冷却时间
            string cdText = cd > 0 ? $"冷却: {cd:F1}s" : "冷却: -";
            CreateTooltipText(activeTooltip, cdText, font, 13, FontStyle.Normal,
                new Color(0.7f, 0.7f, 0.9f), tooltipWidth, ref yOffset, lineHeight);

            // 5秒后自动销毁
            StartCoroutine(DestroyTooltipAfter(5f));
        }

        private void CreateTooltipText(GameObject parent, string content, Font font,
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

        private IEnumerator DestroyTooltipAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (activeTooltip != null)
            {
                Destroy(activeTooltip);
                activeTooltip = null;
            }
        }

        // ===== 不可用反馈 =====
        private void ShowUseFeedback(SkillUseInfo info)
        {
            if (floatingTextUI == null) return;

            string message = "";
            Color color = Color.white;

            switch (info.result)
            {
                case SkillUseResult.LevelTooLow:
                    message = "等级不足";
                    color = new Color(0.9f, 0.3f, 0.3f);
                    break;
                case SkillUseResult.OnCooldown:
                    message = $"冷却中 {info.cooldownRemaining:F1}s";
                    color = new Color(0.5f, 0.5f, 0.8f);
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                Vector3 worldPos = GetIconWorldPos();
                floatingTextUI.Show(message, worldPos + Vector3.up * 0.5f, color);
            }
        }

        public Vector3 GetIconWorldPos()
        {
            if (iconImage == null) return transform.position;
            return iconImage.transform.position;
        }
    }
}
