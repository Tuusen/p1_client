using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Kill Progress")]
        [SerializeField] private Image killProgressFill;
        [SerializeField] private Text killProgressText;

        [Header("Boss Health")]
        [SerializeField] private GameObject bossHealthGroup;
        [SerializeField] private Image bossHpFill;
        [SerializeField] private Text bossHpText;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultDescText;
        [SerializeField] private Button returnButton;

        [Header("References")]
        [SerializeField] private BossSpawner bossSpawner;

        private BossController trackedBoss;

        private void Start()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
            if (bossHealthGroup != null)
                bossHealthGroup.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKillCountChanged += OnKillCountChanged;
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
                UpdateKillProgress(0);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnKillCountChanged -= OnKillCountChanged;
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (trackedBoss != null)
            {
                trackedBoss.OnHealthChanged -= OnBossHealthChanged;
            }
        }

        private void OnKillCountChanged(int killCount)
        {
            UpdateKillProgress(killCount);
        }

        private void UpdateKillProgress(int killCount)
        {
            int target = GameManager.Instance != null ? GameManager.Instance.KillTarget : 100;

            if (killProgressFill != null)
            {
                killProgressFill.fillAmount = Mathf.Clamp01((float)killCount / target);
            }

            if (killProgressText != null)
            {
                killProgressText.text = $"{killCount} / {target}";
            }
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.BossPhase:
                    ShowBossHealth();
                    break;
                case GameManager.GameState.Victory:
                    ShowResult(true);
                    break;
                case GameManager.GameState.Defeat:
                    ShowResult(false);
                    break;
            }
        }

        private void ShowBossHealth()
        {
            // Hide kill progress, show boss health
            if (killProgressFill != null)
                killProgressFill.transform.parent.gameObject.SetActive(false);

            if (bossHealthGroup != null)
                bossHealthGroup.SetActive(true);

            // Track boss health
            if (bossSpawner != null)
            {
                // Delay slightly to allow boss to spawn
                Invoke(nameof(TrackBoss), 0.1f);
            }
        }

        private void TrackBoss()
        {
            if (bossSpawner == null) return;
            trackedBoss = bossSpawner.GetCurrentBoss();
            if (trackedBoss != null)
            {
                trackedBoss.OnHealthChanged += OnBossHealthChanged;
                OnBossHealthChanged(trackedBoss.CurrentHp, trackedBoss.MaxHp);
            }
        }

        private void OnBossHealthChanged(int currentHp, int maxHp)
        {
            if (bossHpFill != null && maxHp > 0)
            {
                bossHpFill.fillAmount = Mathf.Clamp01((float)currentHp / maxHp);
            }

            if (bossHpText != null)
            {
                bossHpText.text = $"{currentHp} / {maxHp}";
            }
        }

        private void ShowResult(bool victory)
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (resultTitleText != null)
                resultTitleText.text = victory ? "VICTORY" : "DEFEAT";

            if (resultDescText != null)
                resultDescText.text = victory
                    ? "Boss has been defeated!"
                    : "Your base was destroyed...";
        }

        public void OnReturnClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
