using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    [Serializable]
    public class PlayerConfig
    {
        public int maxHp;
        public int maxShield;
        public int attackDamage;
        public float attackInterval;
        public float bulletSpeed;
    }

    [Serializable]
    public class EnemyConfig
    {
        public int id;
        public string name;
        public int level;
        public int maxHp;
        public int damage;
        public float moveSpeed;
    }

    [Serializable]
    public class BossConfig
    {
        public int id;
        public string name;
        public int level;
        public int maxHp;
        public int damage;
        public float attackInterval;
        public float bulletSpeed;
    }

    [Serializable]
    public class SpawnConfig
    {
        public int killTarget;
        public float spawnInterval;
        public float minSpawnInterval;
        public float spawnAcceleration;
    }

    [Serializable]
    public class GameConfigData
    {
        public PlayerConfig player;
        public List<EnemyConfig> enemies;
        public BossConfig boss;
        public SpawnConfig spawn;
    }

    public static class GameConfig
    {
        private static GameConfigData _data;

        public static GameConfigData Data
        {
            get
            {
                if (_data == null)
                    Load();
                return _data;
            }
        }

        public static void Load()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("enemy_config");
            if (textAsset != null)
            {
                _data = JsonUtility.FromJson<GameConfigData>(textAsset.text);
            }
            else
            {
                Debug.LogError("Cannot load enemy_config.json from Resources!");
                _data = new GameConfigData();
            }
        }

        public static EnemyConfig GetRandomEnemy()
        {
            if (Data.enemies == null || Data.enemies.Count == 0)
                return null;
            return Data.enemies[UnityEngine.Random.Range(0, Data.enemies.Count)];
        }
    }
}
