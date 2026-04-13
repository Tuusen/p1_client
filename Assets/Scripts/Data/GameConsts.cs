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

    // 1：伤害/治疗（基于攻击力）=[伤害倍率，伤害类型]：立即造成伤害倍率的伤害。倍数为负数时为治疗；
    // 2：护盾/破盾=[护盾值]：立刻为自身添加护盾，可以吸收伤害，负数则是损失护盾值（至多减少为0）；
    // 3：伤害/治疗（基于生命最大值）=[伤害倍率，伤害类型]：立即造成伤害倍率的伤害。倍数为负数时为治疗；
    // 4：击退=[击退力度]：向后击退指定距离；
    // 5：获得经验=[经验值, 技能数量(-1=全部)]：为技能获得经验值，可指定随机技能数量；
    // 6：获得能量=[经验值, 能量类型(-1=全部)]：获得指定能量；
    // 7：获得buff： params=[buffId]：获得buff；
    // 8：获得passive： parmas=[passiveId]：获得passiveId；
    // 9：召唤=[怪物ID, 持续时间, 继承属性百分比, 额外召唤数]：召唤友军单位协助战斗
    // 10:驱散=[BuffId，驱散个数]：随机驱散buff个数，buffId=-1代表全部增益，buffId=-2代表全部减益
    // 11:伤害/治疗（基于已损失生命值）=[伤害倍率，伤害类型]：立即造成伤害倍率的伤害。倍数为负数时为治疗；

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
        public const int LostHp = 11;
    }

    // 101：穿透 [穿透数量]：子弹变为穿透效果，
    // 102：爆炸 [爆炸伤害, 爆炸半径]：子弹命中后爆炸效果，造成范围伤害并且附加子弹同款效果
    // 103：追踪 []：子弹具有自动追踪敌人的能力

    // 201：散射 [额外子弹数, 分散角度]：发射时额外产生多个子弹，分散角度决定额外发射子弹的正负偏转范围；
    // 202：弹射 [弹射次数, 范围半径, 最小距离, 初始伤害修正]：命中后向周围敌人弹射，每次弹射伤害可衰减
    // 203：连射 [连续释放次数]：每间隔0.05秒连续发射多次子弹
    // 204：齐射 [攻击目标数量]

    // 301：附加目标 [eventId]：命中后对目标附加eventId；
    // 302：附加施法者 [eventId]：命中后对释放者附加eventId；
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
    // 1 : 技能伤害变化 [技能Id（-1=全部技能），改变比例]：影响技能伤害
    // 2 ：奥术消耗变化 [奥术Id(-1=全部)，消耗符能类型，消耗数量改变值]：奥术消耗的符能改变（至多减少到0点）
    // 3 ：技能子弹变化：parmas=[技能Id（-1=全部技能）,bulletEventId]：释放的技能都会附加bulletEventId事件

    // 101：无敌 []：不再受到任何伤害/负面状态/控制效果影响
    // 102：反击 [反弹skillId]：被攻击时对攻击对象释放一次skillId
    // 103：冰冻 []：无法移动和攻击
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

    // ===== 窗口系统常量 =====

    /// <summary>
    /// 窗口类型
    /// </summary>
    public enum WinType
    {
        /// <summary>普通界面，可任意关闭</summary>
        Normal = 1,
        /// <summary>常驻界面，仅能通过特定方法关闭</summary>
        Permanent = 2
    }

    /// <summary>
    /// 窗口优先级，数值越大层级越高
    /// </summary>
    public enum WinPriority
    {
        /// <summary>普通优先级</summary>
        Normal = 10,
        /// <summary>弹窗优先级</summary>
        Popup = 100
    }
}
