using UnityEngine;

namespace GeometryTD
{
    public class ArcaneSlotState
    {
        public int arcaneId;
        public string arcaneName;
        public int level;                   // 当前等级，0=未解锁
        public ArcaneConfig cachedConfig;   // 缓存配置引用
    }

    public class ArcaneManager : MonoBehaviour
    {
        private ArcaneSlotState[] slots;
        private int[] runes = new int[4];   // index 0=fire,1=ice,2=electric,3=wind
        private int[] energy = new int[4];  // 0-9, 10 converts to 1 rune

        private BattleManager battleManager;
        private HeroController hero;

        public System.Action OnRunesChanged;
        public System.Action<int> OnArcaneUpgraded;

        public int SlotCount => slots != null ? slots.Length : 0;

        public void Init(int[] arcaneSlotIds, HeroController hero, BattleManager bm)
        {
            this.hero = hero;
            this.battleManager = bm;

            slots = new ArcaneSlotState[arcaneSlotIds.Length];
            for (int i = 0; i < arcaneSlotIds.Length; i++)
            {
                var config = Cfg.Arcane.Get(arcaneSlotIds[i]);
                slots[i] = new ArcaneSlotState
                {
                    arcaneId = arcaneSlotIds[i],
                    arcaneName = config != null ? config.name : "",
                    level = 0,
                    cachedConfig = config
                };
            }
        }

        public ArcaneSlotState GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public int GetRune(int runeType)
        {
            int idx = runeType - 1;
            if (idx < 0 || idx >= 4) return 0;
            return runes[idx];
        }

        public int GetEnergy(int runeType)
        {
            int idx = runeType - 1;
            if (idx < 0 || idx >= 4) return 0;
            return energy[idx];
        }

        public int GetArcaneLevel(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return 0;
            return slots[slotIndex].level;
        }

        // Called by SkillManager after a skill is used
        public void AddEnergy(int mpType, int amount)
        {
            if (mpType == 99)
            {
                // All types
                for (int i = 0; i < 4; i++)
                    AddEnergyToType(i, amount);
            }
            else
            {
                int idx = mpType - 1;
                if (idx >= 0 && idx < 4)
                    AddEnergyToType(idx, amount);
            }
        }

        private void AddEnergyToType(int idx, int amount)
        {
            energy[idx] += amount;
            while (energy[idx] >= 10)
            {
                energy[idx] -= 10;
                runes[idx]++;
            }
            OnRunesChanged?.Invoke();
        }

        private int GetModifiedCost(ArcaneConfig config)
        {
            int cost = config.runeCost;
            if (hero != null)
            {
                int costMod = hero.BuffSystem.GetArcaneCostModifier(config.id, config.runeType);
                cost = Mathf.Max(0, cost + costMod);
            }
            return cost;
        }

        public bool CanUpgrade(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return false;
            var slot = slots[slotIndex];

            var config = slot.cachedConfig;
            if (config == null) return false;

            int runeIdx = config.runeType - 1;
            if (runeIdx < 0 || runeIdx >= 4) return false;

            int modifiedCost = GetModifiedCost(config);
            return runes[runeIdx] >= modifiedCost;
        }

        public bool TryUpgradeArcane(int slotIndex)
        {
            if (!CanUpgrade(slotIndex)) return false;

            var slot = slots[slotIndex];
            var config = slot.cachedConfig;

            // 扣除符能
            int runeIdx = config.runeType - 1;
            int modifiedCost = GetModifiedCost(config);
            runes[runeIdx] -= modifiedCost;

            // 升级
            slot.level++;

            // 刷新被动
            RefreshArcanePassives(slotIndex);

            OnArcaneUpgraded?.Invoke(slotIndex);
            OnRunesChanged?.Invoke();

            return true;
        }

        private void RefreshArcanePassives(int slotIndex)
        {
            if (hero == null || hero.PassiveSystem == null) return;

            var slot = slots[slotIndex];
            var config = slot.cachedConfig;
            if (config == null) return;

            // 清除该奥术的旧被动
            hero.PassiveSystem.RemoveBySource(slot.arcaneId);

            if (config.passives == null || config.passives.Length == 0) return;

            // 计算奥术伤害系数: dmg + upDmg * (level - 1)
            int arcaneDmgRatio = config.dmg + config.upDmg * (slot.level - 1);

            // 找到 <= slot.level 的最大等级阈值
            int maxThreshold = -1;
            for (int i = 0; i < config.passives.Length; i++)
            {
                int lvl = config.passives[i].level;
                if (lvl <= slot.level && lvl > maxThreshold)
                    maxThreshold = lvl;
            }

            if (maxThreshold < 0) return;

            // 构造EventContext
            var ctx = new EventContext
            {
                caster = hero,
                target = hero,
                battleManager = battleManager,
                position = hero.Position,
                arcaneDmgRatio = arcaneDmgRatio
            };

            // 注册该阈值下的所有被动
            for (int i = 0; i < config.passives.Length; i++)
            {
                if (config.passives[i].level == maxThreshold)
                {
                    hero.PassiveSystem.RegisterPassive(
                        config.passives[i].id, ctx,
                        sourceArcaneId: slot.arcaneId,
                        isProtected: true);
                }
            }
        }

        public void ResetAllLevels()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                if (hero != null && hero.PassiveSystem != null)
                    hero.PassiveSystem.RemoveBySource(slots[i].arcaneId);
                slots[i].level = 0;
            }
        }
    }
}
