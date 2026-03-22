using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 配置加载器 - 负责加载和管理JSON配置
    /// </summary>
    public static class ConfigLoader
    {
        private static GameConfig _gameConfig;
        private static Dictionary<string, BulletConfig> _bulletConfigs;
        private static Dictionary<string, EnemyConfig> _enemyConfigs;
        private static Dictionary<string, SkillConfig> _skillConfigs;

        private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, "Config");

        /// <summary>
        /// 获取游戏配置
        /// </summary>
        public static GameConfig GameConfig
        {
            get
            {
                if (_gameConfig == null)
                {
                    LoadAllConfigs();
                }
                return _gameConfig;
            }
        }

        /// <summary>
        /// 加载所有配置
        /// </summary>
        public static void LoadAllConfigs()
        {
            _bulletConfigs = new Dictionary<string, BulletConfig>();
            _enemyConfigs = new Dictionary<string, EnemyConfig>();
            _skillConfigs = new Dictionary<string, SkillConfig>();
            _gameConfig = new GameConfig();

            // 加载玩家配置
            LoadPlayerConfig();

            // 加载子弹配置
            LoadBulletConfigs();

            // 加载怪物配置
            LoadEnemyConfigs();

            // 加载技能配置
            LoadSkillConfigs();
        }

        /// <summary>
        /// 加载玩家配置
        /// </summary>
        private static void LoadPlayerConfig()
        {
            string filePath = Path.Combine(ConfigPath, "player.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    _gameConfig.player = JsonUtility.FromJson<PlayerConfig>(json);
                    Debug.Log($"[ConfigLoader] 加载玩家配置成功");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConfigLoader] 加载玩家配置失败: {e.Message}, 使用默认配置");
                    _gameConfig.player = new PlayerConfig();
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigLoader] 玩家配置文件不存在: {filePath}, 使用默认配置");
                _gameConfig.player = new PlayerConfig();
            }
        }

        /// <summary>
        /// 加载子弹配置
        /// </summary>
        private static void LoadBulletConfigs()
        {
            string filePath = Path.Combine(ConfigPath, "bullets.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var list = JsonUtility.FromJson<BulletConfigList>(json);
                    _gameConfig.bullets = list;

                    foreach (var config in list.bullets)
                    {
                        _bulletConfigs[config.id] = config;
                    }
                    Debug.Log($"[ConfigLoader] 加载子弹配置成功, 共 {list.bullets.Count} 种");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConfigLoader] 加载子弹配置失败: {e.Message}");
                    CreateDefaultBulletConfigs();
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigLoader] 子弹配置文件不存在: {filePath}");
                CreateDefaultBulletConfigs();
            }
        }

        /// <summary>
        /// 加载怪物配置
        /// </summary>
        private static void LoadEnemyConfigs()
        {
            string filePath = Path.Combine(ConfigPath, "enemies.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var list = JsonUtility.FromJson<EnemyConfigList>(json);
                    _gameConfig.enemies = list;

                    foreach (var config in list.enemies)
                    {
                        _enemyConfigs[config.id] = config;
                    }
                    Debug.Log($"[ConfigLoader] 加载怪物配置成功, 共 {list.enemies.Count} 种");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConfigLoader] 加载怪物配置失败: {e.Message}");
                    CreateDefaultEnemyConfigs();
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigLoader] 怪物配置文件不存在: {filePath}");
                CreateDefaultEnemyConfigs();
            }
        }

        /// <summary>
        /// 创建默认子弹配置
        /// </summary>
        private static void CreateDefaultBulletConfigs()
        {
            var defaultBullets = new List<BulletConfig>
            {
                new BulletConfig
                {
                    id = "bullet_01",
                    name = "基础子弹",
                    shape = "sphere",
                    color = "#FFFF00",
                    speed = 20f,
                    damage = 15,
                    scale = 0.3f,
                    lifetime = 3f,
                    hasTrail = true,
                    trailColor = "#FFAA00"
                },
                new BulletConfig
                {
                    id = "bullet_02",
                    name = "强力子弹",
                    shape = "cube",
                    color = "#FF6600",
                    speed = 15f,
                    damage = 30,
                    scale = 0.4f,
                    lifetime = 4f,
                    hasTrail = true,
                    trailColor = "#FF3300"
                },
                new BulletConfig
                {
                    id = "bullet_03",
                    name = "快速子弹",
                    shape = "triangle",
                    color = "#00FFFF",
                    speed = 35f,
                    damage = 8,
                    scale = 0.25f,
                    lifetime = 2f,
                    hasTrail = false
                }
            };

            _gameConfig.bullets.bullets = defaultBullets;
            foreach (var config in defaultBullets)
            {
                _bulletConfigs[config.id] = config;
            }
        }

        /// <summary>
        /// 创建默认怪物配置
        /// </summary>
        private static void CreateDefaultEnemyConfigs()
        {
            var defaultEnemies = new List<EnemyConfig>
            {
                new EnemyConfig
                {
                    id = "enemy_01",
                    name = "基础方块",
                    shape = "cube",
                    color = "#FF0000",
                    scale = 0.8f,
                    health = 50,
                    shield = 0,
                    moveSpeed = 3f,
                    damage = 10,
                    score = 10,
                    spawnWeight = 1f
                },
                new EnemyConfig
                {
                    id = "enemy_02",
                    name = "坚固方块",
                    shape = "cube",
                    color = "#880000",
                    scale = 1f,
                    health = 100,
                    shield = 30,
                    moveSpeed = 2f,
                    damage = 15,
                    score = 25,
                    spawnWeight = 0.6f
                },
                new EnemyConfig
                {
                    id = "enemy_03",
                    name = "快速三角",
                    shape = "triangle",
                    color = "#FF00FF",
                    scale = 0.6f,
                    health = 30,
                    shield = 0,
                    moveSpeed = 5f,
                    damage = 8,
                    score = 15,
                    spawnWeight = 0.8f
                },
                new EnemyConfig
                {
                    id = "enemy_04",
                    name = "球形敌人",
                    shape = "sphere",
                    color = "#00FF00",
                    scale = 0.7f,
                    health = 60,
                    shield = 20,
                    moveSpeed = 3.5f,
                    damage = 12,
                    score = 20,
                    spawnWeight = 0.7f
                },
                new EnemyConfig
                {
                    id = "enemy_05",
                    name = "钻石精英",
                    shape = "diamond",
                    color = "#FFD700",
                    scale = 1.2f,
                    health = 150,
                    shield = 50,
                    moveSpeed = 1.5f,
                    damage = 25,
                    score = 50,
                    spawnWeight = 0.3f
                }
            };

            _gameConfig.enemies.enemies = defaultEnemies;
            foreach (var config in defaultEnemies)
            {
                _enemyConfigs[config.id] = config;
            }
        }

        /// <summary>
        /// 加载技能配置
        /// </summary>
        private static void LoadSkillConfigs()
        {
            string filePath = Path.Combine(ConfigPath, "skills.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var list = JsonUtility.FromJson<SkillConfigList>(json);
                    _gameConfig.skills = list;
                    foreach (var cfg in list.skills)
                        _skillConfigs[cfg.id] = cfg;
                    Debug.Log($"[ConfigLoader] 加载技能配置成功, 共 {list.skills.Count} 个");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConfigLoader] 加载技能配置失败: {e.Message}");
                    CreateDefaultSkillConfigs();
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigLoader] 技能配置文件不存在: {filePath}");
                CreateDefaultSkillConfigs();
            }
        }

        /// <summary>
        /// 创建默认技能配置（8个）
        /// </summary>
        private static void CreateDefaultSkillConfigs()
        {
            string[] names  = { "火焰弹","冰晶矢","雷矛","岩石弹","幽灵球","绿毒箭","钢铁核","暗能冲击" };
            string[] shapes = { "sphere","diamond","triangle","cube","sphere","triangle","diamond","cube" };
            string[] colors = { "#FF4400","#44CCFF","#FFEE00","#AA7722","#BB44FF","#44FF44","#CCCCCC","#440088" };
            int[]    dmgs   = { 35, 20, 15, 60, 28, 18, 45, 38 };
            float[]  spds   = { 18f, 22f, 40f, 10f, 15f, 30f, 14f, 16f };

            var list = new System.Collections.Generic.List<SkillConfig>();
            for (int i = 0; i < 8; i++)
            {
                var c = new SkillConfig
                {
                    id           = $"skill_0{i+1}",
                    name         = names[i],
                    description  = $"{names[i]}技能",
                    iconShape    = shapes[i],
                    iconColor    = colors[i],
                    maxLevel     = 10,
                    expPerLevel  = 10,
                    bulletShape  = shapes[i],
                    bulletColor  = colors[i],
                    bulletSpeed  = spds[i],
                    bulletDamage = dmgs[i],
                    bulletScale  = 0.38f,
                    bulletLifetime = 4f,
                    bulletHasTrail = i % 2 == 0
                };
                list.Add(c);
                _skillConfigs[c.id] = c;
            }
            _gameConfig.skills.skills = list;
        }

        /// <summary>
        /// 获取技能配置
        /// </summary>
        public static SkillConfig GetSkillConfig(string id)
        {
            if (_skillConfigs == null) LoadAllConfigs();
            if (_skillConfigs.TryGetValue(id, out var cfg)) return cfg;
            Debug.LogWarning($"[ConfigLoader] 未找到技能配置: {id}");
            return null;
        }

        /// <summary>
        /// 获取所有技能配置
        /// </summary>
        public static System.Collections.Generic.List<SkillConfig> GetAllSkillConfigs()
        {
            if (_skillConfigs == null) LoadAllConfigs();
            return _gameConfig.skills.skills;
        }

        /// <summary>
        /// 获取子弹配置
        /// </summary>
        public static BulletConfig GetBulletConfig(string id)
        {
            if (_bulletConfigs == null || !_bulletConfigs.ContainsKey(id))
            {
                LoadAllConfigs();
            }

            if (_bulletConfigs.TryGetValue(id, out var config))
            {
                return config;
            }

            Debug.LogWarning($"[ConfigLoader] 未找到子弹配置: {id}, 返回默认配置");
            return _bulletConfigs["bullet_01"];
        }

        /// <summary>
        /// 获取怪物配置
        /// </summary>
        public static EnemyConfig GetEnemyConfig(string id)
        {
            if (_enemyConfigs == null || !_enemyConfigs.ContainsKey(id))
            {
                LoadAllConfigs();
            }

            if (_enemyConfigs.TryGetValue(id, out var config))
            {
                return config;
            }

            Debug.LogWarning($"[ConfigLoader] 未找到怪物配置: {id}, 返回默认配置");
            return _enemyConfigs["enemy_01"];
        }

        /// <summary>
        /// 获取所有子弹配置
        /// </summary>
        public static List<BulletConfig> GetAllBulletConfigs()
        {
            if (_bulletConfigs == null)
            {
                LoadAllConfigs();
            }
            return _gameConfig.bullets.bullets;
        }

        /// <summary>
        /// 获取所有怪物配置
        /// </summary>
        public static List<EnemyConfig> GetAllEnemyConfigs()
        {
            if (_enemyConfigs == null)
            {
                LoadAllConfigs();
            }
            return _gameConfig.enemies.enemies;
        }

        /// <summary>
        /// 根据权重随机选择怪物配置
        /// </summary>
        public static EnemyConfig GetRandomEnemyConfig()
        {
            if (_enemyConfigs == null)
            {
                LoadAllConfigs();
            }

            var enemies = _gameConfig.enemies.enemies;
            float totalWeight = 0f;
            foreach (var enemy in enemies)
            {
                totalWeight += enemy.spawnWeight;
            }

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var enemy in enemies)
            {
                currentWeight += enemy.spawnWeight;
                if (random <= currentWeight)
                {
                    return enemy;
                }
            }

            return enemies[0];
        }

        /// <summary>
        /// 将颜色字符串转换为Color
        /// </summary>
        public static Color ParseColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
            {
                return Color.white;
            }

            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                return color;
            }

            return Color.white;
        }
    }
}
