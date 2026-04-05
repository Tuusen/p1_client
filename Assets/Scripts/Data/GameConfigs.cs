using System;
using System.Collections.Generic;

namespace GeometryTD
{
    // dmgType: 0=无属性, 1=火, 2=冰, 3=电, 4=风

    // ===== 属性系统 =====

    [Serializable]
    public class AttrEntry
    {
        public int id;
        public int value;
    }

    public static class AttributeIds
    {
        // 基础属性 (type=1)
        public const int HP = 1;
        public const int Attack = 2;

        // 特殊属性 (type=2)
        public const int FireDmgBonus = 100;
        public const int IceDmgBonus = 101;
        public const int WindDmgBonus = 102;
        public const int ElecDmgBonus = 103;
        public const int AllElemDmgBonus = 104;
        public const int FireDmgReduce = 105;
        public const int IceDmgReduce = 106;
        public const int WindDmgReduce = 107;
        public const int ElecDmgReduce = 108;
        public const int AllElemDmgReduce = 109;
        public const int HpPercentBonus = 110;
        public const int AtkPercentBonus = 111;
        public const int BossDmgBonus = 112;
        public const int EliteDmgBonus = 113;
        public const int Shield = 114;
        public const int CritRate = 115;
        public const int CritDamage = 116;
        public const int CritResist = 117;
        public const int CritDmgResist = 118;
        public const int HitRate = 119;
        public const int DodgeRate = 120;
        public const int AttackInterval = 121;
        public const int FireEnergy = 122;
        public const int IceEnergy = 123;
        public const int WindEnergy = 124;
        public const int ElecEnergy = 125;
        public const int Proficiency = 126;
        public const int Craftsmanship = 127;
        public const int Selfless = 128;
        public const int SkillCdReduce = 129;
        public const int ArcaneCdReduce = 130;
        public const int MoveSpeed = 131;
        public const int AttackCount = 132;

        // 元素类型 -> 加伤属性ID映射 (dmgType: 1火,2冰,3电,4风)
        public static int GetElemDmgBonusId(int dmgType)
        {
            switch (dmgType)
            {
                case 1: return FireDmgBonus;
                case 2: return IceDmgBonus;
                case 3: return ElecDmgBonus;
                case 4: return WindDmgBonus;
                default: return -1;
            }
        }

        // 元素类型 -> 减免属性ID映射
        public static int GetElemDmgReduceId(int dmgType)
        {
            switch (dmgType)
            {
                case 1: return FireDmgReduce;
                case 2: return IceDmgReduce;
                case 3: return ElecDmgReduce;
                case 4: return WindDmgReduce;
                default: return -1;
            }
        }
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
        public string des;
        public int type;      // 1=基础属性, 2=特殊属性
        public int downLimit;
        public int upLimit;
        public int powerType; // 0=直接值, 1=万分比
    }

    [Serializable]
    public class AttributeConfigList
    {
        public List<AttributeConfig> attributes;
    }

    // ===== Event 事件配置 =====
    // type: 1=伤害/治疗, 2=护盾/破盾, 4=击退, 5=获得经验, 6=获得能量, 7=获得buff, 8=获得passive, 9=召唤, 10=驱散

    public static class EventType
    {
        public const int Damage = 1;       // args=[伤害倍率, 伤害类型] 负数为治疗
        public const int Shield = 2;       // args=[护盾值]
        public const int Knockback = 4;    // args=[击退力度万分比]
        public const int GrantXp = 5;      // args=[经验值, 技能数量(-1=全部)]
        public const int GainEnergy = 6;   // args=[能量值, 能量类型(-1=全部)]
        public const int GainBuff = 7;     // args=[buffId]
        public const int GainPassive = 8;  // args=[passiveId]
        public const int Summon = 9;       // args=[怪物ID, 持续时间, 继承属性%, 额外数量]
        public const int Dispel = 10;      // args=[BuffId(-1=全部增益,-2=全部减益), 驱散个数]
    }

    [Serializable]
    public class EventConfig
    {
        public int id;
        public int type;
        public string name;
        public string des;
        public int[] args;
    }

    [Serializable]
    public class EventConfigList
    {
        public List<EventConfig> events;
    }

    // ===== BulletEvent 子弹事件配置 =====
    // type: 101=穿透,102=爆炸,103=追踪, 201=散射,202=弹射,203=连射,204=齐射, 301=附加目标,302=附加施法者

    public static class BulletEventType
    {
        public const int Pierce = 101;         // args=[穿透数量]
        public const int Explosion = 102;      // args=[爆炸伤害倍率, 爆炸半径]
        public const int Tracking = 103;       // args=[]

