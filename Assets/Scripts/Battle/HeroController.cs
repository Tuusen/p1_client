using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class HeroController : MonoBehaviour
    {
        [Header("配置")]
        private float maxHp;
        private float maxShield;
        private float attackRange;
        private float attackInterval;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;
        private float baseAttack;
        private int attackCount;

        [Header("运行时状态")]
        private float currentHp;
        private float currentShield;
        private float attackTimer;

        // Buff状态
        private float hotHealPerSec;
        private float hotRemaining;
        private float dmgReductionRatio;
        private float dmgReductionRemaining;

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

        [Header("引用")]
        [SerializeField] private HealthBarUI shieldBar;
        [SerializeField] private HealthBarUI hpBar;

        private BattleManager battleManager;
        private SkillConfig normalAttackConfig;
        private Animator animator;
        private CharacterFacing facing;

        public bool IsDead => currentHp <= 0;
        public float AttackRange => attackRange;
        public float BaseAttack => baseAttack;
        public float MaxHp => maxHp;

        public void Init(HeroConfig config, BattleManager manager)
        {
            battleManager = manager;

            maxHp = ConfigManager.GetAttrValue(config.attrs, AttributeIds.HP);
            maxShield = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Shield);
            baseAttack = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Attack);
            attackCount = (int)ConfigManager.GetAttrValue(config.attrs, AttributeIds.AttackCount, 1f);
            if (attackCount < 1) attackCount = 1;

            currentHp = maxHp;
            currentShield = maxShield;

            // 初始化攻击技能
            attackInterval = ConfigManager.GetAttrValue(config.attrs, AttributeIds.AttackInterval, 1f);
            if (attackInterval <= 0) attackInterval = 1f;

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

                    // 使用第一个技能的攻击范围作为默认攻击范围
                    if (i == 0 && attackSkillConfigs[i] != null)
                    {
                        attackRange = attackSkillConfigs[i].attack_range;
                    }
                }
            }
            else
            {
                attackRange = 5f; // 默认近战距离
            }

            normalAttackConfig = attackSkillConfigs != null && attackSkillConfigs.Length > 0 ? attackSkillConfigs[0] : null;

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

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

            // 攻击间隔计时器
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                TryAttack();
            }

            // HoT
            if (hotRemaining > 0)
            {
                float heal = hotHealPerSec * Time.deltaTime;
                currentHp = Mathf.Min(currentHp + heal, maxHp);
                hotRemaining -= Time.deltaTime;
                UpdateBars();
            }

            // 减伤计时
            if (dmgReductionRemaining > 0)
            {
                dmgReductionRemaining -= Time.deltaTime;
                if (dmgReductionRemaining <= 0)
                    dmgReductionRatio = 0;
            }
        }

        private void TryAttack()
        {
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;

            // 从后往前查找第一个未冷却的技能
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

            // 重置该技能的冷却计时器
            attackSkillTimers[skillIndex] = 0f;

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            // 使用技能配置的攻击范围
            float skillRange = skillConfig.attack_range > 0 ? skillConfig.attack_range : attackRange;

            List<Transform> targets = battleManager.GetNearestEnemiesUnique(
                transform.position, skillRange, attackCount);
            if (targets.Count == 0) return;

            facing?.FaceToward(targets[0].position);

            float actualDmg = baseAttack * skillConfig.dmg / 10000f;
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
            float actualDmg = baseAttack * config.dmg / 10000f;
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
                                mods.explosionDmg = baseAttack * evt.param[1] / 10000f;
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
                                mods.burnDmg = baseAttack * evt.param[0] / 10000f;
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
                            hotHealPerSec = maxHp * evt.param[0] / 10000f;
                            hotRemaining = evt.param[1];
                        }
                        break;
                    case SkillEventType.DamageReduction:
                        if (evt.param.Length >= 2)
                        {
                            dmgReductionRatio = evt.param[0];
                            dmgReductionRemaining = evt.param[1];
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
                            retaliationDmg = baseAttack * evt.param[0] / 10000f;
                            retaliationBullets = (int)evt.param[1];
                            retaliationPierceCount = 0;
                            retaliationActive = true;
                            // param[2] = 反击子弹速度, 0时使用默认值
                            // 反击穿刺由单独的 Pierce 事件提供
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
                            shieldBreakDmg = baseAttack * evt.param[1] / 10000f;
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
            float actualDmg = baseAttack * config.dmg / 10000f;

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

            // 减伤
            if (dmgReductionRemaining > 0 && dmgReductionRatio > 0)
            {
                damage *= (1f - dmgReductionRatio / 10000f);
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
