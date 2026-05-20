using UnityEngine;

namespace GeometryTD
{
    public class BossController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHp = 2000;
        [SerializeField] private int damage = 30;
        [SerializeField] private float attackInterval = 1.0f;
        [SerializeField] private float bulletSpeed = 8.0f;

        [Header("References")]
        [SerializeField] private Transform firePoint;

        private int currentHp;
        private Transform target;
        private float attackTimer;
        private Vector3 targetPosition;
        private bool reachedPosition;
        private float moveSpeed = 3.0f;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;

        public event System.Action<int, int> OnHealthChanged;
        public event System.Action OnDeath;

        public void Init(BossConfig config, Transform playerTarget)
        {
            maxHp = config.maxHp;
            damage = config.damage;
            attackInterval = config.attackInterval;
            bulletSpeed = config.bulletSpeed;
            currentHp = maxHp;
            target = playerTarget;
            attackTimer = 0f;
            reachedPosition = false;

            // Boss moves to mirrored position on the right side of screen
            float playerX = playerTarget.position.x;
            float mirroredX = -playerX;
            targetPosition = new Vector3(mirroredX, playerTarget.position.y, 0);

            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        private void Update()
        {
            if (target == null) return;
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameManager.GameState.BossPhase)
                return;

            if (!reachedPosition)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPosition, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    reachedPosition = true;
                    transform.position = targetPosition;
                }
                return;
            }

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                ShootAtPlayer();
            }
        }

        private void ShootAtPlayer()
        {
            if (target == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 dir = (target.position - spawnPos).normalized;

            BulletController bullet = BulletPool.Instance.GetBullet();
            bullet.transform.position = spawnPos;
            bullet.Init(dir, bulletSpeed, damage, false);
        }

        public void TakeDamage(int dmg)
        {
            currentHp -= dmg;
            if (currentHp < 0) currentHp = 0;

            OnHealthChanged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            GameManager.Instance?.OnBossDefeated();
            Destroy(gameObject);
        }
    }
}
