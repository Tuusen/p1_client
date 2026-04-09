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

        public void SetDirectionalFlight(Vector3 direction)
        {
            this.target = null;
            this.hasTarget = false;
            this.pierceDirection = direction.normalized;
            this.lastTargetPos = transform.position + this.pierceDirection * maxAttackRange;
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

            // 预计算本帧移动
            float step = speed * Time.deltaTime;
            Vector3 nextPos = transform.position + direction * step;

            // 线段碰撞检测：检查从当前位置到移动后位置的路径是否经过目标
            if (hasTarget && !isPiercing)
            {
                float segDist = PointToSegmentDistance(lastTargetPos, transform.position, nextPos);
                if (segDist <= 0.5f)
                {
                    transform.position = lastTargetPos;
                    OnArrival();
                    return;
                }
            }

            transform.position = nextPos;

            // 穿刺模式：沿直线飞行，用线段检测路径上的敌人
            if (isPiercing && bulletData != null && bulletData.pierceCount > 0 && battleManager != null)
            {
                float searchRadius = step * 0.5f + 0.5f;
                Vector3 searchCenter = (transform.position - direction * step + nextPos) * 0.5f;
                Transform nearby = battleManager.GetNearestEnemyExcluding(
                    searchCenter, searchRadius, hitTargets);
                if (nearby != null)
                {
                    float segDist = PointToSegmentDistance(nearby.position, transform.position - direction * step, nextPos);
                    if (segDist <= 0.5f)
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
            }

            // 只有本来就无目标的子弹才进入穿透模式
            if (!hasTarget && target == null && bulletData != null)
            {
                isPiercing = true;
                if (bulletData.pierceCount == 0)
                {
                    bulletData.pierceCount = 1;
                }
                lastTargetPos = transform.position + pierceDirection * 50f;
            }
        }

        /// <summary>
        /// 计算点 point 到线段 (segStart, segEnd) 的最短距离
        /// </summary>
        private static float PointToSegmentDistance(Vector3 point, Vector3 segStart, Vector3 segEnd)
        {
            Vector3 seg = segEnd - segStart;
            float sqrLen = seg.sqrMagnitude;
            if (sqrLen < 0.0001f) return Vector3.Distance(point, segStart);
            float t = Mathf.Clamp01(Vector3.Dot(point - segStart, seg) / sqrLen);
            Vector3 closest = segStart + t * seg;
            return Vector3.Distance(point, closest);
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
                    battleManager.DealAoeDamage(transform.position, bulletData.explosionRadius, explDmg, caster);
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
                            hero.TakeDamage(result.finalDamage, caster);
                            if (result.isCrit && battleManager != null)
                                battleManager.ShowDamageText(target.position, result.finalDamage, true);
                        }
                    }
                    else
                    {
                        hero.TakeDamage(damage, caster);
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
                        if (monster != null) monster.TakeDamage(result.finalDamage, caster);
                        else if (boss != null) boss.TakeDamage(result.finalDamage, caster);

                        if (result.isCrit && battleManager != null)
                            battleManager.ShowDamageText(target.position, result.finalDamage, true);
                    }
                }
                else
                {
                    // 退回预计算伤害
                    if (monster != null) { monster.TakeDamage(damage, caster); return; }
                    if (boss != null) boss.TakeDamage(damage, caster);
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
