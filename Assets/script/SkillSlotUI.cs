using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 单个技能槽 UI
    /// 
    /// 功能：
    ///   - 显示技能图标、名称、等级、经验条、冷却遮罩
    ///   - 按钮始终可点击，不可用时弹 Toast 提示原因
    ///   - 获得经验时在槽上显示跳跃 "+N EXP" 文字动画
    ///   - 升级时槽背景闪烁高亮
    /// </summary>
    public class SkillSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        // ── 子节点引用（预制体中用 Inspector 绑定，或由 SkillBarUI 代码注入）──
        [SerializeField] public Image  iconImage;        // 技能图标背景色块
        [SerializeField] public Image  expFillImage;     // 经验条填充
        [SerializeField] public Image  expBgImage;       // 经验条背景
        [SerializeField] public Text   levelText;        // 等级文字 "Lv3"
        [SerializeField] public Image  cooldownMask;     // 冷却半透明遮罩（Radial360）
        [SerializeField] public Text   cooldownText;     // 冷却秒数文字
        [SerializeField] public Button button;           // 点击触发使用
        [SerializeField] public Text   nameText;         // 技能名文字

        // ── 绑定的技能状态 ───────────────────────────────────────────────────
        private SkillState   state;
        private SkillManager manager;
        private int          slotIndex;

        // ── 背景 Image（用于闪烁动画）──────────────────────────────────────
        private Image bgImage;
        private Color bgNormalColor;

        // ── Toast 单例（全局共用一个，避免堆叠）────────────────────────────
        private static GameObject   toastGo;
        private static Text         toastText;
        private static Coroutine    toastCoroutine;
        private static MonoBehaviour toastOwner; // 用于启动协程

        // ── 动画协程 ──────────────────────────────────────────────────────────
        private Coroutine    flashCoroutine;
        private Coroutine    expPopCoroutine;
        private GameObject   currentPopGo;    // 记录当前弹字对象

        // ── 初始化 ───────────────────────────────────────────────────────────
        public void Setup(SkillState skillState, SkillManager mgr, int index)
        {
            state     = skillState;
            manager   = mgr;
            slotIndex = index;

            // 背景色缓存
            bgImage = GetComponent<Image>();
            if (bgImage != null) bgNormalColor = bgImage.color;

            // 图标：优先加载图片，如果没有图片则用颜色展示
            if (iconImage != null)
            {
                bool loaded = false;
                if (!string.IsNullOrEmpty(state.Config.iconPath))
                {
                    // 先尝试 Resources.Load（如果在 Resources 目录）
                    var sprite = Resources.Load<Sprite>(state.Config.iconPath);
                    if (sprite != null)
                    {
                        iconImage.sprite = sprite;
                        iconImage.color  = Color.white;
                        loaded = true;
                    }
                    else
                    {
                        // 尝试从 dataPath 直接加载纹理
                        string fullPath = System.IO.Path.Combine(
                            Application.dataPath, state.Config.iconPath + ".png");
                        if (System.IO.File.Exists(fullPath))
                        {
                            byte[]    bytes   = System.IO.File.ReadAllBytes(fullPath);
                            Texture2D tex     = new Texture2D(2, 2);
                            if (tex.LoadImage(bytes))
                            {
                                iconImage.sprite = Sprite.Create(tex,
                                    new Rect(0, 0, tex.width, tex.height),
                                    new Vector2(0.5f, 0.5f));
                                iconImage.color  = Color.white;
                                loaded = true;
                            }
                        }
                    }
                }
                if (!loaded)
                    iconImage.color = GeometryMeshGenerator.ParseColor(state.Config.iconColor);
            }

            // 技能名字
            if (nameText != null) nameText.text = state.Config.name;
            if (nameText == null && iconImage != null)
            {
                var found = iconImage.transform.Find("NameText")?.GetComponent<Text>();
                if (found != null) found.text = state.Config.name;
            }

            // 按钮：始终可点，不禁用 interactable
            if (button != null)
            {
                button.interactable = true;
                button.onClick.AddListener(OnClick);
            }

            // 监听状态变化
            state.OnStateChanged += Refresh;

            // Toast 初始化（只做一次）
            if (toastGo == null) BuildToast();
            if (toastOwner == null) toastOwner = this;

            Refresh(state);
        }

        private void OnDestroy()
        {
            if (state != null) state.OnStateChanged -= Refresh;
        }

        // ── 刷新显示 ─────────────────────────────────────────────────────────
        private void Refresh(SkillState s)
        {
            UpdateLevel(s);
            UpdateExp(s);
            UpdateCooldown(s);
            UpdateVisualState(s);
        }

        private void UpdateLevel(SkillState s)
        {
            if (levelText == null) return;
            if (s.IsMaxLevel)
                levelText.text = "<color=#FFD700>MAX</color>";
            else if (s.Level == 0)
                levelText.text = "<color=#666666>Lv0</color>";
            else
                levelText.text = $"Lv{s.Level}";
        }

        private void UpdateExp(SkillState s)
        {
            if (expFillImage == null) return;
            if (s.IsMaxLevel)
            {
                expFillImage.fillAmount = 1f;
                expFillImage.color = new Color(1f, 0.85f, 0f);
            }
            else
            {
                float ratio = s.Config.expPerLevel > 0
                    ? (float)s.Exp / s.Config.expPerLevel : 0f;
                expFillImage.fillAmount = ratio;
                expFillImage.color = new Color(0.3f, 0.85f, 1f);
            }
        }

        private void UpdateCooldown(SkillState s)
        {
            if (cooldownMask == null) return;
            if (s.Cooldown > 0f)
            {
                cooldownMask.gameObject.SetActive(true);
                // 共享冷却最大 SELF_COOLDOWN，防止比例超1
                float maxCD = Mathf.Max(s.Cooldown, SkillManager.SELF_COOLDOWN);
                cooldownMask.fillAmount = s.Cooldown / maxCD;
                if (cooldownText != null)
                    cooldownText.text = s.Cooldown >= 1f
                        ? $"{Mathf.CeilToInt(s.Cooldown)}s"
                        : $"{s.Cooldown:F1}s";
            }
            else
            {
                cooldownMask.gameObject.SetActive(false);
                if (cooldownText != null) cooldownText.text = "";
            }
        }

        /// <summary>根据可用状态调整图标透明度（不影响按钮点击）</summary>
        private void UpdateVisualState(SkillState s)
        {
            if (iconImage == null) return;
            Color c = iconImage.color;
            c.a = s.IsReady ? 1f : 0.5f;
            iconImage.color = c;

            // 等级文字颜色变暗
            if (levelText != null)
                levelText.color = s.IsReady
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        // ── 按钮点击 ─────────────────────────────────────────────────────────
        private void OnClick()
        {
            string failReason = manager?.TryUseSkill(slotIndex);
            if (failReason != null)
            {
                // 弹出提示
                ShowToast(failReason, GetComponent<RectTransform>());
            }
            // 成功使用时 SkillState 变化会自动刷新 UI
        }

        // ── 获得经验动画：跳跃 "+N EXP" 文字 ────────────────────────────────
        public void PlayExpGainAnim(int amount)
        {
            // 先销毁上一个弹字，再启动新的
            if (expPopCoroutine != null) StopCoroutine(expPopCoroutine);
            if (currentPopGo != null) { Destroy(currentPopGo); currentPopGo = null; }
            expPopCoroutine = StartCoroutine(ExpPopRoutine(amount));
        }

        private IEnumerator ExpPopRoutine(int amount)
        {
            GameObject popGo = new GameObject("ExpPop");
            currentPopGo = popGo;   // 记录引用以便干预销毁
            popGo.transform.SetParent(transform, false);

            var rt = popGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(100f, 28f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            popGo.transform.localScale = Vector3.one;

            var txt = popGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = new Color(0.3f, 1f, 0.5f);
            txt.text      = $"+{amount} EXP";
            txt.supportRichText = false;

            // 动画：向上飞出 + 淡出，持续 0.9s
            float duration = 0.9f;
            float elapsed  = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos   = startPos + new Vector2(0f, 55f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = 1f - (1f - t) * (1f - t); // ease-out
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);

                Color c = txt.color;
                c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
                txt.color = c;
                yield return null;
            }

            Destroy(popGo);
            currentPopGo = null;
        }

        // ── 升级闪烁动画 ──────────────────────────────────────────────────────
        public void PlayLevelUpAnim()
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (iconImage == null) yield break;

            Color iconNormal = iconImage.color;                         // 保存原色
            Color flashColor = new Color(1f, 0.92f, 0.2f, iconNormal.a); // 金黄闪烁
            int   flashCount = 4;
            float halfPeriod = 0.08f;

            for (int i = 0; i < flashCount; i++)
            {
                iconImage.color = flashColor;
                yield return new WaitForSeconds(halfPeriod);
                iconImage.color = iconNormal;
                yield return new WaitForSeconds(halfPeriod);
            }
            iconImage.color = iconNormal;
        }

        // ── Toast 弹框系统 ────────────────────────────────────────────────────
        private static GameObject tooltipGo;
        private static Text       tooltipText;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (state == null) return;
            if (tooltipGo == null) BuildTooltip();
            if (tooltipGo == null) return;
            tooltipText.text = BuildTooltipContent();
            tooltipGo.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipGo != null) tooltipGo.SetActive(false);
        }

        private string BuildTooltipContent()
        {
            var cfg = state.Config;
            int lv  = state.Level;
            var sb  = new System.Text.StringBuilder();
            sb.AppendLine($"<b><color=#FFD700>{cfg.name}</color></b>");
            sb.AppendLine(cfg.description);
            sb.AppendLine();
            if (lv > 0) sb.AppendLine($"<color=#88FF88>Lv{lv} / {cfg.maxLevel}</color>");
            else        sb.AppendLine("<color=#FF8888>尚未解锁</color>");
            sb.AppendLine($"冷却：{cfg.cooldown:F0}秒");
            if (!string.IsNullOrEmpty(cfg.element) && cfg.element != "none")
                sb.AppendLine($"元素：{GetElementName(cfg.element)}");
            sb.AppendLine();
            string upg = GetUpgradeDesc(cfg, lv);
            if (!string.IsNullOrEmpty(upg))
            {
                sb.AppendLine("<color=#AADDFF>[升级效果]</color>");
                sb.Append(upg);
            }
            return sb.ToString().TrimEnd();
        }

        private static string GetElementName(string el)
        {
            switch (el)
            {
                case "fire":      return "<color=#FF6633>火</color>";
                case "ice":       return "<color=#88DDFF>冰</color>";
                case "lightning": return "<color=#FFEE44>雷</color>";
                case "wind":      return "<color=#88FF88>风</color>";
                default:          return el;
            }
        }

        private static string GetUpgradeDesc(SkillConfig cfg, int lv)
        {
            var sb = new System.Text.StringBuilder();
            if (cfg.bulletCountPerLevel > 0 && cfg.baseBulletCount > 0)
                sb.AppendLine($"发射数：<b>{cfg.baseBulletCount + Mathf.Max(0, lv - 1) * cfg.bulletCountPerLevel}</b>（每级+{cfg.bulletCountPerLevel}）");
            if (cfg.hasExplosion)
                sb.AppendLine(lv >= cfg.explosionUnlockLv
                    ? $"<color=#FF9933>[已解锁]爆炸 R={cfg.explosionRadius:F1}</color>"
                    : $"<color=#888888>[Lv{cfg.explosionUnlockLv}]爆炸</color>");
            if (cfg.hasBurn)
                sb.AppendLine(lv >= cfg.burnUnlockLv
                    ? $"<color=#FF4411>[已解锁]灼伤 {cfg.burnDamagePctPerSec:F0}%/秒</color>"
                    : $"<color=#888888>[Lv{cfg.burnUnlockLv}]灼伤</color>");
            if (cfg.hasFreezeOnHit)
            {
                sb.AppendLine($"冰冻 {cfg.freezeDuration:F0}秒");
                if (cfg.hasPierceOnLv6)
                    sb.AppendLine(lv >= 6 ? "<color=#88DDFF>[已解锁]穿刺</color>" : "<color=#888888>[Lv6]穿刺</color>");
                if (cfg.hasSlowAfterFreeze)
                    sb.AppendLine(lv >= 10 ? $"<color=#88DDFF>[已解锁]解冻后减速{cfg.slowPct:F0}%</color>" : "<color=#888888>[Lv10]解冻后减速</color>");
            }
            if (cfg.hasChain)
            {
                int chains = cfg.baseChainCount + Mathf.Max(0, lv - 1) * cfg.chainCountPerLevel;
                sb.AppendLine($"闪电链追击：<b>{chains}</b>次（每级+{cfg.chainCountPerLevel}）");
                if (cfg.chainAllowRepeat)
                    sb.AppendLine(lv >= 6 ? "<color=#FFEE44>[已解锁]允许重复命中</color>" : "<color=#888888>[Lv6]允许重复命中</color>");
            }
            if (cfg.hasKnockback)
            {
                float d = cfg.knockbackDist * (1f + Mathf.Max(0, lv - 1) * cfg.knockbackPerLevel);
                sb.AppendLine($"击退距离：<b>{d:F1}</b>");
                if (cfg.hasSlowOnLv6)
                    sb.AppendLine(lv >= 6 ? $"<color=#88FF88>[已解锁]减速{cfg.aoeSlowPct:F0}%</color>" : "<color=#888888>[Lv6]减速</color>");
            }
            if (cfg.isHeal)
            {
                float p = cfg.healPct + Mathf.Max(0, lv - 1) * cfg.healPerLevel;
                sb.AppendLine($"治愈已损失血量：<b>{p:F0}%</b>");
                if (cfg.hasHoTOnLv6)
                    sb.AppendLine(lv >= 6 ? $"<color=#FF88AA>[已解锁]持续{cfg.hotPctPerSec:F0}%/秒</color>" : "<color=#888888>[Lv6]持续回悂</color>");
            }
            if (cfg.isEnergyExchange)
            {
                int ex = cfg.baseExpGain + Mathf.Max(0, lv - 1) * cfg.expGainPerLevel;
                sb.AppendLine($"消耗{cfg.hpCostPct:F0}%血  +<b>{ex}</b> EXP");
                if (lv >= 10)     sb.AppendLine("<color=#88DDFF>[已解锁]全部非满级</color>");
                else if (lv >= 6) sb.AppendLine("<color=#88DDFF>[已解锁]最多3个目标</color>");
            }
            if (cfg.isShield)
            {
                float sp = cfg.shieldPct + Mathf.Max(0, lv - 1) * cfg.shieldPerLevel;
                sb.AppendLine($"护盾：<b>{sp:F0}%</b> 最大血量");
                if (cfg.hasReflectOnLv6)
                    sb.AppendLine(lv >= 6 ? "<color=#AABBFF>[已解锁]受击反射</color>" : "<color=#888888>[Lv6]受击反射</color>");
            }
            if (cfg.isSummon)
            {
                float dur = cfg.guardDuration + Mathf.Max(0, lv - 1) * cfg.guardDurationPerLevel;
                sb.AppendLine($"风影持续<b>{dur:F0}s</b>  {cfg.guardFireRate:F1}s/弹");
                if (cfg.guardHomingOnLv6)
                    sb.AppendLine(lv >= 6 ? "<color=#88FF88>[已解锁]子弹追踪</color>" : "<color=#888888>[Lv6]子弹追踪</color>");
            }
            return sb.ToString();
        }

        private static void BuildTooltip()
        {
            Canvas screenCanvas = null;
            foreach (var c in Object.FindObjectsOfType<Canvas>())
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                { screenCanvas = c; break; }
            if (screenCanvas == null) return;

            tooltipGo = new GameObject("SkillTooltip");
            tooltipGo.transform.SetParent(screenCanvas.transform, false);

            var rt = tooltipGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(280f, 320f);
            rt.anchoredPosition = new Vector2(0f, 165f);

            var bg = tooltipGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.12f, 0.96f);

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(tooltipGo.transform, false);
            var brt = borderGo.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-2f, -2f); brt.offsetMax = new Vector2(2f, 2f);
            borderGo.AddComponent<Image>().color = new Color(0.35f, 0.45f, 0.75f, 0.8f);
            borderGo.transform.SetAsFirstSibling();

            var txtGo = new GameObject("TooltipText");
            txtGo.transform.SetParent(tooltipGo.transform, false);
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 10f); trt.offsetMax = new Vector2(-10f, -10f);
            tooltipText = txtGo.AddComponent<Text>();
            tooltipText.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tooltipText.fontSize        = 14;
            tooltipText.color           = new Color(0.92f, 0.92f, 0.95f);
            tooltipText.alignment       = TextAnchor.UpperLeft;
            tooltipText.lineSpacing     = 1.3f;
            tooltipText.supportRichText = true;
            tooltipGo.SetActive(false);
        }

        private static void BuildToast()
        {
            // 找到屏幕空间 Canvas
            Canvas screenCanvas = null;
            foreach (var c in Object.FindObjectsOfType<Canvas>())
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder >= 20)
                { screenCanvas = c; break; }
            }
            if (screenCanvas == null)
            {
                foreach (var c in Object.FindObjectsOfType<Canvas>())
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    { screenCanvas = c; break; }
            }
            if (screenCanvas == null) return;

            toastGo = new GameObject("SkillToast");
            toastGo.transform.SetParent(screenCanvas.transform, false);

            // 背景板
            var rt = toastGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 90f);
            rt.anchoredPosition = new Vector2(0f, -60f); // 屏幕中下方
            toastGo.transform.localScale = Vector3.one;

            var bg = toastGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            // 文字
            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(toastGo.transform, false);
            txtGo.transform.localScale = Vector3.one;
            var trt = txtGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 6);
            trt.offsetMax = new Vector2(-10, -6);

            toastText = txtGo.AddComponent<Text>();
            toastText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toastText.fontSize  = 18;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.color     = new Color(1f, 0.85f, 0.4f);
            toastText.lineSpacing = 1.2f;

            toastGo.SetActive(false);
        }

        private void ShowToast(string msg, RectTransform anchor)
        {
            if (toastGo == null) BuildToast();
            if (toastGo == null) return;

            toastText.text = msg;
            toastGo.SetActive(true);

            if (toastCoroutine != null && toastOwner != null)
                toastOwner.StopCoroutine(toastCoroutine);

            toastOwner = this;
            toastCoroutine = StartCoroutine(HideToastAfter(1.8f));
        }

        private IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (toastGo != null) toastGo.SetActive(false);
        }
    }
}
