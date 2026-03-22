using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 技能执行器 - 静态类，读取 SkillConfig + SkillState.Level 执行具体技能逻辑
    /// 由 SkillManager.TryUseSkill 调用
    /// </summary>
    public static class SkillExecutor
    {
        // ── 主入口 ────────────────────────────────────────────────────────────────
        public static void Execute(SkillConfig cfg, SkillState state,
                                   PlayerController player, Transform target)
        {
            if (player == null) return;
            int level = state.Level;

            switch (cfg.skillType)
            {
                case "bullet":   ExecuteBulletSkill(cfg, level, player, target); break;
                case "aoe":      ExecuteAoe(cfg, level, player);                 break;
                case "heal":     ExecuteHeal(cfg, level, player);                break;
                case "exchange": ExecuteExchange(cfg, level, player);            break;
                case "shield":   ExecuteShield(cfg, level, player);              break;
                case "summon":   ExecuteSummon(cfg, level, player);              break;
                default:
                    // 兼容旧技能（无 skillType）：走 bullet 逻辑
                    ExecuteBulletSkill(cfg, level, player, target);
                    break;
            }
        }

        // ── 技能1/2/3：多发子弹 ──────────────────────────────────────────────────
        private static void ExecuteBulletSkill(SkillConfig cfg, int level,
                                               PlayerController player, Transform target)
        {
            if (target == null)
            {
                target = FindNearestEnemy(player.transform);
                if (target == null) return;
            }

            // 计算发射数量
            int count = cfg.baseBulletCount + (level - 1) * cfg.bulletCountPerLevel;
            count = Mathf.Max(1, count);

            // 基础方向
            Vector2 baseDir = ((Vector2)target.position - (Vector2)player.transform.position).normalized;
            float   baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                // 第一发直射，其余随机偏移
                float spread = (i == 0) ? 0f : Random.Range(-cfg.spreadAngle, cfg.spreadAngle);
                float angle  = baseAngle + spread;
                Vector2 dir  = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                           Mathf.Sin(angle * Mathf.Deg2Rad));

                var bc = SpawnBullet(cfg, player.transform.position, dir);

                // 爆炸（6级解锁）
                if (cfg.hasExplosion && level >= cfg.explosionUnlockLv)
                {
                    bc.hasExplosionOnHit  = true;
                    bc.explosionRadius    = cfg.explosionRadius;
                    bc.explosionDamagePct = cfg.explosionDamagePct;
                }

                // 灼伤（10级解锁）
                if (cfg.hasBurn && level >= cfg.burnUnlockLv)
                {
                    bc.hasBurnOnHit = true;
                    bc.burnDps      = cfg.bulletDamage * cfg.burnDamagePctPerSec / 100f;
                    bc.burnDuration = cfg.burnDuration;
                }

                // 冰冻
                if (cfg.hasFreezeOnHit)
                {
                    bc.hasFreezeOnHit = true;
                    bc.freezeDuration = cfg.freezeDuration;
                    // 10级解锁解冻后减速
                    if (cfg.hasSlowAfterFreeze && level >= 10)
                    {
                        bc.hasSlowAfterFreeze = true;
                        bc.slowPct            = cfg.slowPct;
                        bc.slowDuration       = cfg.slowDuration;
                    }
                }

                // 穿刺（6级解锁）
                if (cfg.hasPierceOnLv6 && level >= 6)
                {
                    bc.isPierce    = true;
                    bc.pierceCount = 99; // 全穿刺
                }

                // 闪电链
                if (cfg.hasChain)
                {
                    int chainMax = cfg.baseChainCount + (level - 1) * cfg.chainCountPerLevel;
                    bc.hasChain          = true;
                    bc.chainCount        = chainMax;
                    bc.chainDamagePct    = 100f;
                    bc.chainFalloff      = cfg.chainDamageFalloff;
                    bc.chainMinDamagePct = cfg.chainMinDamagePct;
                    bc.chainAllowRepeat  = cfg.chainAllowRepeat && level >= 6;
                    bc.chainBonusOnRepeat = cfg.chainBonusOnRepeat && level >= 10;
                    bc.chainHit          = new List<GameObject>();
                }
            }

            // 10级冰冻：发射数翻倍（额外发射，偏移更大）
            if (cfg.hasFreezeOnHit && level >= 10)
            {
                for (int i = 0; i < count; i++)
                {
                    float spread = Random.Range(-10f, 10f);
                    float angle  = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg + spread;
                    Vector2 dir  = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                               Mathf.Sin(angle * Mathf.Deg2Rad));
                    var bc = SpawnBullet(cfg, player.transform.position, dir);
                    bc.hasFreezeOnHit   = true;
                    bc.freezeDuration   = cfg.freezeDuration;
                    bc.hasSlowAfterFreeze = true;
                    bc.slowPct          = cfg.slowPct;
                    bc.slowDuration     = cfg.slowDuration;
                    if (cfg.hasPierceOnLv6) { bc.isPierce = true; bc.pierceCount = 99; }
                }
            }
        }

        // ── 技能4：风暴之力 AOE 击退 ─────────────────────────────────────────────
        private static void ExecuteAoe(SkillConfig cfg, int level, PlayerController player)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Vector2 playerPos    = player.transform.position;

            // 击退距离（每级增加10%）
            float kbDist = cfg.knockbackDist * (1f + (level - 1) * cfg.knockbackPerLevel);
            int   aoeDmg = Mathf.RoundToInt(cfg.bulletDamage * cfg.aoeDamagePct / 100f);

            foreach (var e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue;

                // 伤害
                ec.TakeDamage(aoeDmg);

                // 击退方向：从玩家指向敌人
                Vector2 kbDir = ((Vector2)e.transform.position - playerPos).normalized;
                ec.Knockback(kbDir, kbDist);

                // 6级：减速
                if (cfg.hasSlowOnLv6 && level >= 6)
                    ec.ApplyStatus(new StatusEffect(StatusType.Slow, cfg.aoeSlowDuration,
                        cfg.aoeSlowPct / 100f));

                // 10级：易伤
                if (cfg.hasVulnerableOnLv10 && level >= 10)
                    ec.ApplyStatus(new StatusEffect(StatusType.Vulnerable, cfg.vulnerableDuration,
                        cfg.vulnerablePct / 100f));
            }
        }

        // ── 技能5：治愈之火 ───────────────────────────────────────────────────────
        private static void ExecuteHeal(SkillConfig cfg, int level, PlayerController player)
        {
            float healPct   = cfg.healPct + (level - 1) * cfg.healPerLevel;
            player.HealByPct(healPct / 100f);

            // 6级：持续回复
            if (cfg.hasHoTOnLv6 && level >= 6)
                player.ApplyHoT(cfg.hotPctPerSec / 100f, cfg.hotDuration);

            // 10级：伤害减免
            if (cfg.hasDamageReductionOnLv10 && level >= 10)
                player.ApplyDamageReduction(cfg.damageReductionPct / 100f, cfg.damageReductionDuration);
        }

        // ── 技能6：能量交换 ───────────────────────────────────────────────────────
        private static void ExecuteExchange(SkillConfig cfg, int level, PlayerController player)
        {
            var mgr   = player.SkillMgr;
            int expGain = cfg.baseExpGain + (level - 1) * cfg.expGainPerLevel;

            // 先尝血
            player.SpendHp(cfg.hpCostPct);

            // 收集非满级技能
            List<SkillState> candidates = new List<SkillState>();
            foreach (var s in mgr.Skills)
                if (!s.IsMaxLevel) candidates.Add(s);

            if (candidates.Count == 0) return;

            // 随机选目标
            int targetCount = 1;
            if (level >= 10)  targetCount = candidates.Count; // 10级：全部
            else if (level >= 6) targetCount = Mathf.Min(cfg.extraTargetsOnLv6 + 1, candidates.Count);
            else targetCount = 1;

            // 随机选 targetCount 个（洗牌）
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }

            for (int i = 0; i < targetCount; i++)
            {
                bool lvUp = candidates[i].AddExp(expGain);
                mgr.OnSkillExpChanged?.Invoke(candidates[i], lvUp, expGain);
            }
        }

        // ── 技能7：电磁盾牌 ───────────────────────────────────────────────────────
        private static void ExecuteShield(SkillConfig cfg, int level, PlayerController player)
        {
            float pct   = cfg.shieldPct + (level - 1) * cfg.shieldPerLevel;
            int   shield = Mathf.RoundToInt(player.MaxHp * pct / 100f);
            player.AddShield(shield);

            // 6级：受击反射
            if (cfg.hasReflectOnLv6 && level >= 6)
            {
                int burstCount = level >= 10 ? cfg.reflectBulletCount * 5 : cfg.reflectBulletCount;
                bool isBurst   = level >= 10;
                player.SetShieldReflect(cfg, burstCount, isBurst,
                    cfg.reflectDamagePct, cfg.hasBreakBurstOnLv10 && level >= 10);
            }
        }

        // ── 技能8：风影守卫 ───────────────────────────────────────────────────────
        private static void ExecuteSummon(SkillConfig cfg, int level, PlayerController player)
        {
            float dur = cfg.guardDuration + (level - 1) * cfg.guardDurationPerLevel;

            GameObject guardGo = new GameObject("WindGuard");
            guardGo.transform.position = (Vector2)player.transform.position
                + new Vector2(-1.2f, -0.5f); // 在玩家后下方

            // 视觉：绿色半透明小圆
            var sr = guardGo.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite("sphere", 0.5f,
                new Color(0.5f, 1f, 0.7f, 0.85f));
            sr.color        = new Color(0.5f, 1f, 0.7f, 0.85f);
            sr.sortingOrder = 4;

            var gc = guardGo.AddComponent<GuardController>();
            gc.Setup(cfg, level, player, dur);
        }

        // ── 通用子弹生成 ──────────────────────────────────────────────────────────
        public static BulletController SpawnBullet(SkillConfig cfg, Vector3 origin, Vector2 direction)
        {
            // ── 优先使用预制体（按 bulletPrefabId 字段，或回退代码生成）────────
            var pref = PrefabRef.Instance;
            if (pref != null && !string.IsNullOrEmpty(cfg.bulletPrefabId))
            {
                GameObject prefabGo = pref.GetBulletPrefab(cfg.bulletPrefabId);
                if (prefabGo != null)
                {
                    GameObject bullet = Object.Instantiate(prefabGo, origin, Quaternion.identity);
                    bullet.tag  = "Bullet";
                    bullet.name = "SkillBullet";
                    var bc = bullet.GetComponent<BulletController>();
                    if (bc == null) bc = bullet.AddComponent<BulletController>();
                    bc.SetupDirection(cfg.bulletSpeed, cfg.bulletDamage, cfg.bulletLifetime,
                        direction, cfg.bulletScale);
                    return bc;
                }
            }

            // ── 代码生成回退 ──────────────────────────────────────────────────
            Color col = ConfigLoader.ParseColor(cfg.bulletColor);

            GameObject bulletFb = new GameObject("SkillBullet");
            bulletFb.tag = "Bullet";
            bulletFb.transform.position = origin;

            var sr = bulletFb.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(cfg.bulletShape, cfg.bulletScale, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            if (cfg.bulletHasTrail)
            {
                var tr = bulletFb.AddComponent<TrailRenderer>();
                tr.time        = 0.12f;
                tr.startWidth  = cfg.bulletScale * 0.3f;
                tr.endWidth    = 0f;
                tr.material    = GeometryMeshGenerator.CreateMaterial(
                    ConfigLoader.ParseColor(cfg.bulletTrailColor));
                tr.sortingOrder = 4;
            }

            var bcFb = bulletFb.AddComponent<BulletController>();
            bcFb.SetupDirection(cfg.bulletSpeed, cfg.bulletDamage, cfg.bulletLifetime,
                direction, cfg.bulletScale);
            return bcFb;
        }

        // ── 工具方法 ──────────────────────────────────────────────────────────────
        private static Transform FindNearestEnemy(Transform from)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = Vector2.Distance(from.position, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e.transform; }
            }
            return best;
        }
    }
}
