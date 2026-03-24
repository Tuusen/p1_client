using UnityEngine;

namespace GeometryTD
{
    public class SkillBarUI : MonoBehaviour
    {
        [SerializeField] private SkillSlotUI[] slots;
        [SerializeField] private FloatingTextUI floatingTextUI;

        private SkillManager skillManager;

        public void SetSkillManager(SkillManager manager)
        {
            skillManager = manager;

            if (slots != null && skillManager != null)
            {
                for (int i = 0; i < slots.Length && i < skillManager.SlotCount; i++)
                {
                    if (slots[i] != null)
                        slots[i].Init(i, skillManager);
                }

                skillManager.OnSlotLevelUp += HandleLevelUp;
            }
        }

        private void OnDestroy()
        {
            if (skillManager != null)
                skillManager.OnSlotLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int slotIndex, int newLevel)
        {
            if (floatingTextUI == null) return;
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;
            if (slots[slotIndex] == null) return;

            var state = skillManager.GetSlot(slotIndex);
            if (state == null) return;

            Vector3 worldPos = slots[slotIndex].GetIconWorldPos();
            floatingTextUI.Show(
                $"{state.skillName} Lv.{newLevel}!",
                worldPos + Vector3.up * 0.5f,
                Color.yellow);
        }

        private void Update()
        {
            if (skillManager == null || slots == null) return;

            for (int i = 0; i < slots.Length && i < skillManager.SlotCount; i++)
            {
                if (slots[i] != null)
                    slots[i].UpdateSlot(skillManager.GetSlot(i));
            }
        }
    }
}
