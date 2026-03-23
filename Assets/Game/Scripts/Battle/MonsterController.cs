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

            Vector3 direction = (heroTarget.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

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

            currentHp -= dmg;
            currentHp = Mathf.Max(0, currentHp);
            UpdateBar();

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
