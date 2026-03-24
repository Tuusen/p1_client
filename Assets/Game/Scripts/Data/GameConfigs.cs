using System;
using System.Collections.Generic;

namespace GeometryTD
{
    // dmgType: 0=无属性, 1=火, 2=冰, 3=电, 4=风
    // mpType:  0=不产生能量, 1=火, 2=冰, 3=电, 4=风, 99=所有

    public static class SkillEventType
    {
        public const int Pierce = 1;
        public const int Explosion = 2;
        public const int Freeze = 3;
        public const int Burn = 4;
        public const int ExtraShot = 5;
        public const int Chain = 6;
        public const int Slow = 7;
        public const int Heal = 8;
        public const int HealOverTime = 9;
        public const int DamageReduction = 10;
        public const int SelfDamage = 11;
        public const int GrantXp = 12;
        public const int Shield = 13;
        public const int Retaliation = 14;
        public const int Knockback = 15;
        public const int Vulnerability = 16;
        public const int Summon = 17;
        public const int Homing = 18;
        public const int ShieldBreak = 19;
    }

    [Serializable]
    public class HeroConfig
    {
        public string name;
        public string description;
        public int attack_skill_id;
        public float attack_range;
        public float hp;
        public float shield;
        public float attack_interval;
        public float base_attack;
        public int skill_xp_min;
        public int skill_xp_max;
    }

    [Serializable]
    public class MonsterConfig
    {
        public int id;
        public string name;
        public float hp;
        public int level;
        public float damage;
        public bool is_boss;
        public float move_speed;
        public int attack_skill_id;
        public float attack_range;
        public float attack_interval;
    }

    [Serializable]
    public class MonsterConfigList
    {
        public List<MonsterConfig> monsters;
    }

    [Serializable]
    public class SkillEvent
    {
        public int type;
        public float[] param;
    }

    public class BulletModifiers
    {
        public int pierceCount;
        public float explosionRadius;
        public float explosionDmg;
        public int chainCount;
        public float chainDecayRatio;
        public float chainRange;
        public float chainAoeRadius;
        public float freezeDuration;
        public float burnDmg;
        public float burnDuration;
        public float slowDuration;
        public float slowRatio;
        public bool homing;

        public BulletModifiers Clone()
        {
            return new BulletModifiers
            {
                pierceCount = pierceCount,
                explosionRadius = explosionRadius,
                explosionDmg = explosionDmg,
                chainCount = chainCount,
                chainDecayRatio = chainDecayRatio,
                chainRange = chainRange,
                chainAoeRadius = chainAoeRadius,
                freezeDuration = freezeDuration,
                burnDmg = burnDmg,
                burnDuration = burnDuration,
                slowDuration = slowDuration,
                slowRatio = slowRatio,
                homing = homing
            };
        }
    }

    [Serializable]
    public class SkillConfig
    {
        public int id;
        public int level;
        public string name;
        public string[] desList;
        public string icon;
        public int dmg;
        public int dmgType;
        public int mp;
        public int mpType;
        public float bulletSpeed;
        public int atkCnt;
        public float cd;
        public int bulletStyleId;
        public SkillEvent[] events;
    }

    [Serializable]
    public class SkillConfigList
    {
        public List<SkillConfig> skills;
    }

    [Serializable]
    public class GameConfig
    {
        public int kill_count_for_boss;
        public float monster_spawn_interval;
        public int boss_monster_id;
        public int[] skill_slot_ids;
    }

    [Serializable]
    public class BulletStyleConfig
    {
        public int id;
        public string shape;
        public float size;
        public float colorR;
        public float colorG;
        public float colorB;
        public float trailR;
        public float trailG;
        public float trailB;
        public float trailWidth;
        public float trailTime;
    }

    [Serializable]
    public class BulletStyleConfigList
    {
        public List<BulletStyleConfig> bulletStyles;
    }

    [Serializable]
    public class ArcaneConfig
    {
        public int id;
        public string name;
        public string[] desList;
        public string icon;
        public int dmg;
        public int dmgType;
        public float radius;
        public float tickInterval;
        public float cd;
        public int runeCost;
        public int runeType;
        public SkillEvent[] events;
    }

    [Serializable]
    public class ArcaneConfigList
    {
        public List<ArcaneConfig> arcanes;
    }

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
