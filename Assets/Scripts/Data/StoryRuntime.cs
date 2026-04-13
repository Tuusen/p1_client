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

            PassiveEffectConfig newConfig = Cfg.PassiveEffect.Get(effectId);
            if (newConfig == null) return;

            // 获取同类型的所有藏品
            List<int> sameTypeIndices = new List<int>();
            for (int i = 0; i < ownedEffectIds.Count; i++)
            {
                PassiveEffectConfig existingConfig = Cfg.PassiveEffect.Get(ownedEffectIds[i]);
                if (existingConfig != null && existingConfig.stackType == newConfig.stackType)
                {
                    sameTypeIndices.Add(i);
                }
            }

            // 如果未达到maxStack限制，直接添加
            if (sameTypeIndices.Count < newConfig.maxStack)
            {
                ownedEffectIds.Add(effectId);
                return;
            }

            // 达到上限时，替换color和level最低的藏品
            // 优先生效color高的，color相同判断level高的
            int worstIndex = -1;
            int worstScore = int.MinValue;

            for (int i = 0; i < sameTypeIndices.Count; i++)
            {
                int index = sameTypeIndices[i];
                PassiveEffectConfig existingConfig = Cfg.PassiveEffect.Get(ownedEffectIds[index]);
                if (existingConfig == null) continue;

                // 计算优先级分数: color * 100 + level，分数越高越优先
                int score = existingConfig.color * 100 + existingConfig.level;
                if (score < worstScore)
                {
                    worstScore = score;
                    worstIndex = index;
                }
            }

            // 替换最低优先级的藏品
            if (worstIndex >= 0)
            {
                ownedEffectIds[worstIndex] = effectId;
            }
        }

        /// <summary>
        /// 获取当前生效的藏品列表(已根据maxStack过滤)
        /// </summary>
        public List<PassiveEffectConfig> GetActiveEffects()
        {
            List<PassiveEffectConfig> result = new List<PassiveEffectConfig>();
            if (ownedEffectIds.Count == 0) return result;

            // 按stackType分组处理
            Dictionary<int, List<PassiveEffectEntry>> grouped = new Dictionary<int, List<PassiveEffectEntry>>();

            for (int i = 0; i < ownedEffectIds.Count; i++)
            {
                PassiveEffectConfig config = Cfg.PassiveEffect.Get(ownedEffectIds[i]);
                if (config == null) continue;

                if (!grouped.ContainsKey(config.stackType))
                    grouped[config.stackType] = new List<PassiveEffectEntry>();

                grouped[config.stackType].Add(new PassiveEffectEntry
                {
                    index = i,
                    config = config
                });
            }

            // 对每个类型按优先级排序，保留前maxStack个
            foreach (var kvp in grouped)
            {
                List<PassiveEffectEntry> entries = kvp.Value;
                PassiveEffectConfig sampleConfig = entries[0].config;
                int maxStack = sampleConfig.maxStack;

                // 按color和level降序排序
                entries.Sort((a, b) =>
                {
                    int scoreA = a.config.color * 100 + a.config.level;
                    int scoreB = b.config.color * 100 + b.config.level;
                    return scoreB.CompareTo(scoreA);
                });

                // 只保留前maxStack个
                if (maxStack == -1)
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        result.Add(entries[i].config);
                    }
                } else {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (i < maxStack)
                        {
                            result.Add(entries[i].config);
                        }
                    }
                }
            }

            return result;
        }

        private class PassiveEffectEntry
        {
            public int index;
            public PassiveEffectConfig config;
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
