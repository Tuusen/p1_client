using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class HeroController : UnitController
    {
        private PassiveSystem passiveSystem = new PassiveSystem();

        [Header("运行时状态")]
        private int maxShield;
        private float currentShield;

        // Charge 状态
        private int[] chargeBuffIds;
        private float lastAttackTime;
        private bool isCharging;
        private const float ChargeIdleThreshold = 5f;

        [Header("引用")]
        [SerializeField] private HealthBarUI shieldBar;
        private SkillConfig normalAttackConfig;

        public override PassiveSystem PassiveSystem => passiveSystem;

        public float AttackRange => attackRange;
        public float BaseAttack => attrs != null ? attrs.GetAttack() : 0;

        public override void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            TakeDamage(dmg);
        }

        public override void OnBuffHeal(float heal)
        {
            TriggerPassive(201);    // 被动：受治疗前
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            TriggerPassive(202);    // 被动：受治疗时
            UpdateBar();
            TriggerPassive(203);    // 被动：受治疗后

            // 显示治疗飘字
            if (battleManager != null)
                battleManager.ShowDamageText(transform.position, heal, true);
        }

        public override void AddShield(int value)
        {
            if (value > 0)
                TriggerPassive(204);    // 被动：获得护盾时
            currentShield += value;
            if (currentShield < 0) currentShield = 0;
            if (currentShield > maxHp) currentShield = maxHp;
            UpdateBars();
        }



        public void Init(HeroConfig config, BattleManager manager)
        {
            battleManager = manager;

            // 初始化属性组件
            InitAttrs(config.attrs);

            maxHp = attrs.GetMaxHp();
            maxShield = attrs.GetFinal(AttributeIds.Shield);
            currentHp = maxHp;
            currentShield = maxShield;

            // Charge 配置
            chargeBuffIds = config.charge_buff_ids;
            lastAttackTime = Time.time;
            isCharging = false;

            // 初始化攻击技能
            InitSkills(config.attack_skill_ids);

            InitComponents();
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

            // 调用基类Update
            UnitUpdate();
            if (IsDead) return;

            // Charge 状态检测
            UpdateChargeState();

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
            int skillIndex = SelectSkillIndex();
            if (skillIndex < 0) return;

            ResetSkillTimer(skillIndex);

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

            FaceTarget(targets[0].position);

            float actualDmg = CalculateDamage(skillConfig);

            // 合并 enemyEvents 到子弹的 attachToTargetEventIds
            MergeEnemyEvents(bulletData, skillConfig.enemyEvents);

            if (bulletData.burstCount > 1)
            {
                // Hero 需要遍历多个目标
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    StartCoroutine(BurstFireRoutine(target, actualDmg, skillConfig.bulletSpeed, bulletData, skillConfig.bulletStyleId, skillRange, skillConfig));
                }
            }
            else
            {
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    OnFireBullet(target, actualDmg, skillConfig.bulletSpeed, bulletData, skillConfig.bulletStyleId, skillRange, skillConfig);
                }
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
            PlayAttackAnimation();

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
            float actualDmg = CalculateDamage(config);

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
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    StartCoroutine(BurstFireRoutine(target, actualDmg, config.bulletSpeed, bulletData, config.bulletStyleId, skillRange, config));
                }
            }
            else
            {
                foreach (var target in targets)
                {
                    if (target == null) continue;
                    OnFireBullet(target, actualDmg, config.bulletSpeed, bulletData, config.bulletStyleId, skillRange, config);
                }
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
            float actualDmg = CalculateDamage(config);

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
        // 已移至基类

        // ===== 受伤 =====
        public override void TakeDamage(float damage, IBuffTarget attacker = null)
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
            UpdateBar(); // 调用基类方法更新HP条
        }

        // ===== 工具方法 =====

        protected override void OnFireBullet(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill)
        {
            battleManager.SpawnSkillBulletWithScatter(transform.position, target, damage,
                speed, bulletData, bulletStyleId, attackRange, this, skill);
        }

        protected override void OnDestroyed()
        {
            // Hero 死亡逻辑由 TakeDamage 中的 battleManager.OnHeroDead() 处理
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
    }
}
