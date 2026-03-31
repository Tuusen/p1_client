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
        public const int AttackInterval = 4;
        public const int MoveSpeed = 5;
        public const int Damage = 6;
        public const int AttackCount = 7;
    }

    // ===== 角色配置 =====

    [Serializable]
    public class RoleConfig
    {
        public int id;
        public string name;
        public string prefabPath;
        public string portraitPath;
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
        public int[] attack_skill_ids;
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
        public int[] attack_skill_ids;
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
        public float attack_range;
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
        public int coinNormalKill;
        public int coinEliteKill;
        public int coinBossKill;
        public float coinSelfDestructRate;
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

    // ===== 故事集系统 =====

    // -- 节点类型 --
    public static class StoryNodeType
    {
        public const int Battle = 1;
        public const int Event = 2;
        public const int Shop = 3;
        public const int Ending = 4;
    }

    // -- 结局类型 --
    public static class EndingType
    {
        public const int None = 0;
        public const int Normal = 1;
        public const int True = 2;
        public const int Hidden = 3;
        public const int Fail = 4;
    }

    // -- 藏品效果类型 --
    public static class PassiveEffectType
    {
        public const int AttributeBoost = 1;   // 属性加成
        public const int SkillEnhance = 2;     // 技能增强
        public const int Special = 3;          // 特殊效果
    }

    // -- 技能增强子类型 (effectType=2 时的 targetAttrId) --
    public static class SkillEnhanceType
    {
        public const int CdReduce = 1;         // 技能CD减少
        public const int DamageBoost = 2;      // 技能伤害加成
        public const int CostReduce = 3;       // 能量消耗减少
        public const int ExtraShot = 4;        // 额外射击次数
    }

    // -- 特殊效果子类型 (effectType=3 时的 targetAttrId) --
    public static class SpecialEffectType
    {
        public const int KillHeal = 1;         // 击杀回血
        public const int GoldBonus = 2;        // 金币加成
        public const int StartShield = 3;      // 开局护盾
        public const int MonsterSlow = 4;      // 怪物减速
        public const int CritChance = 5;       // 暴击概率
        public const int CritDamage = 6;       // 暴击伤害
    }

    // -- 藏品效果数值类型 --
    public static class ValueType
    {
        public const int Percentage = 1;       // 百分比
        public const int Flat = 2;             // 固定值
    }

    // -- 故事集主表 --

    [Serializable]
    public class StoryCollectionConfig
    {
        public int id;
        public string name;
        public string description;
        public string icon;
        public int startNodeId;
        public int[] endingNodeIds;
    }

    [Serializable]
    public class StoryCollectionConfigList
    {
        public List<StoryCollectionConfig> collections;
    }

    // -- 节点配置表 --

    [Serializable]
    public class BossEventEntry
    {
        public int dialogueId;
        public int choiceGroupId;
    }

    [Serializable]
    public class NextNodeEntry
    {
        public int nodeId;
        public int[] conditions;
    }

    [Serializable]
    public class StoryNodeConfig
    {
        public int id;
        public int collectionId;
        public string name;
        public string icon;
        public int type;
        public int levelId;
        public BossEventEntry[] bossEvents;
        public int dialogueId;
        public int choiceGroupId;
        public int shopId;
        public int defaultNextNodeId;
        public int failNodeId;
        public int endingType;
        public string endingCg;
        public int branchLineCount;
        public NextNodeEntry[] nextNodes;
    }

    [Serializable]
    public class StoryNodeConfigList
    {
        public List<StoryNodeConfig> nodes;
    }

    // -- 对话配置表 --

    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public int roleId;
        public int portraitSide;
        public string text;
    }

    [Serializable]
    public class DialogueConfig
    {
        public int id;
        public DialogueLine[] lines;
    }

    [Serializable]
    public class DialogueConfigList
    {
        public List<DialogueConfig> dialogues;
    }

    // -- 选项配置表 --

    [Serializable]
    public class ChoiceOption
    {
        public int id;
        public string text;
        public string description;
        public int effectId;
        public bool triggerBattle;
        public int goldReward;
    }

    [Serializable]
    public class ChoiceGroupConfig
    {
        public int id;
        public string title;
        public ChoiceOption[] options;
    }

    [Serializable]
    public class ChoiceGroupConfigList
    {
        public List<ChoiceGroupConfig> choiceGroups;
    }

    // -- 藏品效果配置表 --

    [Serializable]
    public class PassiveEffectConfig
    {
        public int id;
        public string name;
        public string description;
        public string icon;
        public int rarity;
        public int effectType;
        public int targetAttrId;
        public int valueType;
        public float value;
        public bool stackable;
        public int maxStack;
    }

    [Serializable]
    public class PassiveEffectConfigList
    {
        public List<PassiveEffectConfig> effects;
    }

    // -- 商店配置表 --

    [Serializable]
    public class ShopItem
    {
        public int effectId;
        public int price;
        public int weight;
    }

    [Serializable]
    public class EventShopConfig
    {
        public int id;
        public string name;
        public int refreshCount;
        public ShopItem[] items;
    }

    [Serializable]
    public class EventShopConfigList
    {
        public List<EventShopConfig> shops;
    }
}
