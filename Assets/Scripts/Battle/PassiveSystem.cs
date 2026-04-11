using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class ActivePassive
    {
        public int passiveConfigId;
        public PassiveConfig cachedConfig;
        public int triggerCount;
        public bool isActive;
    }

    public class PassiveSystem
    {
        private List<ActivePassive> passives = new List<ActivePassive>();
        private int triggerDepth = 0;
        private const int MaxTriggerDepth = 5;

        public void RegisterPassive(int passiveConfigId, EventContext ctx)
        {
            var config = Cfg.Passive.Get(passiveConfigId);
            if (config == null) return;

            // eventTarget 为空 → 立即触发
            if (config.eventTarget == null || config.eventTarget.Length == 0)
            {
                // 概率判断
                if (config.odds > 0 && config.odds < 10000)
                {
                    if (Random.Range(0, 10000) >= config.odds)
                        return;
                }

                EventExecutor.ExecuteEvents(config.events, ctx);

                // 释放技能
                if (config.skills != null && config.skills.Length > 0)
                {
                    for (int j = 0; j < config.skills.Length; j++)
                    {
                        var skillConfig = Cfg.Skill.Get(config.skills[j]);
                        if (skillConfig != null)
                        {
                            var hero = ctx.caster as HeroController;
                            if (hero != null)
                                hero.UseSkill(skillConfig);
                        }
                    }
                }

                // 附加被动
                if (config.passives != null && config.passives.Length > 0)
                {
                    for (int j = 0; j < config.passives.Length; j++)
                        RegisterPassive(config.passives[j], ctx);
                }
                return;
            }

            // 注册等待触发
            var passive = new ActivePassive
            {
                passiveConfigId = passiveConfigId,
                cachedConfig = config,
                triggerCount = 0,
                isActive = true
            };
            passives.Add(passive);
        }

        /// <summary>
        /// 检查 triggerCode 是否在 codes 数组中（精确匹配）。
        /// eventTarget 触发码定义：
        ///   空=获得立刻触发/不会自动移除；
        ///   1=闪避后；2=命中后；3=战斗开始后；
        ///   101=受伤害前；102=受伤害时；103=受伤害后；104=护盾受伤后；105=护盾破碎后；
        ///   201=受治疗前；202=受治疗时；203=受治疗后；204=获得护盾时；
        ///   300=击败任意；301=击败小怪；302=击败精英；303=击败boss；
        ///   401=死亡前；402=死亡后；
        ///   501=释放普攻前；502=释放普攻后；503=释放技能前；504=释放技能后；505=释放奥术前；506=释放奥术后；
        ///   601=暴击后；602=被暴击后；
        ///   1001-1999=触发1-999次；（该项仅作为移除时检测，触发时不会检测）
        /// </summary>
        public void OnTrigger(int triggerCode, EventContext ctx)
        {
            if (triggerDepth >= MaxTriggerDepth) return;
            triggerDepth++;
            try
            {
                for (int i = passives.Count - 1; i >= 0; i--)
                {
                    var p = passives[i];
                    if (!p.isActive) continue;
                    var cfg = p.cachedConfig;
                    if (cfg == null) continue;

                    // 检查是否匹配触发时机
                    if (!ContainsCode(cfg.eventTarget, triggerCode))
                        continue;

                    // 检查触发条件
                    if (!CheckConditions(cfg.eventCond, ctx))
                        continue;

                    // 概率判断：odds <= 0 或 >= 10000 视为必定触发
                    if (cfg.odds > 0 && cfg.odds < 10000)
                    {
                        if (Random.Range(0, 10000) >= cfg.odds)
                            continue;
                    }

                    // 执行事件
                    EventExecutor.ExecuteEvents(cfg.events, ctx);

                    // 释放技能
                    if (cfg.skills != null && cfg.skills.Length > 0)
                    {
                        for (int j = 0; j < cfg.skills.Length; j++)
                        {
                            var skillConfig = Cfg.Skill.Get(cfg.skills[j]);
                            if (skillConfig != null)
                            {
                                var hero = ctx.caster as HeroController;
                                if (hero != null)
                                    hero.UseSkill(skillConfig);
                            }
                        }
                    }

                    // 附加被动
                    if (cfg.passives != null && cfg.passives.Length > 0)
                    {
                        for (int j = 0; j < cfg.passives.Length; j++)
                            RegisterPassive(cfg.passives[j], ctx);
                    }

                    p.triggerCount++;

                    // 检查移除条件
                    if (ShouldRemove(p, triggerCode))
                    {
                        p.isActive = false;
                        passives.RemoveAt(i);
                    }
                }
            }
            finally
            {
                triggerDepth--;
            }
        }

        private bool ContainsCode(int[] codes, int code)
        {
            if (codes == null) return false;
            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i] == code) return true;
            }
            return false;
        }

        private bool CheckConditions(PassiveConfig.EventCondItem[] conds, EventContext ctx)
        {
            if (conds == null || conds.Length == 0) return true;

            for (int i = 0; i < conds.Length; i++)
            {
                var cond = conds[i];
                switch (cond.id)
                {
                    case 1: // 目标生命值百分比
                        if (cond.args != null && cond.args.Length >= 2)
                        {
                            var condTarget = ctx.target ?? ctx.caster;
                            if (condTarget != null)
                            {
                                int hpPct = condTarget.GetHpPercent();
                                // args[0]=1: 要求 < 阈值, args[0]=2: 要求 > 阈值
                                if (cond.args[0] == 1 && hpPct >= cond.args[1]) return false;
                                if (cond.args[0] == 2 && hpPct <= cond.args[1]) return false;
                            }
                        }
                        break;
                    case 2: // 目标护盾百分比
                        if (cond.args != null && cond.args.Length >= 2)
                        {
                            var condTarget = ctx.target ?? ctx.caster;
                            if (condTarget != null)
                            {
                                int shieldPct = condTarget.GetShieldPercent();
                                if (cond.args[0] == 1 && shieldPct >= cond.args[1]) return false;
                                if (cond.args[0] == 2 && shieldPct <= cond.args[1]) return false;
                            }
                        }
                        break;
                }
            }
            return true;
        }

        private bool ShouldRemove(ActivePassive p, int triggerCode)
        {
            var removeCodes = p.cachedConfig.eventRemove;
            if (removeCodes == null || removeCodes.Length == 0) return false;

            for (int i = 0; i < removeCodes.Length; i++)
            {
                int code = removeCodes[i];

                // 1001-1999 = 触发N次后移除
                if (code >= 1001 && code <= 1999)
                {
                    int maxTriggers = code - 1000;
                    if (p.triggerCount >= maxTriggers)
                        return true;
                }
                else if (code == triggerCode)
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            passives.Clear();
        }
    }
}
