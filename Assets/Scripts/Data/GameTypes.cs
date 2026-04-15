using System;
using System.Collections.Generic;

namespace GeometryTD
{
    // ===== 跨表共用的配置结构体 =====

    [Serializable]
    public class AttrEntry
    {
        public int id;
        public int value;
    }

    // ===== 子弹事件运行时数据 =====

    public class BulletEventData
    {
        public int pierceCount;
        public int explosionDmgRate;
        public int explosionRadius;
        public bool homing;
        public int scatterCount;
        public int scatterAngle;
        public int bounceCount;
        public int bounceDmgMod;
        public int burstCount;
        public int volleyCount;
        public List<int> attachToTargetEventIds;
        public List<int> attachToCasterEventIds;

        public BulletEventData Clone()
        {
            return new BulletEventData
            {
                pierceCount = pierceCount,
                explosionDmgRate = explosionDmgRate,
                explosionRadius = explosionRadius,
                homing = homing,
                scatterCount = scatterCount,
                scatterAngle = scatterAngle,
                bounceCount = bounceCount,
                bounceDmgMod = bounceDmgMod,
                burstCount = burstCount,
                volleyCount = volleyCount,
                attachToTargetEventIds = attachToTargetEventIds != null
                    ? new List<int>(attachToTargetEventIds) : null,
                attachToCasterEventIds = attachToCasterEventIds != null
                    ? new List<int>(attachToCasterEventIds) : null
            };
        }
    }

    // ===== 技能枚举与结构 =====

    public enum SkillCategory
    {
        Self,
        Projectile,
        Aoe,
        Shield,
        Summon
    }

    public enum SkillUseResult
    {
        Success,
        LevelTooLow,
        OnCooldown,
        InvalidSlot
    }

    public struct SkillUseInfo
    {
        public SkillUseResult result;
        public float cooldownRemaining;
    }
}
