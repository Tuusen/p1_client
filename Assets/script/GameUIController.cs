using UnityEngine;
using UnityEngine.UI;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 游戏 HUD 控制器 - 自动绑定同 Canvas 下的 UI 子对象
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        private Text       scoreText;
        private Text       waveText;
        private Text       timeText;
        private GameObject gameOverPanel;
        private Text       finalScoreText;
        private Button     restartButton;

        private void Start()
        {
            // ── 自动查找同 Canvas 下的 UI 节点 ───────────────────────────────────
            scoreText      = FindText("ScoreText");
            waveText       = FindText("WaveText");
            timeText       = FindText("TimeText");
            gameOverPanel  = FindChild("GameOverPanel");
            finalScoreText = FindText("FinalScoreText");
            restartButton  = FindButton("RestartButton");

            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            // ── 注册 GameManager 事件 ────────────────────────────────────────────
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged     += UpdateScore;
                GameManager.Instance.OnWaveChanged      += UpdateWave;
                GameManager.Instance.OnGameTimeChanged  += UpdateTime;
                GameManager.Instance.OnGameOver         += ShowGameOver;
            }

            UpdateScore(0);
            UpdateWave(1);
            UpdateTime(0f);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged    -= UpdateScore;
                GameManager.Instance.OnWaveChanged     -= UpdateWave;
                GameManager.Instance.OnGameTimeChanged -= UpdateTime;
                GameManager.Instance.OnGameOver        -= ShowGameOver;
            }
        }

        // ── 更新显示 ─────────────────────────────────────────────────────────────
        private void UpdateScore(int v) { if (scoreText) scoreText.text = $"分数: {v}"; }
        private void UpdateWave(int v)  { if (waveText)  waveText.text  = $"波次: {v}"; }

        private void UpdateTime(float t)
        {
            if (!timeText) return;
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            timeText.text = $"时间: {m:00}:{s:00}";
        }

        private void ShowGameOver()
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (finalScoreText && GameManager.Instance != null)
                finalScoreText.text = $"最终分数: {GameManager.Instance.Score}";
        }

        private void OnRestartClicked()
        {
            if (gameOverPanel) gameOverPanel.SetActive(false);
            GameManager.Instance?.RestartGame();
        }

        // ── 辅助查找 ─────────────────────────────────────────────────────────────
        private Text FindText(string n)
        {
            var t = FindDeep(n);
            return t ? t.GetComponent<Text>() : null;
        }

        private Button FindButton(string n)
        {
            var t = FindDeep(n);
            return t ? t.GetComponent<Button>() : null;
        }

        private GameObject FindChild(string n)
        {
            var t = FindDeep(n);
            return t ? t.gameObject : null;
        }

        private Transform FindDeep(string n)
        {
            // 先在自身 Canvas 下搜索
            return transform.Find(n) ?? DeepFind(transform, n);
        }

        private static Transform DeepFind(Transform root, string n)
        {
            foreach (Transform child in root)
            {
                if (child.name == n) return child;
                Transform found = DeepFind(child, n);
                if (found) return found;
            }
            return null;
        }
    }
}
