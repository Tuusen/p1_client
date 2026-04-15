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

        public static T OpenWin<T>() where T : BaseWin
        {
            return WinManager.Instance.OpenWin<T>();
        }

        public static T OpenWin<T>(string prefabPath) where T : BaseWin
        {
            return WinManager.Instance.OpenWin<T>(prefabPath);
        }

        public static void CloseWin<T>() where T : BaseWin
        {
            WinManager.Instance.CloseWin<T>();
        }

        // ===== 场景管理 =====

        public static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
