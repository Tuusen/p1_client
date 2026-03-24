using UnityEngine;

namespace GeometryTD
{
    public class SkillBarUI : MonoBehaviour
    {
        [SerializeField] private SkillSlotUI[] slots;

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
            }
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
