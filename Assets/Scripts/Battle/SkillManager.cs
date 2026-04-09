using UnityEngine;

namespace GeometryTD
{
    public class SkillSlotState
    {
        public int skillPoolId;
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

        public static SkillCategory ClassifySkill(SkillConfig config)
        {
            if (config == null) return SkillCategory.Self;

            // Priority: read from config category field
            if (!string.IsNullOrEmpty(config.category) &&
                System.Enum.TryParse<SkillCategory>(config.category, out var cat))
            {
                return cat;
            }

            // Fallback
            if (config.bulletSpeed > 0) return SkillCategory.Projectile;
            if (config.dmg > 0) return SkillCategory.Aoe;
            return SkillCategory.Self;
        }

        public SkillSlotState GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public void Init(int[] skillPoolIds, HeroController hero, BattleManager bm, FloatingTextUI ft)
        {
            this.hero = hero;
            this.battleManager = bm;

            slots = new SkillSlotState[skillPoolIds.Length];
            for (int i = 0; i < skillPoolIds.Length; i++)
            {
                string name = "";
                var poolConfig = Cfg.SkillPool.Get(skillPoolIds[i]);
                if (poolConfig != null) name = poolConfig.name;

                slots[i] = new SkillSlotState
                {
                    skillPoolId = skillPoolIds[i],
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

            SkillConfig config = ConfigManager.Instance.GetSkillConfigByPool(slot.skillPoolId, slot.level);
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
                // 收集非满级的技能槽索引
                int[] eligible = new int[slots.Length];
                int eligibleCount = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].level < 10)
                        eligible[eligibleCount++] = i;
                }
                if (eligibleCount == 0) return;

                int count = Mathf.Min(slotCount, eligibleCount);
                // Fisher-Yates 洗牌后取前 count 个
                for (int i = eligibleCount - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int temp = eligible[i];
                    eligible[i] = eligible[j];
                    eligible[j] = temp;
                }

                for (int i = 0; i < count; i++)
                {
                    AddXpToSlot(eligible[i], xpAmount);
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

        public int PickRandomEligibleSlot()
        {
            if (slots == null || slots.Length == 0) return -1;

            int candidateCount = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10)
                    candidateCount++;
            }
            if (candidateCount == 0) return -1;

            int pick = Random.Range(0, candidateCount);
            int current = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].level < 10)
                {
                    if (current == pick) return i;
                    current++;
                }
            }
            return -1;
        }

        public void AddXpToRandomSlot(int slotIndex, int min, int max)
        {
            if (slots == null || slots.Length == 0) return;
            if (min < 0) min = 0;
            if (max < min) max = min;

            int targetIndex = slotIndex >= 0 ? slotIndex : PickRandomEligibleSlot();
            if (targetIndex < 0 || targetIndex >= slots.Length) return;

            int amount = Random.Range(min, max + 1);
            AddXpToSlot(targetIndex, amount);
        }
    }
}
