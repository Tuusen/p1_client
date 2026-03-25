using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Burst animation: quick scale-up + fade out, then self-destroy.
    /// Can be initialized externally via Init() or auto-start via serialized fields.
    /// </summary>
    public class EffectBurstAnim : MonoBehaviour
    {
        [SerializeField] private float duration = 0.3f;

        private SpriteRenderer sr;
        private float elapsed;
        private Color startColor;
        private bool initialized;
        private Vector3 targetScale;

        public void Init(SpriteRenderer renderer, float dur)
        {
            sr = renderer;
            duration = dur;
            startColor = sr.color;
            targetScale = transform.localScale;
            transform.localScale = targetScale * 0.3f;
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
                    targetScale = transform.localScale;
                    transform.localScale = targetScale * 0.3f;
                    initialized = true;
                }
            }
        }

        private void Update()
        {
            if (!initialized) return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Scale: 0.3 -> 1.0 relative to target scale
            float scale = Mathf.Lerp(0.3f, 1f, t);
            transform.localScale = targetScale * scale;

            // Fade out in the second half
            float alpha = t < 0.5f ? startColor.a : Mathf.Lerp(startColor.a, 0f, (t - 0.5f) / 0.5f);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
