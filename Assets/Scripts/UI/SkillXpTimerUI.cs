using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class SkillXpTimerUI : MonoBehaviour
    {
        [SerializeField] private Slider timerSlider;
        [SerializeField] private Text timerText;

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
    }
}
