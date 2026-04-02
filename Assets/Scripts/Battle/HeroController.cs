using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class HeroController : MonoBehaviour, IBuffTarget
    {
        private AttrComponent attrs;
        private BuffSystem buffSystem = new BuffSystem();

        [Header("配置")]
        private float attackRange;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;

        [Header("运行时状态")]
        private int maxHp;
        private float currentHp;
        private int maxShield;
        private float currentShield;
        private float attackTimer;

        // 反击状态（电磁盾牌）
        private float retaliationDmg;
        private int retaliationBullets;
        private int retaliationPierceCount;
        private bool retaliationActive;
        private int retaliationBulletStyleId;

        // 护盾破裂状态
        private float shieldBreakRadius;
        private float shieldBreakDmg;
        private bool shieldBreakPending;

        // Charge 状态
        private AttrEntry[] chargeBuffConfigs;
        private float lastAttackTime;
        private bool isCharging;
        private const float ChargeIdleThreshold = 5f;

        [Header("引用")]
        [SerializeField] private HealthBarUI shieldBar;
        [SerializeField] private HealthBarUI hpBar;

        private BattleManager battleManager;
        private SkillConfig normalAttackConfig;
        private Animator animator;
        private CharacterFacing facing;

        // IBuffTarget 实现
        public AttrComponent Attrs => attrs;
        public bool IsDead => currentHp <= 0;
        public Vector3 Position => transform.position;

        public float AttackRange => attackRange;
        public float BaseAttack => attrs != null ? attrs.GetAttack() : 0;
        public float MaxHp => maxHp;

        public void OnBuffDamage(float dmg)
        {
            TakeDamage(dmg);
        }

        public void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            UpdateBars();
        }

        public void Init(HeroConfig config, BattleManager manager)
        {
            battleManager = manager;

            // 初始化属性组件
            attrs = GetComponent<AttrComponent>();
            if (attrs == null) attrs = gameObject.AddComponent<AttrComponent>();
            attrs.Init(config.attrs);

            maxHp = attrs.GetMaxHp();
            maxShield = attrs.GetFinal(AttributeIds.Shield);
            currentHp = maxHp;
            currentShield = maxShield;

            // Charge 配置
            chargeBuffConfigs = config.charge_buffs;
            lastAttackTime = Time.time;
            isCharging = false;

            // 初始化攻击技能
            if (config.attack_skill_ids != null && config.attack_skill_ids.Length > 0)
            {
                attackSkillIds = new int[config.attack_skill_ids.Length];
                attackSkillCds = new float[config.attack_skill_ids.Length];
                attackSkillTimers = new float[config.attack_skill_ids.Length];
                attackSkillConfigs = new SkillConfig[config.attack_skill_ids.Length];

                for (int i = 0; i < config.attack_skill_ids.Length; i++)
                {
                    attackSkillIds[i] = config.attack_skill_ids[i];
                    attackSkillConfigs[i] = ConfigManager.Instance.GetSkillConfig(attackSkillIds[i]);
                    attackSkillCds[i] = attackSkillConfigs[i] != null ? attackSkillConfigs[i].cd : 1f;
                    attackSkillTimers[i] = 0f;

                    if (i == 0 && attackSkillConfigs[i] != null)
                    {
                        attackRange = attackSkillConfigs[i].attack_range;
                    }
                }
            }
            else
            {
                attackRange = 5f;
            }

            normalAttackConfig = attackSkillConfigs != null && attackSkillConfigs.Length > 0 ? attackSkillConfigs[0] : null;

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

            buffSystem.Clear();
            UpdateBars();
        }

        public void SetBars(HealthBarUI shield, HealthBarUI hp)
        {
            shieldBar = shield;
            hpBar = hp;
            UpdateBars();
        }

        private void Update()
        {
            if (IsDead || battleManager == null) return;

            // Buff 系统驱动（HoT、减伤等）
            buffSystem.Tick(Time.deltaTime, this);
            if (IsDead) return;

            // Charge 状态检测
            UpdateChargeState();

            // 技能冷却计时
            if (attackSkillTimers != null)
            {
                for (int i = 0; i < attackSkillTimers.Length; i++)
                    attackSkillTimers[i] += Time.deltaTime;
            }

            // 攻击间隔计时器
            float atkInterval = attrs.GetAttackIntervalSec();
            attackTimer += Time.deltaTime;
            if (attackTimer >= atkInterval)
            {
                attackTimer = 0f;
                TryAttack();
            }

            UpdateBars();
        }

        private void UpdateChargeState()
        {
            if (chargeBuffConfigs == null || chargeBuffConfigs.Length == 0) return;

            float idle = Time.time - lastAttackTime;

            if (!isCharging && idle >= ChargeIdleThreshold)
            {
                // 进入蓄力：添加 Charge buff
                isCharging = true;
                for (int i = 0; i < chargeBuffConfigs.Length; i++)
                {
                    var entry = chargeBuffConfigs[i];
                    buffSystem.AddBuff(new BuffEntry
                    {
                        type = BuffType.Charge,
                        duration = -1f, // 永久，直到手动移除
                        attrId = entry.id,
                        value = entry.value
                    });
                }
            }
        }

        private void ExitCharge()
        {
            if (!isCharging) return;
            isCharging = false;
            buffSystem.RemoveBuffsByType(BuffType.Charge);
        }

        private void TryAttack()
        {
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;

            int skillIndex = -1;
            for (int i = attackSkillConfigs.Length - 1; i >= 0; i--)
            {
                if (attackSkillTimers[i] >= attackSkillCds[i])
                {
                    skillIndex = i;
                    break;
                }
            }

            if (skillIndex < 0) return;

            attackSkillTimers[skillIndex] = 0f;

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            float skillRange = skillConfig.attack_range > 0 ? skillConfig.attack_range : attackRange;
            int atkCount = attrs.GetFinal(AttributeIds.AttackCount);
            if (atkCount < 1) atkCount = 1;

            List<Transform> targets = battleManager.GetNearestEnemiesUnique(
                transform.position, skillRange, atkCount);
            if (targets.Count == 0) return;

            // 攻击时退出蓄力
            ExitCharge();
            lastAttackTime = Time.time;

            facing?.FaceToward(targets[0].position);

            float atk = attrs.GetAttack();
            float actualDmg = atk * skillConfig.dmg / 10000f;
            var mods = new BulletModifiers();
            foreach (var target in targets)
                battleManager.SpawnSkillBullet(transform.position, target, actualDmg,
                    skillConfig.bulletSpeed, mods.Clone(), skillConfig.bulletStyleId, skillRange);

            battleManager.OnHeroNormalAttack(transform.position);
            animator?.SetTrigger("Attack");
        }

        // ===== 技能路由 =====
        public void UseSkill(SkillConfig config)
        {
            if (IsDead || battleManager == null || config == null) return;

            var category = SkillManager.ClassifySkill(config);
            switch (category)
            {
                case SkillCategory.Summon:     HandleSummonSkill(config);     break;
                case SkillCategory.Shield:     HandleShieldSkill(config);     break;
                case SkillCategory.Self:       HandleSelfSkill(config);       break;
                case SkillCategory.Projectile: HandleProjectileSkill(config); break;
                case SkillCategory.Aoe:        HandleAoeSkill(config);        break;
            }
        }

        // ===== 弹幕技能 (烈焰圣弹, 急冻冰锥, 闪电连锁) =====
        private void HandleProjectileSkill(SkillConfig config)
        {
            float atk = attrs.GetAttack();
            float actualDmg = atk * config.dmg / 10000f;
            var mods = new BulletModifiers();
            int extraShots = 0;

            if (config.events != null)
            {
                foreach (var evt in config.events)
                {
                    if (evt.param == null) continue;
                    switch (evt.type)
                    {
                        case SkillEventType.Pierce:
                            if (evt.param.Length >= 1) mods.pierceCount = (int)evt.param[0];
                            break;
                        case SkillEventType.Explosion:
                            if (evt.param.Length >= 2)
                            {
                                mods.explosionRadius = evt.param[0];
                                mods.explosionDmg = atk * evt.param[1] / 10000f;
                            }
                            break;
                        case SkillEventType.ExtraShot:
                            if (evt.param.Length >= 1) extraShots = (int)evt.param[0];
                            break;
                        case SkillEventType.Chain:
                            if (evt.param.Length >= 4)
                            {
                                mods.chainCount = (int)evt.param[0];
                                mods.chainDecayRatio = evt.param[1];
                                mods.chainRange = evt.param[2];
                                mods.chainAoeRadius = evt.param[3];
                            }
                            break;
                        case SkillEventType.Freeze:
                            if (evt.param.Length >= 1) mods.freezeDuration = evt.param[0];
                            break;
                        case SkillEventType.Burn:
                            if (evt.param.Length >= 2)
                            {
                                mods.burnDmg = atk * evt.param[0] / 10000f;
                                mods.burnDuration = evt.param[1];
                            }
                            break;
                        case SkillEventType.Slow:
                            if (evt.param.Length >= 2)
                            {
                                mods.slowDuration = evt.param[0];
                                mods.slowRatio = evt.param[1];
                            }
                            break;
                    }
                }
            }

            int totalShots = config.atkCnt + extraShots;
            float skillRange = config.attack_range > 0 ? config.attack_range : attackRange;
            List<Transform> targets = battleManager.GetNearestEnemies(
                transform.position, skillRange, totalShots);
            if (targets.Count == 0) return;

            foreach (var target in targets)
            {
                battleManager.SpawnSkillBullet(transform.position, target, actualDmg,
                    config.bulletSpeed, mods.Clone(), config.bulletStyleId, skillRange);
            }
        }

        // ===== 自身效果技能 (沐浴之火, 牺牲寒冰) =====
        private void HandleSelfSkill(SkillConfig config)
        {
            if (config.events == null) return;

            var efx = battleManager.EventEffectManager;

            foreach (var evt in config.events)
            {
                if (evt.param == null) continue;
                switch (evt.type)
                {
                    case SkillEventType.Heal:
                        if (evt.param.Length >= 1)
                        {
                            float healAmount = maxHp * evt.param[0] / 10000f;
                            currentHp = Mathf.Min(currentHp + healAmount, maxHp);
                            UpdateBars();
                        }
                        break;
                    case SkillEventType.HealOverTime:
                        if (evt.param.Length >= 2)
                        {
                            int healPerSec = (int)(maxHp * evt.param[0] / 10000f);
                            buffSystem.AddBuff(new BuffEntry
                            {
                                type = BuffType.HealOverTime,
                                duration = evt.param[1],
                                value = healPerSec,
                                tickInterval = 1f
                            });
                        }
                        break;
                    case SkillEventType.DamageReduction:
                        if (evt.param.Length >= 2)
                        {
                            buffSystem.AddBuff(new BuffEntry
                            {
                                type = BuffType.AttrModify,
                                duration = evt.param[1],
                                attrId = AttributeIds.AllElemDmgReduce,
                                value = (int)evt.param[0]
                            });
                        }
                        break;
                    case SkillEventType.SelfDamage:
                        if (evt.param.Length >= 1)
                        {
                            float selfDmg = maxHp * evt.param[0] / 10000f;
                            currentHp -= selfDmg;
                            currentHp = Mathf.Max(1f, currentHp);
                            UpdateBars();
                        }
                        break;
                    case SkillEventType.GrantXp:
                        if (evt.param.Length >= 2)
                        {
                            int xpAmount = (int)evt.param[0];
                            int targetCount = (int)evt.param[1];
                            battleManager.GrantSkillXp(xpAmount, targetCount);
                        }
                        break;
                }

                efx?.TriggerEffect(evt.type, transform.position);
            }
        }

        // ===== 护盾技能 (电磁盾牌) =====
        private void HandleShieldSkill(SkillConfig config)
        {
            if (config.events == null) return;

            var efx = battleManager.EventEffectManager;
            int pierceFromEvent = 0;
            float atk = attrs.GetAttack();

            foreach (var evt in config.events)
            {
                if (evt.param == null) continue;
                switch (evt.type)
                {
                    case SkillEventType.Shield:
                        if (evt.param.Length >= 1)
                        {
                            float shieldAmount = maxHp * evt.param[0] / 10000f;
                            float shieldCap = maxShield > 0 ? maxShield : maxHp;
                            currentShield = Mathf.Min(currentShield + shieldAmount, shieldCap);
                            UpdateBars();
                        }
                        break;
                    case SkillEventType.Retaliation:
                        if (evt.param.Length >= 3)
                        {
                            retaliationDmg = atk * evt.param[0] / 10000f;
                            retaliationBullets = (int)evt.param[1];
                            retaliationPierceCount = 0;
                            retaliationActive = true;
                        }
                        break;
                    case SkillEventType.Pierce:
                        if (evt.param.Length >= 1)
                            pierceFromEvent = (int)evt.param[0];
                        break;
                    case SkillEventType.ShieldBreak:
                        if (evt.param.Length >= 2)
                        {
                            shieldBreakRadius = evt.param[0];
                            shieldBreakDmg = atk * evt.param[1] / 10000f;
                            shieldBreakPending = true;
                        }
                        break;
                }

                efx?.TriggerEffect(evt.type, transform.position);
            }

            if (retaliationActive)
            {
                retaliationPierceCount = pierceFromEvent;
                retaliationBulletStyleId = config.bulletStyleId;
            }
        }

        // ===== 全屏AoE技能 (超暴风) =====
        private void HandleAoeSkill(SkillConfig config)
        {
            float atk = attrs.GetAttack();
            float actualDmg = atk * config.dmg / 10000f;

            float knockbackForce = 0f;
            float slowDuration = 0f, slowRatio = 0f;
            float vulnRatio = 0f, vulnDuration = 0f;

            if (config.events != null)
            {
                foreach (var evt in config.events)
                {
                    if (evt.param == null) continue;
                    switch (evt.type)
                    {
                        case SkillEventType.Knockback:
                            if (evt.param.Length >= 1) knockbackForce = evt.param[0];
                            break;
                        case SkillEventType.Slow:
                            if (evt.param.Length >= 2)
                            {
                                slowDuration = evt.param[0];
                                slowRatio = evt.param[1];
                            }
                            break;
                        case SkillEventType.Vulnerability:
                            if (evt.param.Length >= 2)
                            {
                                vulnRatio = evt.param[0];
                                vulnDuration = evt.param[1];
                            }
                            break;
                    }
                }
            }

            battleManager.DealFullScreenAoe(transform.position, actualDmg,
                knockbackForce, slowDuration, slowRatio, vulnRatio, vulnDuration);
        }

        // ===== 召唤技能 =====
        private void HandleSummonSkill(SkillConfig config)
        {
            if (config.events == null) return;

            float duration = 0f, attrRatio = 0f;
            int extraCount = 0, monsterId = 0;
            bool homing = false;

            foreach (var evt in config.events)
            {
                switch (evt.type)
                {
                    case SkillEventType.Summon:
                        if (evt.param != null && evt.param.Length >= 4)
                        {
                            monsterId = (int)evt.param[0];
                            duration = evt.param[1];
                            attrRatio = evt.param[2];
                            extraCount = (int)evt.param[3];
                        }
                        break;
                    case SkillEventType.Homing:
                        homing = true;
                        break;
                }
            }

            if (monsterId <= 0) return;

            int totalSummons = 1 + extraCount;

            for (int i = 0; i < totalSummons; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-1.5f, 1.5f), Random.Range(-2f, -0.5f), 0f);
                battleManager.SpawnSummon(
                    transform.position + offset, duration, attrRatio, monsterId, homing);
            }
        }

        // ===== 受伤 =====
        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            // 减伤（通过 buff 挂载到 AllElemDmgReduce）
            int dmgReduce = attrs.GetFinal(AttributeIds.AllElemDmgReduce);
            if (dmgReduce > 0)
            {
                damage *= (1f - dmgReduce / 10000f);
            }

            bool shieldWasActive = currentShield > 0;

            if (currentShield > 0)
            {
                if (damage <= currentShield)
                {
                    currentShield -= damage;
                }
                else
                {
                    float remaining = damage - currentShield;
                    currentShield = 0;
                    currentHp -= remaining;
                }

                // 反击
                if (retaliationActive && retaliationBullets > 0)
                {
                    FireRetaliationBullets();
                }
            }
            else
            {
                currentHp -= damage;
            }

            // 护盾破裂
            if (shieldWasActive && currentShield <= 0)
            {
                retaliationActive = false;
                if (shieldBreakPending)
                {
                    shieldBreakPending = false;
                    battleManager.DealAoeDamage(transform.position, shieldBreakRadius, shieldBreakDmg);
                }
            }

            currentHp = Mathf.Max(0, currentHp);
            UpdateBars();

            if (currentHp <= 0)
            {
                battleManager.OnHeroDead();
            }
        }

        private void FireRetaliationBullets()
        {
            if (battleManager == null) return;

            List<Transform> targets = battleManager.GetNearestEnemies(
                transform.position, attackRange, retaliationBullets);

            foreach (var target in targets)
            {
                var mods = new BulletModifiers { pierceCount = retaliationPierceCount };
                battleManager.SpawnSkillBullet(
                    transform.position, target, retaliationDmg, 15f, mods, retaliationBulletStyleId, attackRange);
            }
        }

        private void UpdateBars()
        {
            if (shieldBar != null)
            {
                float shieldMax = maxShield > 0 ? maxShield : maxHp;
                shieldBar.SetValue(currentShield, shieldMax);
            }
            if (hpBar != null)
            {
                hpBar.SetValue(currentHp, maxHp);
            }
        }
    }
}
