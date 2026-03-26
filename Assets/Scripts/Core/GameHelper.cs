using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GeometryTD
{
    public static class GameHelper
    {
        private static Font cachedFont;

        public static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try Resources.Load first (for assets in Resources/ folders)
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

#if UNITY_EDITOR
            // Fallback: load by asset path for assets outside Resources/
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/{path}.png");
            if (sprite != null) return sprite;
#endif

            Debug.LogWarning($"[GameHelper] Sprite not found: {path}");
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
    }
}
