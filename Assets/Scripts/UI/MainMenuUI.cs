using UnityEngine;

namespace GeometryTD
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private LevelSelectUI levelSelectUI;
        [SerializeField] private ArcaneSelectUI arcaneSelectUI;

        private void Start()
        {
        }

        public void OnStartButtonClicked()
        {
            if (levelSelectUI != null)
            {
                levelSelectUI.Show();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        public void OnHeroButtonClicked()
        {
            GameHelper.OpenWin<HeroSelectWin>();
        }

        public void OnSkillButtonClicked()
        {
            GameHelper.OpenWin<SkillSelectWin>();
        }

        public void OnArcaneButtonClicked()
        {
            if (arcaneSelectUI != null)
                arcaneSelectUI.Show();
        }

        public void SetLevelSelectUI(LevelSelectUI ui)
        {
            levelSelectUI = ui;
        }
    }
}
