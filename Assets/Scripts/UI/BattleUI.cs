using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class BattleUI : MonoBehaviour
    {
        [Header("进度条")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;

        [Header("结算面板")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Button backButton;

        private bool isBossMode;

        private void Start()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        public void InitProgressBar(int maxKillCount)
        {
            isBossMode = false;
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = maxKillCount;
                progressSlider.value = 0;
            }
            if (progressText != null)
            {
                progressText.text = $"击杀进度: 0/{maxKillCount}";
            }
        }

        public void UpdateKillProgress(int currentKills, int maxKills)
        {
            if (isBossMode) return;

            if (progressSlider != null)
            {
                progressSlider.maxValue = maxKills;
                progressSlider.value = currentKills;
            }
            if (progressText != null)
            {
                progressText.text = $"击杀进度: {currentKills}/{maxKills}";
            }
        }

        public void SwitchToBossHpMode(float currentHp, float maxHp)
        {
            isBossMode = true;
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = maxHp;
                progressSlider.value = currentHp;
            }
            if (progressText != null)
            {
                progressText.text = $"Boss血量: {Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            }
        }

        public void UpdateBossHp(float currentHp, float maxHp)
        {
            if (!isBossMode) return;

            if (progressSlider != null)
            {
                progressSlider.value = currentHp;
            }
            if (progressText != null)
            {
                progressText.text = $"Boss血量: {Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
            }
        }

        public void SwitchToKillMode(int currentKills, int nextBossThreshold)
        {
            isBossMode = false;
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
                progressSlider.maxValue = nextBossThreshold;
                progressSlider.value = currentKills;
            }
            if (progressText != null)
            {
                progressText.text = $"击杀进度: {currentKills}/{nextBossThreshold}";
            }
        }

        public void ShowResult(bool isVictory)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }
            if (resultTitleText != null)
            {
                resultTitleText.text = isVictory ? "胜 利" : "失 败";
                resultTitleText.color = isVictory ? new Color(1f, 0.84f, 0f) : new Color(1f, 0.2f, 0.2f);
            }
        }

        private void OnBackButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BackToMainMenu();
            }
        }
    }
}
