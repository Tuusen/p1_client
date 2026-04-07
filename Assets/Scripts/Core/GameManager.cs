using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometryTD
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string SaveKeyCompletedLevels = "CompletedLevels";
        private const string SaveKeySelectedHero = "SelectedHeroId";
        private const string SaveKeyEquippedSkills = "EquippedSkillIds";
        private const string SaveKeyEquippedArcanes = "EquippedArcaneIds";

        private int selectedLevelId;
        private HashSet<int> completedLevels = new HashSet<int>();

        private int selectedHeroId;
        private int[] equippedSkillIds;
        private int[] equippedArcaneIds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCompletedLevels();
            LoadPlayerSelections();
        }

        public void SelectLevel(int levelId)
        {
            selectedLevelId = levelId;
        }

        public int GetSelectedLevelId()
        {
            return selectedLevelId;
        }

        public void StartSelectedLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Battle");
        }

        public void StartGame()
        {
            if (selectedLevelId <= 0)
                selectedLevelId = 1;
            StartSelectedLevel();
        }

        public void BackToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void MarkLevelCompleted(int levelId)
        {
            completedLevels.Add(levelId);
            SaveCompletedLevels();
        }

        public bool IsLevelCompleted(int levelId)
        {
            return completedLevels.Contains(levelId);
        }

        public bool IsLevelUnlocked(int levelId)
        {
            if (ConfigManager.Instance == null) return false;

            LevelConfig levelConfig = Cfg.Level.Get(levelId);
            if (levelConfig == null) return false;

            if (levelConfig.conditions == null || levelConfig.conditions.Length == 0)
                return true;

            foreach (int condId in levelConfig.conditions)
            {
                ConditionConfig cond = Cfg.Condition.Get(condId);
                if (cond == null) return false;

                if (cond.type == 1)
                {
                    if (!IsLevelCompleted(cond.p1))
                        return false;
                }
            }

            return true;
        }

        // ===== 英雄选择 =====

        public void SelectHero(int heroId)
        {
            selectedHeroId = heroId;
            SavePlayerSelections();
        }

        public int GetSelectedHeroId()
        {
            if (selectedHeroId > 0) return selectedHeroId;
            if (ConfigManager.Instance != null)
                return Cfg.Hero.Meta.default_hero_id;
            return 1;
        }

        // ===== 技能装备 =====

        public void SetEquippedSkills(int[] ids)
        {
            equippedSkillIds = ids;
            SavePlayerSelections();
        }

        public int[] GetEquippedSkills()
        {
            if (equippedSkillIds != null && equippedSkillIds.Length > 0)
                return equippedSkillIds;
            if (ConfigManager.Instance != null)
                return Cfg.Skill.Meta.slot_ids;
            return new int[0];
        }

        public bool HasValidSkillLoadout()
        {
            int[] skills = GetEquippedSkills();
            return skills != null && skills.Length == 8;
        }

        // ===== 奥术装备 =====

        public void SetEquippedArcanes(int[] ids)
        {
            equippedArcaneIds = ids;
            SavePlayerSelections();
        }

        public int[] GetEquippedArcanes()
        {
            if (equippedArcaneIds != null && equippedArcaneIds.Length > 0)
                return equippedArcaneIds;
            if (ConfigManager.Instance != null)
                return Cfg.Arcane.Meta.slot_ids;
            return new int[0];
        }

        // ===== 持久化：玩家选择 =====

        private void LoadPlayerSelections()
        {
            selectedHeroId = PlayerPrefs.GetInt(SaveKeySelectedHero, 0);

            string skillData = PlayerPrefs.GetString(SaveKeyEquippedSkills, "");
            if (!string.IsNullOrEmpty(skillData))
            {
                string[] parts = skillData.Split(',');
                List<int> ids = new List<int>();
                foreach (string part in parts)
                {
                    if (int.TryParse(part.Trim(), out int id) && id > 0)
                        ids.Add(id);
                }
                if (ids.Count > 0)
                    equippedSkillIds = ids.ToArray();
            }

            string arcaneData = PlayerPrefs.GetString(SaveKeyEquippedArcanes, "");
            if (!string.IsNullOrEmpty(arcaneData))
            {
                string[] parts = arcaneData.Split(',');
                List<int> ids = new List<int>();
                foreach (string part in parts)
                {
                    if (int.TryParse(part.Trim(), out int id) && id > 0)
                        ids.Add(id);
                }
                if (ids.Count > 0)
                    equippedArcaneIds = ids.ToArray();
            }
        }

        private void SavePlayerSelections()
        {
            PlayerPrefs.SetInt(SaveKeySelectedHero, selectedHeroId);

            if (equippedSkillIds != null && equippedSkillIds.Length > 0)
            {
                List<string> parts = new List<string>();
                foreach (int id in equippedSkillIds) parts.Add(id.ToString());
                PlayerPrefs.SetString(SaveKeyEquippedSkills, string.Join(",", parts));
            }

            if (equippedArcaneIds != null && equippedArcaneIds.Length > 0)
            {
                List<string> parts = new List<string>();
                foreach (int id in equippedArcaneIds) parts.Add(id.ToString());
                PlayerPrefs.SetString(SaveKeyEquippedArcanes, string.Join(",", parts));
            }

            PlayerPrefs.Save();
        }

        // ===== 持久化：关卡完成 =====

        private void LoadCompletedLevels()
        {
            completedLevels.Clear();
            string data = PlayerPrefs.GetString(SaveKeyCompletedLevels, "");
            if (string.IsNullOrEmpty(data)) return;

            string[] parts = data.Split(',');
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int id))
                    completedLevels.Add(id);
            }
        }

        private void SaveCompletedLevels()
        {
            List<int> ids = new List<int>(completedLevels);
            ids.Sort();
            string data = string.Join(",", ids);
            PlayerPrefs.SetString(SaveKeyCompletedLevels, data);
            PlayerPrefs.Save();
        }
    }
}
