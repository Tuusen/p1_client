using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Spawns configurable visual effects driven by event_effect_config.json.
    /// Attach to the BattleManager GameObject or call via static-like access through BattleManager.
    /// </summary>
    public class EventEffectManager : MonoBehaviour
    {
        // Cached procedural textures (created once, reused)
        private Texture2D circleTexture;
        private Texture2D ringTexture;
        private const int TexSize = 64;

        private void Awake()
        {
            circleTexture = CreateCircleTexture();
            ringTexture = CreateRingTexture();
        }

        /// <summary>
        /// Trigger a visual effect for the given event type at the specified world position.
        /// </summary>
        public void TriggerEffect(int eventType, Vector3 position)
        {
            var config = ConfigManager.Instance.GetEventEffectConfig(eventType);
            if (config == null) return;

            SpawnEffect(config, position);
        }

        private void SpawnEffect(EventEffectConfig config, Vector3 position)
        {
            Color color = new Color(config.colorR, config.colorG, config.colorB, config.colorA);
            float duration = config.duration;

            GameObject go = new GameObject($"EventEffect_{config.eventType}");
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 90;
            sr.color = color;

            Texture2D tex = config.shape == "ring" ? ringTexture : circleTexture;
            float ppu = TexSize / (config.size * 2f);
            sr.sprite = Sprite.Create(tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.5f), ppu);

            if (config.isInstant)
            {
                // Burst: quick expand + fade
                var anim = go.AddComponent<EffectBurstAnim>();
                anim.Init(sr, duration);
            }
            else
            {
                // Persistent: hold then fade
                var anim = go.AddComponent<EffectFadeAnim>();
                anim.Init(sr, duration);
            }
        }

        // ===== Procedural Textures =====

        private static Texture2D CreateCircleTexture()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            float c = TexSize / 2f;
            float rSq = c * c;
            for (int x = 0; x < TexSize; x++)
            {
                for (int y = 0; y < TexSize; y++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= rSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateRingTexture()
        {
            Texture2D tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            float c = TexSize / 2f;
            float outerSq = c * c;
            float inner = c * 0.7f;
            float innerSq = inner * inner;
            for (int x = 0; x < TexSize; x++)
            {
                for (int y = 0; y < TexSize; y++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float dSq = dx * dx + dy * dy;
                    tex.SetPixel(x, y, dSq <= outerSq && dSq >= innerSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }
    }

    /// <summary>
    /// Burst animation: quick scale-up + fade out, then self-destroy.
    /// </summary>
    public class EffectBurstAnim : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float duration;
        private float elapsed;
        private Color startColor;

        public void Init(SpriteRenderer renderer, float dur)
        {
            sr = renderer;
            duration = dur;
            startColor = sr.color;
            transform.localScale = Vector3.one * 0.3f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Scale: 0.3 -> 1.0
            float scale = Mathf.Lerp(0.3f, 1f, t);
            transform.localScale = Vector3.one * scale;

            // Fade out in the second half
            float alpha = t < 0.5f ? startColor.a : Mathf.Lerp(startColor.a, 0f, (t - 0.5f) / 0.5f);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Persistent animation: hold color for most of duration, then fade out, then self-destroy.
    /// </summary>
    public class EffectFadeAnim : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float duration;
        private float elapsed;
        private Color startColor;

        public void Init(SpriteRenderer renderer, float dur)
        {
            sr = renderer;
            duration = dur;
            startColor = sr.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Hold for 70% of duration, then fade out
            float alpha = t < 0.7f ? startColor.a : Mathf.Lerp(startColor.a, 0f, (t - 0.7f) / 0.3f);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
