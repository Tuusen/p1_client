using UnityEngine;
using System.Collections;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 游戏管理器 - 单例模式，管理游戏状态
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("游戏状态")]
        [SerializeField] private int score = 0;
        [SerializeField] private int wave = 1;
        [SerializeField] private float gameTime = 0f;
        [SerializeField] private bool isGameOver = false;
        [SerializeField] private bool isPaused = false;

        [Header("引用")]
        [SerializeField] private PlayerController player;
        [SerializeField] private EnemySpawner spawner;

        // 事件
        public System.Action<int> OnScoreChanged;
        public System.Action<int> OnWaveChanged;
        public System.Action<float> OnGameTimeChanged;
        public System.Action OnGameOver;
        public System.Action OnGameStart;

        public int Score => score;
        public int Wave => wave;
        public float GameTime => gameTime;
        public bool IsGameOver => isGameOver;
        public bool IsPaused => isPaused;
        public PlayerController Player => player;

        private void Awake()
        {
            // 单例设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 加载配置
            ConfigLoader.LoadAllConfigs();

            // 初始化游戏
            InitializeGame();
        }

        private void Update()
        {
            if (isGameOver || isPaused) return;

            // 更新游戏时间
            gameTime += Time.deltaTime;
            OnGameTimeChanged?.Invoke(gameTime);
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        private void InitializeGame()
        {
            score = 0;
            wave = 1;
            gameTime = 0f;
            isGameOver = false;
            isPaused = false;

            // 查找玩家
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            // 查找生成器
            if (spawner == null)
            {
                spawner = FindObjectOfType<EnemySpawner>();
            }

            // 触发事件
            OnScoreChanged?.Invoke(score);
            OnWaveChanged?.Invoke(wave);
            OnGameStart?.Invoke();

            Debug.Log("[GameManager] 游戏初始化完成");
        }

        /// <summary>
        /// 注册玩家
        /// </summary>
        public void RegisterPlayer(PlayerController playerController)
        {
            player = playerController;
        }

        /// <summary>
        /// 添加分数
        /// </summary>
        public void AddScore(int amount)
        {
            score += amount;
            OnScoreChanged?.Invoke(score);
            Debug.Log($"[GameManager] 分数 +{amount}, 当前分数: {score}");
        }

        /// <summary>
        /// 下一波
        /// </summary>
        public void NextWave()
        {
            wave++;
            OnWaveChanged?.Invoke(wave);
            Debug.Log($"[GameManager] 进入第 {wave} 波");

            // 通知生成器增加难度
            if (spawner != null)
            {
                spawner.OnWaveChanged(wave);
            }
        }

        /// <summary>
        /// 敌人被击杀
        /// </summary>
        public void OnEnemyKilled(EnemyController enemy)
        {
            // 通知生成器计数
            if (spawner != null)
            {
                spawner.NotifyEnemyDied();
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        public void GameOver()
        {
            isGameOver = true;
            OnGameOver?.Invoke();
            Debug.Log($"[GameManager] 游戏结束! 最终分数: {score}, 存活时间: {gameTime:F1}秒");

            // 暂停生成器
            if (spawner != null)
            {
                spawner.StopSpawning();
            }
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            // 清理所有敌人
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                Destroy(enemy);
            }

            // 清理所有子弹
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
            foreach (GameObject bullet in bullets)
            {
                Destroy(bullet);
            }

            // 重置玩家
            if (player != null)
            {
                player.Reset();
            }

            // 重置生成器
            if (spawner != null)
            {
                spawner.Reset();
            }

            // 重新初始化
            InitializeGame();
        }

        /// <summary>
        /// 暂停/继续游戏
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        /// <summary>
        /// 设置时间缩放
        /// </summary>
        public void SetTimeScale(float scale)
        {
            Time.timeScale = scale;
        }
    }
}
