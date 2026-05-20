using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometryTD
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            Playing,
            BossPhase,
            Victory,
            Defeat
        }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public int KillCount { get; private set; }
        public int KillTarget { get; private set; }

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<int> OnKillCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameConfig.Load();
        }

        public void StartGame()
        {
            KillCount = 0;
            KillTarget = GameConfig.Data.spawn.killTarget;
            SceneManager.LoadScene("BattleScene");
        }

        public void OnBattleSceneLoaded()
        {
            SetState(GameState.Playing);
        }

        public void ReturnToMainMenu()
        {
            SetState(GameState.MainMenu);
            SceneManager.LoadScene("MainMenuScene");
        }

        public void AddKill()
        {
            if (CurrentState != GameState.Playing) return;

            KillCount++;
            OnKillCountChanged?.Invoke(KillCount);

            if (KillCount >= KillTarget)
            {
                SetState(GameState.BossPhase);
            }
        }

        public void OnBossDefeated()
        {
            SetState(GameState.Victory);
        }

        public void OnPlayerDefeated()
        {
            SetState(GameState.Defeat);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
