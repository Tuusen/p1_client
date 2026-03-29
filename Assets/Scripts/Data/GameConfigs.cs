using System;
using System.Collections.Generic;

namespace GeometryTD
{
    // dmgType: 0=无属性, 1=火, 2=冰, 3=电, 4=风
    // mpType:  0=不产生能量, 1=火, 2=冰, 3=电, 4=风, 99=所有

    // ===== 属性系统 =====

    [Serializable]
    public class AttrEntry
    {
        public int id;
        public float value;
    }

    public static class AttributeIds
    {
        public const int HP = 1;
        public const int Shield = 2;
        public const int Attack = 3;
        public const int AttackRange = 4;
        public const int AttackInterval = 5;
        public const int MoveSpeed = 6;
        public const int Damage = 7;
        public const int AttackCount = 8;
    }

    // ===== 角色配置 =====

    [Serializable]
    public class RoleConfig
    {
        public int id;
        public string name;
        public string prefabPath;
    }

    [Serializable]
    public class RoleConfigList
    {
        public List<RoleConfig> roles;
    }

    // ===== 属性元数据 =====

    [Serializable]
    public class AttributeConfig
    {
        public int id;
        public string name;
        public string description;
    }

    [Serializable]
    public class AttributeConfigList
    {
        public List<AttributeConfig> attributes;
    }

    // ===== 技能事件类型 =====

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
        public int id;
        public string name;
        public string description;
        public int role;
        public int attack_skill_id;
        public float skill_xp_interval;
        public int skill_xp_min;
        public int skill_xp_max;
        public AttrEntry[] attrs;
    }

    [Serializable]
    public class HeroConfigList
    {
        public List<HeroConfig> heroes;
    }

    [Serializable]
    public class MonsterConfig
    {
        public int id;
        public string name;
        public int role;
        public int level;
        public bool is_boss;
        public bool is_elite;
        public int attack_skill_id;
        public float attack_range;
        public float attack_interval;
        public AttrEntry[] attrs;
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
        public string category;
        public string dragHint;
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
        public int default_hero_id;
        public int[] skill_slot_ids;
        public int[] arcane_slot_ids;
    }

    [Serializable]
    public class BulletStyleConfig
    {
        public int id;
        public string prefabPath;
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

    [Serializable]
    public class EventEffectConfig
    {
        public int eventType;
        public float duration;
        public string target;
        public string prefabPath;
    }

    [Serializable]
    public class EventEffectConfigList
    {
        public List<EventEffectConfig> effects;
    }

    // ===== 关卡系统 =====

    [Serializable]
    public class LevelMonsterEntry
    {
        public int id;
        public int generate;
    }

    [Serializable]
    public class LevelEliteEntry
    {
        public int id;
        public int num;
        public int generate;
    }

    [Serializable]
    public class LevelBossEntry
    {
        public int id;
        public int num;
    }

    [Serializable]
    public class LevelConfig
    {
        public int id;
        public string name;
        public string des;
        public string bg;
        public int[] conditions;
        public int hard;
        public float spawn_interval;
        public LevelMonsterEntry[] monsterList;
        public LevelEliteEntry[] superMList;
        public LevelBossEntry[] bossList;
    }

    [Serializable]
    public class LevelConfigList
    {
        public List<LevelConfig> levels;
    }

    [Serializable]
    public class ConditionConfig
    {
        public int id;
        public string desc;
        public int type;
        public int p1;
        public int p2;
    }

    [Serializable]
    public class ConditionConfigList
    {
        public List<ConditionConfig> conditions;
    }
}
