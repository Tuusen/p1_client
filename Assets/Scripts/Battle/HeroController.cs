using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class HeroController : MonoBehaviour, IBuffTarget
    {
        private AttrComponent attrs;
        private BuffSystem buffSystem = new BuffSystem();
        private PassiveSystem passiveSystem = new PassiveSystem();

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

        // Charge 状态
        private int[] chargeBuffIds;
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
        public BuffSystem BuffSystem => buffSystem;
        public PassiveSystem PassiveSystem => passiveSystem;
        public bool IsDead => currentHp <= 0;
        public Vector3 Position => transform.position;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;
        public Transform CachedTransform => base.transform;
        public BattleManager BattleManager => battleManager;

        public float AttackRange => attackRange;
        public float BaseAttack => attrs != null ? attrs.GetAttack() : 0;

        public void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            TakeDamage(dmg);
        }

        public void OnBuffHeal(float heal)
        {
            TriggerPassive(201);    // 被动：受治疗前
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            TriggerPassive(202);    // 被动：受治疗时
            UpdateBars();
            TriggerPassive(203);    // 被动：受治疗后

            // 显示治疗飘字
            if (battleManager != null)
                battleManager.ShowDamageText(transform.position, heal, true);
        }

        public void AddShield(int value)
        {
            if (value > 0)
                TriggerPassive(204);    // 被动：获得护盾时
            currentShield += value;
            if (currentShield < 0) currentShield = 0;
            if (currentShield > maxHp) currentShield = maxHp;
            UpdateBars();
        }

        public int GetHpPercent()
        {
            if (maxHp <= 0) return 0;
            return Mathf.RoundToInt(currentHp / maxHp * 10000);
        }

        public int GetShieldPercent()
        {
            if (maxHp <= 0) return 0;
            return Mathf.RoundToInt(currentShield / maxHp * 10000);
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
            chargeBuffIds = config.charge_buff_ids;
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
                    attackSkillConfigs[i] = Cfg.Skill.Get(attackSkillIds[i]);
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
            passiveSystem.Clear();
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

            // Buff 系统驱动
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
                if (!buffSystem.IsFrozen())
                    TryAttack();
            }

            UpdateBars();
        }

        private void UpdateChargeState()
        {
            if (chargeBuffIds == null || chargeBuffIds.Length == 0) return;

            float idle = Time.time - lastAttackTime;

            if (!isCharging && idle >= ChargeIdleThreshold)
            {
                // 进入蓄力：添加 Charge buff
                isCharging = true;
                animator?.SetTrigger("Charge");
                for (int i = 0; i < chargeBuffIds.Length; i++)
                {
                    buffSystem.AddBuff(chargeBuffIds[i], this);
                }
            }
        }

        private void ExitCharge()
        {
            if (!isCharging) return;
            isCharging = false;
            if (chargeBuffIds != null)
            {
                for (int i = 0; i < chargeBuffIds.Length; i++)
                    buffSystem.RemoveBuffByConfigId(chargeBuffIds[i], 1);
            }
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

            // 构建子弹数据（合并buff附加的bulletEvent）
            int[] allBulletEventIds = MergeBulletEventIds(skillConfig.bulletEvents, buffSystem.CollectExtraBulletEventIds(skillConfig.id));
            var bulletData = BulletEventExecutor.BuildBulletData(allBulletEventIds);

            // 目标数量：优先 volleyCount，其次 AttackCount 属性
            int targetCount = bulletData.volleyCount > 0
                ? bulletData.volleyCount
                : attrs.GetFinal(AttributeIds.AttackCount);
            if (targetCount < 1) targetCount = 1;

            List<Transform> targets = battleManager.GetNearestEnemiesUnique(
                transform.position, skillRange, targetCount);
            if (targets.Count == 0) return;

            // 被动：释放普攻前
            TriggerPassive(501);

            // 攻击时退出蓄力
            ExitCharge();
            lastAttackTime = Time.time;

            facing?.FaceToward(targets[0].position);

            float atk = attrs.GetAttack();
            float actualDmg = atk * skillConfig.dmg / 10000f;

            // Type 1: buff技能伤害修饰
            int dmgMod = buffSystem.GetSkillDmgModifier(skillConfig.id);
            if (dmgMod != 0) actualDmg *= (1f + dmgMod / 10000f);

            // 合并 enemyEvents 到子弹的 attachToTargetEventIds
            MergeEnemyEvents(bulletData, skillConfig.enemyEvents);

            if (bulletData.burstCount > 1)
            {
                StartCoroutine(BurstFireRoutine(targets, actualDmg, skillConfig, bulletData));
            }
            else
            {
                FireBullets(targets, actualDmg, skillConfig, bulletData);
            }

            // 执行自身事件
            var ctx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(skillConfig.events, ctx);

            battleManager.OnHeroNormalAttack(transform.position);
            animator?.SetTrigger("Attack");

            // 被动：释放普攻后
            TriggerPassive(502);
        }

        // ===== 技能路由 =====
        public void UseSkill(SkillConfig config)
        {
            if (IsDead || battleManager == null || config == null) return;

            // 被动：释放技能前
            TriggerPassive(503);

            var category = SkillManager.ClassifySkill(config);
            switch (category)
            {
                case SkillCategory.Summon:     HandleSummonSkill(config);     break;
                case SkillCategory.Shield:     HandleShieldSkill(config);     break;
                case SkillCategory.Self:       HandleSelfSkill(config);       break;
                case SkillCategory.Projectile: HandleProjectileSkill(config); break;
                case SkillCategory.Aoe:        HandleAoeSkill(config);        break;
            }

            // 被动：释放技能后
            TriggerPassive(504);
        }

        // ===== 弹幕技能 =====
        private void HandleProjectileSkill(SkillConfig config)
        {
            float atk = attrs.GetAttack();
            float actualDmg = atk * config.dmg / 10000f;

            // Type 1: buff技能伤害修饰
            int dmgMod = buffSystem.GetSkillDmgModifier(config.id);
            if (dmgMod != 0) actualDmg *= (1f + dmgMod / 10000f);

            // Type 3: 合并buff附加的bulletEvent
            int[] allBulletEventIds = MergeBulletEventIds(config.bulletEvents, buffSystem.CollectExtraBulletEventIds(config.id));
            var bulletData = BulletEventExecutor.BuildBulletData(allBulletEventIds);
            MergeEnemyEvents(bulletData, config.enemyEvents);

            int shotCount = bulletData.volleyCount > 0 ? bulletData.volleyCount : 1;
            float skillRange = config.attack_range > 0 ? config.attack_range : attackRange;

            List<Transform> targets = battleManager.GetNearestEnemies(
                transform.position, skillRange, shotCount);
            if (targets.Count == 0) return;

            if (bulletData.burstCount > 1)
            {
                StartCoroutine(BurstFireRoutine(targets, actualDmg, config, bulletData));
            }
            else
            {
                FireBullets(targets, actualDmg, config, bulletData);
            }

            // 执行自身事件
            var ctx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(config.events, ctx);
        }

        // ===== 自身效果技能 =====
        private void HandleSelfSkill(SkillConfig config)
        {
            var ctx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(config.events, ctx);
        }

        // ===== 护盾技能 =====
        private void HandleShieldSkill(SkillConfig config)
        {
            var ctx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(config.events, ctx);
        }

        // ===== 全屏AoE技能 =====
        private void HandleAoeSkill(SkillConfig config)
        {
            float atk = attrs.GetAttack();
            float actualDmg = atk * config.dmg / 10000f;

            // Type 1: buff技能伤害修饰
            int dmgMod = buffSystem.GetSkillDmgModifier(config.id);
            if (dmgMod != 0) actualDmg *= (1f + dmgMod / 10000f);

            // 执行自身事件
            var selfCtx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(config.events, selfCtx);

            // 全屏AoE伤害 + 敌方事件
            battleManager.DealFullScreenAoe(transform.position, actualDmg, config.enemyEvents, this);
        }

        // ===== 召唤技能 =====
        private void HandleSummonSkill(SkillConfig config)
        {
            var ctx = new EventContext
            {
                caster = this,
                target = this,
                battleManager = battleManager,
                position = transform.position
            };
            EventExecutor.ExecuteEvents(config.events, ctx);
        }

        // ===== 被动触发辅助 =====
        private void TriggerPassive(int triggerCode, IBuffTarget target = null)
        {
            if (passiveSystem == null) return;
            var ctx = new EventContext
            {
                caster = this,
                target = target ?? (IBuffTarget)this,
                battleManager = battleManager,
                position = transform.position
            };
            passiveSystem.OnTrigger(triggerCode, ctx);
        }

        // ===== 受伤 =====
        public void TakeDamage(float damage, IBuffTarget attacker = null)
        {
            if (IsDead) return;

            // 被动：受伤害前
            TriggerPassive(101, attacker);

            // 反击（在无敌判定前触发）
            BuffSystem.TryCounterAttack(this, attacker, buffSystem, battleManager);

            // 无敌免疫伤害
            if (buffSystem.IsInvincible()) return;

            // 减伤（通过 buff 挂载到 AllElemDmgReduce）
            int dmgReduce = attrs.GetFinal(AttributeIds.AllElemDmgReduce);
            if (dmgReduce > 0)
            {
                damage *= (1f - dmgReduce / 10000f);
            }

            // 被动：受伤害时
            TriggerPassive(102, attacker);

            // 记录护盾状态用于判断 104/105
            float shieldBefore = currentShield;

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
            }
            else
            {
                currentHp -= damage;
            }

            currentHp = Mathf.Max(0, currentHp);

            // 被动：护盾受伤后
            if (shieldBefore > 0 && currentShield < shieldBefore)
                TriggerPassive(104, attacker);

            // 被动：护盾破碎后
            if (shieldBefore > 0 && currentShield <= 0)
                TriggerPassive(105, attacker);

            // 被动：受伤害后
            TriggerPassive(103, attacker);

            UpdateBars();

            // 显示飘字
            if (battleManager != null)
                battleManager.ShowDamageText(transform.position, damage, false);

            if (currentHp <= 0)
            {
                TriggerPassive(401, attacker);  // 被动：死亡前
                battleManager.OnHeroDead();
                TriggerPassive(402, attacker);  // 被动：死亡后
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

        // ===== 工具方法 =====

        private void FireBullets(List<Transform> targets, float actualDmg, SkillConfig config, BulletEventData bulletData)
        {
            float skillRange = config.attack_range > 0 ? config.attack_range : attackRange;
            foreach (var target in targets)
            {
                if (target == null) continue;
                battleManager.SpawnSkillBulletWithScatter(transform.position, target, actualDmg,
                    config.bulletSpeed, bulletData, config.bulletStyleId, skillRange, this, config);
            }
        }

        private IEnumerator BurstFireRoutine(List<Transform> targets, float actualDmg, SkillConfig config, BulletEventData bulletData)
        {
            int burstCount = bulletData.burstCount;
            bulletData.burstCount = 0;

            for (int b = 0; b < burstCount; b++)
            {
                if (IsDead || battleManager == null) yield break;
                FireBullets(targets, actualDmg, config, bulletData);
                if (b < burstCount - 1)
                    yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// 将技能的 enemyEvents 合并到 bulletData 的 attachToTargetEventIds 中
        /// </summary>
        private static void MergeEnemyEvents(BulletEventData bulletData, int[] enemyEvents)
        {
            if (enemyEvents == null || enemyEvents.Length == 0) return;
            if (bulletData.attachToTargetEventIds == null)
                bulletData.attachToTargetEventIds = new List<int>();
            for (int i = 0; i < enemyEvents.Length; i++)
                bulletData.attachToTargetEventIds.Add(enemyEvents[i]);
        }

        private static int[] MergeBulletEventIds(int[] baseIds, List<int> extraIds)
        {
            if (extraIds == null || extraIds.Count == 0) return baseIds;
            int baseLen = baseIds != null ? baseIds.Length : 0;
            int[] merged = new int[baseLen + extraIds.Count];
            if (baseIds != null)
                System.Array.Copy(baseIds, merged, baseLen);
            for (int i = 0; i < extraIds.Count; i++)
                merged[baseLen + i] = extraIds[i];
            return merged;
        }
    }
}
