using UnityEngine;

namespace GeometryTD
{
    public class BossController : MonoBehaviour
    {
        private float maxHp;
        private float currentHp;
        private float baseDamage;
        private float moveSpeed;
        private float attackRange;
        private float attackInterval;
        private int attackSkillId;

        private Transform heroTarget;
        private BattleManager battleManager;
        private Vector3 targetPosition;
        private bool reachedPosition;
        private float attackTimer;
        private SkillConfig skillConfig;
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
        private const float KnockbackSpeed = 15f;

        [SerializeField] private HealthBarUI hpBar;

        private bool isDead;

        public bool IsDead => isDead;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        public void Init(MonsterConfig config, Transform hero, BattleManager manager, Vector3 bossPosition, float hardMultiplier = 1f)
        {
            battleManager = manager;
            heroTarget = hero;
            targetPosition = bossPosition;
            reachedPosition = false;
            attackTimer = 0f;

            maxHp = ConfigManager.GetAttrValue(config.attrs, AttributeIds.HP) * hardMultiplier;
            currentHp = maxHp;
            baseDamage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Damage) * hardMultiplier;
            moveSpeed = ConfigManager.GetAttrValue(config.attrs, AttributeIds.MoveSpeed);
            attackRange = config.attack_range;
            attackInterval = config.attack_interval;
            attackSkillId = config.attack_skill_id;

            skillConfig = ConfigManager.Instance.GetSkillConfig(attackSkillId);

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();

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
                if (knockbackRemaining <= 0)
                    reachedPosition = false; // 击退结束后回到原位
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

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                Attack();
                attackTimer = 0f;
            }
        }

        private void Attack()
        {
            if (skillConfig == null || heroTarget == null) return;

            facing?.FaceToward(heroTarget.position);

            float actualDmg = baseDamage * skillConfig.dmg / 10000f;
            battleManager.SpawnBossBullet(transform.position, heroTarget, actualDmg, skillConfig.bulletSpeed);

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

            if (battleManager != null)
            {
                battleManager.UpdateBossHpUI(currentHp, maxHp);
            }

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
