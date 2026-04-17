using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GeometryTD
{
    public static class GameHelper
    {
        private static Font cachedFont;

        public static Sprite LoadSprite(string path, string assetPath = "Resources")
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try Resources.Load first (for assets in Resources/ folders)
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

#if UNITY_EDITOR
            // Fallback: load by asset path for assets outside Resources/
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{assetPath}/{path}.png");
            if (sprite != null) return sprite;
#endif

            Debug.LogWarning($"[GameHelper] Sprite not found: {path}");
            return null;
        }

        public static GameObject LoadPrefab(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try Resources.Load first
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null) return prefab;

#if UNITY_EDITOR
            // Fallback: load by asset path for assets outside Resources/
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/{path}.prefab");
            if (prefab != null) return prefab;
#endif

            Debug.LogWarning($"[GameHelper] Prefab not found: {path}");
            return null;
        }

        public static Font LoadFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.Load<Font>("AnFont");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }

        public static RuntimeAnimatorController LoadAnimator(string path, string assetPath = "Resources")
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try Resources.Load first (for assets in Resources/ folders)
            RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(path);
            if (animator != null) return animator;

#if UNITY_EDITOR
            // Fallback: load by asset path for assets outside Resources/
            animator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/{assetPath}/{path}.controller");
            if (animator != null) return animator;
#endif

            Debug.LogWarning($"[GameHelper] AnimatorController not found: {path}");
            return null;
        }

        public static T OpenWin<T>(string winName = null, object param = null) where T : BaseWin
        {
            // 直接调用 WinManager 的统一 OpenWin 方法
            return WinManager.Instance.OpenWin<T>(winName, param);
        }

        // ===== 场景管理 =====

        public static void LoadScene(string sceneName)
        {
            // 通过 GameManager 统一管理 TimeScale 重置
            GameManager.Instance.ResetTimeScale();
            SceneManager.LoadScene(sceneName);
        }
    }
}
