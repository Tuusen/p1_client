using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class SkillXpTimerUI : MonoBehaviour
    {
        [Header("倒计时")]
        [SerializeField] private Slider timerSlider;
        [SerializeField] private Text timerText;

        [Header("目标技能显示")]
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Text skillNameText;
        [SerializeField] private GameObject skillInfoRoot;

        [Header("粒子特效")]
        [SerializeField] private float particleFlyDuration = 0.5f;
        [SerializeField] private Color particleColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private float particleSize = 20f;

        private Canvas rootCanvas;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            while (rootCanvas != null && rootCanvas.transform.parent != null)
            {
                var parent = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
                if (parent != null) rootCanvas = parent;
                else break;
            }
        }

        public void UpdateTimer(float remaining, float total)
        {
            if (total <= 0f) return;

            if (remaining < 0f) remaining = 0f;

            if (timerSlider != null)
            {
                timerSlider.minValue = 0f;
                timerSlider.maxValue = total;
                timerSlider.value = total - remaining;
            }

            if (timerText != null)
            {
                timerText.text = $"{remaining:F1}s";
            }
        }

        public void SetTargetSkill(string skillName, string iconPath)
        {
            bool hasTarget = !string.IsNullOrEmpty(skillName);

            if (skillInfoRoot != null)
                skillInfoRoot.SetActive(hasTarget);

            if (!hasTarget) return;

            if (skillNameText != null)
                skillNameText.text = skillName;

            if (skillIconImage != null)
            {
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var sprite = GameHelper.LoadSprite(iconPath);
                    if (sprite != null)
                        skillIconImage.sprite = sprite;
                    else
                        skillIconImage.sprite = null;
                }
                else
                {
                    skillIconImage.sprite = null;
                }
            }
        }

        public void PlayXpParticle(RectTransform targetRect)
        {
            if (rootCanvas == null || targetRect == null) return;
            StartCoroutine(FlyParticle(targetRect));
        }

        private IEnumerator FlyParticle(RectTransform targetRect)
        {
            // 创建粒子UI元素
            GameObject particleObj = new GameObject("XpParticle");
            particleObj.transform.SetParent(rootCanvas.transform, false);
            particleObj.transform.SetAsLastSibling();

            RectTransform particleRT = particleObj.AddComponent<RectTransform>();
            particleRT.sizeDelta = new Vector2(particleSize, particleSize);

            Image particleImg = particleObj.AddComponent<Image>();
            particleImg.color = particleColor;
            particleImg.raycastTarget = false;

            // 如果有技能图标，使用相同的sprite
            if (skillIconImage != null && skillIconImage.sprite != null)
                particleImg.sprite = skillIconImage.sprite;

            // 起始位置：进度条位置
            Vector3 startPos = timerSlider != null
                ? timerSlider.transform.position
                : transform.position;
            particleRT.position = startPos;

            // 目标位置：技能图标位置
            Vector3 endPos = targetRect.position;

            // 飞行动画（使用unscaledDeltaTime避免timeScale=0时协程卡死）
            float elapsed = 0f;
            while (elapsed < particleFlyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / particleFlyDuration;

                // 使用平滑曲线
                float smoothT = t * t * (3f - 2f * t);

                // 添加弧线效果
                float arc = Mathf.Sin(t * Mathf.PI) * 50f;
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
                currentPos.y += arc;

                particleRT.position = currentPos;

                // 逐渐缩小并淡出
                float scale = Mathf.Lerp(1f, 0.5f, smoothT);
                particleRT.localScale = Vector3.one * scale;

                float alpha = t < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);
                particleImg.color = new Color(particleColor.r, particleColor.g, particleColor.b, alpha);

                yield return null;
            }

            Destroy(particleObj);
        }
    }
}
