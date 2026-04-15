using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// 单位控制器基类，提供通用的单位行为逻辑
    /// </summary>
    public abstract class UnitController : MonoBehaviour, IBuffTarget
    {
        [Header("引用")]
        [SerializeField] protected HealthBarUI hpBar;

        [Header("组件引用")]
        public AttrComponent attrs;
        public BuffSystem buffSystem = new BuffSystem();
        protected Animator animator;
        protected CharacterFacing facing;
        protected VisualLoader visualLoader;
        public BattleManager battleManager;

        [Header("单位标识")]
        [SerializeField] protected int uid;
        [SerializeField] protected UnitGroup group;
        [SerializeField] protected UnitType unitType;

        [Header("HP状态")]
        public int maxHp;
        public float currentHp;
        public bool isDead;

        [Header("技能攻击配置")]
        protected int[] attackSkillIds;
        protected float[] attackSkillCds;
        protected float[] attackSkillTimers;
        public SkillConfig[] attackSkillConfigs;

        [Header("攻击状态")]
        protected float attackTimer;
        public float attackRange;

        // ===== IBuffTarget 接口属性 =====
        public abstract PassiveSystem PassiveSystem { get; }
        public AttrComponent Attrs => attrs;
        public BuffSystem BuffSystem => buffSystem;
        public bool IsDead => isDead;
        public Vector3 Position => transform.position;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;
        public Transform CachedTransform => base.transform;
        public BattleManager BattleManager => battleManager;
        public int Uid => uid;
        public UnitGroup Group => group;
        public UnitType UnitType => unitType;

        // ===== IBuffTarget 接口方法 =====
        public virtual void OnBuffDamage(float dmg)
        {
            if (buffSystem.IsInvincible()) return;
            TakeDamage(dmg);
        }

        public virtual void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
            UpdateBar();
        }

        public virtual void AddShield(int value) { }

        public virtual int GetHpPercent()
        {
            if (maxHp <= 0) return 0;
            return Mathf.RoundToInt(currentHp / (float)maxHp * 10000);
        }

        public virtual int GetShieldPercent() => 0;

        // ===== 获取单位标识的方法 =====
        public virtual int GetUid() => uid;
        public virtual UnitGroup GetGroup() => group;
        public virtual UnitType GetUnitType() => unitType;

        // ===== 初始化方法 =====
        protected virtual void InitUnit(UnitGroup group, UnitType type)
        {
            this.uid = battleManager.GenerateUid();
            this.group = group;
            this.unitType = type;
        }

        protected virtual void InitAttrs(AttrEntry[] configAttrs)
        {
            attrs = GetComponent<AttrComponent>();
            if (attrs == null) attrs = gameObject.AddComponent<AttrComponent>();
            attrs.Init(configAttrs);
        }

        protected virtual void InitSkills(int[] skillIds)
        {
            if (skillIds != null && skillIds.Length > 0)
            {
                attackSkillIds = new int[skillIds.Length];
                attackSkillCds = new float[skillIds.Length];
                attackSkillTimers = new float[skillIds.Length];
                attackSkillConfigs = new SkillConfig[skillIds.Length];

                for (int i = 0; i < skillIds.Length; i++)
                {
                    attackSkillIds[i] = skillIds[i];
                    attackSkillConfigs[i] = Cfg.Skill.Get(attackSkillIds[i]);
                    attackSkillCds[i] = attackSkillConfigs[i] != null ? attackSkillConfigs[i].cd : 1f;
                    attackSkillTimers[i] = 0f;

                    if (i == 0 && attackSkillConfigs[i] != null)
                    {
                        attackRange = attackSkillConfigs[i].attack_range > 0 ? attackSkillConfigs[i].attack_range : GetDefaultAttackRange();
                    }
                }
            }
            else
            {
                attackRange = GetDefaultAttackRange();
            }
        }

        protected virtual float GetDefaultAttackRange() => 5f;

        protected virtual void InitComponents()
        {
            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();
            visualLoader = GetComponentInChildren<VisualLoader>();
            buffSystem.Clear();
            UpdateBar();
        }

        /// <summary>
        /// 初始化视觉表现（根据 RoleConfig）
        /// </summary>
        protected virtual void InitVisual(int roleId)
        {
            RoleConfig config = Cfg.Role.Get(roleId);
            if (config == null)
            {
                Debug.LogWarning($"[UnitController] 找不到 RoleConfig: roleId={roleId}");
                return;
            }

            if (visualLoader != null)
            {
                visualLoader.LoadVisual(config);
            }
            else
            {
                Debug.LogWarning("[UnitController] 未找到 VisualLoader 组件");
            }
        }

        // ===== Update 循环 =====
        protected virtual void UnitUpdate()
        {
            if (isDead) return;

            // Buff 系统驱动
            buffSystem.Tick(Time.deltaTime, this);
            if (isDead) return;

            // 技能冷却计时
            if (attackSkillTimers != null)
            {
                for (int i = 0; i < attackSkillTimers.Length; i++)
                    attackSkillTimers[i] += Time.deltaTime;
            }
        }

        // ===== 攻击逻辑 =====
        protected virtual int SelectSkillIndex()
        {
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return -1;

            for (int i = attackSkillConfigs.Length - 1; i >= 0; i--)
            {
                if (attackSkillTimers[i] >= attackSkillCds[i])
                {
                    return i;
                }
            }
            return -1;
        }

        protected virtual void ResetSkillTimer(int skillIndex)
        {
            if (skillIndex >= 0 && skillIndex < attackSkillTimers.Length)
            {
                attackSkillTimers[skillIndex] = 0f;
            }
        }

        protected virtual float CalculateDamage(SkillConfig skillConfig)
        {
            float atk = attrs.GetAttack();
            float actualDmg = atk * skillConfig.dmg / 10000f;

            // Type 1: buff技能伤害修饰
            int dmgMod = buffSystem.GetSkillDmgModifier(skillConfig.id);
            if (dmgMod != 0) actualDmg *= (1f + dmgMod / 10000f);

            return actualDmg;
        }

        protected virtual void FaceTarget(Vector3 targetPosition)
        {
            facing?.FaceToward(targetPosition);
        }

        protected virtual void PlayAttackAnimation()
        {
            animator?.SetTrigger("Attack");
        }

        // ===== 子弹事件合并 =====
        protected static int[] MergeBulletEventIds(int[] baseIds, List<int> extraIds)
        {
            if (extraIds == null || extraIds.Count == 0) return baseIds;
            int baseLen = baseIds != null ? baseIds.Length : 0;
            int[] merged = new int[baseLen + extraIds.Count];
            if (baseIds != null)
                System.Array.Copy(baseIds, merged, baseLen);
            for (int i = 0; i < extraIds.Count; i++)
                merged[baseLen + i] = extraIds[i];
            return merged;
        }

        // ===== 受伤逻辑 =====
        public abstract void TakeDamage(float damage, IBuffTarget attacker = null);

        // ===== 死亡逻辑 =====
        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;
            buffSystem.Clear();
            OnDestroyed();
            Destroy(gameObject);
        }

        /// <summary>
        /// 子类重写此方法处理死亡后的特定逻辑
        /// </summary>
        protected abstract void OnDestroyed();

        // ===== 屏幕边界限制 =====
        protected virtual void ClampToScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            Vector3 cp = cam.transform.position;
            Vector3 pos = transform.position;
            if (pos.x > cp.x + w) pos.x = cp.x + w;
            if (pos.x < cp.x - w) pos.x = cp.x - w;
            if (pos.y > cp.y + h) pos.y = cp.y + h;
            if (pos.y < cp.y - h) pos.y = cp.y - h;
            transform.position = pos;
        }

        // ===== UI 更新 =====
        protected virtual void UpdateBar()
        {
            if (hpBar != null)
            {
                hpBar.SetValue(currentHp, maxHp);
            }
        }

        // ===== 被动触发辅助 =====
        protected virtual void TriggerPassive(int triggerCode, IBuffTarget target = null)
        {
            if (PassiveSystem == null) return;
            var ctx = new EventContext
            {
                caster = this,
                target = target ?? (IBuffTarget)this,
                battleManager = battleManager,
                position = transform.position
            };
            PassiveSystem.OnTrigger(triggerCode, ctx);
        }

        // ===== 爆炸连发协程 =====
        protected IEnumerator BurstFireRoutine(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill)
        {
            int burstCount = bulletData.burstCount;
            bulletData.burstCount = 0;

            for (int b = 0; b < burstCount; b++)
            {
                if (isDead || battleManager == null) yield break;
                if (target != null)
                    OnFireBullet(target, damage, speed, bulletData, bulletStyleId, attackRange, skill);
                if (b < burstCount - 1)
                    yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// 子类重写此方法实现具体的子弹发射逻辑
        /// </summary>
        protected abstract void OnFireBullet(Transform target, float damage, float speed, BulletEventData bulletData, int bulletStyleId, float attackRange, SkillConfig skill);
    }
}
