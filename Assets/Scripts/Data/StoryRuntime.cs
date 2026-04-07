using System;
using System.Collections.Generic;

namespace GeometryTD
{
    /// <summary>
    /// 故事集运行时状态，存在于一次冒险过程中（从"开始冒险"到结局/退出）。
    /// 此对象同时作为存档的内存数据，需要能完整序列化/反序列化。
    /// </summary>
    [Serializable]
    public class StoryRuntime
    {
        /// <summary>当前故事集 ID</summary>
        public int collectionId;

        /// <summary>当前所在节点 ID</summary>
        public int currentNodeId;

        /// <summary>局内金币（仅本次冒险有效）</summary>
        public int gold;

        /// <summary>已获得的藏品效果 ID 列表（含重复，表示叠加层数）</summary>
        public List<int> ownedEffectIds = new List<int>();

        /// <summary>
        /// 每个节点的选择记录。
        /// Key = 节点ID，Value = 该节点的每个选项组的选择索引（1-based）。
        /// 例如节点1001有2个boss，玩家分别选了第1个和第3个选项 → {1001, [1,3]}
        /// </summary>
        public List<NodeChoiceRecord> choiceRecords = new List<NodeChoiceRecord>();

        /// <summary>已访问过的节点 ID 列表（按顺序）</summary>
        public List<int> visitedNodeIds = new List<int>();

        /// <summary>上一个节点 ID（用于过渡动画，0表示无）</summary>
        public int previousNodeId;

        // ===== 运行时辅助方法（不序列化） =====

        /// <summary>记录某节点的一次选项选择</summary>
        public void RecordChoice(int nodeId, int choiceIndex)
        {
            NodeChoiceRecord record = FindRecord(nodeId);
            if (record == null)
            {
                record = new NodeChoiceRecord { nodeId = nodeId, choices = new List<int>() };
                choiceRecords.Add(record);
            }
            record.choices.Add(choiceIndex);
        }

        /// <summary>获取某节点已做的所有选择</summary>
        public int[] GetChoicesForNode(int nodeId)
        {
            NodeChoiceRecord record = FindRecord(nodeId);
            if (record != null)
                return record.choices.ToArray();
            return new int[0];
        }

        /// <summary>添加藏品效果</summary>
        public void AddEffect(int effectId)
        {
            if (effectId <= 0) return;

            PassiveEffectConfig config = Cfg.PassiveEffect.Get(effectId);
            if (config == null) return;

            if (!config.stackable)
            {
                // 不可叠加：已有则跳过
                if (ownedEffectIds.Contains(effectId))
                    return;
            }
            else
            {
                // 可叠加：检查是否超过最大层数
                int currentCount = 0;
                for (int i = 0; i < ownedEffectIds.Count; i++)
                {
                    if (ownedEffectIds[i] == effectId)
                        currentCount++;
                }
                if (currentCount >= config.maxStack)
                    return;
            }

            ownedEffectIds.Add(effectId);
        }

        /// <summary>添加金币</summary>
        public void AddGold(int amount)
        {
            if (amount > 0)
                gold += amount;
        }

        /// <summary>消费金币，成功返回true</summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (gold < amount) return false;
            gold -= amount;
            return true;
        }

        /// <summary>标记访问节点并更新当前节点</summary>
        public void MoveToNode(int nodeId)
        {
            previousNodeId = currentNodeId;
            currentNodeId = nodeId;
            if (!visitedNodeIds.Contains(nodeId))
                visitedNodeIds.Add(nodeId);
        }

