using UnityEngine;
using System.Collections.Generic;

namespace GeometryTD
{
    /// <summary>
    /// 视觉加载器：根据 RoleConfig 动态替换槽位 Sprite 和 Animator
    /// 挂载到 Unit 预制体的 VisualRoot 节点上
    /// </summary>
    public class VisualLoader : MonoBehaviour
    {
        [Header("槽位节点引用（自动查找）")]
        [Tooltip("如果不手动指定，会自动查找所有子物体的 SpriteRenderer")]
        [SerializeField] private List<SpriteRenderer> slotRenderers;

        [Header("调试信息")]
        [SerializeField] private int currentRoleId;

        private Animator animator;
        private Dictionary<int, SpriteRenderer> slotMap = new Dictionary<int, SpriteRenderer>();

        private void Awake()
        {
            // 自动收集所有槽位
            CollectSlots();
            
            // 获取 Animator（从父物体或自身）
            animator = GetComponentInParent<Animator>();
        }

        /// <summary>
        /// 根据 RoleConfig 加载视觉资源
        /// </summary>
        public void LoadVisual(RoleConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[VisualLoader] RoleConfig 为空，跳过视觉加载");
                return;
            }

            currentRoleId = config.id;

            // 1. 加载自定义 Animator（如果有配置）
            if (!string.IsNullOrEmpty(config.animatorPath))
            {
                LoadAnimator(config.animatorPath);
            }

            // 2. 替换槽位 Sprite
            if (config.sprite_set != null && config.sprite_set.Length > 0)
            {
                ReplaceSlotSprites(config.sprite_set);
            }
        }

        /// <summary>
        /// 加载自定义 AnimatorController
        /// </summary>
        private void LoadAnimator(string animatorPath)
        {
            if (animator == null)
            {
                Debug.LogWarning("[VisualLoader] 未找到 Animator 组件，无法加载动画控制器");
                return;
            }

            RuntimeAnimatorController customAnimator = GameHelper.LoadAnimator(animatorPath, "Characters");
            if (customAnimator != null)
            {
                animator.runtimeAnimatorController = customAnimator;
            }
            else
            {
                Debug.LogWarning($"[VisualLoader] 无法加载 Animator: {animatorPath}，使用默认 Animator");
            }
        }

        /// <summary>
        /// 替换槽位 Sprite
        /// </summary>
        private void ReplaceSlotSprites(RoleConfig.Sprite_setItem[] sprite_set)
        {
            // 构建配置映射
            Dictionary<int, string> configMap = new Dictionary<int, string>();
            foreach (var entry in sprite_set)
            {
                if (!string.IsNullOrEmpty(entry.spritePath))
                {
                    configMap[entry.slotType] = entry.spritePath;
                }
            }

            // 遍历所有槽位进行替换
            foreach (var kvp in slotMap)
            {
                int slotType = kvp.Key;
                SpriteRenderer renderer = kvp.Value;

                if (configMap.TryGetValue(slotType, out string spritePath))
                {
                    // 配置中有该槽位，尝试加载 Sprite
                    Sprite newSprite = GameHelper.LoadSprite(spritePath, "Characters");
                    if (newSprite != null)
                    {
                        renderer.sprite = newSprite;
                        renderer.gameObject.SetActive(true);
                    }
                    else
                    {
                        // 资源不存在，隐藏该槽位（兜底机制）
                        Debug.LogWarning($"[VisualLoader] 找不到 Sprite: {spritePath} (slotType={slotType})");
                        renderer.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // 配置中没有该槽位，隐藏（兜底机制）
                    renderer.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 自动收集所有槽位 SpriteRenderer
        /// 规则：查找所有子物体中名为 "{slotType}_Slot" 或直接使用索引映射
        /// </summary>
        private void CollectSlots()
        {
            slotMap.Clear();

            if (slotRenderers != null && slotRenderers.Count > 0)
            {
                // 如果手动指定了槽位，使用手动配置
                for (int i = 0; i < slotRenderers.Count; i++)
                {
                    if (slotRenderers[i] != null)
                    {
                        slotMap[i] = slotRenderers[i];
                    }
                }
            }
            else
            {
                // 自动查找：通过节点名称解析 slotType
                // 命名规范：{slotType}_Slot 或 Slot_{slotType}
                var allRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
                
                foreach (var renderer in allRenderers)
                {
                    string nodeName = renderer.gameObject.name;
                    int slotType = ParseSlotType(nodeName);
                    
                    if (slotType >= 0)
                    {
                        slotMap[slotType] = renderer;
                    }
                }
            }
        }

        /// <summary>
        /// 从节点名称解析 slotType
        /// 支持格式："0_Slot", "Slot_1", "body_Slot"(需要映射表)
        /// </summary>
        private int ParseSlotType(string nodeName)
        {
            // 尝试直接解析数字前缀
            string[] parts = nodeName.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[0], out int slotType))
            {
                return slotType;
            }

            // 尝试解析数字后缀
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out slotType))
            {
                return slotType;
            }

            // 如果命名不符合规范，返回 -1（跳过）
            Debug.LogWarning($"[VisualLoader] 无法从节点名称解析 slotType: {nodeName}");
            return -1;
        }

        /// <summary>
        /// 手动注册槽位（适用于动态生成的情况）
        /// </summary>
        public void RegisterSlot(int slotType, SpriteRenderer renderer)
        {
            if (renderer != null)
            {
                slotMap[slotType] = renderer;
            }
        }

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void ClearSlots()
        {
            slotMap.Clear();
        }
    }
}
