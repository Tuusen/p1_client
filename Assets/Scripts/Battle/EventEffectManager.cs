using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Spawns prefab-based visual effects driven by event_effect_config.json.
    /// </summary>
    public class EventEffectManager : MonoBehaviour
    {
        /// <summary>
        /// Trigger a visual effect for the given event type at the specified world position.
        /// </summary>
        public void TriggerEffect(int eventType, Vector3 position)
        {
            var config = Cfg.EventEffect.Get(eventType);
            if (config == null) return;

            SpawnEffect(config, position);
        }

        private void SpawnEffect(EventEffectConfig config, Vector3 position)
        {
            GameObject prefab = ConfigManager.Instance.GetEffectPrefab(config.eventType);
            if (prefab == null)
            {
                Debug.LogWarning($"[EventEffect] 未找到 eventType={config.eventType} 的特效 Prefab");
                return;
            }
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
