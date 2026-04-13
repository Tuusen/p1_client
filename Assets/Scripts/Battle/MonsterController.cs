using UnityEngine;

namespace GeometryTD
{
    public class MonsterController : MonoBehaviour, IBuffTarget
    {
        private AttrComponent attrs;
        private BuffSystem buffSystem = new BuffSystem();

        private int maxHp;
        private float currentHp;
        private Transform heroTarget;
        private BattleManager battleManager;
        private bool isDead;
        private Animator animator;
        private CharacterFacing facing;

        private bool isElite;

        // 技能攻击相关
        private bool hasSkill;
        private float skillAttackRange;
        private float attackTimer;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;
        private const float DefaultSkillAttackRange = 15f;

        [SerializeField] private HealthBarUI hpBar;

        // IBuffTarget 实现
        public AttrComponent Attrs => attrs;
        public BuffSystem BuffSystem => buffSystem;
        public PassiveSystem PassiveSystem => null;
        public bool IsDead => isDead;
        public bool IsElite => isElite;
        public Vector3 Position => transform.position;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;
        public Transform CachedTransform => base.transform;
        public BattleManager BattleManager => battleManager;

        public void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            TakeDamage(dmg);
        }

        public void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            UpdateBar();
        }

        public void AddShield(int value) { }

        public int GetHpPercent()
        {
            if (maxHp <= 0) return 0;
            return Mathf.RoundToInt(currentHp / (float)maxHp * 10000);
        }

        public int GetShieldPercent() => 0;

        public void Init(MonsterConfig config, Transform hero, BattleManager manager, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            isElite = config.is_elite;

            // 初始化属性组件
            attrs = GetComponent<AttrComponent>();
            if (attrs == null) attrs = gameObject.AddComponent<AttrComponent>();
            attrs.Init(config.attrs);

            // 应用难度乘数到基础属性
            if (hardMultiplier != 1f)
            {
                attrs.SetBase(AttributeIds.HP, (int)(attrs.GetBase(AttributeIds.HP) * hardMultiplier));
                attrs.SetBase(AttributeIds.Attack, (int)(attrs.GetBase(AttributeIds.Attack) * hardMultiplier));
            }

            maxHp = attrs.GetMaxHp();
            currentHp = maxHp;

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

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
                    attackSkillConfigs[i] = Cfg.Skill.Get(attackSkillIds[i]);
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

            // 冰冻中只停止移动
            if (buffSystem.IsFrozen())
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            // 移速（AttrComponent 已含 buff 加成）
            float currentSpeed = attrs.GetMoveSpeed();

            // 技能冷却计时
            if (hasSkill)
            {
                for (int i = 0; i < attackSkillTimers.Length; i++)
                    attackSkillTimers[i] += Time.deltaTime;
            }

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

            animator?.SetTrigger("Attack");
        }

        public void TakeDamage(float dmg, IBuffTarget attacker = null)
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
