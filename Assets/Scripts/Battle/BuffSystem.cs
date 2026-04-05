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
    }

    public interface IBuffTarget
    {
        AttrComponent Attrs { get; }
        BuffSystem BuffSystem { get; }
        void OnBuffDamage(float dmg);
        void OnBuffHeal(float heal);
        bool IsDead { get; }
        Vector3 Position { get; }
    }

    public class BuffSystem
    {
        private List<BuffEntry> buffs = new List<BuffEntry>();

        public void AddBuff(int buffConfigId, IBuffTarget target)
        {
            var config = ConfigManager.Instance.GetBuffConfig(buffConfigId);
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
                return;
            }

            // 新增buff
            var entry = new BuffEntry
            {
                buffConfigId = buffConfigId,
                cachedConfig = config,
                stackCount = 1,
                remainingTime = config.lastTime > 0 ? config.lastTime / 1000f : -1f,
                tickTimer = 0f
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

        public void RemoveBuffsByType(int buffType)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].cachedConfig != null && buffs[i].cachedConfig.type == buffType)
                    buffs.RemoveAt(i);
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

        public bool IsFrozen()
        {
            return HasSpecialEffect(103);
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

                        // evtDmgRate 快照伤害
                        // TODO: 未生效 - evtDmgRate 快照伤害计算

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
                            // TODO: 未生效 - evtWhenEnd 结束事件
                            Debug.Log($"[BuffSystem] evtWhenEnd 未生效, buffId={buff.buffConfigId}");
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

            for (int i = 0; i < buffs.Count; i++)
            {
                var cfg = buffs[i].cachedConfig;
                if (cfg == null || cfg.attribute == null) continue;

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
    }
}