        /// <summary>
        /// 根据当前节点的选择记录，匹配 nextNodes 条件，返回下一个节点 ID。
        /// 如果无法匹配则返回 defaultNextNodeId，仍然无法匹配返回 0。
        /// </summary>
        public int ResolveNextNodeId(StoryNodeConfig currentNode)
        {
            if (currentNode == null) return 0;

            // 商店等无选项节点，直接用 defaultNextNodeId
            if (currentNode.nextNodes == null || currentNode.nextNodes.Length == 0)
                return currentNode.defaultNextNodeId;

            int[] playerChoices = GetChoicesForNode(currentNode.id);

            // 找到最匹配的条目（非0条件越多，越精确）
            int bestNodeId = 0;
            int bestScore = -1;

            for (int i = 0; i < currentNode.nextNodes.Length; i++)
            {
                StoryNodeConfig.NextNodesItem entry = currentNode.nextNodes[i];
                if (entry.conditions == null) continue;

                bool match = true;
                int score = 0;

                for (int j = 0; j < entry.conditions.Length; j++)
                {
                    int required = entry.conditions[j];
                    if (required == 0)
                        continue; // 通配符，任意匹配

                    // 检查玩家是否做了这个选项组的选择
                    if (j < playerChoices.Length && playerChoices[j] == required)
                    {
                        score++; // 精确匹配加分
                    }
                    else
                    {
                        match = false;
                        break;
                    }
                }

                if (match && score > bestScore)
                {
                    bestScore = score;
                    bestNodeId = entry.nodeId;
                }
            }

            // 如果没有精确匹配，寻找全0保底路径
            if (bestNodeId == 0)
            {
                for (int i = 0; i < currentNode.nextNodes.Length; i++)
                {
                    StoryNodeConfig.NextNodesItem entry = currentNode.nextNodes[i];
                    if (entry.conditions == null) continue;

                    bool allZero = true;
                    for (int j = 0; j < entry.conditions.Length; j++)
                    {
                        if (entry.conditions[j] != 0)
                        {
                            allZero = false;
                            break;
                        }
                    }
                    if (allZero)
                    {
                        bestNodeId = entry.nodeId;
                        break;
                    }
                }
            }

            return bestNodeId > 0 ? bestNodeId : currentNode.defaultNextNodeId;
        }

        private NodeChoiceRecord FindRecord(int nodeId)
        {
            for (int i = 0; i < choiceRecords.Count; i++)
            {
                if (choiceRecords[i].nodeId == nodeId)
                    return choiceRecords[i];
            }
            return null;
        }
    }

    /// <summary>
    /// 单个节点的选择记录
    /// </summary>
    [Serializable]
    public class NodeChoiceRecord
    {
        /// <summary>节点 ID</summary>
        public int nodeId;

        /// <summary>该节点中每个选项组的选择结果（1-based索引，按顺序追加）</summary>
        public List<int> choices = new List<int>();
    }

    /// <summary>
    /// 故事集永久进度数据（跨冒险持久化）。
    /// 记录已解锁结局等成就性信息，不随冒险结束清除。
    /// </summary>
    [Serializable]
    public class StoryProgressData
    {
        /// <summary>故事集 ID</summary>
        public int collectionId;

        /// <summary>已解锁的结局节点 ID 列表</summary>
        public List<int> unlockedEndingIds = new List<int>();

        /// <summary>解锁结局</summary>
        public bool UnlockEnding(int endingNodeId)
        {
            if (unlockedEndingIds.Contains(endingNodeId))
                return false;
            unlockedEndingIds.Add(endingNodeId);
            return true;
        }

        /// <summary>检查结局是否已解锁</summary>
        public bool IsEndingUnlocked(int endingNodeId)
        {
            return unlockedEndingIds.Contains(endingNodeId);
        }

        /// <summary>计算完成度百分比（已解锁结局数 / 总结局数）</summary>
        public float GetCompletionRate(StoryCollectionConfig collectionConfig)
        {
            if (collectionConfig == null || collectionConfig.endingNodeIds == null
                || collectionConfig.endingNodeIds.Length == 0)
                return 0f;

            int total = collectionConfig.endingNodeIds.Length;
            int unlocked = 0;
            for (int i = 0; i < collectionConfig.endingNodeIds.Length; i++)
            {
                if (unlockedEndingIds.Contains(collectionConfig.endingNodeIds[i]))
                    unlocked++;
            }
            return (float)unlocked / total;
        }
    }

    /// <summary>
    /// 故事集存档的顶层包装，用于 JSON 序列化存入 PlayerPrefs。
    /// 包含运行时状态（中途存档）。
    /// </summary>
    [Serializable]
    public class StorySaveData
    {
        /// <summary>冒险运行时快照</summary>
        public StoryRuntime runtime;

        /// <summary>存档时间戳（UTC ticks）</summary>
        public long saveTimeTicks;
    }

    /// <summary>
    /// 所有故事集的永久进度汇总，用于 JSON 序列化存入 PlayerPrefs。
    /// </summary>
    [Serializable]
    public class StoryProgressSaveData
    {
        public List<StoryProgressData> progressList = new List<StoryProgressData>();
    }
}
