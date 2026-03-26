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

            attackDamage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Damage) * ratio;
            if (attackDamage <= 0f)
                attackDamage = ConfigManager.GetAttrValue(config.attrs, AttributeIds.Attack) * ratio;

            attackInterval = config.attack_interval > 0 ? config.attack_interval : 1f;
            attackRange = config.attack_range > 0 ? config.attack_range : 50f;
            attackTimer = 0f;

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

            // 攻击计时
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                Attack();
            }
        }

        private void Attack()
        {
            if (battleManager == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, attackRange);
            if (target == null) return;

            facing?.FaceToward(target.position);

            var mods = new BulletModifiers { homing = this.homing };
            battleManager.SpawnSkillBullet(transform.position, target, attackDamage, 8f, mods);

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
