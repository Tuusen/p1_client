using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class ArcaneSlotState
    {
        public int arcaneId;
        public string arcaneName;
        public float cooldownRemaining;
        public float maxCooldown;
    }

    public class ActiveArcane
    {
        public int arcaneId;
        public Vector3 position;
        public float tickInterval;
        public float tickTimer;
        public ArcaneConfig config;
    }

    public class ArcaneManager : MonoBehaviour
    {
        private ArcaneSlotState[] slots;
        private int[] runes = new int[4];   // index 0=fire,1=ice,2=electric,3=wind
        private int[] energy = new int[4];  // 0-9, 10 converts to 1 rune
        private List<ActiveArcane> activeArcanes = new List<ActiveArcane>();

        private BattleManager battleManager;
        private HeroController hero;

        public System.Action OnRunesChanged;
        public System.Action<int> OnArcanePlaced;
        public System.Action<int> OnArcaneCooldownDone;

        public int SlotCount => slots != null ? slots.Length : 0;

        public void Init(int[] arcaneSlotIds, HeroController hero, BattleManager bm)
        {
            this.hero = hero;
            this.battleManager = bm;

            slots = new ArcaneSlotState[arcaneSlotIds.Length];
            for (int i = 0; i < arcaneSlotIds.Length; i++)
            {
                var config = ConfigManager.Instance.GetArcaneConfig(arcaneSlotIds[i]);
                slots[i] = new ArcaneSlotState
                {
                    arcaneId = arcaneSlotIds[i],
                    arcaneName = config != null ? config.name : "",
                    cooldownRemaining = 0f,
                    maxCooldown = 0f
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

        public List<ActiveArcane> GetActiveArcanes() => activeArcanes;

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

        public bool CanCast(int slotIndex)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return false;
            var slot = slots[slotIndex];
            if (slot.cooldownRemaining > 0f) return false;

            var config = ConfigManager.Instance.GetArcaneConfig(slot.arcaneId);
            if (config == null) return false;

            int runeIdx = config.runeType - 1;
            if (runeIdx < 0 || runeIdx >= 4) return false;
            return runes[runeIdx] >= config.runeCost;
        }

        public bool TryCastArcane(int slotIndex, Vector3 worldPos)
        {
            if (!CanCast(slotIndex)) return false;

            var slot = slots[slotIndex];
            var config = ConfigManager.Instance.GetArcaneConfig(slot.arcaneId);

            // Consume runes
            int runeIdx = config.runeType - 1;
            runes[runeIdx] -= config.runeCost;
            OnRunesChanged?.Invoke();

            // Start cooldown
            slot.cooldownRemaining = config.cd;
            slot.maxCooldown = config.cd;

            // Create active arcane
            var active = new ActiveArcane
            {
                arcaneId = slot.arcaneId,
                position = worldPos,
                tickInterval = config.tickInterval,
                tickTimer = 0f, // tick immediately on first frame
                config = config
            };
            activeArcanes.Add(active);
            OnArcanePlaced?.Invoke(slotIndex);

            return true;
        }

        private void Update()
        {
            if (slots == null) return;

            // Update slot cooldowns
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].cooldownRemaining > 0f)
                {
                    slots[i].cooldownRemaining -= Time.deltaTime;
                    if (slots[i].cooldownRemaining <= 0f)
                    {
                        slots[i].cooldownRemaining = 0f;
                        OnArcaneCooldownDone?.Invoke(i);
                    }
                }
            }

            // Tick active arcanes
            for (int i = activeArcanes.Count - 1; i >= 0; i--)
            {
                var a = activeArcanes[i];
                a.tickTimer -= Time.deltaTime;
                if (a.tickTimer <= 0f)
                {
                    a.tickTimer += a.tickInterval;
                    TickArcane(a);
                }
            }
        }

        private void TickArcane(ActiveArcane a)
        {
            if (battleManager == null || hero == null) return;

            var config = a.config;
            float actualDmg = hero.BaseAttack * config.dmg / 10000f;
            bool fullScreen = config.radius < 0f;

            if (fullScreen)
            {
                battleManager.DealFullScreenAoe(a.position, actualDmg, config.enemyEvents, hero);
                SpawnTickVfx(a.position, 20f, config.dmgType);
            }
            else
            {
                // 对范围内敌人造成伤害 + 执行敌方事件
                var enemies = battleManager.GetEnemiesInRadius(a.position, config.radius);
                foreach (var enemy in enemies)
                {
                    if (enemy == null) continue;

                    IBuffTarget target = enemy.GetComponent<MonsterController>() as IBuffTarget;
                    if (target == null)
                        target = enemy.GetComponent<BossController>() as IBuffTarget;

                    if (target != null)
                    {
                        target.OnBuffDamage(actualDmg);

                        if (config.enemyEvents != null && config.enemyEvents.Length > 0)
                        {
                            var ctx = new EventContext
                            {
                                caster = hero,
                                target = target,
                                battleManager = battleManager,
                                position = enemy.position
                            };
                            EventExecutor.ExecuteEvents(config.enemyEvents, ctx);
                        }
                    }
                }

                SpawnTickVfx(a.position, config.radius, config.dmgType);
            }

            // 执行自身事件（对英雄）
            if (config.events != null && config.events.Length > 0)
            {
                var selfCtx = new EventContext
                {
                    caster = hero,
                    target = hero,
                    battleManager = battleManager,
                    position = hero.Position
                };
                EventExecutor.ExecuteEvents(config.events, selfCtx);
            }
        }

        // Spawn a brief expanding circle VFX at arcane tick position
        private void SpawnTickVfx(Vector3 center, float radius, int dmgType)
        {
            Color vfxColor;
            switch (dmgType)
            {
                case 1: vfxColor = new Color(1f, 0.4f, 0.1f, 0.5f); break;  // fire
                case 2: vfxColor = new Color(0.3f, 0.7f, 1f, 0.5f); break;  // ice
                case 3: vfxColor = new Color(0.9f, 0.9f, 0.2f, 0.5f); break; // electric
                case 4: vfxColor = new Color(0.3f, 0.9f, 0.5f, 0.5f); break; // wind
                default: vfxColor = new Color(1f, 1f, 1f, 0.4f); break;
            }

            GameObject vfx = new GameObject("ArcaneTickVfx");
            vfx.transform.position = center;
            var sr = vfx.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 80;
            sr.color = vfxColor;

            int texSize = 64;
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            float c = texSize / 2f;
            float rSq = c * c;
            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= rSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), texSize / (radius * 2f));

            Destroy(vfx, 0.5f);
        }
    }
}
