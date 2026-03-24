using UnityEngine;

namespace GeometryTD
{
    public class SkillSlotState
    {
        public int skillId;
        public int level;
        public int xp;
        public float cooldownRemaining;
        public float maxCooldown;
    }

    public class SkillManager : MonoBehaviour
    {
        private SkillSlotState[] slots;
        private HeroController hero;
        private BattleManager battleManager;
        private FloatingTextUI floatingText;

        public int SlotCount => slots != null ? slots.Length : 0;

        public SkillSlotState GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public void Init(int[] skillSlotIds, HeroController hero, BattleManager bm, FloatingTextUI ft)
        {
            this.hero = hero;
            this.battleManager = bm;
            this.floatingText = ft;

            slots = new SkillSlotState[skillSlotIds.Length];
            for (int i = 0; i < skillSlotIds.Length; i++)
            {
                slots[i] = new SkillSlotState
                {
                    skillId = skillSlotIds[i],
                    level = 0,
                    xp = 0,
                    cooldownRemaining = 0f,
                    maxCooldown = 0f
                };
            }
        }

        private void Update()
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].cooldownRemaining > 0f)
                {
                    slots[i].cooldownRemaining -= Time.deltaTime;
                    if (slots[i].cooldownRemaining < 0f)
                        slots[i].cooldownRemaining = 0f;
                }
            }
        }

        public bool TryUseSkill(int slotIndex)
        {
            if (slots == null || hero == null || hero.IsDead) return false;
            if (slotIndex < 0 || slotIndex >= slots.Length) return false;

            SkillSlotState slot = slots[slotIndex];
            if (slot.level <= 0) return false;
            if (slot.cooldownRemaining > 0f) return false;

            SkillConfig config = ConfigManager.Instance.GetSkillConfig(slot.skillId, slot.level);
            if (config == null || config.cd <= 0) return false;

            hero.UseSkill(config);
            slot.cooldownRemaining = config.cd;
            slot.maxCooldown = config.cd;
            return true;
        }

        public void AddXpToAll(int amount, Vector3 sourcePos)
        {
            if (slots == null) return;

            int levelUpCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level >= 10) continue;
                slots[i].xp += amount;
                while (slots[i].xp >= 10 && slots[i].level < 10)
                {
                    slots[i].xp -= 10;
                    slots[i].level++;
                    levelUpCount++;
                }
            }

            if (levelUpCount > 0 && floatingText != null && hero != null)
            {
                floatingText.Show(
                    levelUpCount > 1 ? $"技能升级 x{levelUpCount}!" : "技能升级!",
                    hero.transform.position + Vector3.up * 1.8f,
                    Color.yellow);
            }
        }
    }
}
