using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class SkillSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text levelText;
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

            if (cooldownOverlay != null)
            {
                if (state.level > 0 && state.cooldownRemaining > 0f && state.maxCooldown > 0f)
                {
                    cooldownOverlay.gameObject.SetActive(true);
                    cooldownOverlay.fillAmount = state.cooldownRemaining / state.maxCooldown;
                }
                else
                {
                    cooldownOverlay.gameObject.SetActive(false);
                }
            }

            if (slotButton != null)
            {
                slotButton.interactable = state.level > 0 && state.cooldownRemaining <= 0f;
            }
        }
    }
}
