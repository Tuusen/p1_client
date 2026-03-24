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
        private BulletModifiers modifiers;
        private HashSet<Transform> hitTargets = new HashSet<Transform>();

        // 普通子弹 / Boss子弹
        public void Init(Transform target, float speed, float damage, bool isEnemyBullet, BattleManager bm)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = isEnemyBullet;
            this.battleManager = bm;
            this.modifiers = new BulletModifiers();
            this.hasTarget = target != null;

            if (hasTarget)
            {
                lastTargetPos = target.position;
                hitTargets.Add(target);
            }
        }

        // 技能子弹
        public void InitSkillBullet(Transform target, float speed, float damage,
                                     BattleManager bm, BulletModifiers mods)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = false;
            this.battleManager = bm;
            this.modifiers = mods ?? new BulletModifiers();
            this.hasTarget = target != null;

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

            // 追踪：目标死亡后寻找新目标
            if (modifiers != null && modifiers.homing && target == null && battleManager != null)
            {
                Transform newTarget = battleManager.GetNearestEnemyExcluding(
                    transform.position, 50f, hitTargets);
                if (newTarget != null)
                {
                    target = newTarget;
                    lastTargetPos = target.position;
                }
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
                OnArrival();
            }
        }

        private void OnArrival()
        {
            if (target != null)
            {
                ApplyDamage();
                ApplyStatusEffects();

                // 爆炸
                if (modifiers != null && modifiers.explosionRadius > 0f && battleManager != null)
                {
                    battleManager.DealAoeDamage(transform.position, modifiers.explosionRadius, modifiers.explosionDmg);
                }
            }

            // 连锁弹射优先
            if (modifiers != null && modifiers.chainCount > 0 && battleManager != null)
            {
                modifiers.chainCount--;
                damage *= modifiers.chainDecayRatio / 10000f;

                if (modifiers.chainAoeRadius > 0)
                    battleManager.DealAoeDamage(transform.position, modifiers.chainAoeRadius, damage);

                Transform next = battleManager.GetNearestEnemyExcluding(
                    transform.position, modifiers.chainRange, hitTargets);
                if (next != null)
                {
                    target = next;
                    lastTargetPos = target.position;
                    hitTargets.Add(next);
                    return;
                }
            }
            // 穿刺
            else if (modifiers != null && modifiers.pierceCount > 0 && battleManager != null && !isEnemyBullet)
            {
                modifiers.pierceCount--;
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

        private void ApplyStatusEffects()
        {
            if (isEnemyBullet || target == null || modifiers == null) return;

            MonsterController mc = target.GetComponent<MonsterController>();
            if (mc != null)
            {
                if (modifiers.freezeDuration > 0) mc.ApplyFreeze(modifiers.freezeDuration);
                if (modifiers.burnDuration > 0) mc.ApplyBurn(modifiers.burnDmg, modifiers.burnDuration);
                if (modifiers.slowDuration > 0) mc.ApplySlow(modifiers.slowDuration, modifiers.slowRatio);
                return;
            }

            BossController bc = target.GetComponent<BossController>();
            if (bc != null)
            {
                if (modifiers.freezeDuration > 0) bc.ApplyFreeze(modifiers.freezeDuration);
                if (modifiers.burnDuration > 0) bc.ApplyBurn(modifiers.burnDmg, modifiers.burnDuration);
                if (modifiers.slowDuration > 0) bc.ApplySlow(modifiers.slowDuration, modifiers.slowRatio);
            }
        }
    }
}
