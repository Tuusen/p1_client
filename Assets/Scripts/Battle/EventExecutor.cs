using UnityEngine;

namespace GeometryTD
{
    public struct EventContext
    {
        public IBuffTarget caster;
        public IBuffTarget target;
        public BattleManager battleManager;
        public Vector3 position;
    }

    public static class EventExecutor
    {
        public static void ExecuteEvents(int[] eventIds, EventContext ctx)
        {
            if (eventIds == null || eventIds.Length == 0) return;
            for (int i = 0; i < eventIds.Length; i++)
                ExecuteEvent(eventIds[i], ctx);
        }

        public static void ExecuteEvent(int eventId, EventContext ctx)
        {
            var config = Cfg.Event.Get(eventId);
            if (config == null) return;

            var args = config.args;
            var effectTarget = ctx.target ?? ctx.caster;

            switch (config.type)
            {
                case EventType.Damage:
                    HandleDamage(args, effectTarget, ctx);
                    break;
                case EventType.Shield:
                    HandleShield(args, effectTarget);
                    break;
                case EventType.DamagePercentage:
                    HandleDamagePercentage(args, effectTarget, ctx);
                    break;
                case EventType.Knockback:
                    HandleKnockback(args, effectTarget, ctx);
                    break;
                case EventType.GrantXp:
                    HandleGrantXp(args, ctx);
                    break;
                case EventType.GainEnergy:
                    HandleGainEnergy(args, ctx);
                    break;
                case EventType.GainBuff:
                    HandleGainBuff(args, effectTarget, ctx);
                    break;
                case EventType.GainPassive:
                    HandleGainPassive(args, effectTarget, ctx);
                    break;
                case EventType.Summon:
                    HandleSummon(args, ctx);
                    break;
                case EventType.Dispel:
                    HandleDispel(args, effectTarget);
                    break;
                default:
                    Debug.LogWarning($"[EventExecutor] 未知事件类型: {config.type}, eventId={eventId}");
                    break;
            }
        }

        private static void HandleDamage(int[] args, IBuffTarget target, EventContext ctx)
        {
            if (args == null || args.Length < 2 || target == null) return;
            int dmgRate = args[0];
            int dmgType = args[1];
            if (ctx.caster == null || ctx.caster.Attrs == null) return;

            if (dmgRate > 0)
            {
                var dmgCtx = new DamageContext
                {
                    attackerAttrs = ctx.caster.Attrs,
                    defenderAttrs = target.Attrs,
                    skillDmgRatio = dmgRate,
                    skillDmgType = dmgType,
                    isTargetBoss = target is BossController,
                    isTargetElite = (target as MonsterController)?.IsElite ?? false
                };
                var result = DamageCalculator.Calculate(dmgCtx);
                if (!result.isMiss && result.finalDamage > 0)
                    target.OnBuffDamage(result.finalDamage);
            }
            else if (dmgRate < 0)
            {
                float baseAtk = ctx.caster.Attrs.GetAttack();
                float amount = baseAtk * Mathf.Abs(dmgRate) / 10000f;
                target.OnBuffHeal(amount);
            }
        }

        private static void HandleDamagePercentage(int[] args, IBuffTarget target, EventContext ctx) 
        {
            if (args == null || args.Length < 2 || target == null) return;
            int dmgRate = args[0];
            int dmgType = args[1];
            if (ctx.caster == null || ctx.caster.Attrs == null) return;

            if (dmgRate > 0)
            {
                var dmgCtx = new DamageContext
                {
                    attackerAttrs = ctx.caster.Attrs,
                    defenderAttrs = target.Attrs,
                    skillDmgRatio = dmgRate,
                    skillDmgType = dmgType,
                    isTargetBoss = target is BossController,
                    isTargetElite = (target as MonsterController)?.IsElite ?? false
                };
                var isMiss = DamageCalculator.checkMiss(dmgCtx);
                if (!isMiss) {
                    float maxHp = target.Attrs.GetMaxHp();
                    float amount = maxHp * Mathf.Abs(dmgRate) / 10000f;
                    target.OnBuffDamage(amount);
                }
            }
            else if (dmgRate < 0)
            {
                float maxHp = ctx.caster.Attrs.GetMaxHp();
                float amount = maxHp * Mathf.Abs(dmgRate) / 10000f;
                target.OnBuffHeal(amount);
            }
        }

        private static void HandleShield(int[] args, IBuffTarget target)
        {
            if (args == null || args.Length < 1 || target == null) return;
            int shieldValue = args[0];
            target.AddShield(shieldValue);
        }

        private static void HandleKnockback(int[] args, IBuffTarget target, EventContext ctx)
        {
            if (args == null || args.Length < 1 || target == null) return;
            if (target.BuffSystem != null && target.BuffSystem.IsInvincible()) return;
            float distance = args[0]/10000f;

            var mono = target as MonoBehaviour;
            if (mono != null && ctx.caster != null)
            {
                Vector3 dir = (target.Position - ctx.caster.Position).normalized;
                mono.transform.position += dir * distance;
            }
        }

        private static void HandleGrantXp(int[] args, EventContext ctx)
        {
            if (args == null || args.Length < 2 || ctx.battleManager == null) return;
            int xpAmount = args[0];
            int skillCount = args[1];
            ctx.battleManager.GrantSkillXp(xpAmount, skillCount);
        }

        private static void HandleGainEnergy(int[] args, EventContext ctx)
        {
            if (args == null || args.Length < 2 || ctx.battleManager == null) return;
            int amount = args[0];
            int energyType = args[1];

            var arcMgr = ctx.battleManager.ArcaneManager;
            if (arcMgr == null) return;

            if (energyType == -1)
            {
                // 所有类型
                for (int i = 1; i <= 4; i++)
                    arcMgr.AddEnergy(i, amount);
            }
            else if (energyType >= 1 && energyType <= 4)
            {
                arcMgr.AddEnergy(energyType, amount);
            }
        }

        private static void HandleGainBuff(int[] args, IBuffTarget target, EventContext ctx)
        {
            if (args == null || args.Length < 1 || target == null) return;
            int buffId = args[0];
            if (target.BuffSystem != null)
                target.BuffSystem.AddBuff(buffId, target, ctx.caster);
        }

        private static void HandleGainPassive(int[] args, IBuffTarget target, EventContext ctx)
        {
            if (args == null || args.Length < 1 || target == null) return;
            int passiveId = args[0];
            if (target.PassiveSystem != null)
                target.PassiveSystem.RegisterPassive(passiveId, ctx);
        }

        private static void HandleDispel(int[] args, IBuffTarget target)
        {
            if (args == null || args.Length < 2 || target == null) return;
            if (target.BuffSystem == null) return;
            int buffId = args[0];
            int count = args[1];

            if (buffId == -1)
                target.BuffSystem.RemoveBuffsByType(1, count);
            else if (buffId == -2)
                target.BuffSystem.RemoveBuffsByType(2, count);
            else
                target.BuffSystem.RemoveBuffByConfigId(buffId, count);
        }

        private static void HandleSummon(int[] args, EventContext ctx)
        {
            if (args == null || args.Length < 4 || ctx.battleManager == null) return;
            int monsterId = args[0];
            float duration = args[1];
            float attrRatio = args[2];
            int extraCount = args[3];

            if (monsterId <= 0) return;
            int totalSummons = 1 + extraCount;

            for (int i = 0; i < totalSummons; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-1.5f, 1.5f), Random.Range(-2f, -0.5f), 0f);
                ctx.battleManager.SpawnSummon(
                    ctx.position + offset, duration, attrRatio, monsterId, false);
            }
        }
    }
}
