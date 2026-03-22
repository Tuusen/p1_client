using UnityEngine;
using System.Collections;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 怪物生成器 - 从最右侧按波次持续随机生成敌人
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("生成设置")]
        [SerializeField] public float spawnInterval      = 1.8f;   // 初始生成间隔
        [SerializeField] public float minSpawnInterval   = 0.4f;   // 最短间隔
        [SerializeField] public float intervalDecrement  = 0.15f;  // 每波加速
        [SerializeField] public float spawnRangeY        = 3.5f;   // Y轴随机范围
        [SerializeField] public float spawnX             = 12f;    // 生成点X坐标

        [Header("波次设置")]
        [SerializeField] public int  baseEnemiesPerWave  = 5;      // 第1波敌人数
        [SerializeField] public int  enemiesIncPerWave   = 2;      // 每波增加数量

        // ── 运行时 ─────────────────────────────────────────────────────────────
        private int   wave             = 1;
        private int   spawnedThisWave  = 0;
        private int   killedThisWave   = 0;
        private bool  running          = false;
        private Coroutine loop;

        private int EnemiesThisWave => baseEnemiesPerWave + (wave - 1) * enemiesIncPerWave;

        // ── 生命周期 ────────────────────────────────────────────────────────────
        private void Start() => StartSpawning();

        // ── 公开接口 ─────────────────────────────────────────────────────────────
        public void StartSpawning()
        {
            if (running) return;
            running = true;
            loop = StartCoroutine(SpawnLoop());
            Debug.Log("[Spawner] 开始生成，第1波");
        }

        public void StopSpawning()
        {
            running = false;
            if (loop != null) { StopCoroutine(loop); loop = null; }
        }

        public void Reset()
        {
            StopSpawning();
            wave = 1; spawnedThisWave = 0; killedThisWave = 0;
            spawnInterval = 1.8f;
            StartSpawning();
        }

        public void OnWaveChanged(int w) => wave = w;

        /// <summary>GameManager 通知有敌人死亡</summary>
        public void NotifyEnemyDied() => killedThisWave++;

        // ── 核心循环 ─────────────────────────────────────────────────────────────
        private IEnumerator SpawnLoop()
        {
            // 开始前等一小会，让场景完成初始化
            yield return new WaitForSeconds(0.5f);

            while (running)
            {
                // 生成一个敌人
                SpawnOneEnemy();
                spawnedThisWave++;

                // 判断本波是否全部生成完毕
                if (spawnedThisWave >= EnemiesThisWave)
                {
                    Debug.Log($"[Spawner] 第{wave}波全部生成({spawnedThisWave}只)，等待清场...");

                    // 等待本波所有敌人死亡
                    yield return new WaitUntil(() =>
                    {
                        if (killedThisWave >= spawnedThisWave) return true;
                        // 备用检测：统计活着的敌人数量
                        var aliveEnemies = GameObject.FindGameObjectsWithTag("Enemy");
                        int aliveCount = 0;
                        foreach (var e in aliveEnemies)
                        {
                            if (e == null) continue;
                            var ec = e.GetComponent<EnemyController>();
                            if (ec != null && !ec.IsDead) aliveCount++;
                        }
                        return aliveCount == 0;
                    });

                    // 进入下一波
                    wave++;
                    spawnedThisWave = 0;
                    killedThisWave  = 0;
                    spawnInterval   = Mathf.Max(minSpawnInterval,
                                         spawnInterval - intervalDecrement);

                    GameManager.Instance?.NextWave();
                    Debug.Log($"[Spawner] 进入第{wave}波  间隔={spawnInterval:F2}s  总数={EnemiesThisWave}");

                    yield return new WaitForSeconds(1.5f); // 波间间隔
                }
                else
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }

        // ── 生成单只敌人 ──────────────────────────────────────────────────────────
        private void SpawnOneEnemy()
        {
            EnemyConfig cfg = ConfigLoader.GetRandomEnemyConfig();

            // 随机 Y 位置
            float y = Random.Range(-spawnRangeY, spawnRangeY);
            Vector3 pos = new Vector3(spawnX, y, 0f);

            GameObject enemy = new GameObject($"Enemy_{cfg.id}");
            enemy.tag = "Enemy";
            enemy.transform.position = pos;

            EnemyController ec = enemy.AddComponent<EnemyController>();
            ec.Initialize(cfg.id);

            Debug.Log($"[Spawner] 生成 {cfg.name} at ({pos.x:F1},{pos.y:F1})  波{wave} {spawnedThisWave+1}/{EnemiesThisWave}");
        }

        // ── Gizmos ──────────────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(spawnX, -spawnRangeY, 0),
                            new Vector3(spawnX,  spawnRangeY, 0));
        }
    }
}
