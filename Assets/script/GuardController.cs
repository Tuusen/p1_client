using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 风影守卫控制器
    /// 召唤物持续攻击最近的敌人，技能等级影响持续时间/追踪/三射
    /// </summary>
    public class GuardController : MonoBehaviour
    {
        private SkillConfig      cfg;
        private int              level;
        private PlayerController owner;

        private float duration;     // 剩余存活时间
        private float fireTimer;    // 射击计时

        // ── 初始化 ────────────────────────────────────────────────────────────────
        public void Setup(SkillConfig skillCfg, int skillLevel, PlayerController playerCtrl, float dur)
        {
            cfg      = skillCfg;
            level    = skillLevel;
            owner    = playerCtrl;
            duration = dur;
        }

        // ── 每帧 ──────────────────────────────────────────────────────────────────
        private void Update()
        {
            duration -= Time.deltaTime;
            if (duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // 跟随玩家（始终在玩家后下方）
            if (owner != null)
                transform.position = (Vector2)owner.transform.position + new Vector2(-1.2f, -0.5f);

            // 射击计时
            fireTimer += Time.deltaTime;
            if (fireTimer >= cfg.guardFireRate)
            {
                fireTimer = 0f;
                TryFire();
            }
        }

        // ── 射击逻辑 ──────────────────────────────────────────────────────────────
        private void TryFire()
        {
            // 找最近的敌人
            Transform nearest = FindNearestEnemy();
            if (nearest == null) return;

            Vector2 dir = ((Vector2)nearest.position - (Vector2)transform.position).normalized;
            FireBullet(dir);

            // 10级：±30° 额外两发
            if (cfg.guardTripleShotOnLv10 && level >= 10)
            {
                float angle = cfg.guardTripleAngle;
                FireBullet(RotateVector(dir, angle));
                FireBullet(RotateVector(dir, -angle));
            }
        }

        private void FireBullet(Vector2 direction)
        {
            int dmg = Mathf.RoundToInt(cfg.bulletDamage * cfg.guardDamagePct / 100f);

            Color col = ConfigLoader.ParseColor(cfg.bulletColor);
            col = new Color(0.6f, 1f, 0.8f, 0.9f); // 风影子弹颜色：绿白

            GameObject bullet = new GameObject("GuardBullet");
            bullet.tag = "Bullet";
            bullet.transform.position = transform.position;

            var sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite("sphere", 0.2f, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            var bc = bullet.AddComponent<BulletController>();
            bc.SetupDirection(cfg.bulletSpeed, dmg, 3f, direction, 0.2f);

            // 6级：追踪
            if (cfg.guardHomingOnLv6 && level >= 6)
            {
                bc.isHoming = true;
                bc.homingTurnSpeed = 270f;
            }

            // 10级：穿透2次
            if (cfg.guardTripleShotOnLv10 && level >= 10)
            {
                bc.isPierce    = true;
                bc.pierceCount = cfg.guardPierceCount;
            }
        }

        // ── 工具 ──────────────────────────────────────────────────────────────────
        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
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
}
