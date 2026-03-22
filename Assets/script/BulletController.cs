using UnityEngine;
using System.Collections.Generic;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 子弹控制器 - 支持穿刺/追踪/爆炸/链式/状态效果
    /// 通过 Setup 注入基础参数，通过公开字段开关高级功能
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        // ── 基础参数 ──────────────────────────────────────────────────────────────
        private float   speed;
        private int     damage;
        private float   lifetime;
        private float   lifeTimer;
        private Vector2 moveDir;
        private float   hitRadius;

        // ── 穿刺 ──────────────────────────────────────────────────────────────────
        public bool isPierce    = false;
        public int  pierceCount = 0;       // 剩余可穿刺次数
        private HashSet<GameObject> pierceHit = new HashSet<GameObject>();

        // ── 追踪 ──────────────────────────────────────────────────────────────────
        public bool isHoming     = false;
        public float homingTurnSpeed = 360f; // 每秒最大转向角度

        // ── 爆炸 ──────────────────────────────────────────────────────────────────
        public bool  hasExplosionOnHit    = false;
        public float explosionRadius      = 1.5f;
        public float explosionDamagePct   = 100f;  // 相对基础伤害的百分比

        // ── 灼伤 ──────────────────────────────────────────────────────────────────
        public bool  hasBurnOnHit  = false;
        public float burnDps       = 0f;  // 每秒伤害量
        public float burnDuration  = 5f;

        // ── 冰冻 ──────────────────────────────────────────────────────────────────
        public bool  hasFreezeOnHit  = false;
        public float freezeDuration  = 3f;
        public bool  hasSlowAfterFreeze = false;
        public float slowPct            = 50f;
        public float slowDuration       = 3f;

        // ── 闪电链 ────────────────────────────────────────────────────────────────
        public bool  hasChain         = false;
        public int   chainCount       = 1;     // 剩余追击次数
        public float chainDamagePct   = 100f;  // 当前链节伤害（百分比相对基础）
        public float chainFalloff     = 0.1f;  // 每跳衰减
        public float chainMinDamagePct = 0.3f;
        public bool  chainAllowRepeat  = false;
        public bool  chainBonusOnRepeat = false;
        public List<GameObject> chainHit = new List<GameObject>();  // 已命中列表

        // ── 命中回调（由 SkillExecutor 注入，护盾反射等用）────────────────────────
        public System.Action<BulletController, EnemyController> onHitCallback;

        // ── 初始化 ────────────────────────────────────────────────────────────────
        public void Setup(float spd, int dmg, float life, Vector2 targetPos, float scale)
        {
            speed     = spd;
            damage    = dmg;
            lifetime  = life;
            hitRadius = scale * 0.6f;
            lifeTimer = 0f;
            moveDir   = (targetPos - (Vector2)transform.position).normalized;
        }

        /// <summary>直接指定方向（用于多发/散射子弹）</summary>
        public void SetupDirection(float spd, int dmg, float life, Vector2 direction, float scale)
        {
            speed     = spd;
            damage    = dmg;
            lifetime  = life;
            hitRadius = scale * 0.6f;
            lifeTimer = 0f;
            moveDir   = direction.normalized;
        }

        // ── 每帧逻辑 ──────────────────────────────────────────────────────────────
        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime) { Destroy(gameObject); return; }

            // 追踪：每帧朝最近敌人转向
            if (isHoming)
            {
                Transform nearest = FindNearestEnemy();
                if (nearest != null)
                {
                    Vector2 desired = ((Vector2)nearest.position - (Vector2)transform.position).normalized;
                    float   angle   = Vector2.SignedAngle(moveDir, desired);
                    float   maxTurn = homingTurnSpeed * Time.deltaTime;
                    float   turn    = Mathf.Clamp(angle, -maxTurn, maxTurn);
                    moveDir = RotateVector(moveDir, turn);
                }
            }

            // 旋转图像朝向运动方向
            float rot = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rot);

            // 直线移动
            transform.Translate(moveDir * speed * Time.deltaTime, Space.World);

            // 距离检测命中
            CheckHit();
        }

        private void CheckHit()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject e in enemies)
            {
                if (e == null) continue;
                if (isPierce && pierceHit.Contains(e)) continue; // 已穿刺过，跳过
                if (hasChain && chainHit.Contains(e)) continue;  // 链式：主弹已命中

                float dist = Vector2.Distance(transform.position, e.transform.position);
                if (dist > hitRadius) continue;

                EnemyController ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue;

                OnHitEnemy(ec);

                if (isPierce)
                {
                    pierceHit.Add(e);
                    pierceCount--;
                    if (pierceCount <= 0)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
                else if (!hasChain)
                {
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    // 链式：命中后继续追击，不销毁子弹（链式逻辑在 OnHitEnemy 中处理）
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void OnHitEnemy(EnemyController ec)
        {
            // 基础伤害
            int actualDamage = Mathf.RoundToInt(damage * chainDamagePct / 100f);
            ec.TakeDamage(actualDamage);
            Debug.Log($"[Bullet] 命中 {ec.name}，伤害 {actualDamage}");

            // 爆炸
            if (hasExplosionOnHit) DoExplosion();

            // 灼伤
            if (hasBurnOnHit)
                ec.ApplyStatus(new StatusEffect(StatusType.Burn, burnDuration, burnDps));

            // 冰冻
            if (hasFreezeOnHit)
            {
                ec.ApplyStatus(new StatusEffect(StatusType.Freeze, freezeDuration, 0f));
                if (hasSlowAfterFreeze)
                {
                    // 解冻后触发减速：用协程延迟添加减速
                    var helper = new GameObject("SlowHelper").AddComponent<SlowAfterFreezeHelper>();
                    helper.Init(ec, slowPct, slowDuration, freezeDuration);
                }
            }

            // 回调（护盾反射等）
            onHitCallback?.Invoke(this, ec);

            // 闪电链
            if (hasChain && chainCount > 0)
                DoChain(ec);
        }

        // ── 爆炸 ──────────────────────────────────────────────────────────────────
        private void DoExplosion()
        {
            int explosionDmg = Mathf.RoundToInt(damage * explosionDamagePct / 100f);
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d <= explosionRadius)
                {
                    var ec = e.GetComponent<EnemyController>();
                    if (ec != null && !ec.IsDead)
                        ec.TakeDamage(explosionDmg);
                }
            }
            SpawnExplosionVFX();
        }

        private void SpawnExplosionVFX()
        {
            // 简单的爆炸视觉：橙色圆圈扩散
            GameObject vfx = new GameObject("ExplosionVFX");
            vfx.transform.position = transform.position;
            var sr = vfx.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite("sphere", explosionRadius * 2f,
                new Color(1f, 0.5f, 0.1f, 0.7f));
            sr.color        = new Color(1f, 0.5f, 0.1f, 0.7f);
            sr.sortingOrder = 8;
            Object.Destroy(vfx, 0.25f);
        }

        // ── 闪电链 ────────────────────────────────────────────────────────────────
        private void DoChain(EnemyController hitEnemy)
        {
            if (chainCount <= 0) return;

            chainHit.Add(hitEnemy.gameObject);

            // 找下一个目标
            EnemyController nextTarget = FindChainTarget();
            if (nextTarget == null) return;

            // 计算下一跳伤害
            float nextDamagePct = Mathf.Max(chainMinDamagePct * 100f,
                chainDamagePct - chainFalloff * 100f);

            // 生成链式子弹
            GameObject chainBullet = new GameObject("ChainBullet");
            chainBullet.tag = "Bullet";
            chainBullet.transform.position = transform.position;

            // 复制外观
            var origSr = GetComponent<SpriteRenderer>();
            if (origSr != null)
            {
                var sr = chainBullet.AddComponent<SpriteRenderer>();
                sr.sprite       = origSr.sprite;
                sr.color        = new Color(0.8f, 0.9f, 1f); // 淡蓝色
                sr.sortingOrder = origSr.sortingOrder;
            }

            var bc = chainBullet.AddComponent<BulletController>();
            bc.Setup(speed, damage, 3f, (Vector2)nextTarget.transform.position, hitRadius / 0.6f);
            bc.hasChain           = true;
            bc.chainCount         = chainCount - 1;
            bc.chainDamagePct     = nextDamagePct;
            bc.chainFalloff       = chainFalloff;
            bc.chainMinDamagePct  = chainMinDamagePct;
            bc.chainAllowRepeat   = chainAllowRepeat;
            bc.chainBonusOnRepeat = chainBonusOnRepeat;
            bc.chainHit           = new List<GameObject>(chainHit);

            // 10级：重复命中时额外伤害
            if (chainBonusOnRepeat && chainAllowRepeat && chainHit.Contains(nextTarget.gameObject))
            {
                int hitCount = 0;
                foreach (var go in chainHit)
                    if (go == nextTarget.gameObject) hitCount++;
                int bonusDmg = Mathf.RoundToInt(damage * 0.1f * hitCount);
                nextTarget.TakeDamage(bonusDmg);
            }

            // VFX：画一条闪电线
            SpawnLightningVFX(transform.position, nextTarget.transform.position);
        }

        private EnemyController FindChainTarget()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            EnemyController best         = null;
            EnemyController bestRepeat   = null; // 备选：已命中但可重复的目标
            float bestDist       = float.MaxValue;
            float bestRepeatDist = float.MaxValue;

            foreach (var e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue;

                float dist = Vector2.Distance(transform.position, e.transform.position);
                bool  alreadyHit = chainHit.Contains(e);

                if (!alreadyHit)
                {
                    // 优先选未命中目标
                    if (dist < bestDist) { bestDist = dist; best = ec; }
                }
                else if (chainAllowRepeat)
                {
                    // 备选已命中目标（允许重复时）
                    if (dist < bestRepeatDist) { bestRepeatDist = dist; bestRepeat = ec; }
                }
            }

            // 有未命中目标就追击它，否则如果允许重复就备选已命中的
            return best != null ? best : bestRepeat;
        }

        private void SpawnLightningVFX(Vector3 from, Vector3 to)
        {
            // 简单闪电线：LineRenderer
            GameObject lgo = new GameObject("LightningVFX");
            var lr = lgo.AddComponent<LineRenderer>();
            lr.positionCount   = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.08f;
            lr.endWidth   = 0.02f;
            lr.material   = GeometryMeshGenerator.CreateMaterial(new Color(0.6f, 0.8f, 1f));
            lr.sortingOrder = 9;
            Object.Destroy(lgo, 0.15f);
        }

        // ── 工具方法 ──────────────────────────────────────────────────────────────
        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue; // 过滤未初始化或已死目标
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e.transform; }
            }
            return best;
        }

        private static Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }

    /// <summary>
    /// 辅助组件：冰冻解除后添加减速（协程替代）
    /// </summary>
    public class SlowAfterFreezeHelper : MonoBehaviour
    {
        private EnemyController target;
        private float           slowPct;
        private float           slowDuration;
        private float           delay;
        private float           timer;

        public void Init(EnemyController ec, float slow, float dur, float d)
        {
            target = ec; slowPct = slow; slowDuration = dur; delay = d; timer = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= delay)
            {
                if (target != null && !target.IsDead)
                    target.ApplyStatus(new StatusEffect(StatusType.Slow, slowDuration, slowPct / 100f));
                Destroy(gameObject);
            }
        }
    }
}
