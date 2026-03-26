using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class MonsterSpawner : MonoBehaviour
    {
        private float spawnInterval;
        private float spawnTimer;
        private bool isSpawning;

        private List<MonsterConfig> normalMonsterConfigs;
        private BattleManager battleManager;

        public void Init(BattleManager manager, float interval, List<MonsterConfig> configs)
        {
            battleManager = manager;
            spawnInterval = interval;
            normalMonsterConfigs = configs;
            spawnTimer = 0f;
            isSpawning = true;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        public void StartSpawning()
        {
            isSpawning = true;
        }

        private void Update()
        {
            if (!isSpawning || normalMonsterConfigs == null || normalMonsterConfigs.Count == 0)
                return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnMonster();
                spawnTimer = 0f;
            }
        }

        private void SpawnMonster()
        {
            int index = Random.Range(0, normalMonsterConfigs.Count);
            MonsterConfig config = normalMonsterConfigs[index];

            float spawnX = 12f;
            float spawnY = Random.Range(-3.5f, 3.5f);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            battleManager.SpawnMonster(config, spawnPos);
        }
    }
}
