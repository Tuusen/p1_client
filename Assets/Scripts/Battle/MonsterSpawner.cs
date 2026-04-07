using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class MonsterSpawner : MonoBehaviour
    {
        private BattleManager battleManager;
        private LevelConfig levelConfig;
        private float hardMultiplier;

        private float spawnInterval;
        private float spawnTimer;
        private bool isSpawning;

        private int spawnCount;
        private int killCount;
        private int currentBossIndex;
        private bool bossActive;

        private int[] eliteLastTrigger;

        public int KillCount => killCount;

        public void Init(BattleManager manager, LevelConfig config, float hard)
        {
            battleManager = manager;
            levelConfig = config;
            hardMultiplier = hard;

            spawnInterval = config.spawn_interval;
            spawnTimer = 0f;
            spawnCount = 0;
            killCount = 0;
            currentBossIndex = 0;
            bossActive = false;
            isSpawning = true;

            if (config.superMList != null)
                eliteLastTrigger = new int[config.superMList.Length];
            else
                eliteLastTrigger = new int[0];
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
            if (!isSpawning || bossActive || levelConfig == null) return;
            if (levelConfig.monsterList == null || levelConfig.monsterList.Length == 0) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnWave();
                spawnTimer = 0f;
            }
        }

        private void SpawnWave()
        {
            int index = Random.Range(0, levelConfig.monsterList.Length);
            LevelConfig.MonsterListItem entry = levelConfig.monsterList[index];
            MonsterConfig monsterConfig = Cfg.Monster.Get(entry.id);
            if (monsterConfig == null) return;

            for (int i = 0; i < entry.generate; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPos();
                battleManager.SpawnMonster(monsterConfig, spawnPos, hardMultiplier);
            }

            spawnCount += entry.generate;
            CheckEliteSpawn();
        }

        private void CheckEliteSpawn()
        {
            if (levelConfig.superMList == null) return;

            for (int i = 0; i < levelConfig.superMList.Length; i++)
            {
                LevelConfig.SuperMListItem elite = levelConfig.superMList[i];
                if (elite.num <= 0) continue;

                int triggerCount = spawnCount / elite.num;
                if (triggerCount > eliteLastTrigger[i])
                {
                    eliteLastTrigger[i] = triggerCount;

                    MonsterConfig eliteConfig = Cfg.Monster.Get(elite.id);
                    if (eliteConfig == null) continue;

                    for (int j = 0; j < elite.generate; j++)
                    {
                        Vector3 spawnPos = GetRandomSpawnPos();
                        battleManager.SpawnMonster(eliteConfig, spawnPos, hardMultiplier);
                    }
                }
            }
        }

        public void OnMonsterKilled()
        {
            killCount++;
            CheckBossSpawn();
        }

        private void CheckBossSpawn()
        {
            if (bossActive) return;
            if (levelConfig.bossList == null || levelConfig.bossList.Length == 0) return;
            if (currentBossIndex >= levelConfig.bossList.Length) return;

            LevelConfig.BossListItem bossEntry = levelConfig.bossList[currentBossIndex];
            if (killCount >= bossEntry.num)
            {
                bossActive = true;
                battleManager.SpawnLevelBoss(bossEntry.id, hardMultiplier);
            }
        }

        public void OnBossKilled()
        {
            bossActive = false;
            currentBossIndex++;

            if (currentBossIndex >= levelConfig.bossList.Length)
            {
                isSpawning = false;
                battleManager.OnLevelComplete();
            }
            else
            {
                battleManager.OnBossDefeatedContinue(GetNextBossKillThreshold());
            }
        }

        public int GetNextBossKillThreshold()
        {
            if (levelConfig.bossList == null || levelConfig.bossList.Length == 0) return 0;
            if (currentBossIndex >= levelConfig.bossList.Length) return 0;
            return levelConfig.bossList[currentBossIndex].num;
        }

        public bool IsBossActive()
        {
            return bossActive;
        }

        private Vector3 GetRandomSpawnPos()
        {
            float spawnX = 12f;
            float spawnY = Random.Range(-3.5f, 3.5f);
            return new Vector3(spawnX, spawnY, 0f);
        }
    }
}
