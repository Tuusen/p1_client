using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }

        public List<HeroConfig> HeroConfigs { get; private set; }
        public List<MonsterConfig> MonsterConfigs { get; private set; }
        public List<SkillConfig> SkillConfigs { get; private set; }
        public GameConfig GameConfig { get; private set; }
        public List<BulletStyleConfig> BulletStyleConfigs { get; private set; }
        public List<ArcaneConfig> ArcaneConfigs { get; private set; }
        public List<EventEffectConfig> EventEffectConfigs { get; private set; }
        public List<LevelConfig> LevelConfigs { get; private set; }
        public List<ConditionConfig> ConditionConfigs { get; private set; }
        public List<RoleConfig> RoleConfigs { get; private set; }
        public List<AttributeConfig> AttributeConfigs { get; private set; }

        // Story Collection 相关配置
        public List<StoryCollectionConfig> StoryCollectionConfigs { get; private set; }
        public List<StoryNodeConfig> StoryNodeConfigs { get; private set; }
        public List<DialogueConfig> DialogueConfigs { get; private set; }
        public List<ChoiceGroupConfig> ChoiceGroupConfigs { get; private set; }
        public List<PassiveEffectConfig> PassiveEffectConfigs { get; private set; }
        public List<EventShopConfig> EventShopConfigs { get; private set; }

        private Dictionary<int, Dictionary<int, SkillConfig>> skillLookup;
        private Dictionary<int, BulletStyleConfig> bulletStyleLookup;
        private Dictionary<int, ArcaneConfig> arcaneLookup;
        private Dictionary<int, EventEffectConfig> eventEffectLookup;
        private Dictionary<int, LevelConfig> levelLookup;
        private Dictionary<int, ConditionConfig> conditionLookup;
        private Dictionary<int, RoleConfig> roleLookup;
        private Dictionary<int, HeroConfig> heroLookup;

        // Story Collection 查询索引
        private Dictionary<int, StoryCollectionConfig> storyCollectionLookup;
        private Dictionary<int, StoryNodeConfig> storyNodeLookup;
        private Dictionary<int, DialogueConfig> dialogueLookup;
        private Dictionary<int, ChoiceGroupConfig> choiceGroupLookup;
        private Dictionary<int, PassiveEffectConfig> passiveEffectLookup;
        private Dictionary<int, EventShopConfig> eventShopLookup;

        private Dictionary<int, GameObject> bulletPrefabCache;
        private Dictionary<int, GameObject> effectPrefabCache;
        private Dictionary<int, GameObject> rolePrefabCache;

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
            HeroConfigs = LoadConfig<HeroConfigList>("Configs/hero_config").heroes;
            MonsterConfigs = LoadConfig<MonsterConfigList>("Configs/monster_config").monsters;
            SkillConfigs = LoadConfig<SkillConfigList>("Configs/skill_config").skills;
            GameConfig = LoadConfig<GameConfig>("Configs/game_config");
            BulletStyleConfigs = LoadConfig<BulletStyleConfigList>("Configs/bullet_config").bulletStyles;
            ArcaneConfigs = LoadConfig<ArcaneConfigList>("Configs/arcane_config").arcanes;
            EventEffectConfigs = LoadConfig<EventEffectConfigList>("Configs/event_effect_config").effects;
            LevelConfigs = LoadConfig<LevelConfigList>("Configs/level_config").levels;
            ConditionConfigs = LoadConfig<ConditionConfigList>("Configs/condition_config").conditions;
            RoleConfigs = LoadConfig<RoleConfigList>("Configs/role_config").roles;
            AttributeConfigs = LoadConfig<AttributeConfigList>("Configs/attribute_config").attributes;
            StoryCollectionConfigs = LoadConfig<StoryCollectionConfigList>("Configs/story_collection_config").collections;
            StoryNodeConfigs = LoadConfig<StoryNodeConfigList>("Configs/story_node_config").nodes;
            DialogueConfigs = LoadConfig<DialogueConfigList>("Configs/dialogue_config").dialogues;
            ChoiceGroupConfigs = LoadConfig<ChoiceGroupConfigList>("Configs/choice_option_config").choiceGroups;
            PassiveEffectConfigs = LoadConfig<PassiveEffectConfigList>("Configs/passive_effect_config").effects;
            EventShopConfigs = LoadConfig<EventShopConfigList>("Configs/event_shop_config").shops;
            BuildSkillLookup();
            BuildBulletStyleLookup();
            BuildArcaneLookup();
            BuildEventEffectLookup();
            BuildLevelLookup();
            BuildConditionLookup();
            BuildRoleLookup();
            BuildHeroLookup();
            BuildStoryCollectionLookup();
            BuildStoryNodeLookup();
            BuildDialogueLookup();
            BuildChoiceGroupLookup();
            BuildPassiveEffectLookup();
            BuildEventShopLookup();
            PreloadPrefabs();
            PreloadRolePrefabs();
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

        public GameObject GetBulletPrefab(int styleId)
        {
            if (bulletPrefabCache != null && bulletPrefabCache.TryGetValue(styleId, out var prefab))
                return prefab;
            return null;
        }

        public GameObject GetEffectPrefab(int eventType)
        {
            if (effectPrefabCache != null && effectPrefabCache.TryGetValue(eventType, out var prefab))
                return prefab;
            return null;
        }

        private void PreloadPrefabs()
        {
            bulletPrefabCache = new Dictionary<int, GameObject>();
            if (BulletStyleConfigs != null)
            {
                foreach (var style in BulletStyleConfigs)
                {
                    if (string.IsNullOrEmpty(style.prefabPath)) continue;
                    GameObject prefab = Resources.Load<GameObject>(style.prefabPath);
                    if (prefab != null)
                        bulletPrefabCache[style.id] = prefab;
                    else
                        Debug.LogWarning($"[ConfigManager] 无法加载子弹Prefab: {style.prefabPath} (id={style.id})");
                }
            }

            effectPrefabCache = new Dictionary<int, GameObject>();
            if (EventEffectConfigs != null)
            {
                foreach (var effect in EventEffectConfigs)
                {
                    if (string.IsNullOrEmpty(effect.prefabPath)) continue;
                    GameObject prefab = Resources.Load<GameObject>(effect.prefabPath);
                    if (prefab != null)
                        effectPrefabCache[effect.eventType] = prefab;
                    else
                        Debug.LogWarning($"[ConfigManager] 无法加载特效Prefab: {effect.prefabPath} (eventType={effect.eventType})");
                }
            }
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

        private void BuildEventEffectLookup()
        {
            eventEffectLookup = new Dictionary<int, EventEffectConfig>();
            if (EventEffectConfigs == null) return;
            foreach (var effect in EventEffectConfigs)
                eventEffectLookup[effect.eventType] = effect;
        }

        public EventEffectConfig GetEventEffectConfig(int eventType)
        {
            if (eventEffectLookup != null && eventEffectLookup.TryGetValue(eventType, out var config))
                return config;
            return null;
        }

        private void BuildLevelLookup()
        {
            levelLookup = new Dictionary<int, LevelConfig>();
            if (LevelConfigs == null) return;
            foreach (var level in LevelConfigs)
                levelLookup[level.id] = level;
        }

        private void BuildConditionLookup()
        {
            conditionLookup = new Dictionary<int, ConditionConfig>();
            if (ConditionConfigs == null) return;
            foreach (var cond in ConditionConfigs)
                conditionLookup[cond.id] = cond;
        }

        public LevelConfig GetLevelConfig(int levelId)
        {
            if (levelLookup != null && levelLookup.TryGetValue(levelId, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到关卡配置, id: {levelId}");
            return null;
        }

        public ConditionConfig GetConditionConfig(int conditionId)
        {
            if (conditionLookup != null && conditionLookup.TryGetValue(conditionId, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到条件配置, id: {conditionId}");
            return null;
        }

        // ===== 角色配置 =====

        private void BuildRoleLookup()
        {
            roleLookup = new Dictionary<int, RoleConfig>();
            if (RoleConfigs == null) return;
            foreach (var role in RoleConfigs)
                roleLookup[role.id] = role;
        }

        public RoleConfig GetRoleConfig(int roleId)
        {
            if (roleLookup != null && roleLookup.TryGetValue(roleId, out var config))
                return config;
            Debug.LogWarning($"[ConfigManager] 未找到角色配置, id: {roleId}");
            return null;
        }

        public GameObject GetRolePrefab(int roleId)
        {
            if (rolePrefabCache != null && rolePrefabCache.TryGetValue(roleId, out var prefab))
                return prefab;
            return null;
        }

        private void PreloadRolePrefabs()
        {
            rolePrefabCache = new Dictionary<int, GameObject>();
            if (RoleConfigs == null) return;
            foreach (var role in RoleConfigs)
            {
                if (string.IsNullOrEmpty(role.prefabPath)) continue;
                GameObject prefab = GameHelper.LoadPrefab(role.prefabPath);
                if (prefab != null)
                    rolePrefabCache[role.id] = prefab;
                else
                    Debug.LogWarning($"[ConfigManager] 无法加载角色Prefab: {role.prefabPath} (id={role.id})");
            }
        }

        // ===== 英雄配置 =====

        private void BuildHeroLookup()
        {
            heroLookup = new Dictionary<int, HeroConfig>();
            if (HeroConfigs == null) return;
            foreach (var hero in HeroConfigs)
                heroLookup[hero.id] = hero;
        }

        public HeroConfig GetHeroConfig(int heroId)
        {
            if (heroLookup != null && heroLookup.TryGetValue(heroId, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到英雄配置, id: {heroId}");
            return null;
        }

        // ===== 属性辅助方法 =====

        public static float GetAttrValue(AttrEntry[] attrs, int attrId, float defaultValue = 0f)
        {
            if (attrs == null) return defaultValue;
            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i].id == attrId)
                    return attrs[i].value;
            }
            return defaultValue;
        }

        // ===== 故事集配置 =====

        private void BuildStoryCollectionLookup()
        {
            storyCollectionLookup = new Dictionary<int, StoryCollectionConfig>();
            if (StoryCollectionConfigs == null) return;
            foreach (var config in StoryCollectionConfigs)
                storyCollectionLookup[config.id] = config;
        }

        public StoryCollectionConfig GetStoryCollectionConfig(int id)
        {
            if (storyCollectionLookup != null && storyCollectionLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到故事集配置, id: {id}");
            return null;
        }

        // ===== 节点配置 =====

        private void BuildStoryNodeLookup()
        {
            storyNodeLookup = new Dictionary<int, StoryNodeConfig>();
            if (StoryNodeConfigs == null) return;
            foreach (var config in StoryNodeConfigs)
                storyNodeLookup[config.id] = config;
        }

        public StoryNodeConfig GetStoryNodeConfig(int id)
        {
            if (storyNodeLookup != null && storyNodeLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到故事节点配置, id: {id}");
            return null;
        }

        public List<StoryNodeConfig> GetNodesByCollection(int collectionId)
        {
            List<StoryNodeConfig> result = new List<StoryNodeConfig>();
            if (StoryNodeConfigs == null) return result;
            foreach (var node in StoryNodeConfigs)
            {
                if (node.collectionId == collectionId)
                    result.Add(node);
            }
            return result;
        }

        // ===== 对话配置 =====

        private void BuildDialogueLookup()
        {
            dialogueLookup = new Dictionary<int, DialogueConfig>();
            if (DialogueConfigs == null) return;
            foreach (var config in DialogueConfigs)
                dialogueLookup[config.id] = config;
        }

        public DialogueConfig GetDialogueConfig(int id)
        {
            if (dialogueLookup != null && dialogueLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到对话配置, id: {id}");
            return null;
        }

        // ===== 选项组配置 =====

        private void BuildChoiceGroupLookup()
        {
            choiceGroupLookup = new Dictionary<int, ChoiceGroupConfig>();
            if (ChoiceGroupConfigs == null) return;
            foreach (var config in ChoiceGroupConfigs)
                choiceGroupLookup[config.id] = config;
        }

        public ChoiceGroupConfig GetChoiceGroupConfig(int id)
        {
            if (choiceGroupLookup != null && choiceGroupLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到选项组配置, id: {id}");
            return null;
        }

        // ===== 藏品效果配置 =====

        private void BuildPassiveEffectLookup()
        {
            passiveEffectLookup = new Dictionary<int, PassiveEffectConfig>();
            if (PassiveEffectConfigs == null) return;
            foreach (var config in PassiveEffectConfigs)
                passiveEffectLookup[config.id] = config;
        }

        public PassiveEffectConfig GetPassiveEffectConfig(int id)
        {
            if (passiveEffectLookup != null && passiveEffectLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到藏品效果配置, id: {id}");
            return null;
        }

        // ===== 商店配置 =====

        private void BuildEventShopLookup()
        {
            eventShopLookup = new Dictionary<int, EventShopConfig>();
            if (EventShopConfigs == null) return;
            foreach (var config in EventShopConfigs)
                eventShopLookup[config.id] = config;
        }

        public EventShopConfig GetEventShopConfig(int id)
        {
            if (eventShopLookup != null && eventShopLookup.TryGetValue(id, out var config))
                return config;
            Debug.LogError($"[ConfigManager] 未找到商店配置, id: {id}");
            return null;
        }
    }
}
