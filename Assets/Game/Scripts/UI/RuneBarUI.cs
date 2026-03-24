using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class RuneBarUI : MonoBehaviour
    {
        [SerializeField] private Text[] runeCountTexts;    // 4 elements: fire, ice, electric, wind
        [SerializeField] private Slider[] energySliders;   // 4 elements

        private ArcaneManager arcaneManager;

        private static readonly Color[] RuneColors = new Color[]
        {
            new Color(1f, 0.4f, 0.2f),    // fire
            new Color(0.3f, 0.7f, 1f),    // ice
            new Color(0.8f, 0.8f, 0.2f),  // electric
            new Color(0.3f, 0.9f, 0.5f),  // wind
        };

        public void SetArcaneManager(ArcaneManager manager)
        {
            arcaneManager = manager;
            if (arcaneManager != null)
                arcaneManager.OnRunesChanged += RefreshDisplay;
        }

        private void OnDestroy()
        {
            if (arcaneManager != null)
                arcaneManager.OnRunesChanged -= RefreshDisplay;
        }

        private void RefreshDisplay()
        {
            if (arcaneManager == null) return;

            for (int i = 0; i < 4; i++)
            {
                int runeType = i + 1;
                int count = arcaneManager.GetRune(runeType);
                int energy = arcaneManager.GetEnergy(runeType);

                if (runeCountTexts != null && i < runeCountTexts.Length && runeCountTexts[i] != null)
                    runeCountTexts[i].text = count.ToString();

                if (energySliders != null && i < energySliders.Length && energySliders[i] != null)
                    energySliders[i].value = energy;
            }
        }

        private void Update()
        {
            // Periodic refresh for smooth slider updates
            RefreshDisplay();
        }
    }
}
