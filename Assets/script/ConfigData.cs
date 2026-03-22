using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 子弹配置数据
    /// </summary>
    [Serializable]
    public class BulletConfig
    {
        public string id;
        public string name;
        public string shape; // "sphere", "cube", "triangle"
        public string color; // hex color
        public float speed = 15f;
        public int damage = 10;
        public float scale = 0.3f;
        public float lifetime = 5f;
        public bool hasTrail = false;
        public string trailColor;
    }

    /// <summary>
    /// 怪物配置数据
    /// </summary>
    [Serializable]
    public class EnemyConfig
    {
        public string id;
        public string name;
        public string shape; // "cube", "sphere", "triangle", "diamond"
        public string color; // hex color
        public float scale = 1f;
        public int health = 100;
        public int shield = 0;
        public float moveSpeed = 3f;
        public int damage = 10;
        public int score = 10;
        public float spawnWeight = 1f;
    }

    /// <summary>
    /// 子弹配置列表
    /// </summary>
    [Serializable]
    public class BulletConfigList
    {
        public List<BulletConfig> bullets = new List<BulletConfig>();
    }

    /// <summary>
    /// 怪物配置列表
    /// </summary>
    [Serializable]
    public class EnemyConfigList
    {
        public List<EnemyConfig> enemies = new List<EnemyConfig>();
    }

    /// <summary>
    /// 玩家配置数据
    /// </summary>
    [Serializable]
    public class PlayerConfig
    {
        public string shape = "triangle";
        public string color = "#00FF00";
        public float scale = 1f;
        public int health = 100;
        public int shield = 50;
        public float attackRange = 10f;
        public float attackInterval = 0.5f;
        public string defaultBulletId = "bullet_01";
    }

    /// <summary>
    /// 技能配置数据
    /// </summary>
    [Serializable]
    public class SkillConfig
    {
        public string id;
        public string name;
        public string description;
        public string iconShape  = "circle";   // 图标形状
        public string iconColor  = "#AAAAFF";  // 图标颜色
        public string iconPath   = "";          // 图标图片路径（Resources相对路径）
        public int    maxLevel   = 10;          // 最大等级
        public int    expPerLevel = 10;         // 每级所需经验
        public float  cooldown   = 10f;         // 技能自冷却时间

        // 技能类型与元素
        public string skillType = "bullet";     // bullet/heal/shield/summon/aoe/exchange
        public string element   = "none";       // fire/ice/lightning/wind/none

        // 子弹基础参数
        public string bulletShape      = "sphere";
        public string bulletColor      = "#FFFFFF";
        public float  bulletSpeed      = 20f;
        public int    bulletDamage     = 15;
        public float  bulletScale      = 0.35f;
        public float  bulletLifetime   = 4f;
        public bool   bulletHasTrail   = false;
        public string bulletTrailColor = "#FFFFFF";

        // 多发子弹（技能1/2/3）
        public int   baseBulletCount     = 1;   // 基础发射数
        public float spreadAngle         = 5f;  // 多发偏移角度范围（±度）
        public int   bulletCountPerLevel = 1;   // 每级增加发射数

        // 爆炸效果（技能1）
        public bool  hasExplosion       = false;
        public int   explosionUnlockLv  = 6;   // 解锁爆炸的等级
        public float explosionRadius    = 1.5f;
        public float explosionDamagePct = 100f; // 爆炸伤害百分比

        // 灼伤效果（技能1）
        public bool  hasBurn             = false;
        public int   burnUnlockLv        = 10;
        public float burnDamagePctPerSec = 40f; // 每秒伤害为基础伤害的百分比
        public float burnDuration        = 5f;

        // 冰冻效果（技能2）
        public bool  hasFreezeOnHit     = false;
        public float freezeDuration     = 3f;
        public bool  hasPierceOnLv6     = false; // 6级穿刺
        public bool  hasSlowAfterFreeze = false; // 10级解冻后减速
        public float slowPct            = 50f;   // 减速百分比
        public float slowDuration       = 3f;

        // 闪电链（技能3）
        public bool  hasChain           = false;
        public int   baseChainCount     = 1;    // 基础追击次数
        public int   chainCountPerLevel = 1;    // 每级增加追击次数
        public float chainDamageFalloff = 0.1f; // 每跳衰减
        public float chainMinDamagePct  = 0.3f; // 最低伤害比例
        public bool  chainAllowRepeat   = false; // 6级后允许重复命中
        public bool  chainBonusOnRepeat = false; // 10级重复额外伤害

        // AOE 击退（技能4）
        public bool  hasKnockback        = false;
        public float knockbackDist       = 3f;
        public float knockbackPerLevel   = 0.1f; // 每级增加10%
        public float aoeDamagePct        = 20f;  // AOE基础伤害百分比
        public bool  hasSlowOnLv6        = false;
        public float aoeSlowPct          = 80f;
        public float aoeSlowDuration     = 5f;
        public bool  hasVulnerableOnLv10 = false;
        public float vulnerablePct       = 50f;
        public float vulnerableDuration  = 5f;

        // 治愈（技能5）
        public bool  isHeal                    = false;
        public float healPct                   = 5f;  // 恢复已损失生命值百分比
        public float healPerLevel              = 5f;
        public bool  hasHoTOnLv6               = false;  // 持续回复
        public float hotDuration               = 10f;
        public float hotPctPerSec              = 5f;
        public bool  hasDamageReductionOnLv10  = false;
        public float damageReductionPct        = 50f;
        public float damageReductionDuration   = 10f;

        // 能量交换（技能6）
        public bool  isEnergyExchange  = false;
        public float minHpPct          = 20f;  // 可用最低血量百分比
        public float hpCostPct         = 20f;
        public int   baseExpGain       = 10;
        public int   expGainPerLevel   = 5;
        public int   extraTargetsOnLv6 = 2;   // 6级额外目标数

        // 护盾（技能7）
        public bool  isShield            = false;
        public float shieldPct           = 10f;  // 最大血量百分比
        public float shieldPerLevel      = 10f;
        public bool  hasReflectOnLv6     = false;
        public bool  hasBreakBurstOnLv10 = false;
        public int   reflectBulletCount  = 1;
        public float reflectDamagePct    = 50f;

        // 风影召唤（技能8）
        public bool  isSummon              = false;
        public float guardDuration         = 3f;
        public float guardDurationPerLevel = 2f;
        public float guardFireRate         = 0.3f;
        public float guardDamagePct        = 10f;
        public bool  guardHomingOnLv6      = false;
        public bool  guardTripleShotOnLv10 = false;
        public float guardTripleAngle      = 30f;
        public int   guardPierceCount      = 2;

        // 能量奖励
        public string energyType         = "none"; // fire/ice/lightning/wind
        public int    baseEnergyGain     = 1;
        public int    energyGainPerLevel = 1;
    }

    /// <summary>
    /// 技能配置列表
    /// </summary>
    [Serializable]
    public class SkillConfigList
    {
        public List<SkillConfig> skills = new List<SkillConfig>();
    }

    /// <summary>
    /// 游戏配置数据
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public PlayerConfig    player  = new PlayerConfig();
        public BulletConfigList bullets = new BulletConfigList();
        public EnemyConfigList  enemies = new EnemyConfigList();
        public SkillConfigList  skills  = new SkillConfigList();
    }
}
