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
        private float lifeTime = 5f;

        private float maxAttackRange = 50f;
        private Vector3 startPosition;

        private BattleManager battleManager;
        private BulletEventData bulletData;
        private IBuffTarget caster;
        private SkillConfig skillConfig; // 存储来源技能，用于访问 eventEffect
        private HashSet<Transform> hitTargets = new HashSet<Transform>();

        /// <summary>
        /// 弹射状态：弹射后子弹沿直线飞行，不再追踪目标
        /// </summary>
        private bool isBouncing;

        /// <summary>
        /// 记录每个敌人被命中时子弹的位置，用于追踪穿刺子弹的重复命中冷却
        /// </summary>
        private Dictionary<Transform, Vector3> hitCooldownPositions = new Dictionary<Transform, Vector3>();
        /// <summary>
        /// 子弹离开命中点多远后才能再次命中同一敌人
        /// </summary>
        private const float REHIT_MIN_DISTANCE = 3f;

        private Vector3 flyDirection; // 当前飞行方向（归一化）

        // 追踪弹弧线飞行相关
        private Vector3 currentVelocity; // 当前飞行方向（归一化）
        private float turnSpeed = 6f; // 转向速率（弧度/秒）

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
            this.maxAttackRange = attackRange;
            this.startPosition = transform.position;

            if (target != null)
            {
                lastTargetPos = target.position;
                flyDirection = (target.position - transform.position).normalized;
            }
        }

        // 技能子弹
        public void InitSkillBullet(Transform target, float speed, float damage,
                                     BattleManager bm, BulletEventData data, float attackRange,
                                     IBuffTarget caster = null, SkillConfig skill = null)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = false;
            this.battleManager = bm;
            this.bulletData = data ?? new BulletEventData();
            this.caster = caster;
            this.skillConfig = skill;
            this.maxAttackRange = attackRange;
            this.startPosition = transform.position;

            if (target != null)
            {
                lastTargetPos = target.position;
                flyDirection = (target.position - transform.position).normalized;
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
            flyDirection = direction.normalized;
            target = null;
            lastTargetPos = transform.position + flyDirection * maxAttackRange;
        }

        private void Update()
        {
            // 1. 生命/距离检查
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float traveledDist = Vector3.Distance(transform.position, startPosition);
            if (traveledDist > maxAttackRange)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;

            // 更新命中冷却（追踪穿刺子弹专用）
            UpdateHitCooldowns();

            // 2. 追踪逻辑（仅 homing 子弹，弹射状态下跳过追踪）
            if (!isBouncing && bulletData != null && bulletData.homing)
            {
                // 目标死亡时寻找新目标
                if (target == null && battleManager != null)
                {
                    // 追踪+穿刺时，排除未冷却的敌人
                    HashSet<Transform> exclude = GetCooldownExcludeSet();
                    Transform newTarget = battleManager.GetNearestEnemyExcluding(
                        transform.position, 50f, exclude);
                    if (newTarget != null)
                    {
                        target = newTarget;
                        lastTargetPos = target.position;
                    }
                }

                if (target != null)
                    lastTargetPos = target.position;

                // 平滑转向
                Vector3 toTarget = (lastTargetPos - transform.position).normalized;
                if (currentVelocity.sqrMagnitude < 0.001f)
                    currentVelocity = toTarget;

                float maxRadiansDelta = turnSpeed * Time.deltaTime;
                currentVelocity = Vector3.RotateTowards(currentVelocity, toTarget, maxRadiansDelta, 0f);
                currentVelocity.Normalize();
                flyDirection = currentVelocity;
            }
            // 3. 非追踪子弹：flyDirection 不变，始终直线

            // 4. 移动
            Vector3 moveStart = transform.position;
            Vector3 nextPos = transform.position + flyDirection * step;
            transform.position = nextPos;

            // 5. 统一碰撞检测
            if (battleManager != null)
            {
                float searchRadius = step * 0.5f + 0.5f;
                Vector3 searchCenter = (moveStart + nextPos) * 0.5f;

                // 对敌方子弹（isEnemyBullet），检测英雄碰撞用线段距离判断
                if (isEnemyBullet)
                {
                    // 敌方子弹的碰撞检测：检查是否接近英雄
                    if (target != null)
                    {
                        float segDist = PointToSegmentDistance(target.position, moveStart, nextPos);
                        if (segDist <= 0.5f)
                        {
                            transform.position = target.position;
                            OnHitEnemy(target);
                            return;
                        }
                    }
                }
                else
                {
                    // 己方子弹：检测路径上的敌人
                    // 追踪穿刺子弹需要排除未冷却的敌人
                    HashSet<Transform> exclude = (bulletData != null && bulletData.homing && bulletData.pierceCount > 0)
                        ? GetCooldownExcludeSet()
                        : hitTargets;
                    Transform hit = battleManager.GetNearestEnemyExcluding(
                        searchCenter, searchRadius, exclude);

                    if (hit != null)
                    {
                        float segDist = PointToSegmentDistance(hit.position, moveStart, nextPos);
                        if (segDist <= 0.5f)
                        {
                            OnHitEnemy(hit);
                            return;
                        }
                    }
                }
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

        private void OnHitEnemy(Transform enemy)
        {
            target = enemy;
            ApplyDamage();
            ExecuteHitEvents();

            // 爆炸 AOE
            if (bulletData != null && bulletData.explosionRadius > 0 && battleManager != null)
            {
                float explDmg = damage * bulletData.explosionDmgRate / 10000f;
                battleManager.DealAoeDamage(transform.position, bulletData.explosionRadius, explDmg, caster);

                // 对爆炸波及的目标施加事件效果（排除已直接命中的主目标）
                if (bulletData.attachToTargetEventIds != null && bulletData.attachToTargetEventIds.Count > 0)
                {
                    List<Transform> enemiesInRadius = battleManager.GetEnemiesInRadius(transform.position, bulletData.explosionRadius);
                    for (int i = 0; i < enemiesInRadius.Count; i++)
                    {
                        Transform e = enemiesInRadius[i];
                        if (e == target) continue;
                        IBuffTarget hitTarget = e.GetComponent<MonsterController>() as IBuffTarget;
                        if (hitTarget == null)
                            hitTarget = e.GetComponent<BossController>() as IBuffTarget;
                        if (hitTarget != null)
                        {
                            var ctx = new EventContext
                            {
                                caster = caster,
                                target = hitTarget,
                                battleManager = battleManager,
                                position = e.position
                            };
                            for (int j = 0; j < bulletData.attachToTargetEventIds.Count; j++)
                                EventExecutor.ExecuteEvent(bulletData.attachToTargetEventIds[j], ctx);
                        }
                    }
                }
            }

            // 弹射优先
            if (bulletData != null && bulletData.bounceCount > 0 && battleManager != null)
            {
                bulletData.bounceCount--;
                if (bulletData.bounceDmgMod > 0)
                    damage *= bulletData.bounceDmgMod / 10000f;

                // 进入弹射状态：沿直线飞行，不再追踪
                isBouncing = true;
                flyDirection = GetBounceDirection(transform.position, maxAttackRange, enemy);
                target = null;

                // 从弹射点重新计算攻击距离
                startPosition = transform.position;

                // 仅排除当前敌人，防止连续命中同一敌人，但允许之后再次命中
                hitTargets.Clear();
                hitTargets.Add(enemy);
                return;
            }

            // 穿刺
            if (bulletData != null && bulletData.pierceCount > 0)
            {
                bulletData.pierceCount--;

                // 追踪穿刺子弹使用 hitCooldownPositions 管理（需要冷却才能再次命中）
                // 非追踪穿刺子弹使用 hitTargets 管理（直线飞行不会回头）
                if (bulletData.homing)
                {
                    hitCooldownPositions[enemy] = transform.position;
                }
                else
                {
                    hitTargets.Add(enemy);
                }

                if (bulletData.pierceCount <= 0)
                {
                    Destroy(gameObject);
                    return;
                }

                // 追踪弹：寻找新目标继续追踪
                if (bulletData.homing && battleManager != null)
                {
                    HashSet<Transform> cooldownExclude = GetCooldownExcludeSet();
                    Transform newTarget = battleManager.GetNearestEnemyExcluding(
                        transform.position, 50f, cooldownExclude);
                    target = newTarget;
                    if (target != null)
                        lastTargetPos = target.position;
                }
                else
                {
                    // 非追踪弹：继续直线飞行，flyDirection 不变
                    target = null;
                }
                return;
            }

            // 无弹射无穿刺 → 销毁
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
                        if (result.isMiss)
                        {
                            // 英雄闪避 → 触发被动 1
                            TriggerHeroPassive(hero, 1, caster);
                        }
                        else
                        {
                            hero.TakeDamage(result.finalDamage, caster);
                            if (result.isCrit)
                            {
                                if (battleManager != null)
                                    battleManager.ShowDamageText(target.position, result.finalDamage, true);
                                // 英雄被暴击 → 触发被动 602（仅存活时）
                                if (!hero.IsDead)
                                    TriggerHeroPassive(hero, 602, caster);
                            }
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

                        // 英雄命中 → 触发被动 2
                        IBuffTarget hitTarget = (IBuffTarget)monster ?? (IBuffTarget)boss;
                        TriggerCasterPassive(2, hitTarget);

                        // 英雄暴击 → 触发被动 601
                        if (result.isCrit)
                            TriggerCasterPassive(601, hitTarget);
                    }
                }
                else
                {
                    // 退回预计算伤害（无 DamageContext，无法判断命中/暴击）
                    if (monster != null) { monster.TakeDamage(damage, caster); TriggerCasterPassive(2, monster); return; }
                    if (boss != null) { boss.TakeDamage(damage, caster); TriggerCasterPassive(2, boss); }
                }
            }
        }

        /// <summary>
        /// 触发英雄（被击方）的被动
        /// </summary>
        private void TriggerHeroPassive(HeroController hero, int triggerCode, IBuffTarget attacker)
        {
            if (hero == null || hero.PassiveSystem == null) return;
            var ctx = new EventContext
            {
                caster = hero,
                target = attacker ?? (IBuffTarget)hero,
                battleManager = battleManager,
                position = hero.transform.position
            };
            hero.PassiveSystem.OnTrigger(triggerCode, ctx);
        }

        /// <summary>
        /// 触发施法者（英雄）的被动
        /// </summary>
        private void TriggerCasterPassive(int triggerCode, IBuffTarget hitTarget)
        {
            if (caster == null || caster.PassiveSystem == null) return;
            var ctx = new EventContext
            {
                caster = caster,
                target = hitTarget ?? caster,
                battleManager = battleManager,
                position = caster.Position
            };
            caster.PassiveSystem.OnTrigger(triggerCode, ctx);
        }

        private void ExecuteHitEvents()
        {
            if (isEnemyBullet || target == null || bulletData == null) return;

            // 处理 skill.eventEffect - 一次性特效（在子弹命中时播放一次后移除）
            if (skillConfig != null && skillConfig.eventEffect > 0)
            {
                if (battleManager != null && battleManager.EventEffectManager != null)
                {
                    float effectScale = (bulletData != null && bulletData.explosionRadius > 0)
                        ? bulletData.explosionRadius
                        : 1f;
                    battleManager.EventEffectManager.TriggerOneShotEffect(skillConfig.eventEffect, target.position, effectScale);
                }
            }

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

        /// <summary>
        /// 每帧清理已满足冷却距离的命中记录
        /// </summary>
        private void UpdateHitCooldowns()
        {
            if (hitCooldownPositions.Count == 0) return;

            // 收集已冷却的敌人
            List<Transform> cooledDown = null;
            foreach (var kvp in hitCooldownPositions)
            {
                if (kvp.Key == null || Vector3.Distance(transform.position, kvp.Value) >= REHIT_MIN_DISTANCE)
                {
                    if (cooledDown == null) cooledDown = new List<Transform>();
                    cooledDown.Add(kvp.Key);
                }
            }
            if (cooledDown != null)
            {
                for (int i = 0; i < cooledDown.Count; i++)
                    hitCooldownPositions.Remove(cooledDown[i]);
            }
        }

        /// <summary>
        /// 获取当前需要排除的敌人集合（hitTargets + 未冷却的 hitCooldownPositions）
        /// </summary>
        private HashSet<Transform> GetCooldownExcludeSet()
        {
            HashSet<Transform> exclude = new HashSet<Transform>(hitTargets);
            foreach (var kvp in hitCooldownPositions)
            {
                if (kvp.Key != null)
                    exclude.Add(kvp.Key);
            }
            return exclude;
        }

        /// <summary>
        /// 计算弹射方向：
        /// 1. 有其他敌人时，随机选一个敌人，朝该敌人方向弹射
        /// 2. 仅有一个敌人时，朝当前飞行方向的右侧随机方向弹射
        /// </summary>
        private Vector3 GetBounceDirection(Vector3 from, float maxRange, Transform currentEnemy)
        {
            List<Transform> candidates = battleManager.GetEnemiesInRadius(from, maxRange);

            // 收集除当前命中敌人外的其他敌人
            List<Transform> others = null;
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    Transform e = candidates[i];
                    if (e == null || e == currentEnemy) continue;
                    if (others == null) others = new List<Transform>();
                    others.Add(e);
                }
            }

            if (others != null && others.Count > 0)
            {
                // 随机选一个敌人，朝其方向弹射
                Transform picked = others[Random.Range(0, others.Count)];
                return (picked.position - from).normalized;
            }

            // 仅有一个敌人：朝当前飞行方向的右侧随机偏转 30°~150°
            float angle = Random.Range(30f, 150f);
            return Quaternion.Euler(0, angle, 0) * flyDirection;
        }
    }
}
