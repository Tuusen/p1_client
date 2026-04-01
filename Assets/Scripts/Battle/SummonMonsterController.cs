using UnityEngine;

namespace GeometryTD
{
    public class SummonMonsterController : MonoBehaviour
    {
        private float maxHp;
        private float currentHp;
        private float attackDamage;
        private float attackInterval;
        private float attackRange;
        private float attackTimer;
        private float duration;
        private bool homing;
        private BattleManager battleManager;
        private Animator animator;
        private CharacterFacing facing;
        private int[] attackSkillIds;
        private float[] attackSkillCds;
        private float[] attackSkillTimers;
        private SkillConfig[] attackSkillConfigs;

        // 状态效果
        private bool isFrozen;
        private float freezeTimer;
        private float burnDmg;
        private float burnTimer;
        private float burnTickTimer;
        private float slowTimer;
        private float slowRatio;
        private float vulnerabilityRatio;
        private float vulnerabilityTimer;
        private Vector3 knockbackDir;
        private float knockbackRemaining;
        private const float KnockbackSpeed = 20f;

        public void Init(MonsterConfig config, float attrRatio, float dur, bool isHoming, BattleManager bm)
        {
            battleManager = bm;
            duration = dur;
            homing = isHoming;

            float ratio = attrRatio / 10000f;

            maxHp = ConfigManager.GetAttrValue(config.attrs, AttributeIds.HP) * ratio;
            if (maxHp <= 0f) maxHp = 1f;
            currentHp = maxHp;

            attackDamage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Attack) * ratio;
            if (attackDamage <= 0f)
                attackDamage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Attack) * ratio;

            attackInterval = ConfigManager.GetAttrValue(config.attrs, AttributeIds.AttackInterval, 1f);
            if (attackInterval <= 0) attackInterval = 1f;
            attackRange = 50f; // 默认攻击范围

            // 初始化攻击技能
            if (config.attack_skill_ids != null && config.attack_skill_ids.Length > 0)
            {
                attackSkillIds = new int[config.attack_skill_ids.Length];
                attackSkillCds = new float[config.attack_skill_ids.Length];
                attackSkillTimers = new float[config.attack_skill_ids.Length];
                attackSkillConfigs = new SkillConfig[config.attack_skill_ids.Length];

                for (int i = 0; i < config.attack_skill_ids.Length; i++)
                {
                    attackSkillIds[i] = config.attack_skill_ids[i];
                    attackSkillConfigs[i] = ConfigManager.Instance.GetSkillConfig(attackSkillIds[i]);
                    attackSkillCds[i] = attackSkillConfigs[i] != null ? attackSkillConfigs[i].cd : 1f;
                    attackSkillTimers[i] = 0f;

                    // 使用第一个技能的攻击范围
                    if (i == 0 && attackSkillConfigs[i] != null)
                    {
                        attackRange = attackSkillConfigs[i].attack_range > 0 ? attackSkillConfigs[i].attack_range : 50f;
                    }
                }
            }

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();
        }

        private void Update()
        {
            // 存活倒计时
            duration -= Time.deltaTime;
            if (duration <= 0f)
            {
                Die();
                return;
            }

            // 击退
            if (knockbackRemaining > 0)
            {
                float step = KnockbackSpeed * Time.deltaTime;
                if (step > knockbackRemaining) step = knockbackRemaining;
                transform.position += knockbackDir * step;
                knockbackRemaining -= step;
                ClampToScreen();
                return;
            }

            // 灼烧 DoT
            if (burnTimer > 0)
            {
                burnTickTimer += Time.deltaTime;
                if (burnTickTimer >= 1f)
                {
                    burnTickTimer -= 1f;
                    currentHp -= burnDmg;
                    if (currentHp <= 0f)
                    {
                        Die();
                        return;
                    }
                }
                burnTimer -= Time.deltaTime;
            }

            // 冰冻
            if (isFrozen)
            {
                freezeTimer -= Time.deltaTime;
                if (freezeTimer <= 0) isFrozen = false;
                animator?.SetBool("IsMoving", false);
                return;
            }

            // 减速计时
            if (slowTimer > 0)
                slowTimer -= Time.deltaTime;

            // 易伤计时
            if (vulnerabilityTimer > 0)
                vulnerabilityTimer -= Time.deltaTime;

            // 统一攻击间隔计时器
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (battleManager == null) return;
            if (attackSkillConfigs == null || attackSkillConfigs.Length == 0) return;

            // 从后往前查找第一个未冷却的技能
            int skillIndex = -1;
            for (int i = attackSkillConfigs.Length - 1; i >= 0; i--)
            {
                if (attackSkillTimers[i] >= attackSkillCds[i])
                {
                    skillIndex = i;
                    break;
                }
            }

            if (skillIndex < 0) return;

            // 重置该技能的冷却计时器
            attackSkillTimers[skillIndex] = 0f;

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, skillConfig.attack_range);
            if (target == null) return;

            facing?.FaceToward(target.position);

            var mods = new BulletModifiers { homing = this.homing };
            float bulletSpeed = skillConfig.bulletSpeed;
            float actualDamage = attackDamage * skillConfig.dmg / 10000f;
            Debug.LogWarning($"伤害是：{actualDamage}，范围是：{skillConfig.attack_range}，目标为:{target}");
            battleManager.SpawnSkillBullet(transform.position, target, actualDamage, bulletSpeed, mods, skillConfig.bulletStyleId, skillConfig.attack_range);

            animator?.SetTrigger("Attack");
        }

        // ===== 状态效果 =====

        public void ApplyFreeze(float duration)
        {
            isFrozen = true;
            if (duration > freezeTimer) freezeTimer = duration;
        }

        public void ApplyBurn(float dmgPerTick, float duration)
        {
            burnDmg = dmgPerTick;
            burnTimer = duration;
            burnTickTimer = 0f;
        }

        public void ApplySlow(float duration, float ratio)
        {
            slowRatio = ratio;
            if (duration > slowTimer) slowTimer = duration;
        }

        public void ApplyVulnerability(float duration, float ratio)
        {
            vulnerabilityRatio = ratio;
            if (duration > vulnerabilityTimer) vulnerabilityTimer = duration;
        }

        public void ApplyKnockback(Vector3 sourcePos, float force)
        {
            knockbackDir = Vector3.right;
            knockbackRemaining = force;
        }

        private void ClampToScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            Vector3 cp = cam.transform.position;
            Vector3 pos = transform.position;
            if (pos.x > cp.x + w) { pos.x = cp.x + w; knockbackRemaining = 0; }
            if (pos.x < cp.x - w) { pos.x = cp.x - w; knockbackRemaining = 0; }
            if (pos.y > cp.y + h) { pos.y = cp.y + h; knockbackRemaining = 0; }
            if (pos.y < cp.y - h) { pos.y = cp.y - h; knockbackRemaining = 0; }
            transform.position = pos;
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
