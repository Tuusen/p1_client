using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 飘字系统 - 在世界坐标位置显示伤害/治疗数字
    /// 用法：FloatingText.Show(transform.position, "-50", FloatingText.Type.Damage);
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public enum FloatType { Damage, Heal, ShieldDamage }

        // ── 配置 ──────────────────────────────────────────────────────────────
        private const float FLOAT_SPEED   = 1.8f;   // 上浮速度（世界单位/秒）
        private const float FADE_START    = 0.4f;   // 开始淡出时间比例
        private const float DURATION      = 0.9f;   // 总持续时间

        // ── 运行时 ────────────────────────────────────────────────────────────
        private TextMesh   textMesh;
        private float      elapsed;
        private Color      baseColor;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            // 向上飘动（稍微有个减速效果）
            float speed = FLOAT_SPEED * (1f - elapsed / DURATION * 0.6f);
            transform.Translate(Vector3.up * speed * Time.deltaTime, Space.World);

            // 淡出
            float t = elapsed / DURATION;
            if (t > FADE_START)
            {
                float alpha = 1f - (t - FADE_START) / (1f - FADE_START);
                Color c = textMesh.color;
                c.a = Mathf.Clamp01(alpha);
                textMesh.color = c;
            }

            if (elapsed >= DURATION)
                Destroy(gameObject);
        }

        // ── 静态工厂方法 ──────────────────────────────────────────────────────
        /// <summary>
        /// 在指定世界坐标显示飘字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="text">显示内容（如 "-50" 或 "+30"）</param>
        /// <param name="type">飘字类型（Damage/Heal/ShieldDamage）</param>
        public static void Show(Vector3 worldPos, string text, FloatType type = FloatType.Damage)
        {
            // 随机横向偏移，避免数字堆叠
            float offsetX = Random.Range(-0.3f, 0.3f);
            Vector3 spawnPos = worldPos + new Vector3(offsetX, 0.4f, 0f);

            GameObject go = new GameObject("FloatingText");
            go.transform.position = spawnPos;

            // TextMesh（世界空间渲染，始终朝向相机）
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text      = text;
            tm.fontSize  = 80;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // 设置颜色
            switch (type)
            {
                case FloatType.Damage:
                    tm.color = new Color(1f, 0.25f, 0.1f);   // 红色 - 受伤
                    break;
                case FloatType.Heal:
                    tm.color = new Color(0.3f, 1f, 0.45f);   // 绿色 - 治疗
                    break;
                case FloatType.ShieldDamage:
                    tm.color = new Color(0.4f, 0.7f, 1f);    // 蓝色 - 盾受伤
                    break;
            }

            // 缩放：TextMesh 默认较大，缩小到合适
            go.transform.localScale = Vector3.one * 0.025f;

            // 添加 MeshRenderer 排序层，让文字显示在前面
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 20;

            go.AddComponent<FloatingText>();
        }
    }
}
