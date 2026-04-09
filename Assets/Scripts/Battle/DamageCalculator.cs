using UnityEngine;

namespace GeometryTD
{
    public struct DamageContext
    {
        public AttrComponent attackerAttrs;
        public AttrComponent defenderAttrs;
        public int skillDmgRatio;
        public int skillDmgType;
        public bool isTargetBoss;
        public bool isTargetElite;
    }

    public struct DamageResult
    {
        public int finalDamage;
        public bool isCrit;
        public bool isMiss;
    }

    public static class DamageCalculator
    {
        public static bool checkMiss(DamageContext ctx)
        {
            if (ctx.attackerAttrs == null)
                return true;

            // 1. Hit check
            int hitRate = ctx.attackerAttrs.GetFinal(AttributeIds.HitRate);
            int dodgeRate = ctx.defenderAttrs != null ? ctx.defenderAttrs.GetFinal(AttributeIds.DodgeRate) : 0;
            int effectiveHitRate = hitRate - dodgeRate;
            if (effectiveHitRate < 10000)
            {
                int roll = Random.Range(0, 10000);
                if (roll >= effectiveHitRate)
                {
                    return true;
                }
            }

            return false;
        }

        public static DamageResult Calculate(DamageContext ctx)
        {
            var result = new DamageResult();

            if (ctx.attackerAttrs == null)
                return result;

            // 1. Hit check
            if (checkMiss(ctx))
            {
                result.isMiss = true;
                return result;
            }

            // 2. Base damage = GetAttack() * skillDmgRatio / 10000
            long baseAtk = ctx.attackerAttrs.GetAttack();
            long baseDmg = baseAtk * ctx.skillDmgRatio / 10000;

            // 3. Elemental damage bonus (attacker)
            int elemBonusId = AttributeIds.GetElemDmgBonusId(ctx.skillDmgType);
            int elemBonus = 0;
            if (elemBonusId > 0)
                elemBonus = ctx.attackerAttrs.GetFinal(elemBonusId);
            int allElemBonus = ctx.attackerAttrs.GetFinal(AttributeIds.AllElemDmgBonus);
            int totalBonus = elemBonus + allElemBonus;

            // 4. Elemental damage reduction (defender)
            int totalReduce = 0;
            if (ctx.defenderAttrs != null)
            {
                int elemReduceId = AttributeIds.GetElemDmgReduceId(ctx.skillDmgType);
                int elemReduce = 0;
                if (elemReduceId > 0)
                    elemReduce = ctx.defenderAttrs.GetFinal(elemReduceId);
                int allElemReduce = ctx.defenderAttrs.GetFinal(AttributeIds.AllElemDmgReduce);
                totalReduce = elemReduce + allElemReduce;
            }

            // 5. Crit check
            int critRate = ctx.attackerAttrs.GetFinal(AttributeIds.CritRate);
            int critResist = ctx.defenderAttrs != null ? ctx.defenderAttrs.GetFinal(AttributeIds.CritResist) : 0;
            int effectiveCritRate = Mathf.Max(0, critRate - critResist);
            int critMultiplier = 0;
            if (effectiveCritRate > 0)
            {
                int critRoll = Random.Range(0, 10000);
                if (critRoll < effectiveCritRate)
                {
                    result.isCrit = true;
                    int critDmg = ctx.attackerAttrs.GetFinal(AttributeIds.CritDamage);
                    int critDmgResist = ctx.defenderAttrs != null ? ctx.defenderAttrs.GetFinal(AttributeIds.CritDmgResist) : 0;
                    critMultiplier = Mathf.Max(0, critDmg - critDmgResist);
                }
            }

            // 6. Boss/Elite bonus
            int bossEliteBonus = 0;
            if (ctx.isTargetBoss)
                bossEliteBonus += ctx.attackerAttrs.GetFinal(AttributeIds.BossDmgBonus);
            if (ctx.isTargetElite)
                bossEliteBonus += ctx.attackerAttrs.GetFinal(AttributeIds.EliteDmgBonus);

            // 7. Final formula:
            // Max(0, baseDmg * (1 + totalBonus/10000 - totalReduce/10000) * (1 + critMult/10000) * (1 + bossEliteBonus/10000))
            float elemMult = 1f + totalBonus / 10000f - totalReduce / 10000f;
            float critMult = 1f + critMultiplier / 10000f;
            float beMult = 1f + bossEliteBonus / 10000f;

            float finalDmg = baseDmg * elemMult * critMult * beMult;
            result.finalDamage = Mathf.Max(0, Mathf.RoundToInt(finalDmg));

            return result;
        }
    }
}
