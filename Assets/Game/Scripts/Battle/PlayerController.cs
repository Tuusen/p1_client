using UnityEngine;

namespace GeometryTD
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHp = 200;
        [SerializeField] private int maxShield = 100;
        [SerializeField] private int attackDamage = 25;
        [SerializeField] private float attackInterval = 1.0f;
        [SerializeField] private float bulletSpeed = 12.0f;

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private HealthBarUI healthBar;

        public int CurrentHp { get; private set; }
        public int CurrentShield { get; private set; }
        public int MaxHp => maxHp;
        public int MaxShield => maxShield;

        private float attackTimer;

        public event System.Action OnDeath;
        public event System.Action<int, int, int, int> OnHealthChanged;

        private void Start()
        {
            var config = GameConfig.Data.player;
            if (config != null)
            {
                maxHp = config.maxHp;
                maxShield = config.maxShield;
                attackDamage = config.attackDamage;
                attackInterval = config.attackInterval;
                bulletSpeed = config.bulletSpeed;
            }

            CurrentHp = maxHp;
            CurrentShield = maxShield;
            attackTimer = 0f;

            if (healthBar != null)
            {
                healthBar.SetupDual(maxShield, maxHp);
                healthBar.UpdateDual(CurrentShield, CurrentHp);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            if (state != GameManager.GameState.Playing && state != GameManager.GameState.BossPhase)
                return;

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                ShootAtNearestEnemy();
            }
        }

        private void ShootAtNearestEnemy()
        {
            Transform nearest = FindNearestEnemy();
            if (nearest == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 dir = (nearest.position - spawnPos).normalized;

            BulletController bullet = BulletPool.Instance.GetBullet();
            bullet.transform.position = spawnPos;
            bullet.Init(dir, bulletSpeed, attackDamage, true);
        }

        private Transform FindNearestEnemy()
        {
            float minDist = float.MaxValue;
            Transform nearest = null;

            // Check regular enemies
            var enemies = EnemyManager.Instance?.ActiveEnemies;
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null) continue;
                    float dist = Vector2.Distance(transform.position, enemy.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = enemy.transform;
                    }
                }
            }

            // Also check for boss during BossPhase
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameManager.GameState.BossPhase)
            {
                var boss = Object.FindObjectOfType<BossController>();
                if (boss != null)
                {
                    float dist = Vector2.Distance(transform.position, boss.transform.position);
                    if (dist < minDist)
                    {
                        nearest = boss.transform;
                    }
                }
            }

            return nearest;
        }

        public void TakeDamage(int damage)
        {
            if (CurrentShield > 0)
            {
                int shieldAbsorb = Mathf.Min(CurrentShield, damage);
                CurrentShield -= shieldAbsorb;
                damage -= shieldAbsorb;
            }

            if (damage > 0)
            {
                CurrentHp -= damage;
            }

            OnHealthChanged?.Invoke(CurrentShield, maxShield, CurrentHp, maxHp);

            if (healthBar != null)
            {
                healthBar.UpdateDual(CurrentShield, CurrentHp);
            }

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                OnDeath?.Invoke();
                GameManager.Instance?.OnPlayerDefeated();
            }
        }
    }
}
