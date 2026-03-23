using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }

        public HeroConfig HeroConfig { get; private set; }
        public List<MonsterConfig> MonsterConfigs { get; private set; }
        public List<SkillConfig> SkillConfigs { get; private set; }
        public GameConfig GameConfig { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllConfigs();
        }

        private void LoadAllConfigs()
        {
            HeroConfig = LoadConfig<HeroConfig>("Configs/hero_config");
            MonsterConfigs = LoadConfig<MonsterConfigList>("Configs/monster_config").monsters;
            SkillConfigs = LoadConfig<SkillConfigList>("Configs/skill_config").skills;
            GameConfig = LoadConfig<GameConfig>("Configs/game_config");
        }

        private T LoadConfig<T>(string path)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogError($"[ConfigManager] 无法加载配置文件: {path}");
                return default;
            }
            T config = JsonUtility.FromJson<T>(textAsset.text);
            if (config == null)
            {
                Debug.LogError($"[ConfigManager] 配置文件解析失败: {path}");
                return default;
            }
            return config;
        }

        public SkillConfig GetSkillConfig(int skillId)
        {
            for (int i = 0; i < SkillConfigs.Count; i++)
            {
                if (SkillConfigs[i].id == skillId)
                    return SkillConfigs[i];
            }
            Debug.LogError($"[ConfigManager] 未找到技能配置, id: {skillId}");
            return null;
        }

        public MonsterConfig GetMonsterConfig(int monsterId)
        {
            for (int i = 0; i < MonsterConfigs.Count; i++)
            {
                if (MonsterConfigs[i].id == monsterId)
                    return MonsterConfigs[i];
            }
            Debug.LogError($"[ConfigManager] 未找到怪物配置, id: {monsterId}");
            return null;
        }

        public MonsterConfig GetBossConfig()
        {
            for (int i = 0; i < MonsterConfigs.Count; i++)
            {
                if (MonsterConfigs[i].is_boss)
                    return MonsterConfigs[i];
            }
            Debug.LogError("[ConfigManager] 未找到Boss配置");
            return null;
        }

        public List<MonsterConfig> GetNormalMonsterConfigs()
        {
            List<MonsterConfig> normals = new List<MonsterConfig>();
            for (int i = 0; i < MonsterConfigs.Count; i++)
            {
                if (!MonsterConfigs[i].is_boss)
                    normals.Add(MonsterConfigs[i]);
            }
            return normals;
        }
    }
}
