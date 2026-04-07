using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// 故事集存档管理器。
    /// 负责：运行时中途存档（StorySaveData）、永久进度存档（StoryProgressData）。
    /// 使用 PlayerPrefs + JsonUtility 序列化，与项目现有存档方式一致。
    /// </summary>
    public class StorySaveManager
    {
        // PlayerPrefs 键前缀
        private const string SaveKeyPrefix = "StorySave_";          // 运行时存档: StorySave_{collectionId}
        private const string ProgressKey = "StoryProgress";         // 永久进度

        private static StorySaveManager instance;
        public static StorySaveManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new StorySaveManager();
                return instance;
            }
        }

        // 内存中缓存的永久进度
        private StoryProgressSaveData progressCache;

        // ===== 运行时存档（中途存档/继续冒险） =====

        /// <summary>保存运行时状态（每次做出选择后调用）</summary>
        public void SaveRuntime(StoryRuntime runtime)
        {
            if (runtime == null) return;

            StorySaveData saveData = new StorySaveData
            {
                runtime = runtime,
                saveTimeTicks = System.DateTime.UtcNow.Ticks
            };

            string json = JsonUtility.ToJson(saveData);
            string key = GetSaveKey(runtime.collectionId);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        /// <summary>读取运行时存档，返回null表示无存档</summary>
        public StorySaveData LoadRuntime(int collectionId)
        {
            string key = GetSaveKey(collectionId);
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json))
                return null;

            StorySaveData saveData = JsonUtility.FromJson<StorySaveData>(json);
            return saveData;
        }

        /// <summary>检查是否有运行时存档</summary>
        public bool HasRuntimeSave(int collectionId)
        {
            string key = GetSaveKey(collectionId);
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>删除运行时存档（冒险结束或玩家主动重置时调用）</summary>
        public void DeleteRuntimeSave(int collectionId)
        {
            string key = GetSaveKey(collectionId);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        /// <summary>创建全新的运行时状态</summary>
        public StoryRuntime CreateNewRuntime(int collectionId)
        {
            StoryCollectionConfig config = Cfg.StoryCollection.Get(collectionId);
            if (config == null)
            {
                Debug.LogError($"[StorySaveManager] 创建运行时失败，未找到故事集配置: {collectionId}");
                return null;
            }

            StoryRuntime runtime = new StoryRuntime
            {
                collectionId = collectionId,
                currentNodeId = config.startNodeId,
                gold = 0,
                ownedEffectIds = new List<int>(),
                choiceRecords = new List<NodeChoiceRecord>(),
                visitedNodeIds = new List<int>(),
                previousNodeId = 0
            };

            runtime.visitedNodeIds.Add(config.startNodeId);
            return runtime;
        }

        // ===== 永久进度（已解锁结局等） =====

        /// <summary>获取某故事集的永久进度</summary>
        public StoryProgressData GetProgress(int collectionId)
        {
            LoadProgressIfNeeded();

            for (int i = 0; i < progressCache.progressList.Count; i++)
            {
                if (progressCache.progressList[i].collectionId == collectionId)
                    return progressCache.progressList[i];
            }

            // 首次访问，创建空进度
            StoryProgressData newProgress = new StoryProgressData
            {
                collectionId = collectionId,
                unlockedEndingIds = new List<int>()
            };
            progressCache.progressList.Add(newProgress);
            return newProgress;
        }

        /// <summary>解锁结局并保存永久进度，返回是否为新解锁</summary>
        public bool UnlockEnding(int collectionId, int endingNodeId)
        {
            StoryProgressData progress = GetProgress(collectionId);
            bool isNew = progress.UnlockEnding(endingNodeId);
            if (isNew)
                SaveProgress();
            return isNew;
        }

        /// <summary>获取完成度百分比</summary>
        public float GetCompletionRate(int collectionId)
        {
            StoryCollectionConfig config = Cfg.StoryCollection.Get(collectionId);
            StoryProgressData progress = GetProgress(collectionId);
            return progress.GetCompletionRate(config);
        }

        /// <summary>保存永久进度到 PlayerPrefs</summary>
        public void SaveProgress()
        {
            LoadProgressIfNeeded();
            string json = JsonUtility.ToJson(progressCache);
            PlayerPrefs.SetString(ProgressKey, json);
            PlayerPrefs.Save();
        }

        // ===== 内部方法 =====

        private string GetSaveKey(int collectionId)
        {
            return SaveKeyPrefix + collectionId;
        }

        private void LoadProgressIfNeeded()
        {
            if (progressCache != null) return;

            string json = PlayerPrefs.GetString(ProgressKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                progressCache = JsonUtility.FromJson<StoryProgressSaveData>(json);
            }

            if (progressCache == null)
            {
                progressCache = new StoryProgressSaveData
                {
                    progressList = new List<StoryProgressData>()
                };
            }
        }
    }
}