        public const int Scatter = 201;        // args=[额外子弹数, 分散角度]
        public const int Bounce = 202;         // args=[弹射次数, 范围半径, 最小距离, 初始伤害修正]
        public const int Burst = 203;          // args=[连续释放次数]
        public const int Volley = 204;         // args=[攻击目标数量]

        public const int AttachToTarget = 301; // args=[eventId] 命中后对目标附加
        public const int AttachToCaster = 302; // args=[eventId] 命中后对施法者附加
    }

    [Serializable]
    public class BulletEventConfig
    {
        public int id;
        public int type;
        public string name;
        public string des;
        public int[] args;
    }

    [Serializable]
    public class BulletEventConfigList
    {
        public List<BulletEventConfig> bulletEvents;
    }

    // ===== Buff 配置 =====
    // type: 1=增益, 2=减益

    [Serializable]
    public class EvtDmgRateEntry
    {
        public int type;   // 伤害类型
        public int rate;   // 伤害倍率（万分比）
    }

    [Serializable]
    public class BuffSpecialEvent
    {
        public int type;   // 1=技能伤害变化, 2=奥术消耗变化, 3=技能子弹变化, 101=无敌, 102=反击, 103=冰冻
        public int[] args;
    }

    [Serializable]
    public class BuffConfig
    {
        public int id;
        public string name;
        public string icon;
        public string desc;
        public int overlap;            // 叠加上限
        public int probability;        // 上buff概率（万分比）
        public int lastTime;           // 持续时间（毫秒）
        public int jumpTime;           // 间隔跳伤时间（毫秒）
        public string persistJson;     // buff持续特效
        public string position;        // buff位置
        public int type;               // 1=增益, 2=减益
        public int dispel;             // 可否驱散
        public AttrEntry[] attribute;           // 持续属性变化
        public EvtDmgRateEntry[] evtDmgRate;    // buff伤害快照
        public int[] evtDamage;                 // 每跳触发event
        public int[] evtWhenEnd;                // 结束触发event
        public BuffSpecialEvent[] specialEvent; // 特殊事件
    }

    [Serializable]
    public class BuffConfigList
    {
        public List<BuffConfig> buffs;
    }

    // ===== Passive 被动配置 =====

    [Serializable]
    public class PassiveCondEntry
    {
        public int id;     // 1=目标生命值百分比, 2=目标护盾百分比
        public int[] args;
    }

    [Serializable]
    public class PassiveConfig
    {
        public int id;
        public string name;
        public string icon;
        public string des;
        public int[] eventTarget;           // 触发时机
        public int[] eventRemove;           // 移除时机
        public PassiveCondEntry[] eventCond; // 触发条件
        public int[] events;                // 触发时附加的事件ID
    }

    [Serializable]
    public class PassiveConfigList
    {
        public List<PassiveConfig> passives;
    }

    // ===== BulletEventData 运行时数据（替代旧 BulletModifiers）=====

    public class BulletEventData
    {
        public int pierceCount;
        public int explosionDmgRate;
        public int explosionRadius;
        public bool homing;
        public int scatterCount;
        public int scatterAngle;
        public int bounceCount;
        public int bounceRadius;
        public int bounceMinDist;
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
                bounceRadius = bounceRadius,
                bounceMinDist = bounceMinDist,
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

    // ===== 英雄配置 =====

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
        public int[] charge_buff_ids; // 引用 buff_config 中的 buffId
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

    // ===== 技能配置 =====

    [Serializable]
    public class SkillPoolConfig
    {
        public int id;
        public string name;
        public string[] desList;
        public string icon;
        public string dragHint;
    }

    [Serializable]
    public class SkillPoolConfigList
    {
        public List<SkillPoolConfig> skill_pool_config;
    }

    [Serializable]
    public class SkillConfig
    {
        public int id;
        public int level;
        public string name;
        public string des;
        public string icon;
        public string category;
        public int dmg;
        public int dmgType;
        public float bulletSpeed;
        public float cd;
        public int bulletStyleId;
        public float attack_range;
        public int[] events;        // 自身事件ID数组
        public int[] enemyEvents;   // 敌方事件ID数组
        public int[] bulletEvents;  // 子弹事件ID数组
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

    // ===== 奥术配置 =====

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
        public int[] events;        // 自身事件ID数组
        public int[] enemyEvents;   // 敌方事件ID数组
        public int[] bulletEvents;  // 子弹事件ID数组
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
