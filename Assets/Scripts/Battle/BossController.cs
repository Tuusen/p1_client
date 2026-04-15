using UnityEngine;

namespace GeometryTD
{
    public class BossController : UnitController
    {
        private Transform heroTarget;
        private Vector3 targetPosition;
        private bool reachedPosition;
        private SkillConfig skillConfig;

        public override PassiveSystem PassiveSystem => null;

        public override void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            TakeDamage(dmg);
        }

        public override void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            UpdateBar();
        }

        public override void AddShield(int value) { }



        public void Init(MonsterConfig config, Transform hero, BattleManager manager, Vector3 bossPosition, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            targetPosition = bossPosition;
            reachedPosition = false;

            // 初始化uid和group
            InitUnit(UnitGroup.Enemy, UnitType.Boss);

            // 初始化属性组件
            InitAttrs(config.attrs);

            if (hardMultiplier != 1f)
            {
                attrs.SetBase(AttributeIds.HP, (int)(attrs.GetBase(AttributeIds.HP) * hardMultiplier));
                attrs.SetBase(AttributeIds.Attack, (int)(attrs.GetBase(AttributeIds.Attack) * hardMultiplier));
            }

            maxHp = attrs.GetMaxHp();
            currentHp = maxHp;
            attackTimer = 0f;

            // 初始化攻击技能
            InitSkills(config.attack_skill_ids);

            skillConfig = attackSkillConfigs != null && attackSkillConfigs.Length > 0 ? attackSkillConfigs[0] : null;

            InitComponents();
        }

        public void SetBar(HealthBarUI bar)
        {
            hpBar = bar;
            UpdateBar();
        }

        private void Update()
        {
            if (IsDead || heroTarget == null) return;

            // 调用基类Update
            UnitUpdate();
            if (IsDead) return;

            // 被击退后检查是否需要回到目标位置
            if (reachedPosition)
            {
                float distToTarget = Vector3.Distance(transform.position, targetPosition);
                if (distToTarget > 0.3f)
                    reachedPosition = false;
            }

            // 冰冻
            if (buffSystem.IsFrozen())
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            float currentSpeed = attrs.GetMoveSpeed();

            if (!reachedPosition)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                facing?.FaceToward(heroTarget.position);
                animator?.SetBool("IsMoving", true);

                if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
                {
                    transform.position = targetPosition;
                    reachedPosition = true;
                    animator?.SetBool("IsMoving", false);
                }
                return;
            }

            float atkInterval = attrs.GetAttackIntervalSec();
            attackTimer += Time.deltaTime;
            if (attackTimer >= atkInterval)
            {
                attackTimer = 0f;
                TryAttack();
            }

            ClampToScreen();
        }

        private void TryAttack()
        {
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;
            if (heroTarget == null) return;

            int skillIndex = SelectSkillIndex();
            if (skillIndex < 0) return;

            ResetSkillTimer(skillIndex);

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            FaceTarget(heroTarget.position);

            float actualDmg = CalculateDamage(skillConfig);
            battleManager.SpawnBossBullet(transform.position, heroTarget, actualDmg, skillConfig.bulletSpeed);

            PlayAttackAnimation();
        }

        public override void TakeDamage(float dmg, IBuffTarget attacker = null)
        {
            if (IsDead) return;

            BuffSystem.TryCounterAttack(this, attacker, buffSystem, battleManager);
            if (buffSystem.IsInvincible()) return;

            currentHp -= dmg;
            currentHp = Mathf.Max(0, currentHp);
            UpdateBar();

            if (battleManager != null)
                battleManager.ShowDamageText(transform.position, dmg, false);

            if (battleManager != null)
            {
                battleManager.UpdateBossHpUI(currentHp, maxHp);
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }



        protected override void Die()
        {
            if (isDead) return;
            isDead = true;
            buffSystem.Clear();

            if (battleManager != null)
            {
                battleManager.OnBossKilled();
            }
            Destroy(gameObject);
        }

        protected override void OnDestroyed()
        {
            // Boss 死亡逻辑已在上层 Die() 中处理
        }

        protected override void OnFireBullet(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill)
        {
            battleManager.SpawnBossBullet(transform.position, target, damage, speed);
        }


    }
}
