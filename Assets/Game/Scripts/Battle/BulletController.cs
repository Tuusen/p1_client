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

        private bool isPiercing;
        private Vector3 pierceDirection;

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
            if (!isPiercing && direction.sqrMagnitude > 0.001f)
                pierceDirection = direction;
            transform.position += direction * speed * Time.deltaTime;

            // 穿刺模式：沿直线飞行，检测路径上的敌人
            if (isPiercing && modifiers != null && modifiers.pierceCount > 0 && battleManager != null)
            {
                Transform nearby = battleManager.GetNearestEnemyExcluding(
                    transform.position, 0.5f, hitTargets);
                if (nearby != null)
                {
                    target = nearby;
                    ApplyDamage();
                    ApplyStatusEffects();
                    hitTargets.Add(nearby);
                    target = null;
                    modifiers.pierceCount--;
                    if (modifiers.pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

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
                    var efx = battleManager.EventEffectManager;
                    if (efx != null) efx.TriggerEffect(SkillEventType.Explosion, transform.position);
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
            // 穿刺：进入直线飞行模式，方向不变
            else if (modifiers != null && modifiers.pierceCount > 0 && battleManager != null && !isEnemyBullet)
            {
                isPiercing = true;
                target = null;
                lastTargetPos = transform.position + pierceDirection * 50f;
                return;
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

            Vector3 pos = target.position;
            var efx = battleManager != null ? battleManager.EventEffectManager : null;

            MonsterController mc = target.GetComponent<MonsterController>();
            if (mc != null)
            {
                if (modifiers.freezeDuration > 0) { mc.ApplyFreeze(modifiers.freezeDuration); efx?.TriggerEffect(SkillEventType.Freeze, pos); }
                if (modifiers.burnDuration > 0)   { mc.ApplyBurn(modifiers.burnDmg, modifiers.burnDuration); efx?.TriggerEffect(SkillEventType.Burn, pos); }
                if (modifiers.slowDuration > 0)    { mc.ApplySlow(modifiers.slowDuration, modifiers.slowRatio); efx?.TriggerEffect(SkillEventType.Slow, pos); }
                return;
            }

            BossController bc = target.GetComponent<BossController>();
            if (bc != null)
            {
                if (modifiers.freezeDuration > 0) { bc.ApplyFreeze(modifiers.freezeDuration); efx?.TriggerEffect(SkillEventType.Freeze, pos); }
                if (modifiers.burnDuration > 0)   { bc.ApplyBurn(modifiers.burnDmg, modifiers.burnDuration); efx?.TriggerEffect(SkillEventType.Burn, pos); }
                if (modifiers.slowDuration > 0)    { bc.ApplySlow(modifiers.slowDuration, modifiers.slowRatio); efx?.TriggerEffect(SkillEventType.Slow, pos); }
            }
        }
    }
}
