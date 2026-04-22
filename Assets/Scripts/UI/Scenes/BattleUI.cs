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

        [Header("倍速按钮")]
        [SerializeField] private ToggleGroup speedToggleGroup;
        [SerializeField] private Toggle speedToggle05x;
        [SerializeField] private Toggle speedToggle1x;
        [SerializeField] private Toggle speedToggle15x;

        private bool isBossMode;
        private bool lastResultIsVictory;
        private BattleManager battleManager;

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

            // 查找BattleManager
            battleManager = FindObjectOfType<BattleManager>();

            // 绑定倍速按钮
            speedToggle05x.onValueChanged.AddListener((isOn) => OnSpeedToggleChanged(isOn, GameConsts.GameSpeed.speed1));
            speedToggle1x.onValueChanged.AddListener((isOn) => OnSpeedToggleChanged(isOn, GameConsts.GameSpeed.speed2));
            speedToggle15x.onValueChanged.AddListener((isOn) => OnSpeedToggleChanged(isOn, GameConsts.GameSpeed.speed3));
           
            // 通过GameManager获取当前选择的倍速
            float selectedSpeed = GameManager.Instance.getSelectedTimeScale();
            speedToggle05x.isOn = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed1);
            speedToggle1x.isOn = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed2);
            speedToggle15x.isOn = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed3);

            RefreshSpeedToggleVisual();
        }

        /// <summary>
        /// 刷新倍速Toggle的视觉状态（防止拖拽操作导致状态丢失）
        /// </summary>
        private void RefreshSpeedToggleVisual()
        {
            float selectedSpeed = GameManager.Instance.getSelectedTimeScale();
            
            // 使用isOn赋值确保触发完整的视觉更新
            bool should05x = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed1);
            bool should1x = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed2);
            bool should15x = Mathf.Approximately(selectedSpeed, GameConsts.GameSpeed.speed3);
            
            if (should05x)
            {
                speedToggle05x.isOn = true;
            } else if (should1x)
            {
                speedToggle1x.isOn = true;
            } else if (should15x)
            {
                speedToggle15x.isOn = true;
            }
        }


        private void OnSpeedToggleChanged(bool isOn, float speed)
        {
            if (isOn)
            {
                GameManager.Instance.SetTimeScale(speed);
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
            lastResultIsVictory = isVictory;
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
            // Story mode: navigate to story/event scene
            if (StoryManager.Instance != null && StoryManager.Instance.IsInAdventure)
            {
                if (lastResultIsVictory)
                {
                    StoryManager.Instance.AdvanceToNextNode();
                    StoryManager.Instance.EnterStoryScene();
                }
                else
                {
                    StoryManager.Instance.EnterEventScene();
                }
                return;
            }

            // Normal mode: back to main menu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BackToMainMenu();
            }
        }
    }
}
