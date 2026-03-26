using UnityEngine;

namespace GeometryTD
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private LevelSelectUI levelSelectUI;

        private void Start()
        {
            if (levelSelectUI != null)
                levelSelectUI.Show();
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

        public void SetLevelSelectUI(LevelSelectUI ui)
        {
            levelSelectUI = ui;
        }
    }
}
