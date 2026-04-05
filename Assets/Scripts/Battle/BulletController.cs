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

        private float maxAttackRange = 50f;
        private Vector3 startPosition;

        private BattleManager battleManager;
        private BulletEventData bulletData;
        private IBuffTarget caster;
        private HashSet<Transform> hitTargets = new HashSet<Transform>();

        private bool isPiercing;
        private Vector3 pierceDirection;

        // DamageCalculator 上下文（可选）
        private AttrComponent attackerAttrs;
        private int skillDmgRatio;
        private int skillDmgType;

        // 普通子弹 / Boss子弹
        public void Init(Transform target, float speed, float damage, bool isEnemyBullet, BattleManager bm, float attackRange = 50f)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = isEnemyBullet;
            this.battleManager = bm;
            this.bulletData = new BulletEventData();
            this.hasTarget = target != null;
            this.maxAttackRange = attackRange;
            this.startPosition = transform.position;

            if (hasTarget)
            {
                lastTargetPos = target.position;
                hitTargets.Add(target);
            }
        }

        // 技能子弹
        public void InitSkillBullet(Transform target, float speed, float damage,
                                     BattleManager bm, BulletEventData data, float attackRange,
                                     IBuffTarget caster = null)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = false;
            this.battleManager = bm;
            this.bulletData = data ?? new BulletEventData();
            this.caster = caster;
            this.hasTarget = target != null;
            this.maxAttackRange = attackRange;
            this.startPosition = transform.position;

            if (hasTarget)
            {
                lastTargetPos = target.position;
                hitTargets.Add(target);
            }
        }

        /// <summary>
        /// 设置 DamageCalculator 上下文，使子弹在命中时使用完整伤害公式
        /// （含命中/暴击/元素加减/Boss加成）。不调用则退回预计算 damage。
        /// </summary>
        public void SetDamageContext(AttrComponent attacker, int dmgRatio, int dmgType)
        {
            attackerAttrs = attacker;
            skillDmgRatio = dmgRatio;
            skillDmgType = dmgType;
        }

        private void Update()
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 检查是否超出攻击距离
            float traveledDist = Vector3.Distance(transform.position, startPosition);
            if (traveledDist > maxAttackRange)
            {
                Destroy(gameObject);
                return;
            }

            // 追踪：目标死亡后寻找新目标
            if (bulletData != null && bulletData.homing && target == null && battleManager != null)
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
            if (isPiercing && bulletData != null && bulletData.pierceCount > 0 && battleManager != null)
            {
                Transform nearby = battleManager.GetNearestEnemyExcluding(
                    transform.position, 0.5f, hitTargets);
                if (nearby != null)
                {
                    target = nearby;
                    ApplyDamage();
                    ExecuteHitEvents();
                    hitTargets.Add(nearby);
                    target = null;
                    bulletData.pierceCount--;
                    if (bulletData.pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // 只有在目标仍然存在时才检测到达
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, lastTargetPos);
                if (dist < 0.3f)
                {
                    OnArrival();
                }
            } else {
                // 超出攻击范围，继续飞行（穿透+1）
                isPiercing = true;
                if (bulletData == null) {
                    bulletData = new BulletEventData();
                };
                bulletData.pierceCount = 1;
                lastTargetPos = transform.position + pierceDirection * 50f;
            }
        }

        private void OnArrival()
        {
            if (target != null)
            {
                ApplyDamage();
                ExecuteHitEvents();

                // 爆炸
                if (bulletData != null && bulletData.explosionRadius > 0 && battleManager != null)
                {
                    float explDmg = damage * bulletData.explosionDmgRate / 10000f;
                    battleManager.DealAoeDamage(transform.position, bulletData.explosionRadius, explDmg);
                }
            }

            // 弹射优先
            if (bulletData != null && bulletData.bounceCount > 0 && battleManager != null)
            {
                bulletData.bounceCount--;
                if (bulletData.bounceDmgMod > 0)
                    damage *= bulletData.bounceDmgMod / 10000f;

                float bounceRange = bulletData.bounceRadius > 0 ? bulletData.bounceRadius : 10f;
                Transform next = battleManager.GetNearestEnemyExcluding(
                    transform.position, bounceRange, hitTargets);
                if (next != null)
                {
                    target = next;
                    lastTargetPos = target.position;
                    hitTargets.Add(next);
                    return;
                }
            }
            // 穿刺：进入直线飞行模式，方向不变
            else if (bulletData != null && bulletData.pierceCount > 0 && battleManager != null && !isEnemyBullet)
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
                    if (attackerAttrs != null && hero.Attrs != null)
                    {
                        var ctx = new DamageContext
                        {
                            attackerAttrs = attackerAttrs,
                            defenderAttrs = hero.Attrs,
                            skillDmgRatio = skillDmgRatio,
                            skillDmgType = skillDmgType,
                            isTargetBoss = false,
                            isTargetElite = false
                        };
                        var result = DamageCalculator.Calculate(ctx);
                        if (!result.isMiss)
                        {
                            hero.TakeDamage(result.finalDamage);
                            if (result.isCrit && battleManager != null)
                                battleManager.ShowDamageText(target.position, result.finalDamage, true);
                        }
                    }
                    else
                    {
                        hero.TakeDamage(damage);
                    }
                }
            }
            else
            {
                AttrComponent defenderAttrs = null;
                bool isBoss = false;
                bool isElite = false;

                MonsterController monster = target.GetComponent<MonsterController>();
                if (monster != null)
                {
                    defenderAttrs = monster.Attrs;
                    isElite = monster.IsElite;
                }

                BossController boss = null;
                if (monster == null)
                {
                    boss = target.GetComponent<BossController>();
                    if (boss != null)
                    {
                        defenderAttrs = boss.Attrs;
                        isBoss = true;
                    }
                }

                if (attackerAttrs != null && defenderAttrs != null)
                {
                    var ctx = new DamageContext
                    {
                        attackerAttrs = attackerAttrs,
                        defenderAttrs = defenderAttrs,
                        skillDmgRatio = skillDmgRatio,
                        skillDmgType = skillDmgType,
                        isTargetBoss = isBoss,
                        isTargetElite = isElite
                    };
                    var result = DamageCalculator.Calculate(ctx);
                    if (!result.isMiss)
                    {
                        if (monster != null) monster.TakeDamage(result.finalDamage);
                        else if (boss != null) boss.TakeDamage(result.finalDamage);

                        if (result.isCrit && battleManager != null)
                            battleManager.ShowDamageText(target.position, result.finalDamage, true);
                    }
                }
                else
                {
                    // 退回预计算伤害
                    if (monster != null) { monster.TakeDamage(damage); return; }
                    if (boss != null) boss.TakeDamage(damage);
                }
            }
        }

        private void ExecuteHitEvents()
        {
            if (isEnemyBullet || target == null || bulletData == null) return;

            // 对目标附加事件（冰冻、灼烧、减速等通过 buff 事件实现）
            if (bulletData.attachToTargetEventIds != null && bulletData.attachToTargetEventIds.Count > 0)
            {
                IBuffTarget hitTarget = target.GetComponent<MonsterController>() as IBuffTarget;
                if (hitTarget == null)
                    hitTarget = target.GetComponent<BossController>() as IBuffTarget;

                if (hitTarget != null)
                {
                    var ctx = new EventContext
                    {
                        caster = caster,
                        target = hitTarget,
                        battleManager = battleManager,
                        position = target.position
                    };
                    for (int i = 0; i < bulletData.attachToTargetEventIds.Count; i++)
                        EventExecutor.ExecuteEvent(bulletData.attachToTargetEventIds[i], ctx);
                }
            }

            // 对施法者附加事件
            if (bulletData.attachToCasterEventIds != null && bulletData.attachToCasterEventIds.Count > 0 && caster != null)
            {
                var ctx = new EventContext
                {
                    caster = caster,
                    target = caster,
                    battleManager = battleManager,
                    position = caster.Position
                };
                for (int i = 0; i < bulletData.attachToCasterEventIds.Count; i++)
                    EventExecutor.ExecuteEvent(bulletData.attachToCasterEventIds[i], ctx);
            }
        }
    }
}
