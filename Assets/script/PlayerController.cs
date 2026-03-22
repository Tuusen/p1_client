using UnityEngine;
using UnityEngine.UI;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 玩家控制器 - 2D 版本
    /// 固定在最左侧，自动向最近敌人发射普通子弹；
    /// 集成 SkillManager，每次攻击随机给技能加 1~10 经验。
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ── 运行时状态 ──────────────────────────────────────────────────────────
        private int   maxHp, maxSp, hp, sp;
        private float attackRange, attackInterval, attackTimer;
        private string bulletId;

        private Transform           target;
        private HealthBarController hpBar;
        public  SkillManager        SkillMgr { get; private set; }

        public bool IsDead => hp <= 0;
        public int  MaxHp  => maxHp;

        // 护盾反射配置（技能7）
        private bool       shieldReflectActive = false;
        private SkillConfig shieldReflectCfg;
        private int         shieldReflectBulletCount;
        private bool        shieldReflectIsBurst;
        private float       shieldReflectDamagePct;
        private bool        shieldBreakBurst;
        private int         currentShieldForReflect; // 当前护盾値，用于检测护盾破损

        // 玩家状态效果
        private float hotTimer       = 0f;
        private float hotPctPerSec   = 0f;
        private float hotRemaining   = 0f;
        private float dmgReductionPct = 0f;
        private float dmgReductionRemaining = 0f;

        // ── 初始化 ──────────────────────────────────────────────────────────────
        private void Start()
        {
            ConfigLoader.LoadAllConfigs();
            PlayerConfig cfg = ConfigLoader.GameConfig.player;

            maxHp = cfg.health;   hp = maxHp;
            maxSp = cfg.shield;   sp = maxSp;
            attackRange    = cfg.attackRange;
            attackInterval = cfg.attackInterval;
            bulletId       = cfg.defaultBulletId;

            // 血条
            hpBar = GetComponentInChildren<HealthBarController>();
            hpBar?.Initialize(maxHp, maxSp, true);

            // 能量管理器
            gameObject.AddComponent<EnergyManager>();

            // 技能管理器
            SkillMgr = gameObject.AddComponent<SkillManager>();
            SkillMgr.Initialize();

            // 技能子弹委托
            SkillMgr.OnFireSkillBullet += FireSkillBullet;

            // 通知 UI
            var skillBarUI = Object.FindObjectOfType<SkillBarUI>();
            skillBarUI?.Build(SkillMgr);

            // 注册
            GameManager.Instance?.RegisterPlayer(this);

            Debug.Log($"[Player] 初始化完成 HP={maxHp} SP={maxSp} Range={attackRange} Interval={attackInterval}");
        }

        // ── 每帧逻辑 ──────────────────────────────────────────────────────────────
        private void Update()
        {
            if (IsDead) return;

            // 持续治愈 HoT
            if (hotRemaining > 0f)
            {
                hotRemaining -= Time.deltaTime;
                hotTimer     += Time.deltaTime;
                if (hotTimer >= 1f)
                {
                    hotTimer -= 1f;
                    int lostHp = maxHp - hp;
                    if (lostHp > 0)
                    {
                        int healAmt = Mathf.RoundToInt(lostHp * hotPctPerSec);
                        hp = Mathf.Min(maxHp, hp + healAmt);
                        hpBar?.UpdateHealth(hp, sp);
                    }
                }
            }

            // 伤害减免倒计时
            if (dmgReductionRemaining > 0f)
                dmgReductionRemaining -= Time.deltaTime;

            attackTimer += Time.deltaTime;
            FindClosestEnemy();

            if (target != null && attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                FireBullet();
                SkillMgr?.GainRandomExp(); // 每次普通攻击后随机给技能加经验
            }
        }

        // ── 查找最近敌人 ──────────────────────────────────────────────────────────
        private void FindClosestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float best = attackRange;
            target = null;

            foreach (GameObject e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue; // 过滤未初始化或已死目标
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < best) { best = d; target = e.transform; }
            }
        }

        // ── 发射普通子弹 ──────────────────────────────────────────────────────────
        private void FireBullet()
        {
            if (target == null) return;

            BulletConfig bc  = ConfigLoader.GetBulletConfig(bulletId);
            Color        col = GeometryMeshGenerator.ParseColor(bc.color);

            SpawnBullet(bc.shape, bc.color, bc.speed, bc.damage,
                        bc.scale, bc.lifetime, bc.hasTrail, bc.trailColor,
                        (Vector2)target.position);

            Debug.Log($"[Player] 普通攻击→{target.name}  伤害={bc.damage}");
        }

        // ── 发射技能子弹（由 SkillManager 委托调用）──────────────────────────────
        private void FireSkillBullet(SkillConfig skill, Transform tgt)
        {
            if (tgt == null) return;
            SpawnBullet(skill.bulletShape, skill.bulletColor,
                        skill.bulletSpeed, skill.bulletDamage,
                        skill.bulletScale, skill.bulletLifetime,
                        skill.bulletHasTrail, skill.bulletTrailColor,
                        (Vector2)tgt.position);

            Debug.Log($"[Player] 技能[{skill.name}]→{tgt.name}  伤害={skill.bulletDamage}");
        }

        // ── 通用子弹生成 ──────────────────────────────────────────────────────────
        private void SpawnBullet(string shape, string colorHex,
            float speed, int damage, float scale, float lifetime,
            bool hasTrail, string trailColorHex,
            Vector2 targetPos)
        {
            Color col = GeometryMeshGenerator.ParseColor(colorHex);

            GameObject bullet = new GameObject("Bullet");
            bullet.tag = "Bullet";
            bullet.transform.position = transform.position;

            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(shape, scale, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            if (hasTrail)
            {
                TrailRenderer tr = bullet.AddComponent<TrailRenderer>();
                tr.time       = 0.12f;
                tr.startWidth = scale * 0.3f;
                tr.endWidth   = 0f;
                tr.material   = GeometryMeshGenerator.CreateMaterial(
                    GeometryMeshGenerator.ParseColor(trailColorHex));
                tr.sortingOrder = 4;
            }

            BulletController ctrl = bullet.AddComponent<BulletController>();
            ctrl.Setup(speed, damage, lifetime, targetPos, scale);
        }

        // ── 受到伤害 ──────────────────────────────────────────────────────────────
        public void TakeDamage(int dmg)
        {
            if (IsDead) return;

            // 伤害减免
            if (dmgReductionRemaining > 0f)
                dmg = Mathf.RoundToInt(dmg * (1f - dmgReductionPct));

            if (sp > 0)
            {
                int abs = Mathf.Min(sp, dmg);

                // 护盾受击时触发反射（6级后）
                if (shieldReflectActive && !shieldBreakBurst)
                    DoShieldReflect(Vector2.right);

                sp  -= abs;
                dmg -= abs;

                // 护盾破损时（10级）
                if (sp <= 0 && shieldBreakBurst)
                    DoShieldBreakBurst();
            }
            if (dmg > 0) hp = Mathf.Max(0, hp - dmg);

            hpBar?.UpdateHealth(hp, sp);
            Debug.Log($"[Player] 受伤 HP={hp}/{maxHp}  SP={sp}/{maxSp}");

            if (hp <= 0)
            {
                Debug.Log("[Player] 玩家死亡！");
                GameManager.Instance?.GameOver();
            }
        }

        public void HealByPct(float pct)
        {
            if (IsDead) return;
            int lostHp = maxHp - hp;
            int healAmt = Mathf.RoundToInt(lostHp * pct);
            if (healAmt <= 0) return;
            hp = Mathf.Min(maxHp, hp + healAmt);
            hpBar?.UpdateHealth(hp, sp);
        }

        public void ApplyHoT(float pctPerSec, float duration)
        {
            hotPctPerSec = pctPerSec;
            hotRemaining = Mathf.Max(hotRemaining, duration);
            hotTimer = 0f;
        }

        public void ApplyDamageReduction(float reductionPct, float duration)
        {
            dmgReductionPct = reductionPct;
            dmgReductionRemaining = Mathf.Max(dmgReductionRemaining, duration);
        }

        public void AddShield(int amount)
        {
            sp += amount;
            if (sp > maxSp) maxSp = sp;
            hpBar?.Initialize(maxHp, maxSp, true);
            hpBar?.UpdateHealth(hp, sp);
        }

        public bool CanAffordHpCost(float hpCostPct)
        {
            float hpRatio = maxHp > 0 ? (float)hp / maxHp * 100f : 0f;
            return hpRatio > hpCostPct;
        }

        public void SpendHp(float pct)
        {
            int cost = Mathf.RoundToInt(maxHp * pct / 100f);
            hp = Mathf.Max(1, hp - cost);
            hpBar?.UpdateHealth(hp, sp);
        }

        public void SetShieldReflect(SkillConfig cfg, int bulletCount, bool isBurst,
                                     float damagePct, bool breakBurst)
        {
            shieldReflectCfg         = cfg;
            shieldReflectBulletCount = bulletCount;
            shieldReflectIsBurst     = isBurst;
            shieldReflectDamagePct   = damagePct;
            shieldBreakBurst         = breakBurst;
            shieldReflectActive      = true;
        }

        private void DoShieldReflect(Vector2 fromDir)
        {
            if (shieldReflectCfg == null) return;
            int dmg = Mathf.RoundToInt(shieldReflectCfg.bulletDamage * shieldReflectDamagePct / 100f);
            for (int i = 0; i < shieldReflectBulletCount; i++)
            {
                float spread = i == 0 ? 0f : Random.Range(-5f, 5f);
                float angle  = Mathf.Atan2(fromDir.y, fromDir.x) * Mathf.Rad2Deg + spread;
                Vector2 dir  = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                           Mathf.Sin(angle * Mathf.Deg2Rad));
                var bc = SkillExecutor.SpawnBullet(shieldReflectCfg, transform.position, dir);
                bc.isPierce = true;
                bc.pierceCount = 99;
            }
        }

        private void DoShieldBreakBurst()
        {
            if (shieldReflectCfg == null) return;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                          Mathf.Sin(angle * Mathf.Deg2Rad));
                var bc = SkillExecutor.SpawnBullet(shieldReflectCfg, transform.position, dir);
                bc.isPierce = true; bc.pierceCount = 99;
            }
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue;
                Vector2 kbDir = ((Vector2)e.transform.position - (Vector2)transform.position).normalized;
                ec.Knockback(kbDir, 4f);
            }
            shieldBreakBurst = false; // 只触发一次
        }

        // ── 重置 ──────────────────────────────────────────────────────────────────
        public void Reset()
        {
            PlayerConfig cfg = ConfigLoader.GameConfig.player;
            maxHp = cfg.health; hp = maxHp;
            maxSp = cfg.shield; sp = maxSp;
            attackTimer = 0f; target = null;
            hpBar?.Initialize(maxHp, maxSp, true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
