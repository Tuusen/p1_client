using System.Collections.Generic;
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
        private float baseAttack;

        [Header("运行时状态")]
        private float currentHp;
        private float currentShield;
        private float attackTimer;

        [Header("引用")]
        [SerializeField] private HealthBarUI shieldBar;
        [SerializeField] private HealthBarUI hpBar;

        private BattleManager battleManager;
        private SkillConfig normalAttackConfig;

        public bool IsDead => currentHp <= 0;
        public float AttackRange => attackRange;

        public void Init(HeroConfig config, BattleManager manager)
        {
            battleManager = manager;

            maxHp = config.hp;
            maxShield = config.shield;
            attackRange = config.attack_range;
            attackInterval = config.attack_interval;
            attackSkillId = config.attack_skill_id;
            baseAttack = config.base_attack;

            currentHp = maxHp;
            currentShield = maxShield;
            attackTimer = 0f;

            normalAttackConfig = ConfigManager.Instance.GetSkillConfig(attackSkillId);

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
            if (normalAttackConfig == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, attackRange);
            if (target == null) return;

            float actualDmg = baseAttack * normalAttackConfig.dmg / 10000f;
            battleManager.SpawnHeroBullet(transform.position, target, actualDmg, normalAttackConfig.bulletSpeed);
        }

        public void UseSkill(SkillConfig config)
        {
            if (IsDead || battleManager == null) return;
            if (config == null || config.atkCnt <= 0 || config.dmg <= 0) return;

            float actualDmg = baseAttack * config.dmg / 10000f;

            int pierceCount = 0;
            float explosionRadius = 0f;
            float explosionDmg = 0f;

            if (config.events != null)
            {
                foreach (var evt in config.events)
                {
                    if (evt.type == 1 && evt.param != null && evt.param.Length >= 1)
                        pierceCount = (int)evt.param[0];
                    else if (evt.type == 2 && evt.param != null && evt.param.Length >= 2)
                    {
                        explosionRadius = evt.param[0];
                        explosionDmg = baseAttack * evt.param[1] / 10000f;
                    }
                }
            }

            List<Transform> targets = battleManager.GetNearestEnemies(
                transform.position, attackRange, config.atkCnt);
            if (targets.Count == 0) return;

            foreach (var target in targets)
            {
                battleManager.SpawnSkillBullet(transform.position, target, actualDmg,
                    config.bulletSpeed, pierceCount, explosionRadius, explosionDmg);
            }
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
