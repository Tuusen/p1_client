using System.Collections;
using UnityEngine;

namespace GeometryTD
{
    public class SummonMonsterController : UnitController
    {
        private float duration;
        private bool homing;
        private IBuffTarget summoner;

        public override PassiveSystem PassiveSystem => null;

        public override void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            currentHp -= dmg;
            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public override void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
        }

        public override void AddShield(int value) { }

        public override void TakeDamage(float damage, IBuffTarget attacker = null)
        {
            if (IsDead) return;

            BuffSystem.TryCounterAttack(this, attacker, buffSystem, battleManager);
            if (buffSystem.IsInvincible()) return;

            currentHp -= damage;
            currentHp = Mathf.Max(0, currentHp);

            if (battleManager != null)
                battleManager.ShowDamageText(transform.position, damage, false);

            if (currentHp <= 0)
            {
                Die();
            }
        }



        public void Init(MonsterConfig config, float attrRatio, float dur, bool isHoming, BattleManager bm, IBuffTarget caster = null)
        {
            battleManager = bm;
            duration = dur;
            homing = isHoming;
            isDead = false;
            summoner = caster;

            // 初始化group（召唤物的阵营取决于召唤者）
            UnitGroup summonGroup = UnitGroup.Neutral; // 默认中立
            if (caster is UnitController unitCaster)
            {
                summonGroup = unitCaster.Group;
            }
            InitUnit(summonGroup, UnitType.Summon);

            // 初始化属性组件 + 继承逻辑
            InitAttrs(config.attrs);

            // 对于config中没有的属性，从召唤者继承（乘以attrRatio）
            if (summoner != null && summoner.Attrs != null)
            {
                var allMetas = Cfg.Attribute.All;
                if (allMetas != null)
                {
                    for (int i = 0; i < allMetas.Count; i++)
                    {
                        var meta = allMetas[i];
                        if (!attrs.HasBase(meta.id))
                        {
                            // config中没有这个属性，从召唤者继承
                            int original = summoner.Attrs.GetBase(meta.id);
                            if (meta.type == 1 && summoner.Attrs.HasBase(meta.id))
                            {
                                // type 1: 按 attrRatio 缩放
                                int scaled = (int)((long)original * attrRatio / 10000);
                                if (meta.id == AttributeIds.HP && scaled <= 0) scaled = 1;
                                attrs.SetBase(meta.id, scaled);
                            } else {
                                // type 2: 保持原值
                                attrs.SetBase(meta.id, original);
                            }
                        }
                        // config中有的属性保持原值
                    }
                }
            }

            maxHp = attrs.GetMaxHp();
            if (maxHp <= 0) maxHp = 1;
            currentHp = maxHp;

            attackRange = 50f;

            // 初始化攻击技能
            InitSkills(config.attack_skill_ids);

            InitComponents();
            InitVisual(config.role);  // 初始化视觉表现
            if (this.group == UnitGroup.Player)
            {
                // 进入战斗时朝向右边
                facing?.FaceRight();
            }
        }

        private void Update()
        {
            if (isDead) return;

            // 存活倒计时
            duration -= Time.deltaTime;
            if (duration <= 0f)
            {
                Die();
                return;
            }

            // 调用基类Update
            UnitUpdate();
            if (isDead) return;

            if (buffSystem.IsFrozen())
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            float atkInterval = attrs.GetAttackIntervalSec();
            attackTimer += Time.deltaTime;
            if (attackTimer >= atkInterval)
            {
                attackTimer = 0f;
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (battleManager == null) return;
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;

            int skillIndex = SelectSkillIndex();
            if (skillIndex < 0) return;

            ResetSkillTimer(skillIndex);

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, skillConfig.attack_range);
            if (target == null) return;

            FaceTarget(target.position);

            // Type 3: 合并buff附加的bulletEvent
            int[] allBulletEventIds = MergeBulletEventIds(skillConfig.bulletEvents, buffSystem.CollectExtraBulletEventIds(skillConfig.id));
            var bulletData = BulletEventExecutor.BuildBulletData(allBulletEventIds);
            if (this.homing) bulletData.homing = true;

            float bulletSpeed = skillConfig.bulletSpeed;
            float actualDamage = CalculateDamage(skillConfig);

            if (bulletData.burstCount > 1)
            {
                StartCoroutine(BurstFireRoutine(target, actualDamage, bulletSpeed, bulletData, skillConfig.bulletStyleId, skillConfig.attack_range, skillConfig));
            }
            else
            {
                OnFireBullet(target, actualDamage, bulletSpeed, bulletData, skillConfig.bulletStyleId, skillConfig.attack_range, skillConfig);
            }

            PlayAttackAnimation();
        }





        protected override void Die()
        {
            if (isDead) return;
            isDead = true;
            buffSystem.Clear();
            OnDestroyed();
            Destroy(gameObject);
        }

        protected override void OnDestroyed()
        {
            // Summon 死亡不需要通知 BattleManager
        }

        protected override void OnFireBullet(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill)
        {
            battleManager.SpawnSkillBulletWithScatter(transform.position, target, damage, speed, bulletData, bulletStyleId, attackRange, this, skill);
        }


    }
}
