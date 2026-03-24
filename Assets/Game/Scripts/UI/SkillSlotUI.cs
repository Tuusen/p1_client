using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class SkillSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text levelText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text cooldownText;
        [SerializeField] private Slider xpSlider;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Button slotButton;

        private int slotIndex;
        private SkillManager skillManager;

        public void Init(int index, SkillManager manager)
        {
            slotIndex = index;
            skillManager = manager;

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnSlotClicked);
            }

            // 初始化技能名
            if (nameText != null && skillManager != null)
            {
                var state = skillManager.GetSlot(index);
                if (state != null)
                    nameText.text = state.skillName;
            }
        }

        private void OnSlotClicked()
        {
            if (skillManager != null)
            {
                skillManager.TryUseSkill(slotIndex);
            }
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

            if (cooldownOverlay != null)
            {
                if (inCooldown)
                {
                    cooldownOverlay.gameObject.SetActive(true);
                    cooldownOverlay.fillAmount = state.cooldownRemaining / state.maxCooldown;
                }
                else
                {
                    cooldownOverlay.gameObject.SetActive(false);
                }
            }

            if (cooldownText != null)
            {
                if (inCooldown)
                {
                    cooldownText.gameObject.SetActive(true);
                    cooldownText.text = $"{state.cooldownRemaining:F1}s";
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                }
            }

            if (slotButton != null)
            {
                slotButton.interactable = state.level > 0 && state.cooldownRemaining <= 0f;
            }
        }
    }
}
