using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// 伤害数字类型枚举
    /// </summary>
    public enum DamageTextType
    {
        Normal,     // 普通伤害/治疗：从头顶飘过
        Splash      // 爆炸波及伤害：随机位置出现 + 缩放效果
    }

    public class FloatingTextUI : MonoBehaviour
    {
        // 飘字计数器，用于左右交替偏移
        private static int damageTextCounter = 0;

        private const int DamageFontSize = 32;

        public void Show(string text, Vector3 worldPos, Color color, DamageTextType textType = DamageTextType.Normal)
        {
            StartCoroutine(FloatText(text, worldPos, color, textType));
        }

        /// <summary>
        /// 计算飘字偏移位置（左右交替，防止重叠）
        /// </summary>
        private Vector3 CalculateOffset(DamageTextType textType)
        {
            // 左右交替偏移
            int side = (damageTextCounter % 2 == 0) ? 1 : -1;
            damageTextCounter++;

            float baseX = Random.Range(0.2f, 0.4f) * side;
            float baseY = Random.Range(0.3f, 0.5f);

            return new Vector3(baseX, baseY, 0f);
        }

        /// <summary>
        /// 普通飘字效果：从头顶快速飘过
        /// </summary>
        private IEnumerator FloatText(string text, Vector3 worldPos, Color color, DamageTextType textType)
        {
            GameObject go = new GameObject("FloatingText");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 50);
            rt.localScale = Vector3.one * 0.01f;

            // 计算偏移位置
            Vector3 offset = CalculateOffset(textType);
            go.transform.position = worldPos + offset;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            Text t = textObj.AddComponent<Text>();
            t.text = text;
            t.fontSize = DamageFontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.font = GameHelper.LoadFont();

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            if (textType == DamageTextType.Splash)
            {
                // 爆炸波及伤害：随机位置出现 + 快速缩放效果
                yield return StartCoroutine(PlaySplashEffect(go, t, worldPos, color));
            }
            else
            {
                // 普通伤害/治疗：从头顶飘过
                yield return StartCoroutine(PlayNormalEffect(go, t, worldPos, color));
            }

            Destroy(go);
        }

        /// <summary>
        /// 普通飘字效果：从头顶快速向上飘过并渐隐
        /// </summary>
        private IEnumerator PlayNormalEffect(GameObject go, Text t, Vector3 worldPos, Color color)
        {
            float duration = 0.8f;
            float elapsed = 0f;
            float floatDistance = 1.2f;

            // 起始位置（已在 FloatText 中设置，这里用当前位置）
            Vector3 startPos = go.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // 向上移动
                go.transform.position = startPos + Vector3.up * progress * floatDistance;
                
                // 渐隐效果
                t.color = new Color(color.r, color.g, color.b, 1f - progress);
                
                yield return null;
            }

            Destroy(go);
        }

        /// <summary>
        /// 爆炸波及伤害效果：随机位置出现 + 快速缩放效果
        /// </summary>
        private IEnumerator PlaySplashEffect(GameObject go, Text t, Vector3 worldPos, Color color)
        {
            // 左右交替偏移
            int side = (damageTextCounter % 2 == 0) ? 1 : -1;
            damageTextCounter++;

            // 随机偏移位置（围绕目标身体周围）
            float randomRadius = Random.Range(0.3f, 0.8f);
            float randomAngle = Random.Range(0f, 360f);
            float rad = randomAngle * Mathf.Deg2Rad;
            
            Vector3 randomOffset = new Vector3(
                Mathf.Cos(rad) * randomRadius * side,
                Random.Range(0f, 0.5f),
                0f
            );
            Vector3 displayPos = worldPos + randomOffset;
            go.transform.position = displayPos;

            // 爆炸字号保持统一大小
            t.fontSize = DamageFontSize;

            // 基础缩放值
            float baseScale = 0.01f;
            go.transform.localScale = Vector3.one * baseScale;

            float scaleDuration = 0.2f;
            float holdDuration = 0.15f;
            float fadeDuration = 0.15f;
            float elapsed = 0f;

            // 阶段1：快速放大
            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / scaleDuration;
                // 从0快速放大到1.2倍（基于基础缩放）
                float scale = Mathf.Lerp(baseScale, baseScale * 1.2f, progress);
                go.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            // 阶段2：回弹到正常大小
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / 0.08f;
                // 从1.2倍回弹到1.0倍
                float scale = Mathf.Lerp(baseScale * 1.2f, baseScale, progress);
                go.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            // 阶段3：保持一小段时间
            elapsed = 0f;
            go.transform.localScale = Vector3.one * baseScale;
            while (elapsed < holdDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 阶段4：快速淡出
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeDuration;
                t.color = new Color(color.r, color.g, color.b, 1f - progress);
                yield return null;
            }
        }
    }
}
