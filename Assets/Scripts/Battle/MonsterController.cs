using UnityEngine;

namespace GeometryTD
{
    public class MonsterController : MonoBehaviour
    {
        private float maxHp;
        private float currentHp;
        private float damage;
        private float moveSpeed;
        private Transform heroTarget;
        private BattleManager battleManager;
        private bool isDead;
        private Animator animator;
        private CharacterFacing facing;

        // 状态效果
        private bool isFrozen;
        private float freezeTimer;
        private float burnDmg;
        private float burnTimer;
        private float burnTickTimer;
        private float slowTimer;
        private float slowRatio;
        private float vulnerabilityRatio;
        private float vulnerabilityTimer;
        private Vector3 knockbackDir;
        private float knockbackRemaining;
        private const float KnockbackSpeed = 20f;

        private bool isElite;

        // 技能攻击相关
        private bool hasSkill;
        private float skillAttackRange;
        private float attackInterval;
        private float attackTimer;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;
        private const float DefaultSkillAttackRange = 15f;
        private const float DefaultAttackInterval = 1f;

        [SerializeField] private HealthBarUI hpBar;

        public bool IsDead => isDead;
        public bool IsElite => isElite;

        public void Init(MonsterConfig config, Transform hero, BattleManager manager, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            isElite = config.is_elite;

            maxHp = ConfigManager.GetAttrValue(config.attrs, AttributeIds.HP) * hardMultiplier;
            currentHp = maxHp;
            damage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Attack) * hardMultiplier;
            moveSpeed = ConfigManager.GetAttrValue(config.attrs, AttributeIds.MoveSpeed);

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

            // 统一从 attrs 读取攻击间隔
            attackInterval = ConfigManager.GetAttrValue(config.attrs, AttributeIds.AttackInterval, DefaultAttackInterval);
            if (attackInterval <= 0) attackInterval = DefaultAttackInterval;

            // 初始化技能攻击
            hasSkill = false;
            if (config.attack_skill_ids != null && config.attack_skill_ids.Length > 0)
            {
                attackSkillIds = config.attack_skill_ids;
                attackSkillCds = new float[attackSkillIds.Length];
                attackSkillTimers = new float[attackSkillIds.Length];
                attackSkillConfigs = new SkillConfig[attackSkillIds.Length];

                for (int i = 0; i < attackSkillIds.Length; i++)
                {
                    attackSkillConfigs[i] = ConfigManager.Instance.GetSkillConfig(attackSkillIds[i]);
                    attackSkillCds[i] = attackSkillConfigs[i] != null ? attackSkillConfigs[i].cd : 1f;
                    attackSkillTimers[i] = 0f;
                }

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

            // 击退
            if (knockbackRemaining > 0)
            {
                float step = KnockbackSpeed * Time.deltaTime;
                if (step > knockbackRemaining) step = knockbackRemaining;
                transform.position += knockbackDir * step;
                knockbackRemaining -= step;
                ClampToScreen();
                return;
            }

            // 灼烧 DoT
            if (burnTimer > 0)
            {
                burnTickTimer += Time.deltaTime;
                if (burnTickTimer >= 1f)
                {
                    burnTickTimer -= 1f;
                    TakeDamage(burnDmg);
                    if (IsDead) return;
                }
                burnTimer -= Time.deltaTime;
            }

            // 冰冻
            if (isFrozen)
            {
                freezeTimer -= Time.deltaTime;
                if (freezeTimer <= 0) isFrozen = false;
                animator?.SetBool("IsMoving", false);
                return;
            }

            // 减速
            float currentSpeed = moveSpeed;
            if (slowTimer > 0)
            {
                currentSpeed *= (1f - slowRatio / 10000f);
                slowTimer -= Time.deltaTime;
            }

            // 易伤计时
            if (vulnerabilityTimer > 0)
                vulnerabilityTimer -= Time.deltaTime;

            float dist = Vector3.Distance(transform.position, heroTarget.position);

            // 有技能的怪物：保持在技能攻击距离内
            if (hasSkill)
            {
                // 统一攻击间隔计时器
                attackTimer += Time.deltaTime;

                // 在技能攻击范围内，保持静止并攻击
                if (dist <= skillAttackRange)
                {
                    // 停止移动
                    animator?.SetBool("IsMoving", false);
                    facing?.FaceToward(heroTarget.position);

                    // 尝试攻击
                    if (attackTimer >= attackInterval)
                    {
                        attackTimer = 0f;
                        TrySkillAttack();
                    }
                }
                else
                {
                    // 向英雄移动直到达到技能攻击范围
                    Vector3 direction = (heroTarget.position - transform.position).normalized;
                    transform.position += direction * currentSpeed * Time.deltaTime;
                    facing?.FaceToward(heroTarget.position);
                    animator?.SetBool("IsMoving", true);
                }
            }
            else
            {
                // 无技能的怪物：近战行为，保持原逻辑
                Vector3 direction = (heroTarget.position - transform.position).normalized;
                transform.position += direction * currentSpeed * Time.deltaTime;
                facing?.FaceToward(heroTarget.position);
                animator?.SetBool("IsMoving", true);

                if (dist < 0.5f)
                {
                    HeroController hero = heroTarget.GetComponent<HeroController>();
                    if (hero != null)
                    {
                        hero.TakeDamage(damage);
                    }
                    Die();
                }
            }
        }

        private void TrySkillAttack()
        {
            if (battleManager == null) return;

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
            if (skillConfig != null)
            {
                float actualDamage = damage * skillConfig.dmg / 10000f;
                battleManager.SpawnMonsterBullet(transform.position, heroTarget, actualDamage, skillConfig.bulletSpeed);
            }
            else
            {
                battleManager.SpawnMonsterBullet(transform.position, heroTarget, damage, 8f);
            }

            animator?.SetTrigger("Attack");
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;

            if (vulnerabilityTimer > 0 && vulnerabilityRatio > 0)
                dmg *= (1f + vulnerabilityRatio / 10000f);

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

        public void ApplyFreeze(float duration)
        {
            isFrozen = true;
            if (duration > freezeTimer) freezeTimer = duration;
        }

        public void ApplyBurn(float dmgPerTick, float duration)
        {
            burnDmg = dmgPerTick;
            burnTimer = duration;
            burnTickTimer = 0f;
        }

        public void ApplySlow(float duration, float ratio)
        {
            slowRatio = ratio;
            if (duration > slowTimer) slowTimer = duration;
        }

        public void ApplyVulnerability(float duration, float ratio)
        {
            vulnerabilityRatio = ratio;
            if (duration > vulnerabilityTimer) vulnerabilityTimer = duration;
        }

        public void ApplyKnockback(Vector3 sourcePos, float force)
        {
            knockbackDir = Vector3.right;
            knockbackRemaining = force;
        }

        private void ClampToScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            Vector3 cp = cam.transform.position;
            Vector3 pos = transform.position;
            if (pos.x > cp.x + w) { pos.x = cp.x + w; knockbackRemaining = 0; }
            if (pos.x < cp.x - w) { pos.x = cp.x - w; knockbackRemaining = 0; }
            if (pos.y > cp.y + h) { pos.y = cp.y + h; knockbackRemaining = 0; }
            if (pos.y < cp.y - h) { pos.y = cp.y - h; knockbackRemaining = 0; }
            transform.position = pos;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (battleManager != null)
            {
                battleManager.OnMonsterKilled(this);
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
