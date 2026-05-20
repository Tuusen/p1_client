using UnityEngine;

namespace GeometryTD
{
    public class SpawnManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerTransform;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnYMin = -4f;
        [SerializeField] private float spawnYMax = 4f;
        [SerializeField] private float spawnXOffset = 2f;

        private float spawnTimer;
        private float currentInterval;
        private bool spawning;

        private void Start()
        {
            var config = GameConfig.Data.spawn;
            currentInterval = config.spawnInterval;
            spawnTimer = 0f;
            spawning = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.BossPhase)
            {
                spawning = false;
            }
            else if (state == GameManager.GameState.Victory ||
                     state == GameManager.GameState.Defeat)
            {
                spawning = false;
            }
        }

        private void Update()
        {
            if (!spawning) return;
            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameManager.GameState.Playing)
                return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentInterval)
            {
                spawnTimer = 0f;
                SpawnEnemy();

                // Gradually increase difficulty
                var config = GameConfig.Data.spawn;
                currentInterval = Mathf.Max(
                    config.minSpawnInterval,
                    currentInterval - config.spawnAcceleration);
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || playerTransform == null) return;

            EnemyConfig config = GameConfig.GetRandomEnemy();
            if (config == null) return;

            float spawnX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x + spawnXOffset;
            float spawnY = Random.Range(spawnYMin, spawnYMax);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0);

            GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            EnemyController enemy = go.GetComponent<EnemyController>();
            enemy.Init(config, playerTransform);
        }
    }
}
