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

        [SerializeField] private HealthBarUI hpBar;

        public bool IsDead => isDead;

        public void Init(MonsterConfig config, Transform hero, BattleManager manager)
        {
            battleManager = manager;
            heroTarget = hero;

            maxHp = config.hp;
            currentHp = maxHp;
            damage = config.damage;
            moveSpeed = config.move_speed;

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

            // 移动
            Vector3 direction = (heroTarget.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;

            float dist = Vector3.Distance(transform.position, heroTarget.position);
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
            knockbackDir = (transform.position - sourcePos).normalized;
            knockbackRemaining = force;
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
