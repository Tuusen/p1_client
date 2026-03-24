using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class BulletController : MonoBehaviour
    {
        private Transform target;
        private Vector3 lastTargetPos;
        private float speed;
        private float damage;
        private bool isEnemyBullet;
        private bool hasTarget;
        private float lifeTime = 5f;

        private BattleManager battleManager;
        private int remainingPierceCount;
        private float explosionRadius;
        private float explosionDmg;
        private HashSet<Transform> hitTargets = new HashSet<Transform>();

        public void Init(Transform target, float speed, float damage, bool isEnemyBullet,
                         BattleManager bm = null, int pierceCount = 0,
                         float explosionRadius = 0f, float explosionDmg = 0f)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = isEnemyBullet;
            this.hasTarget = target != null;
            this.battleManager = bm;
            this.remainingPierceCount = pierceCount;
            this.explosionRadius = explosionRadius;
            this.explosionDmg = explosionDmg;

            if (hasTarget)
            {
                lastTargetPos = target.position;
                hitTargets.Add(target);
            }
        }

        private void Update()
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (target != null)
            {
                lastTargetPos = target.position;
            }

            Vector3 direction = (lastTargetPos - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            float dist = Vector3.Distance(transform.position, lastTargetPos);
            if (dist < 0.3f)
            {
                if (target != null)
                {
                    ApplyDamage();

                    if (explosionRadius > 0f && battleManager != null)
                    {
                        battleManager.DealAoeDamage(transform.position, explosionRadius, explosionDmg);
                    }
                }

                if (remainingPierceCount > 0 && battleManager != null && !isEnemyBullet)
                {
                    remainingPierceCount--;
                    Transform next = battleManager.GetNearestEnemyExcluding(
                        transform.position, 20f, hitTargets);
                    if (next != null)
                    {
                        target = next;
                        lastTargetPos = target.position;
                        hitTargets.Add(next);
                        return;
                    }
                }

                Destroy(gameObject);
            }
        }

        private void ApplyDamage()
        {
            if (isEnemyBullet)
            {
                HeroController hero = target.GetComponent<HeroController>();
                if (hero != null)
                {
                    hero.TakeDamage(damage);
                }
            }
            else
            {
                MonsterController monster = target.GetComponent<MonsterController>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                    return;
                }

                BossController boss = target.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                }
            }
        }
    }
}
