using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 2D 几何形状生成器 - 使用SpriteRenderer + 程序生成Sprite
    /// </summary>
    public static class GeometryMeshGenerator
    {
        // ─────────────────────────── 2D Sprite 创建 ───────────────────────────

        /// <summary>
        /// 根据形状名称创建对应的 GameObject（带 SpriteRenderer）
        /// </summary>
        public static GameObject CreateShape(string shape, float size, Color color)
        {
            GameObject go = new GameObject("Shape");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSprite(shape, size, color);
            sr.color = color;
            return go;
        }

        /// <summary>
        /// 生成几何形状 Sprite
        /// </summary>
        public static Sprite CreateSprite(string shape, float size, Color color)
        {
            int texSize = 64;
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);

            // 清透明背景
            Color clear = Color.clear;
            for (int x = 0; x < texSize; x++)
                for (int y = 0; y < texSize; y++)
                    tex.SetPixel(x, y, clear);

            switch (shape.ToLower())
            {
                case "cube":
                case "square":
                    FillRect(tex, 6, 6, texSize - 6, texSize - 6, color);
                    break;
                case "sphere":
                case "circle":
                    FillCircle(tex, texSize / 2, texSize / 2, texSize / 2 - 4, color);
                    break;
                case "triangle":
                    FillTriangle(tex, color);
                    break;
                case "diamond":
                    FillDiamond(tex, color);
                    break;
                default:
                    FillRect(tex, 6, 6, texSize - 6, texSize - 6, color);
                    break;
            }

            tex.Apply();

            float pixelsPerUnit = texSize / size;
            return Sprite.Create(tex,
                new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        // ─────────────────────────── 颜色解析 ───────────────────────────

        public static Color ParseColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return Color.white;
            if (ColorUtility.TryParseHtmlString(hexColor, out Color c)) return c;
            return Color.white;
        }

        // ─────────────────────────── 绘制工具 ───────────────────────────

        private static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    tex.SetPixel(x, y, c);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int x = 0; x < tex.width; x++)
                for (int y = 0; y < tex.height; y++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                        tex.SetPixel(x, y, c);
        }

        private static void FillTriangle(Texture2D tex, Color c)
        {
            int s = tex.width;
            // 向右的等腰三角形
            for (int x = 0; x < s; x++)
            {
                float t = (float)x / s;
                int halfH = Mathf.RoundToInt(t * s * 0.5f);
                int mid = s / 2;
                for (int y = mid - halfH; y <= mid + halfH; y++)
                {
                    if (y >= 0 && y < s)
                        tex.SetPixel(x, y, c);
                }
            }
        }

        private static void FillDiamond(Texture2D tex, Color c)
        {
            int s = tex.width;
            int mid = s / 2;
            for (int x = 0; x < s; x++)
                for (int y = 0; y < s; y++)
                    if (Mathf.Abs(x - mid) + Mathf.Abs(y - mid) <= mid - 4)
                        tex.SetPixel(x, y, c);
        }

        // ─────────────────────────── 旧接口保留（兼容） ───────────────────────────

        public static Mesh CreateMeshByShape(string shape, float size) => null;
        public static Material CreateMaterial(Color color, string name = "Mat") =>
            new Material(Shader.Find("Sprites/Default")) { color = color };
    }
}
