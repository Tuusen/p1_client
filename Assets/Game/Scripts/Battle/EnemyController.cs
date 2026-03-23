using UnityEngine;

namespace GeometryTD
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHp = 50;
        [SerializeField] private int damage = 10;
        [SerializeField] private float moveSpeed = 2.0f;

        [Header("References")]
        [SerializeField] private HealthBarUI healthBar;

        private int currentHp;
        private Transform target;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;

        public void Init(EnemyConfig config, Transform playerTarget)
        {
            maxHp = config.maxHp;
            damage = config.damage;
            moveSpeed = config.moveSpeed;
            currentHp = maxHp;
            target = playerTarget;

            if (healthBar != null)
            {
                healthBar.SetupSingle(maxHp);
                healthBar.UpdateSingle(currentHp);
            }
        }

        private void Update()
        {
            if (target == null) return;
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            if (state != GameManager.GameState.Playing && state != GameManager.GameState.BossPhase)
                return;

            Vector2 dir = (target.position - transform.position).normalized;
            transform.Translate(dir * moveSpeed * Time.deltaTime);
        }

        public void TakeDamage(int dmg)
        {
            currentHp -= dmg;

            if (healthBar != null)
            {
                healthBar.UpdateSingle(currentHp);
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            EnemyManager.Instance?.Unregister(this);
            GameManager.Instance?.AddKill();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                EnemyManager.Instance?.Unregister(this);
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            EnemyManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            EnemyManager.Instance?.Unregister(this);
        }
    }
}
