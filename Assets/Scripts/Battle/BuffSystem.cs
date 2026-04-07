using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class BuffEntry
    {
        public int buffConfigId;
        public BuffConfig cachedConfig;
        public int stackCount;
        public float remainingTime;   // 秒
        public float tickTimer;       // 跳伤计时（秒）
        public IBuffTarget caster;    // 施加者引用，用于 evtDmgRate 伤害计算
    }

    public interface IBuffTarget
    {
        AttrComponent Attrs { get; }
        BuffSystem BuffSystem { get; }
        PassiveSystem PassiveSystem { get; }
        void OnBuffDamage(float dmg);
        void OnBuffHeal(float heal);
        void AddShield(int value);
        bool IsDead { get; }
        Vector3 Position { get; }
        int GetHpPercent();
        int GetShieldPercent();
    }

    public class BuffSystem
    {
        private List<BuffEntry> buffs = new List<BuffEntry>();

        public void AddBuff(int buffConfigId, IBuffTarget target, IBuffTarget caster = null)
        {
            var config = Cfg.Buff.Get(buffConfigId);
            if (config == null) return;

            // 概率判定
            if (config.probability > 0 && config.probability < 10000)
            {
                if (Random.Range(0, 10000) >= config.probability)
                    return;
            }

            // 查找已存在的同ID buff
            BuffEntry existing = null;
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].buffConfigId == buffConfigId)
                {
                    existing = buffs[i];
                    break;
                }
            }

            if (existing != null)
            {
                // 叠加逻辑
                if (config.overlap > 0 && existing.stackCount < config.overlap)
                {
                    existing.stackCount++;
                }
                // 刷新持续时间
                if (config.lastTime > 0)
                    existing.remainingTime = config.lastTime / 1000f;
                existing.tickTimer = 0f;
                if (caster != null)
                    existing.caster = caster;
                return;
            }

            // 新增buff
            var entry = new BuffEntry
            {
                buffConfigId = buffConfigId,
                cachedConfig = config,
                stackCount = 1,
                remainingTime = config.lastTime > 0 ? config.lastTime / 1000f : -1f,
                tickTimer = 0f,
                caster = caster
            };
            buffs.Add(entry);
        }

        public void RemoveBuffByConfigId(int buffConfigId, int count)
        {
            int removed = 0;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].buffConfigId == buffConfigId)
                {
                    buffs.RemoveAt(i);
                    removed++;
                    if (count > 0 && removed >= count) break;
                }
            }
        }

        public void RemoveBuffsByType(int buffType, int count = 0)
        {
            int removed = 0;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].cachedConfig != null && buffs[i].cachedConfig.type == buffType)
                {
                    buffs.RemoveAt(i);
                    removed++;
                    if (count > 0 && removed >= count) break;
                }
            }
        }

        public bool HasSpecialEffect(int specialType)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.specialEvent == null) continue;
                for (int j = 0; j < cfg.specialEvent.Length; j++)
                {
                    if (cfg.specialEvent[j].type == specialType)
                        return true;
                }
            }
            return false;
        }

        public bool IsInvincible()
        {
            return HasSpecialEffect(BuffSpecialEventType.Invincible);
        }

        public bool IsFrozen()
        {
            if (IsInvincible()) return false;
            return HasSpecialEffect(BuffSpecialEventType.Freeze);
        }

        public void Tick(float deltaTime, IBuffTarget target)
        {
            if (target == null || target.IsDead) return;

            ReapplyAttrBonuses(target);

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                var cfg = buff.cachedConfig;
                if (cfg == null) { buffs.RemoveAt(i); continue; }

                // 跳伤/跳效果
                if (cfg.jumpTime > 0)
                {
                    float jumpSec = cfg.jumpTime / 1000f;
                    buff.tickTimer += deltaTime;
                    while (buff.tickTimer >= jumpSec)
                    {
                        buff.tickTimer -= jumpSec;

                        // evtDmgRate 伤害计算（无敌时跳过）
                        if (cfg.evtDmgRate != null && cfg.evtDmgRate.Length > 0 && !IsInvincible())
                        {
                            var casterMono = buff.caster as MonoBehaviour;
                            if (casterMono != null && !buff.caster.IsDead)
                            {
                                for (int j = 0; j < cfg.evtDmgRate.Length; j++)
                                {
                                    var rateEntry = cfg.evtDmgRate[j];
                                    var dmgCtx = new DamageContext
                                    {
                                        attackerAttrs = buff.caster.Attrs,
                                        defenderAttrs = target.Attrs,
                                        skillDmgRatio = rateEntry.rate,
                                        skillDmgType = rateEntry.type,
                                        isTargetBoss = target is BossController,
                                        isTargetElite = (target as MonsterController)?.IsElite ?? false
                                    };
                                    var result = DamageCalculator.Calculate(dmgCtx);
                                    if (!result.isMiss && result.finalDamage > 0)
                                        target.OnBuffDamage(result.finalDamage);
                                }
                            }
                        }

                        // evtDamage 触发事件
                        if (cfg.evtDamage != null && cfg.evtDamage.Length > 0)
                        {
                            var ctx = new EventContext
                            {
                                caster = target,
                                target = target,
                                position = target.Position
                            };
                            EventExecutor.ExecuteEvents(cfg.evtDamage, ctx);
                        }

                        if (target.IsDead) return;
                    }
                }

                // 持续时间倒计时
                if (buff.remainingTime > 0)
                {
                    buff.remainingTime -= deltaTime;
                    if (buff.remainingTime <= 0)
                    {
                        // buff过期 - 触发结束事件
                        if (cfg.evtWhenEnd != null && cfg.evtWhenEnd.Length > 0)
                        {
                            var endCtx = new EventContext
                            {
                                caster = buff.caster ?? target,
                                target = target,
                                position = target.Position
                            };
                            EventExecutor.ExecuteEvents(cfg.evtWhenEnd, endCtx);
                        }

                        buffs.RemoveAt(i);
                    }
                }
                // remainingTime == -1 表示永久buff，不自动过期
            }
        }

        private void ReapplyAttrBonuses(IBuffTarget target)
        {
            if (target.Attrs == null) return;

            target.Attrs.ClearBonuses();
            bool invincible = IsInvincible();

            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.attribute == null) continue;
                // 无敌时跳过减益buff的属性（防止减速等控制效果）
                if (invincible && cfg.type == 2) continue;

                int stacks = buffs[i].stackCount;
                for (int j = 0; j < cfg.attribute.Length; j++)
                {
                    var attr = cfg.attribute[j];
                    target.Attrs.AddBonus(attr.id, attr.value * stacks);
                }
            }
        }

        public void Clear()
        {
            buffs.Clear();
        }

        public List<BuffEntry> GetActiveBuffs()
        {
            return buffs;
        }

        /// <summary>
        /// Type 1: 获取技能伤害累积修正值（万分比）
        /// </summary>
        public int GetSkillDmgModifier(int skillId)
        {
            int total = 0;
            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.specialEvent == null) continue;
                for (int j = 0; j < cfg.specialEvent.Length; j++)
                {
                    var se = cfg.specialEvent[j];
                    if (se.type != BuffSpecialEventType.SkillDmgMod) continue;
                    if (se.args == null || se.args.Length < 2) continue;
                    if (se.args[0] != -1 && se.args[0] != skillId) continue;
                    total += se.args[1] * buffs[i].stackCount;
                }
            }
            return total;
        }

        /// <summary>
        /// Type 2: 获取奥术消耗累积改变值
        /// </summary>
        public int GetArcaneCostModifier(int arcaneId, int runeType)
        {
            int total = 0;
            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.specialEvent == null) continue;
                for (int j = 0; j < cfg.specialEvent.Length; j++)
                {
                    var se = cfg.specialEvent[j];
                    if (se.type != BuffSpecialEventType.ArcaneCostMod) continue;
                    if (se.args == null || se.args.Length < 3) continue;
                    if (se.args[0] != -1 && se.args[0] != arcaneId) continue;
                    if (se.args[1] != -1 && se.args[1] != runeType) continue;
                    total += se.args[2] * buffs[i].stackCount;
                }
            }
            return total;
        }

        /// <summary>
        /// Type 3: 收集额外子弹事件ID
        /// </summary>
        public List<int> CollectExtraBulletEventIds(int skillId)
        {
            List<int> result = null;
            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.specialEvent == null) continue;
                for (int j = 0; j < cfg.specialEvent.Length; j++)
                {
                    var se = cfg.specialEvent[j];
                    if (se.type != BuffSpecialEventType.SkillBulletMod) continue;
                    if (se.args == null || se.args.Length < 2) continue;
                    if (se.args[0] != -1 && se.args[0] != skillId) continue;
                    if (result == null) result = new List<int>();
                    result.Add(se.args[1]);
                }
            }
            return result;
        }

        /// <summary>
        /// Type 102: 反击 - 被攻击时释放技能
        /// </summary>
        public static void TryCounterAttack(IBuffTarget defender, IBuffTarget attacker,
            BuffSystem buffSystem, BattleManager battleManager)
        {
            if (attacker == null || battleManager == null || buffSystem == null) return;
            var attackerMono = attacker as MonoBehaviour;
            if (attackerMono == null || attacker.IsDead) return;

            for (int i = 0; i < buffSystem.buffs.Count; i++)
            {
                var cfg = buffSystem.buffs[i].cachedConfig;
                if (cfg == null || cfg.specialEvent == null) continue;
                for (int j = 0; j < cfg.specialEvent.Length; j++)
                {
                    var se = cfg.specialEvent[j];
                    if (se.type != BuffSpecialEventType.Counter) continue;
                    if (se.args == null || se.args.Length < 1) continue;

                    int skillId = se.args[0];
                    var skillConfig = Cfg.Skill.Get(skillId);
                    if (skillConfig == null) continue;

                    float atk = defender.Attrs.GetAttack();
                    float actualDmg = atk * skillConfig.dmg / 10000f;

                    var bulletData = BulletEventExecutor.BuildBulletData(skillConfig.bulletEvents);
                    if (skillConfig.enemyEvents != null && skillConfig.enemyEvents.Length > 0)
                    {
                        if (bulletData.attachToTargetEventIds == null)
                            bulletData.attachToTargetEventIds = new List<int>();
                        for (int k = 0; k < skillConfig.enemyEvents.Length; k++)
                            bulletData.attachToTargetEventIds.Add(skillConfig.enemyEvents[k]);
                    }

                    float skillRange = skillConfig.attack_range > 0 ? skillConfig.attack_range : 10f;
                    var defenderMono = defender as MonoBehaviour;
                    if (defenderMono != null)
                    {
                        battleManager.SpawnSkillBulletWithScatter(
                            defenderMono.transform.position, attackerMono.transform,
                            actualDmg, skillConfig.bulletSpeed, bulletData,
                            skillConfig.bulletStyleId, skillRange, defender);
                    }
                }
            }
        }
    }
}
