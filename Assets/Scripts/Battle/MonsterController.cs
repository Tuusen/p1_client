using UnityEngine;

namespace GeometryTD
{
    public class MonsterController : UnitController
    {
        private Transform heroTarget;
        private bool isElite;

        // 技能攻击相关
        private bool hasSkill;
        private float skillAttackRange;
        private const float DefaultSkillAttackRange = 15f;

        public override PassiveSystem PassiveSystem => null;
        public bool IsElite => isElite;

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



        public void Init(MonsterConfig config, Transform hero, BattleManager manager, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            isElite = config.is_elite;

            // 初始化属性组件
            InitAttrs(config.attrs);

            // 应用难度乘数到基础属性
            if (hardMultiplier != 1f)
            {
                attrs.SetBase(AttributeIds.HP, (int)(attrs.GetBase(AttributeIds.HP) * hardMultiplier));
                attrs.SetBase(AttributeIds.Attack, (int)(attrs.GetBase(AttributeIds.Attack) * hardMultiplier));
            }

            maxHp = attrs.GetMaxHp();
            currentHp = maxHp;

            // 初始化技能攻击
            hasSkill = false;
            if (config.attack_skill_ids != null && config.attack_skill_ids.Length > 0)
            {
                InitSkills(config.attack_skill_ids);

                if (attackSkillConfigs[0] != null)
                {
                    skillAttackRange = attackSkillConfigs[0].attack_range > 0 ? attackSkillConfigs[0].attack_range : DefaultSkillAttackRange;
                }
                else
                {
                    skillAttackRange = DefaultSkillAttackRange;
                }

                hasSkill = true;
            }
            else
            {
                skillAttackRange = 0.5f; // 默认近战距离
            }

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

            // 冰冻中只停止移动
            if (buffSystem.IsFrozen())
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            // 移速（AttrComponent 已含 buff 加成）
            float currentSpeed = attrs.GetMoveSpeed();

            float dist = Vector3.Distance(transform.position, heroTarget.position);

            if (hasSkill)
            {
                attackTimer += Time.deltaTime;
                float atkInterval = attrs.GetAttackIntervalSec();

                if (dist <= skillAttackRange)
                {
                    animator?.SetBool("IsMoving", false);
                    facing?.FaceToward(heroTarget.position);

                    if (attackTimer >= atkInterval)
                    {
                        attackTimer = 0f;
                        TrySkillAttack();
                    }
                }
                else
                {
                    Vector3 direction = (heroTarget.position - transform.position).normalized;
                    transform.position += direction * currentSpeed * Time.deltaTime;
                    facing?.FaceToward(heroTarget.position);
                    animator?.SetBool("IsMoving", true);
                }
            }
            else
            {
                Vector3 direction = (heroTarget.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                facing?.FaceToward(heroTarget.position);
                animator?.SetBool("IsMoving", true);

                if (dist < 0.5f)
                {
                    HeroController hero = heroTarget.GetComponent<HeroController>();
                    if (hero != null)
                    {
                        hero.TakeDamage(attrs.GetAttack(), this);
                    }
                    Die();
                }
            }

            ClampToScreen();
        }

        private void TrySkillAttack()
        {
            if (battleManager == null) return;

            int skillIndex = SelectSkillIndex();
            if (skillIndex < 0) return;

            ResetSkillTimer(skillIndex);

            float atk = attrs.GetAttack();
            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig != null)
            {
                float actualDamage = atk * skillConfig.dmg / 10000f;
                battleManager.SpawnMonsterBullet(transform.position, heroTarget, actualDamage, skillConfig.bulletSpeed);
            }
            else
            {
                battleManager.SpawnMonsterBullet(transform.position, heroTarget, atk, 8f);
            }

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
                battleManager.OnMonsterKilled(this);
            }
            Destroy(gameObject);
        }

        protected override void OnDestroyed()
        {
            // Monster 死亡逻辑已在上层 Die() 中处理
        }

        protected override void OnFireBullet(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill)
        {
            battleManager.SpawnMonsterBullet(transform.position, target, damage, speed);
        }


    }
}
