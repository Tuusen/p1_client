using UnityEngine;

namespace GeometryTD
{
    public class HeroController : MonoBehaviour
    {
        [Header("配置")]
        private float maxHp;
        private float maxShield;
        private float attackRange;
        private float attackInterval;
        private int attackSkillId;

        [Header("运行时状态")]
        private float currentHp;
        private float currentShield;
        private float attackTimer;

        [Header("引用")]
        [SerializeField] private HealthBarUI shieldBar;
        [SerializeField] private HealthBarUI hpBar;

        private BattleManager battleManager;
        private SkillConfig skillConfig;

        public bool IsDead => currentHp <= 0;

        public void Init(HeroConfig config, BattleManager manager)
        {
            battleManager = manager;

            maxHp = config.hp;
            maxShield = config.shield;
            attackRange = config.attack_range;
            attackInterval = config.attack_interval;
            attackSkillId = config.attack_skill_id;

            currentHp = maxHp;
            currentShield = maxShield;
            attackTimer = 0f;

            skillConfig = ConfigManager.Instance.GetSkillConfig(attackSkillId);

            UpdateBars();
        }

        public void SetBars(HealthBarUI shield, HealthBarUI hp)
        {
            shieldBar = shield;
            hpBar = hp;
            UpdateBars();
        }

        private void Update()
        {
            if (IsDead || battleManager == null) return;

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                TryAttack();
                attackTimer = 0f;
            }
        }

        private void TryAttack()
        {
            if (skillConfig == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, attackRange);
            if (target == null) return;

            battleManager.SpawnHeroBullet(transform.position, target, skillConfig.damage, skillConfig.bullet_speed);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            if (currentShield > 0)
            {
                if (damage <= currentShield)
                {
                    currentShield -= damage;
                }
                else
                {
                    float remaining = damage - currentShield;
                    currentShield = 0;
                    currentHp -= remaining;
                }
            }
            else
            {
                currentHp -= damage;
            }

            currentHp = Mathf.Max(0, currentHp);
            UpdateBars();

            if (currentHp <= 0)
            {
                battleManager.OnHeroDead();
            }
        }

        private void UpdateBars()
        {
            if (shieldBar != null)
            {
                shieldBar.SetValue(currentShield, maxShield);
            }
            if (hpBar != null)
            {
                hpBar.SetValue(currentHp, maxHp);
            }
        }
    }
}
