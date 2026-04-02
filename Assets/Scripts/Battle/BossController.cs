using UnityEngine;

namespace GeometryTD
{
    public class BossController : MonoBehaviour, IBuffTarget
    {
        private AttrComponent attrs;
        private BuffSystem buffSystem = new BuffSystem();

        private int maxHp;
        private float currentHp;
        private float attackRange;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;

        private Transform heroTarget;
        private BattleManager battleManager;
        private Vector3 targetPosition;
        private bool reachedPosition;
        private float attackTimer;
        private SkillConfig skillConfig;
        private Animator animator;
        private CharacterFacing facing;

        [SerializeField] private HealthBarUI hpBar;

        private bool isDead;

        // IBuffTarget 实现
        public AttrComponent Attrs => attrs;
        public bool IsDead => isDead;
        public Vector3 Position => transform.position;

        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        public void OnBuffDamage(float dmg)
        {
            TakeDamage(dmg);
        }

        public void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            UpdateBar();
        }

        public void Init(MonsterConfig config, Transform hero, BattleManager manager, Vector3 bossPosition, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            targetPosition = bossPosition;
            reachedPosition = false;

            // 初始化属性组件
            attrs = GetComponent<AttrComponent>();
            if (attrs == null) attrs = gameObject.AddComponent<AttrComponent>();
            attrs.Init(config.attrs);

            if (hardMultiplier != 1f)
            {
                attrs.SetBase(AttributeIds.HP, (int)(attrs.GetBase(AttributeIds.HP) * hardMultiplier));
                attrs.SetBase(AttributeIds.Attack, (int)(attrs.GetBase(AttributeIds.Attack) * hardMultiplier));
            }

            maxHp = attrs.GetMaxHp();
            currentHp = maxHp;
            attackTimer = 0f;

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
                attackRange = 15.0f;
            }

            skillConfig = attackSkillConfigs != null && attackSkillConfigs.Length > 0 ? attackSkillConfigs[0] : null;

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

            buffSystem.Clear();
            UpdateBar();
        }

        public void SetBar(HealthBarUI bar)
        {
            hpBar = bar;
            UpdateBar();
        }

        private void Update()
        {
            if (IsDead || heroTarget == null) return;

            // Buff 系统驱动
            buffSystem.Tick(Time.deltaTime, this);
            if (IsDead) return;

            // 击退中
            if (buffSystem.IsKnockingBack())
            {
                ClampToScreen();
                return;
            }

            // 击退刚结束需要回到目标位置
            if (!buffSystem.HasBuff(BuffType.Knockback) && reachedPosition)
            {
                float distToTarget = Vector3.Distance(transform.position, targetPosition);
                if (distToTarget > 0.3f)
                    reachedPosition = false;
            }

            // 冰冻
            if (buffSystem.HasBuff(BuffType.Freeze))
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            float currentSpeed = attrs.GetMoveSpeed();

            // 技能冷却计时
            if (attackSkillTimers != null)
            {
                for (int i = 0; i < attackSkillTimers.Length; i++)
                    attackSkillTimers[i] += Time.deltaTime;
            }

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
        }

        private void TryAttack()
        {
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;
            if (heroTarget == null) return;

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

            facing?.FaceToward(heroTarget.position);

            float atk = attrs.GetAttack();
            float actualDmg = atk * skillConfig.dmg / 10000f;
            battleManager.SpawnBossBullet(transform.position, heroTarget, actualDmg, skillConfig.bulletSpeed);

            animator?.SetTrigger("Attack");
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;

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

        // === Buff 快捷方法 ===

        public void ApplyFreeze(float duration)
        {
            var buff = new BuffEntry { type = BuffType.Freeze, duration = duration };
            buffSystem.AddBuff(buff);
        }

        public void ApplyBurn(float dmgPerTick, float duration)
        {
            var buff = new BuffEntry
            {
                type = BuffType.DamageOverTime,
                duration = duration,
                value = (int)dmgPerTick,
                tickInterval = 1f
            };
            buffSystem.AddBuff(buff);
        }

        public void ApplySlow(float duration, float ratio)
        {
            var buff = new BuffEntry
            {
                type = BuffType.AttrModify,
                duration = duration,
                attrId = AttributeIds.MoveSpeed,
                value = -(int)ratio
            };
            buffSystem.AddBuff(buff);
        }

        public void ApplyVulnerability(float duration, float ratio)
        {
            var buff = new BuffEntry
            {
                type = BuffType.AttrModify,
                duration = duration,
                attrId = AttributeIds.AllElemDmgReduce,
                value = -(int)ratio
            };
            buffSystem.AddBuff(buff);
        }

        public void ApplyKnockback(Vector3 sourcePos, float force)
        {
            var buff = new BuffEntry
            {
                type = BuffType.Knockback,
                duration = force,
                knockbackDir = Vector3.right
            };
            buffSystem.AddBuff(buff);
        }

        private void ClampToScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            Vector3 cp = cam.transform.position;
            Vector3 pos = transform.position;
            if (pos.x > cp.x + w) pos.x = cp.x + w;
            if (pos.x < cp.x - w) pos.x = cp.x - w;
            if (pos.y > cp.y + h) pos.y = cp.y + h;
            if (pos.y < cp.y - h) pos.y = cp.y - h;
            transform.position = pos;
        }

        private void Die()
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

        private void UpdateBar()
        {
            if (hpBar != null)
            {
                hpBar.SetValue(currentHp, maxHp);
            }
        }
    }
}
