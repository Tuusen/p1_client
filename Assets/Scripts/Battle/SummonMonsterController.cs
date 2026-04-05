using UnityEngine;

namespace GeometryTD
{
    public class SummonMonsterController : MonoBehaviour, IBuffTarget
    {
        private AttrComponent attrs;
        private BuffSystem buffSystem = new BuffSystem();

        private int maxHp;
        private float currentHp;
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
        private bool isDead;

        // IBuffTarget 实现
        public AttrComponent Attrs => attrs;
        public BuffSystem BuffSystem => buffSystem;
        public PassiveSystem PassiveSystem => null;
        public bool IsDead => isDead;
        public Vector3 Position => transform.position;

        public void OnBuffDamage(float dmg)
        {
            currentHp -= dmg;
            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public void OnBuffHeal(float heal)
        {
            currentHp = Mathf.Min(currentHp + heal, maxHp);
        }

        public void AddShield(int value) { }

        public int GetHpPercent()
        {
            if (maxHp <= 0) return 0;
            return Mathf.RoundToInt(currentHp / (float)maxHp * 10000);
        }

        public int GetShieldPercent() => 0;

        public void Init(MonsterConfig config, float attrRatio, float dur, bool isHoming, BattleManager bm)
        {
            battleManager = bm;
            duration = dur;
            homing = isHoming;
            isDead = false;

            // 初始化属性组件 + 继承逻辑
            attrs = GetComponent<AttrComponent>();
            if (attrs == null) attrs = gameObject.AddComponent<AttrComponent>();
            attrs.Init(config.attrs);

            // 属性继承：type=1(基础) 按 attrRatio/10000 缩放，type=2(特殊) 完整继承
            var allMetas = ConfigManager.Instance.GetAllAttrMetas();
            if (allMetas != null)
            {
                for (int i = 0; i < allMetas.Count; i++)
                {
                    var meta = allMetas[i];
                    if (meta.type == 1 && attrs.HasBase(meta.id))
                    {
                        int original = attrs.GetBase(meta.id);
                        int scaled = (int)((long)original * attrRatio / 10000);
                        if (meta.id == AttributeIds.HP && scaled <= 0) scaled = 1;
                        attrs.SetBase(meta.id, scaled);
                    }
                    // type=2 保持原值
                }
            }

            maxHp = attrs.GetMaxHp();
            if (maxHp <= 0) maxHp = 1;
            currentHp = maxHp;

            attackRange = 50f;

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

                    if (i == 0 && attackSkillConfigs[i] != null)
                    {
                        attackRange = attackSkillConfigs[i].attack_range > 0 ? attackSkillConfigs[i].attack_range : 50f;
                    }
                }
            }

            animator = GetComponentInChildren<Animator>();
            facing = GetComponent<CharacterFacing>();
            buffSystem.Clear();
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

            // Buff 系统驱动
            buffSystem.Tick(Time.deltaTime, this);
            if (isDead) return;

            if (buffSystem.IsFrozen())
            {
                animator?.SetBool("IsMoving", false);
                return;
            }

            // 技能冷却计时
            if (attackSkillTimers != null)
            {
                for (int i = 0; i < attackSkillTimers.Length; i++)
                    attackSkillTimers[i] += Time.deltaTime;
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

            attackSkillTimers[skillIndex] = 0f;

            var skillConfig = attackSkillConfigs[skillIndex];
            if (skillConfig == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, skillConfig.attack_range);
            if (target == null) return;

            facing?.FaceToward(target.position);

            var bulletData = BulletEventExecutor.BuildBulletData(skillConfig.bulletEvents);
            if (this.homing) bulletData.homing = true;

            float bulletSpeed = skillConfig.bulletSpeed;
            float atk = attrs.GetAttack();
            float actualDamage = atk * skillConfig.dmg / 10000f;
            battleManager.SpawnSkillBullet(transform.position, target, actualDamage, bulletSpeed, bulletData, skillConfig.bulletStyleId, skillConfig.attack_range);

            animator?.SetTrigger("Attack");
        }

        private void ClampToScreen()
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

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            buffSystem.Clear();
            Destroy(gameObject);
        }
    }
}
