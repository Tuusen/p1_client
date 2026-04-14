using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometryTD
{
    /// <summary>
    /// 故事集核心管理器（单例，跨场景持久化）。
    /// 管理故事集的完整生命周期：开始/继续/推进节点/结束冒险。
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        private static StoryManager _instance;
        public static StoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("StoryManager");
                    _instance = obj.AddComponent<StoryManager>();
                }
                return _instance;
            }
        }

        /// <summary>当前冒险的运行时状态，null表示未在冒险中</summary>
        public StoryRuntime Runtime { get; private set; }

        /// <summary>是否正在故事集冒险中</summary>
        public bool IsInAdventure => Runtime != null;

        /// <summary>当前节点配置（便捷访问）</summary>
        public StoryNodeConfig CurrentNode
        {
            get
            {
                if (Runtime == null) return null;
                return Cfg.StoryNode.Get(Runtime.currentNodeId);
            }
        }

        /// <summary>当前故事集配置（便捷访问）</summary>
        public StoryCollectionConfig CurrentCollection
        {
            get
            {
                if (Runtime == null) return null;
                return Cfg.StoryCollection.Get(Runtime.collectionId);
            }
        }

        // ===== 事件回调 =====

        /// <summary>节点切换事件：参数为(旧节点ID, 新节点ID)</summary>
        public event Action<int, int> OnNodeChanged;

        /// <summary>金币变化事件：参数为当前金币数</summary>
        public event Action<int> OnGoldChanged;

        /// <summary>藏品获得事件：参数为藏品效果ID</summary>
        public event Action<int> OnEffectAcquired;

        /// <summary>冒险结束事件：参数为(结局类型, 结局节点ID, 是否为新解锁)</summary>
        public event Action<int, int, bool> OnAdventureEnded;

        /// <summary>当前战斗中Boss的索引（boss死亡事件用）</summary>
        private int currentBossEventIndex;

        /// <summary>战斗失败前的节点ID（用于"重试"功能）</summary>
        private int failedAtNodeId;

        /// <summary>是否需要播放节点过渡动画（瞬态，不序列化）</summary>
        private bool shouldPlayTransition;
        public bool ShouldPlayTransition => shouldPlayTransition;

        public void ClearTransitionFlag()
        {
            shouldPlayTransition = false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ===== 冒险生命周期 =====

        /// <summary>开始新冒险</summary>
        public void StartNewAdventure(int collectionId)
        {
            // 删除旧的中途存档
            StorySaveManager.Instance.DeleteRuntimeSave(collectionId);

            Runtime = StorySaveManager.Instance.CreateNewRuntime(collectionId);
            if (Runtime == null)
            {
                Debug.LogError($"[StoryManager] 无法创建运行时, collectionId: {collectionId}");
                return;
            }

            currentBossEventIndex = 0;
            failedAtNodeId = 0;

            // 保存初始状态
            StorySaveManager.Instance.SaveRuntime(Runtime);
        }

        /// <summary>继续冒险（从存档恢复）</summary>
        public bool ContinueAdventure(int collectionId)
        {
            StorySaveData saveData = StorySaveManager.Instance.LoadRuntime(collectionId);
            if (saveData == null || saveData.runtime == null)
            {
                Debug.LogWarning($"[StoryManager] 无存档可继续, collectionId: {collectionId}");
                return false;
            }

            Runtime = saveData.runtime;
            currentBossEventIndex = 0;
            failedAtNodeId = 0;
            return true;
        }

        /// <summary>结束冒险（到达结局或玩家退出）</summary>
        public void EndAdventure(int endingNodeId)
        {
            if (Runtime == null) return;

            int collectionId = Runtime.collectionId;

            // 如果是结局节点，解锁结局
            bool isNewEnding = false;
            StoryNodeConfig endingNode = Cfg.StoryNode.Get(endingNodeId);
            int endingType = EndingType.None;
            if (endingNode != null && endingNode.endingType != EndingType.None)
            {
                endingType = endingNode.endingType;
                isNewEnding = StorySaveManager.Instance.UnlockEnding(collectionId, endingNodeId);
            }

            // 清除中途存档
            StorySaveManager.Instance.DeleteRuntimeSave(collectionId);

            OnAdventureEnded?.Invoke(endingType, endingNodeId, isNewEnding);

            Runtime = null;
        }

        /// <summary>放弃冒险（中途退出但保留存档）</summary>
        public void AbandonAdventure()
        {
            if (Runtime == null) return;
            StorySaveManager.Instance.SaveRuntime(Runtime);
            Runtime = null;
        }

        // ===== 节点推进 =====

        /// <summary>
        /// 处理节点完成后的推进。
        /// 根据当前节点的选择记录和 nextNodes 条件匹配下一个节点。
        /// </summary>
        public void AdvanceToNextNode()
        {
            if (Runtime == null) return;

            StoryNodeConfig currentNode = CurrentNode;
            if (currentNode == null) return;

            int nextNodeId = Runtime.ResolveNextNodeId(currentNode);
            if (nextNodeId <= 0)
            {
                Debug.LogError($"[StoryManager] 无法解析下一节点, 当前节点: {currentNode.id}");
                return;
            }

            MoveToNode(nextNodeId);
        }

        /// <summary>直接跳转到指定节点（事件触发战斗时使用）</summary>
        public void JumpToNode(int nodeId)
        {
            if (Runtime == null) return;
            MoveToNode(nodeId);
        }

        /// <summary>战斗失败时跳转到失败结局节点</summary>
        public void HandleBattleFailed()
        {
            if (Runtime == null) return;

            StoryNodeConfig currentNode = CurrentNode;
            failedAtNodeId = Runtime.currentNodeId;

            int failNodeId = 0;
            if (currentNode != null && currentNode.failNodeId > 0)
            {
                failNodeId = currentNode.failNodeId;
            }

            if (failNodeId > 0)
            {
                MoveToNode(failNodeId);
            }
            else
            {
                Debug.LogWarning("[StoryManager] 战斗失败但无失败节点配置");
            }
        }

        /// <summary>
        /// 从失败结局重试（回到失败前的关卡）。
        /// 清除该节点的选择记录，以便重新挑战。
        /// </summary>
        public bool RetryFromFailure()
        {
            if (Runtime == null || failedAtNodeId <= 0) return false;

            // 移除失败节点的访问记录
            StoryNodeConfig failNode = CurrentNode;
            if (failNode != null && failNode.endingType == EndingType.Fail)
            {
                Runtime.visitedNodeIds.Remove(failNode.id);
            }

            // 回到失败前的节点，清除该节点的选择记录以便重新挑战
            ClearNodeChoices(failedAtNodeId);
            Runtime.currentNodeId = failedAtNodeId;
            currentBossEventIndex = 0;

            StorySaveManager.Instance.SaveRuntime(Runtime);
            failedAtNodeId = 0;
            return true;
        }

        private void MoveToNode(int nodeId)
        {
            int oldNodeId = Runtime.currentNodeId;
            Runtime.MoveToNode(nodeId);
            currentBossEventIndex = 0;

            StorySaveManager.Instance.SaveRuntime(Runtime);
            shouldPlayTransition = true;
            OnNodeChanged?.Invoke(oldNodeId, nodeId);
        }

        private void ClearNodeChoices(int nodeId)
        {
            for (int i = Runtime.choiceRecords.Count - 1; i >= 0; i--)
            {
                if (Runtime.choiceRecords[i].nodeId == nodeId)
                {
                    Runtime.choiceRecords.RemoveAt(i);
                    break;
                }
            }
        }

        // ===== 选择处理 =====

        /// <summary>
        /// 处理玩家的选项选择。
        /// 记录选择、发放奖励（藏品/金币），自动保存。
        /// </summary>
        /// <param name="choiceOptionIndex">选择的选项在选项组中的索引（1-based）</param>
        /// <param name="choiceOption">选中的选项配置</param>
        public void ProcessChoice(int choiceOptionIndex, ChoiceConfig choiceOption)
        {
            if (Runtime == null || choiceOption == null) return;

            // 记录选择
            Runtime.RecordChoice(Runtime.currentNodeId, choiceOptionIndex);

            // 发放藏品
            if (choiceOption.effectId > 0)
            {
                Runtime.AddEffect(choiceOption.effectId);
                OnEffectAcquired?.Invoke(choiceOption.effectId);
            }

            // 发放金币
            if (choiceOption.goldReward > 0)
            {
                Runtime.AddGold(choiceOption.goldReward);
                OnGoldChanged?.Invoke(Runtime.gold);
            }

            StorySaveManager.Instance.SaveRuntime(Runtime);
        }

        // ===== Boss 死亡事件 =====

        /// <summary>
        /// 获取当前 Boss 死亡后应触发的对话和选项。
        /// 自动推进 bossEventIndex。
        /// 返回 null 表示该 Boss 没有配置事件。
        /// </summary>
        public StoryNodeConfig.BossEventsItem GetCurrentBossEvent()
        {
            if (Runtime == null) return null;

            StoryNodeConfig node = CurrentNode;
            if (node == null || node.bossEvents == null || node.bossEvents.Length == 0)
                return null;

            if (currentBossEventIndex >= node.bossEvents.Length)
                return null;

            StoryNodeConfig.BossEventsItem entry = node.bossEvents[currentBossEventIndex];
            currentBossEventIndex++;
            return entry;
        }

        /// <summary>重置 Boss 事件索引（进入新战斗节点时调用）</summary>
        public void ResetBossEventIndex()
        {
            currentBossEventIndex = 0;
        }

        // ===== 金币系统 =====

        /// <summary>战斗中增加金币（击杀怪物时调用）</summary>
        public void AddBattleGold(int amount)
        {
            if (Runtime == null || amount <= 0) return;

            // 应用金币加成藏品效果
            float bonus = GetSpecialEffectValue(SpecialEffectType.GoldBonus);
            if (bonus > 0)
            {
                amount = Mathf.RoundToInt(amount * (1f + bonus / 100f));
            }

            Runtime.AddGold(amount);
            OnGoldChanged?.Invoke(Runtime.gold);
        }

        /// <summary>商店消费金币</summary>
        public bool SpendGold(int amount)
        {
            if (Runtime == null) return false;
            bool success = Runtime.SpendGold(amount);
            if (success)
                OnGoldChanged?.Invoke(Runtime.gold);
            return success;
        }

        /// <summary>商店购买藏品</summary>
        public bool PurchaseEffect(int effectId, int price)
        {
            if (!SpendGold(price)) return false;

            Runtime.AddEffect(effectId);
            OnEffectAcquired?.Invoke(effectId);
            StorySaveManager.Instance.SaveRuntime(Runtime);
            return true;
        }

        // ===== 藏品效果查询 =====

        /// <summary>
        /// 获取属性加成总值（通过passive实现）。
        /// 返回 (百分比加成总和, 固定值加成总和)。
        /// </summary>
        public (float percentBonus, float flatBonus) GetAttributeBonus(int attrId)
        {
            // 属性加成现在由被动技能系统直接处理
            // 此方法保留用于兼容，实际属性计算在被动技能激活时进行
            return (0f, 0f);
        }

        /// <summary>
        /// 获取技能增强总值（通过passive实现）。
        /// </summary>
        public float GetSkillEnhanceValue(int enhanceType)
        {
            // 技能增强现在由被动技能系统直接处理
            return 0f;
        }

        /// <summary>
        /// 获取特殊效果总值（通过passive实现）。
        /// </summary>
        public float GetSpecialEffectValue(int specialType)
        {
            // 特殊效果现在由被动技能系统直接处理
            return 0f;
        }

        /// <summary>
        /// 获取当前战斗生效的所有passive技能ID列表
        /// </summary>
        public List<int> GetActivePassiveIds()
        {
            List<int> result = new List<int>();
            if (Runtime == null) return result;

            List<PassiveEffectConfig> activeEffects = Runtime.GetActiveEffects();
            for (int i = 0; i < activeEffects.Count; i++)
            {
                PassiveEffectConfig config = activeEffects[i];
                if (config == null || config.passives == null) continue;

                for (int j = 0; j < config.passives.Length; j++)
                {
                    if (config.passives[j] > 0 && !result.Contains(config.passives[j]))
                    {
                        result.Add(config.passives[j]);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取当前战斗生效的金币加成(万分比)
        /// </summary>
        public int GetGoldBonus()
        {
            if (Runtime == null) return 0;

            int totalBonus = 0;
            List<PassiveEffectConfig> activeEffects = Runtime.GetActiveEffects();
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i] != null)
                    totalBonus += activeEffects[i].addGold;
            }
            return totalBonus;
        }

        /// <summary>
        /// 获取所有已拥有的藏品配置列表（用于UI展示，包含所有未筛选的藏品）
        /// </summary>
        public List<PassiveEffectConfig> GetOwnedEffects()
        {
            List<PassiveEffectConfig> result = new List<PassiveEffectConfig>();
            if (Runtime == null) return result;

            for (int i = 0; i < Runtime.ownedEffectIds.Count; i++)
            {
                PassiveEffectConfig config = Cfg.PassiveEffect.Get(Runtime.ownedEffectIds[i]);
                if (config != null)
                    result.Add(config);
            }
            return result;
        }

        /// <summary>
        /// 获取当前战斗中生效的藏品配置列表（用于UI展示）
        /// </summary>
        public List<PassiveEffectConfig> GetActiveEffects()
        {
            if (Runtime == null) return new List<PassiveEffectConfig>();
            return Runtime.GetActiveEffects();
        }

        /// <summary>
        /// 获取所有已拥有藏品的数量
        /// </summary>
        public int GetOwnedEffectsCount()
        {
            return Runtime != null ? Runtime.ownedEffectIds.Count : 0;
        }

        /// <summary>
        /// 获取开局额外护盾值（特殊效果：开局护盾）
        /// </summary>
        public float GetStartShieldBonus()
        {
            return GetSpecialEffectValue(SpecialEffectType.StartShield);
        }

        // ===== 场景管理 =====

        /// <summary>加载战斗场景</summary>
        public void EnterBattleScene(int levelId)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectLevel(levelId);
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("Battle");
        }

        /// <summary>加载事件场景（结局/商店/休息节点共用）</summary>
        public void EnterEventScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Event");
        }

        /// <summary>返回故事集场景</summary>
        public void EnterStoryScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Story");
        }

        /// <summary>返回主菜单</summary>
        public void BackToMainMenu()
        {
            if (Runtime != null)
            {
                AbandonAdventure();
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// 根据当前节点类型执行对应行为。
        /// 由故事集场景中的UI调用。
        /// </summary>
        public void ExecuteCurrentNode()
        {
            StoryNodeConfig node = CurrentNode;
            if (node == null) return;

            switch (node.type)
            {
                case StoryNodeType.Battle:
                    ResetBossEventIndex();
                    EnterBattleScene(node.levelId);
                    break;

                case StoryNodeType.Event:
                case StoryNodeType.Ending:
                    EnterEventScene();
                    break;

                case StoryNodeType.Shop:
                    EnterEventScene();
                    break;
            }
        }

        // ===== 工具方法 =====

        /// <summary>获取当前金币数量</summary>
        public int GetGold()
        {
            return Runtime != null ? Runtime.gold : 0;
        }

        /// <summary>检查某故事集是否有存档</summary>
        public bool HasSave(int collectionId)
        {
            return StorySaveManager.Instance.HasRuntimeSave(collectionId);
        }

        /// <summary>获取某故事集的完成度</summary>
        public float GetCompletionRate(int collectionId)
        {
            return StorySaveManager.Instance.GetCompletionRate(collectionId);
        }

        /// <summary>获取某故事集的永久进度</summary>
        public StoryProgressData GetProgress(int collectionId)
        {
            return StorySaveManager.Instance.GetProgress(collectionId);
        }
    }
}
