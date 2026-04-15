using UnityEngine;
using System.Collections;

namespace GeometryTD
{
    /// <summary>
    /// 管理 event_effect_config.json 驱动的视觉特效。
    /// 支持两种模式：
    /// 1. 持续挂载特效 - 跟随目标移动，随目标销毁而销毁
    /// 2. 一次性特效 - 播放后自动销毁
    /// </summary>
    public class EventEffectManager : MonoBehaviour
    {
        /// <summary>
        /// 触发一次性特效（播放后自动销毁）
        /// </summary>
        public void TriggerOneShotEffect(int eventType, Vector3 position, float scale = 1f)
        {
            if (eventType <= 0) return;
            var config = Cfg.EventEffect.Get(eventType);
            if (config == null)
            {
                Debug.LogWarning($"[EventEffect] 未找到 eventType={eventType} 的特效配置");
                return;
            }

            StartCoroutine(SpawnOneShotEffectCoroutine(config, position, scale));
        }

        /// <summary>
        /// 触发特效并获取生成的 GameObject
        /// </summary>
        private IEnumerator SpawnOneShotEffectCoroutine(EventEffectConfig config, Vector3 position, float scale = 1f)
        {
            GameObject prefab = ConfigManager.Instance.GetEffectPrefab(config.eventType);
            if (prefab == null)
            {
                Debug.LogWarning($"[EventEffect] 未找到 eventType={config.eventType} 的特效 Prefab");
                yield break;
            }

            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
            if (scale != 1f)
                effect.transform.localScale = Vector3.one * scale;

            // 等待特效动画播放完成（如果有 Animation/Animator 组件）
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null)
            {
                // 获取动画剪辑长度
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float clipLength = animator.runtimeAnimatorController != null ? GetAnimationClipLength(animator) : 0.5f;
                yield return new WaitForSeconds(clipLength);
            }
            else
            {
                // 默认等待0.5秒后销毁
                yield return new WaitForSeconds(0.5f);
            }

            if (effect != null)
                Destroy(effect);
        }

        private float GetAnimationClipLength(Animator animator)
        {
            if (animator.runtimeAnimatorController == null) return 0.5f;
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
                return clips[0].length;
            return 0.5f;
        }

        /// <summary>
        /// 触发持续挂载特效（跟随目标移动，随目标销毁而销毁）
        /// </summary>
        public void TriggerAttachedEffect(int eventType, Transform target)
        {
            if (eventType <= 0 || target == null) return;

            var config = Cfg.EventEffect.Get(eventType);
            if (config == null)
            {
                Debug.LogWarning($"[EventEffect] 未找到 eventType={eventType} 的特效配置");
                return;
            }

            GameObject prefab = ConfigManager.Instance.GetEffectPrefab(config.eventType);
            if (prefab == null)
            {
                Debug.LogWarning($"[EventEffect] 未找到 eventType={config.eventType} 的特效 Prefab");
                return;
            }

            // 创建挂载特效
            GameObject effect = new GameObject($"AttachedEffect_{eventType}");
            effect.transform.SetParent(target);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;

            // 实例化特效预制件
            GameObject prefabInstance = Instantiate(prefab, effect.transform);
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localRotation = Quaternion.identity;

            // 添加跟随脚本
            AttachedEffectController controller = effect.AddComponent<AttachedEffectController>();
            controller.Init(prefabInstance);
        }
    }

    /// <summary>
    /// 挂在特效对象上的控制器，负责跟随父级并管理生命周期
    /// </summary>
    public class AttachedEffectController : MonoBehaviour
    {
        private GameObject prefabInstance;

        public void Init(GameObject prefab)
        {
            prefabInstance = prefab;
        }

        private void Update()
        {
            // 保持特效在父级位置（因为已经在子层级，所以本地坐标为0即可）
            if (prefabInstance != null)
            {
                prefabInstance.transform.localPosition = Vector3.zero;
                prefabInstance.transform.localRotation = Quaternion.identity;
            }

            // 如果父级已被销毁，自动销毁自身
            if (transform.parent == null)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (prefabInstance != null)
                Destroy(prefabInstance);
        }
    }
}
