using UnityEngine;

namespace GeometryTD
{
    public class BossSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform playerTransform;

        private BossController currentBoss;

        private void Start()
        {
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
                // Clear remaining enemies
                EnemyManager.Instance?.ClearAll();
                SpawnBoss();
            }
        }

        private void SpawnBoss()
        {
            if (bossPrefab == null || playerTransform == null) return;

            BossConfig config = GameConfig.Data.boss;
            if (config == null) return;

            float spawnX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x + 3f;
            Vector3 spawnPos = new Vector3(spawnX, playerTransform.position.y, 0);

            GameObject go = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            currentBoss = go.GetComponent<BossController>();
            currentBoss.Init(config, playerTransform);
        }

        public BossController GetCurrentBoss()
        {
            return currentBoss;
        }
    }
}
