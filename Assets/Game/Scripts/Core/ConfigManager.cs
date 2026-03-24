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
        public List<BulletStyleConfig> BulletStyleConfigs { get; private set; }
        public List<ArcaneConfig> ArcaneConfigs { get; private set; }

        private Dictionary<int, Dictionary<int, SkillConfig>> skillLookup;
        private Dictionary<int, BulletStyleConfig> bulletStyleLookup;
        private Dictionary<int, ArcaneConfig> arcaneLookup;

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
            BulletStyleConfigs = LoadConfig<BulletStyleConfigList>("Configs/bullet_config").bulletStyles;
            ArcaneConfigs = LoadConfig<ArcaneConfigList>("Configs/arcane_config").arcanes;
            BuildSkillLookup();
            BuildBulletStyleLookup();
            BuildArcaneLookup();
        }

        private void BuildSkillLookup()
        {
            skillLookup = new Dictionary<int, Dictionary<int, SkillConfig>>();
            if (SkillConfigs == null) return;
            foreach (var skill in SkillConfigs)
            {
                if (!skillLookup.ContainsKey(skill.id))
                    skillLookup[skill.id] = new Dictionary<int, SkillConfig>();
                skillLookup[skill.id][skill.level] = skill;
            }
        }

        private void BuildBulletStyleLookup()
        {
            bulletStyleLookup = new Dictionary<int, BulletStyleConfig>();
            if (BulletStyleConfigs == null) return;
            foreach (var style in BulletStyleConfigs)
                bulletStyleLookup[style.id] = style;
        }

        public BulletStyleConfig GetBulletStyleConfig(int styleId)
        {
            if (bulletStyleLookup != null && bulletStyleLookup.TryGetValue(styleId, out var config))
                return config;
            return null;
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
            return GetSkillConfig(skillId, 1);
        }

        public SkillConfig GetSkillConfig(int skillId, int level)
        {
            if (skillLookup != null &&
                skillLookup.TryGetValue(skillId, out var levels) &&
                levels.TryGetValue(level, out var config))
            {
                return config;
            }
            Debug.LogError($"[ConfigManager] 未找到技能配置, id: {skillId}, level: {level}");
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

        private void BuildArcaneLookup()
        {
            arcaneLookup = new Dictionary<int, ArcaneConfig>();
            if (ArcaneConfigs == null) return;
            foreach (var arcane in ArcaneConfigs)
                arcaneLookup[arcane.id] = arcane;
        }

        public ArcaneConfig GetArcaneConfig(int arcaneId)
        {
            if (arcaneLookup != null && arcaneLookup.TryGetValue(arcaneId, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到奥术配置, id: {arcaneId}");
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
