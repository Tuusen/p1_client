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
        private BulletModifiers modifiers;
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
            this.modifiers = new BulletModifiers();
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
                                     BattleManager bm, BulletModifiers mods, float attackRange)
        {
            this.target = target;
            this.speed = speed;
            this.damage = damage;
            this.isEnemyBullet = false;
            this.battleManager = bm;
            this.modifiers = mods ?? new BulletModifiers();
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
                if (modifiers == null) {
                    modifiers = new BulletModifiers();
                };
                modifiers.pierceCount = 1;
                lastTargetPos = transform.position + pierceDirection * 50f;
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
