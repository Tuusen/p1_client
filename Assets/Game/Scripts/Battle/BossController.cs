using UnityEngine;

namespace GeometryTD
{
    public class BossController : MonoBehaviour
    {
        private float maxHp;
        private float currentHp;
        private float damage;
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

        [SerializeField] private HealthBarUI hpBar;

        private bool isDead;

        public bool IsDead => isDead;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;

        public void Init(MonsterConfig config, Transform hero, BattleManager manager, Vector3 bossPosition)
        {
            battleManager = manager;
            heroTarget = hero;
            targetPosition = bossPosition;
            reachedPosition = false;
            attackTimer = 0f;

            maxHp = config.hp;
            currentHp = maxHp;
            damage = config.damage;
            moveSpeed = config.move_speed;
            attackRange = config.attack_range;
            attackInterval = config.attack_interval;
            attackSkillId = config.attack_skill_id;

            skillConfig = ConfigManager.Instance.GetSkillConfig(attackSkillId);

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

            if (!reachedPosition)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;

                if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
                {
                    transform.position = targetPosition;
                    reachedPosition = true;
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

            battleManager.SpawnBossBullet(transform.position, heroTarget, skillConfig.damage, skillConfig.bullet_speed);
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;

            currentHp -= dmg;
            currentHp = Mathf.Max(0, currentHp);
            UpdateBar();

            if (battleManager != null)
            {
                battleManager.UpdateBossHpUI(currentHp, maxHp);
            }

            if (currentHp <= 0)
            {
                Die();
            }
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
