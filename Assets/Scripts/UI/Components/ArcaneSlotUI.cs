using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GeometryTD
{
    public class ArcaneSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private Text levelText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeButtonText;
        [SerializeField] private CanvasGroup slotCanvasGroup;

        private int slotIndex;
        private ArcaneManager arcaneManager;

        public void Init(int index, ArcaneManager manager)
        {
            slotIndex = index;
            arcaneManager = manager;

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        public void UpdateSlot(ArcaneSlotState state)
        {
            if (state == null) return;

            var config = state.cachedConfig ?? Cfg.Arcane.Get(state.arcaneId);
            int level = state.level;

            // Load icon from config
            if (config != null && !string.IsNullOrEmpty(config.icon) && iconImage != null && iconImage.sprite == null)
            {
                var sprite = GameHelper.LoadSprite(config.icon);
                if (sprite != null)
                    iconImage.sprite = sprite;
            }

            // 图标灰度：未解锁时灰色
            if (iconImage != null)
                iconImage.color = level > 0 ? Color.white : new Color(0.3f, 0.3f, 0.3f);

            // 等级显示
            if (levelText != null)
                levelText.text = level > 0 ? $"Lv.{level}" : "";

            // 升级按钮
            bool canUpgrade = arcaneManager != null && arcaneManager.CanUpgrade(slotIndex);
            if (upgradeButton != null)
                upgradeButton.interactable = canUpgrade;
            if (upgradeButtonText != null)
                upgradeButtonText.text = level == 0 ? "解锁" : "升级";

            // 消耗颜色
            if (costText != null && config != null)
            {
                int currentRunes = arcaneManager != null ? arcaneManager.GetRune(config.runeType) : 0;
                costText.color = currentRunes >= config.runeCost
                    ? new Color(0.2f, 0.9f, 0.2f)
                    : new Color(0.9f, 0.3f, 0.3f);
            }

            // 整体透明度
            if (slotCanvasGroup != null)
                slotCanvasGroup.alpha = canUpgrade ? 1.0f : 0.7f;
        }

        private void OnUpgradeClicked()
        {
            if (arcaneManager != null)
                arcaneManager.TryUpgradeArcane(slotIndex);
        }

        // ===== 点击打开详情 =====
        public void OnPointerClick(PointerEventData eventData)
        {
            ShowDetail();
        }

        private void ShowDetail()
        {
            if (arcaneManager == null) return;
            var state = arcaneManager.GetSlot(slotIndex);
            if (state == null) return;

            var config = state.cachedConfig ?? Cfg.Arcane.Get(state.arcaneId);
            if (config == null) return;

            var param = new SkillArcaneDetailWinParam
            {
                id = state.arcaneId,
                isSkill = false,
                currentLevel = state.level
            };
            GameHelper.OpenWin<SkillArcaneDetailWin>(param: param);
        }

        public Vector3 GetIconWorldPos()
        {
            if (iconImage == null) return transform.position;
            return iconImage.transform.position;
        }
    }
}
