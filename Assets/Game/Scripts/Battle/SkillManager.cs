using UnityEngine;

namespace GeometryTD
{
    public class SkillSlotState
    {
        public int skillId;
        public string skillName;
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
                string name = "";
                var nameConfig = ConfigManager.Instance.GetSkillConfig(skillSlotIds[i], 0);
                if (nameConfig != null) name = nameConfig.name;

                slots[i] = new SkillSlotState
                {
                    skillId = skillSlotIds[i],
                    skillName = name,
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

            // 释放后重置等级和经验
            slot.level = 0;
            slot.xp = 0;

            return true;
        }

        public void AddXpToRandomSlot(int min, int max)
        {
            if (slots == null || slots.Length == 0) return;
            if (min < 0) min = 0;
            if (max < min) max = min;

            // 收集未满级且不在冷却中的槽位
            int candidateCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10 && slots[i].cooldownRemaining <= 0f)
                    candidateCount++;
            }
            if (candidateCount == 0) return;

            // 随机选一个候选槽位
            int pick = Random.Range(0, candidateCount);
            int current = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10 && slots[i].cooldownRemaining <= 0f)
                {
                    if (current == pick)
                    {
                        int amount = Random.Range(min, max + 1);
                        slots[i].xp += amount;

                        bool leveledUp = false;
                        while (slots[i].xp >= 10 && slots[i].level < 10)
                        {
                            slots[i].xp -= 10;
                            slots[i].level++;
                            leveledUp = true;
                        }

                        if (leveledUp && floatingText != null && hero != null)
                        {
                            floatingText.Show(
                                $"{slots[i].skillName} Lv.{slots[i].level}!",
                                hero.transform.position + Vector3.up * 1.8f,
                                Color.yellow);
                        }
                        return;
                    }
                    current++;
                }
            }
        }
    }
}
