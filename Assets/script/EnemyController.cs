using UnityEngine;
using UnityEngine.UI;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 怪物控制器 - 2D 版本
    /// 向玩家直线移动，距离够近时造成伤害并消失
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        // ── 运行时状态 ──────────────────────────────────────────────────────────
        private int   maxHp, maxSp, hp, sp;
        private int   damage, score;
        private float moveSpeed;
        private string enemyId;

        private Transform              playerTrans;
        private HealthBarController    hpBar;
        private StatusEffectController statusCtrl;   // 状态效果控制器

        public bool   IsDead  => hp <= 0;
        public string EnemyId => enemyId;
        public int    MaxHp   => maxHp;
        public int    BaseAttack => damage;

        // 状态效果转发属性
        public bool  IsFrozen         => statusCtrl != null && statusCtrl.IsFrozen;
        public float SpeedMultiplier  => statusCtrl != null ? statusCtrl.SpeedMultiplier  : 1f;
        public float DamageMultiplier => statusCtrl != null ? statusCtrl.DamageMultiplier : 1f;

        // 对外事件：护盾技能用于反射
        public System.Action<Vector2> OnTakeDamageDirection;

        // ── 由 EnemySpawner 调用 ──────────────────────────────────────────────────
        public void Initialize(string id)
        {
            enemyId = id;
            EnemyConfig cfg = ConfigLoader.GetEnemyConfig(id);

            maxHp = cfg.health;    hp = maxHp;
            maxSp = cfg.shield;    sp = maxSp;
            damage    = cfg.damage;
            score     = cfg.score;
            moveSpeed = cfg.moveSpeed;

            // 找玩家
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTrans = p.transform;

            // 视觉：若预制体已有 SpriteRenderer 则直接复用，否则代码生成
            visual = GetComponent<SpriteRenderer>();
            if (visual == null)
                BuildVisual(cfg);
            else
            {
                // 预制体模式：用配置覆盖颜色和形状（让编辑预制体的形状/颜色生效以配置为准）
                Color col = GeometryMeshGenerator.ParseColor(cfg.color);
                visual.sprite = GeometryMeshGenerator.CreateSprite(cfg.shape, cfg.scale, col);
                visual.color  = col;
                visual.enabled = false; // 下一帧再显示
            }

            // 头顶血条：若预制体已有 HealthBarController 则复用，否则代码生成
            hpBar = GetComponentInChildren<HealthBarController>();
            if (hpBar == null)
                BuildHealthBar(cfg);
            else
                hpBar.Initialize(maxHp, maxSp, false);

            // 状态效果控制器
            statusCtrl = gameObject.GetComponent<StatusEffectController>();
            if (statusCtrl == null) statusCtrl = gameObject.AddComponent<StatusEffectController>();
            statusCtrl.Init(this, transform);
        }

        // ── 2D 视觉：SpriteRenderer ──────────────────────────────────────────────
        private SpriteRenderer visual;  // 延迟显示用

        private void BuildVisual(EnemyConfig cfg)
        {
            Color col = GeometryMeshGenerator.ParseColor(cfg.color);
            visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite       = GeometryMeshGenerator.CreateSprite(cfg.shape, cfg.scale, col);
            visual.color        = col;
            visual.sortingOrder = 3;
            visual.enabled      = false;  // 初始隐藏，下一帧再显示
        }

        // ── 头顶血条（World Space Canvas）────────────────────────────────────────
        private void BuildHealthBar(EnemyConfig cfg)
        {
            bool  hasShield = cfg.shield > 0;
            float barH   = 10f;
            float gap    = 2f;
            float totalH = hasShield ? barH * 2 + gap : barH;
            float barOffsetY = cfg.scale * 0.9f + 0.3f;

            GameObject hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(transform);
            hbObj.transform.localPosition = new Vector3(0, barOffsetY, 0);

            Canvas canvas = hbObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            hbObj.AddComponent<CanvasScaler>();

            RectTransform cr = hbObj.GetComponent<RectTransform>();
            cr.sizeDelta  = new Vector2(120f, totalH);
            cr.localScale = Vector3.one * 0.01f;

            // 血条行
            float hpY = hasShield ? (barH + gap) * 0.5f : 0f;
            CreateBarRow("HealthRow", hbObj.transform, barH, hpY,
                new Color(0.15f, 0.08f, 0.08f, 0.9f), "HealthFill", new Color(0.9f, 0.2f, 0.15f));

            // 盾条行
            if (hasShield)
                CreateBarRow("ShieldRow", hbObj.transform, barH, -(barH + gap) * 0.5f,
                    new Color(0.08f, 0.12f, 0.2f, 0.9f), "ShieldFill", new Color(0.3f, 0.55f, 1f));

            hpBar = hbObj.AddComponent<HealthBarController>();
            hpBar.Initialize(maxHp, maxSp, false);
        }

        private static void CreateBarRow(string rowName, Transform parent, float barH, float posY,
            Color bgColor, string fillName, Color fillColor)
        {
            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent);
            row.transform.localScale = Vector3.one;
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 0.5f);
            rowRt.anchorMax = new Vector2(1, 0.5f);
            rowRt.pivot     = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(0, barH);
            rowRt.anchoredPosition = new Vector2(0, posY);
            row.AddComponent<Image>().color = bgColor;

            GameObject fill = new GameObject(fillName);
            fill.transform.SetParent(row.transform);
            fill.transform.localScale = Vector3.one;
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Image img = fill.AddComponent<Image>();
            img.color      = fillColor;
            img.type       = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
        }

        private static void CreateFilledBarRect(string name, Transform parent,
            Color color, float fillAmt = 1f, bool stretchFull = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localScale = Vector3.one;
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            if (!stretchFull)
            {
                img.type       = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.fillAmount = fillAmt;
            }
        }

        // ── 每帧移动 + 距离检测 ───────────────────────────────────────────────────
        // ── 对外接口：应用状态效果
        public void ApplyStatus(StatusEffect effect)
        {
            statusCtrl?.ApplyStatus(effect);
        }

        // ── 对外接口：击退
        public void Knockback(Vector2 direction, float distance)
        {
            if (IsDead) return;
            transform.Translate((Vector3)(direction.normalized * distance), Space.World);
        }

        private void Update()
        {
            if (visual != null && !visual.enabled) visual.enabled = true;

            if (IsDead || playerTrans == null) return;

            float speedMult = SpeedMultiplier;
            Vector2 dir = ((Vector2)playerTrans.position - (Vector2)transform.position).normalized;
            transform.Translate(dir * moveSpeed * speedMult * Time.deltaTime, Space.World);

            // 碰撞检测：距离 ≤ 碰撞半径
            float dist = Vector2.Distance(transform.position, playerTrans.position);
            if (dist <= 0.55f)
            {
                PlayerController pc = playerTrans.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead)
                {
                    pc.TakeDamage(damage);
                    Debug.Log($"[Enemy:{enemyId}] 撞击玩家，伤害 {damage}");
                }
                Die(false);
            }
        }

        // ── 受到子弹伤害 ─────────────────────────────────────────────────────────
        public void TakeDamage(int dmg)
        {
            if (IsDead) return;

            // 应用易伤系数
            float mult = DamageMultiplier;
            dmg = Mathf.RoundToInt(dmg * mult);

            if (sp > 0)
            {
                int abs = Mathf.Min(sp, dmg);
                sp  -= abs;
                dmg -= abs;
                // 护盾飘字（蓝色）
                if (abs > 0)
                    FloatingText.Show(transform.position, $"-{abs}", FloatingText.FloatType.ShieldDamage);
            }
            if (dmg > 0)
            {
                hp = Mathf.Max(0, hp - dmg);
                // 血量飘字（红色）
                FloatingText.Show(transform.position, $"-{dmg}", FloatingText.FloatType.Damage);
            }

            hpBar?.UpdateHealth(hp, sp);

            if (hp <= 0) Die(true);
        }

        /// <summary>接受伤害并记录方向（用于反射子弹）</summary>
        public void TakeDamageFrom(int dmg, Vector2 fromDirection)
        {
            OnTakeDamageDirection?.Invoke(fromDirection);
            TakeDamage(dmg);
        }

        private void Die(bool byBullet)
        {
            if (IsDead && hp > 0) return; // 防重入（hp已被置0）

            if (byBullet) GameManager.Instance?.AddScore(score);
            GameManager.Instance?.OnEnemyKilled(this);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.4f, 0, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.55f);
        }
    }
}
