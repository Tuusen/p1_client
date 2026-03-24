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

        public System.Action<int, int> OnSlotLevelUp;

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

        public SkillUseInfo TryUseSkill(int slotIndex, Vector3 releaseWorldPos)
        {
            SkillUseInfo info = new SkillUseInfo();

            if (slots == null || hero == null)
            {
                info.result = SkillUseResult.InvalidSlot;
                return info;
            }
            if (hero.IsDead)
            {
                info.result = SkillUseResult.InvalidSlot;
                return info;
            }
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                info.result = SkillUseResult.InvalidSlot;
                return info;
            }

            SkillSlotState slot = slots[slotIndex];

            if (slot.level <= 0)
            {
                info.result = SkillUseResult.LevelTooLow;
                return info;
            }
            if (slot.cooldownRemaining > 0f)
            {
                info.result = SkillUseResult.OnCooldown;
                info.cooldownRemaining = slot.cooldownRemaining;
                return info;
            }

            SkillConfig config = ConfigManager.Instance.GetSkillConfig(slot.skillId, slot.level);
            if (config == null || config.cd <= 0)
            {
                info.result = SkillUseResult.InvalidSlot;
                return info;
            }

            hero.UseSkill(config);

            slot.cooldownRemaining = config.cd;
            slot.maxCooldown = config.cd;
            slot.level = 0;
            slot.xp = 0;

            info.result = SkillUseResult.Success;
            return info;
        }

        public void GrantXpToSlots(int xpAmount, int slotCount)
        {
            if (slots == null || slots.Length == 0) return;

            if (slotCount < 0)
            {
                for (int i = 0; i < slots.Length; i++)
                    AddXpToSlot(i, xpAmount);
            }
            else
            {
                int count = Mathf.Min(slotCount, slots.Length);
                bool[] picked = new bool[slots.Length];
                int assigned = 0;
                int safety = 100;
                while (assigned < count && safety > 0)
                {
                    safety--;
                    int idx = Random.Range(0, slots.Length);
                    if (picked[idx]) continue;
                    picked[idx] = true;
                    AddXpToSlot(idx, xpAmount);
                    assigned++;
                }
            }
        }

        private void AddXpToSlot(int index, int amount)
        {
            if (index < 0 || index >= slots.Length) return;
            var slot = slots[index];
            if (slot.level >= 10) return;

            slot.xp += amount;
            bool leveledUp = false;
            while (slot.xp >= 10 && slot.level < 10)
            {
                slot.xp -= 10;
                slot.level++;
                leveledUp = true;
            }

            if (leveledUp)
                OnSlotLevelUp?.Invoke(index, slot.level);
        }

        public void AddXpToRandomSlot(int min, int max)
        {
            if (slots == null || slots.Length == 0) return;
            if (min < 0) min = 0;
            if (max < min) max = min;

            int candidateCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10 && slots[i].cooldownRemaining <= 0f)
                    candidateCount++;
            }
            if (candidateCount == 0) return;

            int pick = Random.Range(0, candidateCount);
            int current = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10 && slots[i].cooldownRemaining <= 0f)
                {
                    if (current == pick)
                    {
                        int amount = Random.Range(min, max + 1);
                        AddXpToSlot(i, amount);
                        return;
                    }
                    current++;
                }
            }
        }
    }
}
