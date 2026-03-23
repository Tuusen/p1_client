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

        public void Init(Transform target, float speed, float damage, bool isEnemyBullet)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = isEnemyBullet;
            this.hasTarget = target != null;
            if (hasTarget)
            {
                lastTargetPos = target.position;
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
