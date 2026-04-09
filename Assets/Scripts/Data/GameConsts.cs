namespace GeometryTD
{
    // dmgType: 0=无属性, 1=火, 2=冰, 3=电, 4=风

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

    // 1=伤害/治疗（基于攻击力）
    // 2=护盾/破盾
    // 3=伤害/治疗（基于生命最大值）
    // 4=击退
    // 5=获得经验
    // 6=获得能量
    // 7=获得buff
    // 8=获得passive
    // 9=召唤
    // 10=驱散
    public static class EventType
    {
        public const int Damage = 1;
        public const int Shield = 2;
        public const int DamagePercentage = 3;
        public const int Knockback = 4;
        public const int GrantXp = 5;
        public const int GainEnergy = 6;
        public const int GainBuff = 7;
        public const int GainPassive = 8;
        public const int Summon = 9;
        public const int Dispel = 10;
    }

    // type: 101=穿透,102=爆炸,103=追踪, 201=散射,202=弹射,203=连射,204=齐射, 301=附加目标,302=附加施法者
    public static class BulletEventType
    {
        public const int Pierce = 101;
        public const int Explosion = 102;
        public const int Tracking = 103;
        public const int Scatter = 201;
        public const int Bounce = 202;
        public const int Burst = 203;
        public const int Volley = 204;
        public const int AttachToTarget = 301;
        public const int AttachToCaster = 302;
    }

    // specialEvent.type:
    // 1=技能伤害变化
    // 2=奥术消耗变化
    // 3=技能子弹变化
    // 101=无敌
    // 102=反击| 反击技能Id
    // 103=冰冻
    // 104=
    public static class BuffSpecialEventType
    {
        public const int SkillDmgMod = 1;
        public const int ArcaneCostMod = 2;
        public const int SkillBulletMod = 3;
        public const int Invincible = 101;
        public const int Counter = 102;
        public const int Freeze = 103;
    }

    // ===== 故事集系统常量 =====

    public static class StoryNodeType
    {
        public const int Battle = 1;
        public const int Event = 2;
        public const int Shop = 3;
        public const int Ending = 4;
    }

    public static class EndingType
    {
        public const int None = 0;
        public const int Normal = 1;
        public const int True = 2;
        public const int Hidden = 3;
        public const int Fail = 4;
    }

    public static class PassiveEffectType
    {
        public const int AttributeBoost = 1;
        public const int SkillEnhance = 2;
        public const int Special = 3;
    }

    public static class SkillEnhanceType
    {
        public const int CdReduce = 1;
        public const int DamageBoost = 2;
        public const int CostReduce = 3;
        public const int ExtraShot = 4;
    }

    public static class SpecialEffectType
    {
        public const int KillHeal = 1;
        public const int GoldBonus = 2;
        public const int StartShield = 3;
        public const int MonsterSlow = 4;
        public const int CritChance = 5;
        public const int CritDamage = 6;
    }

    public static class ValueType
    {
        public const int Percentage = 1;
        public const int Flat = 2;
    }
}
