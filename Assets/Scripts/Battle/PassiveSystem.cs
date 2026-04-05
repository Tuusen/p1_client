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

        public void RegisterPassive(int passiveConfigId, EventContext ctx)
        {
            var config = ConfigManager.Instance.GetPassiveConfig(passiveConfigId);
            if (config == null) return;

            // eventTarget 为空 → 立即触发
            if (config.eventTarget == null || config.eventTarget.Length == 0)
            {
                EventExecutor.ExecuteEvents(config.events, ctx);
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

        public void OnTrigger(int triggerCode, EventContext ctx)
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

                // 执行事件
                EventExecutor.ExecuteEvents(cfg.events, ctx);
                p.triggerCount++;

                // 检查移除条件
                if (ShouldRemove(p, triggerCode))
                {
                    p.isActive = false;
                    passives.RemoveAt(i);
                }
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

        private bool CheckConditions(PassiveCondEntry[] conds, EventContext ctx)
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
