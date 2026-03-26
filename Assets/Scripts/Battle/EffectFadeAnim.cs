using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Persistent animation: hold color for most of duration, then fade out, then self-destroy.
    /// Can be initialized externally via Init() or auto-start via serialized fields.
    /// </summary>
    public class EffectFadeAnim : MonoBehaviour
    {
        [SerializeField] private float duration = 0.5f;

        private SpriteRenderer sr;
        private float elapsed;
        private Color startColor;
        private bool initialized;

        public void Init(SpriteRenderer renderer, float dur)
        {
            sr = renderer;
            duration = dur;
            startColor = sr.color;
            initialized = true;
        }

        private void Start()
        {
            if (!initialized)
            {
                sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    startColor = sr.color;
                    initialized = true;
                }
            }
        }

        private void Update()
        {
            if (!initialized) return;

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
