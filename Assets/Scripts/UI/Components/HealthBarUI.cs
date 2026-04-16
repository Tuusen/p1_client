using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Text valueText;
        [SerializeField] private bool showText = true;

        public void SetValue(float current, float max)
        {
            if (slider != null)
            {
                slider.maxValue = max;
                slider.value = current;
            }

            if (showText && valueText != null)
            {
                valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        public void SetShowText(bool show)
        {
            showText = show;
            if (valueText != null)
            {
                valueText.gameObject.SetActive(show);
            }
        }
    }
}
