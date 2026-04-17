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

        // 倍速管理
        private float selectedTimeScale; // 玩家选择的倍速

        private bool isPaused = false;

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
            
            // 初始化倍速
            selectedTimeScale = GameSpeed.speed1;
            isPaused = false;
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
            ResetTimeScale();
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
            ResetTimeScale();
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
                return GameConsts.MetaConsts.DefaultHeroId;
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
                return GameConsts.MetaConsts.SkillSlotIds;
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
                return GameConsts.MetaConsts.ArcaneSlotIds;
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

        // ===== 倍速管理 =====
        /// <summary>
        /// 内部统一设置 Time.timeScale（禁止外部直接调用 Time.timeScale）
        /// </summary>
        private void SetTimeScaleInternal(float timeScale)
        {
            if (isPaused) {
                if (timeScale == GameSpeed.stop)
                {
                    Time.timeScale = GameSpeed.stop;
                }
                // 暂停状态下不允许修改倍速,除非是改为0
                return;
            } else {
                Time.timeScale = timeScale;
            }; 
        }

        /// <summary>
        /// 设置游戏倍速
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            selectedTimeScale = timeScale;
            SetTimeScaleInternal(timeScale);
        }

        /// <summary>
        /// 暂停游戏（倍速设为0）
        /// </summary>
        public void PauseGame()
        {
            isPaused = true;
            SetTimeScaleInternal(GameSpeed.stop);
        }

        /// <summary>
        /// 拖拽慢放（固定0.3倍速）
        /// </summary>
        public void StartDragSlowMotion()
        {
            SetTimeScaleInternal(GameSpeed.drag);
        }

        /// <summary>
        /// 结束拖拽慢放（恢复到选择的倍速）
        /// </summary>
        public void EndDragSlowMotion()
        {
            if (isPaused) return;
            ResetTimeScale();
        }

        /// <summary>
        /// 重置 TimeScale 为 1（场景切换时调用）
        /// </summary>
        public void ResetTimeScale()
        {
            isPaused = false;
            // 根据当前场景决定恢复的倍速
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "Battle")
            {
                // 战斗场景：恢复到玩家选择的倍速
                SetTimeScaleInternal(selectedTimeScale);
            }
            else
            {
                // 其他场景：恢复到默认速度1
                SetTimeScaleInternal(GameSpeed.normal);
            }
        }

        public float getSelectedTimeScale() {
            return selectedTimeScale;
        }
    }
}
