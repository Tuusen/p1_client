using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometryTD
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string SaveKeyCompletedLevels = "CompletedLevels";

        private int selectedLevelId;
        private HashSet<int> completedLevels = new HashSet<int>();

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

            LevelConfig levelConfig = ConfigManager.Instance.GetLevelConfig(levelId);
            if (levelConfig == null) return false;

            if (levelConfig.conditions == null || levelConfig.conditions.Length == 0)
                return true;

            foreach (int condId in levelConfig.conditions)
            {
                ConditionConfig cond = ConfigManager.Instance.GetConditionConfig(condId);
                if (cond == null) return false;

                if (cond.type == 1)
                {
                    if (!IsLevelCompleted(cond.p1))
                        return false;
                }
            }

            return true;
        }

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
