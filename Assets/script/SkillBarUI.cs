using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 技能栏 UI - 屏幕底部横排 8 个技能槽
    /// 
    /// 两种模式：
    ///   1. 预制体模式：将 SkillSlot.prefab 拖入 slotPrefab 字段，运行时实例化
    ///   2. 代码模式：slotPrefab 为空时，自动用代码生成槽位（与之前相同）
    /// 
    /// 由 PlayerController.Start() 调用 Build(mgr) 动态绑定技能状态
    /// </summary>
    public class SkillBarUI : MonoBehaviour
    {
        [Header("技能槽预制体（可选，留空则代码自动生成）")]
        [SerializeField] public GameObject slotPrefab;

        // 每个槽的像素尺寸（仅代码模式下生效）
        private const float SLOT_W = 110f;
        private const float SLOT_H = 140f;
        private const float GAP    = 6f;
        private const float ICON_H = 80f;
        private const float EXP_H  = 8f;
        private const float LV_H   = 20f;
        private const float PAD    = 8f;

        private List<SkillSlotUI> slots   = new List<SkillSlotUI>();
        private SkillManager        boundMgr;  // 保存 mgr 引用，用于反注册事件

        // ── 由 PlayerController 调用 ──────────────────────────────────────────
        public void Build(SkillManager mgr)
        {
            // 若 slotPrefab 未在 Inspector 拖入，自动从 PrefabRef 获取
            if (slotPrefab == null)
                slotPrefab = PrefabRef.Instance?.skillSlotPrefab;

            // 反注册旧 mgr
            if (boundMgr != null) boundMgr.OnSkillExpChanged -= OnExpChanged;
            boundMgr = mgr;

            // 清除旧槽
            foreach (Transform child in transform) Destroy(child.gameObject);
            slots.Clear();

            int count = mgr.Skills.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject slotGo = slotPrefab != null
                    ? BuildFromPrefab(i, count)
                    : BuildSlotGo(i, count);

                SkillSlotUI slotUI = slotGo.GetComponent<SkillSlotUI>();
                if (slotUI == null)
                    slotUI = slotGo.AddComponent<SkillSlotUI>();

                // 预制体模式：自动从子节点查找引用（若未在 Inspector 绑定）
                if (slotPrefab != null)
                    AutoBindReferences(slotUI, slotGo);

                slotUI.Setup(mgr.Skills[i], mgr, i);
                slots.Add(slotUI);
            }

            // 监听经验和升级事件
            mgr.OnSkillExpChanged += OnExpChanged;
        }

        private void OnDestroy()
        {
            if (boundMgr != null) boundMgr.OnSkillExpChanged -= OnExpChanged;
        }

        // 经验变化事件：驱动对应槽动画
        private void OnExpChanged(SkillState skill, bool leveledUp, int amount)
        {
            if (boundMgr == null) return;
            int idx = boundMgr.Skills.IndexOf(skill);
            if (idx < 0 || idx >= slots.Count) return;
            SkillSlotUI slot = slots[idx];
            if (slot == null) return;
            slot.PlayExpGainAnim(amount);   // 跳跃 +N EXP
            if (leveledUp) slot.PlayLevelUpAnim(); // 升级闪烁
        }

        // ── 预制体模式：实例化并排列位置 ────────────────────────────────────
        private GameObject BuildFromPrefab(int i, int total)
        {
            GameObject go = Instantiate(slotPrefab, transform);
            go.name = "Slot_" + i;
            go.transform.localScale = Vector3.one;

            // 横排居中
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            float slotW  = rt.sizeDelta.x > 0 ? rt.sizeDelta.x : SLOT_W;
            float slotH  = rt.sizeDelta.y > 0 ? rt.sizeDelta.y : SLOT_H;
            float gap    = GAP;
            float totalW = total * slotW + (total - 1) * gap;
            float startX = -totalW * 0.5f + slotW * 0.5f;
            float posX   = startX + i * (slotW + gap);

            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(posX, 0f);

            return go;
        }

        // ── 自动从子节点查找 UI 引用（预制体中未手动绑定时的后备方案）──────
        private static void AutoBindReferences(SkillSlotUI slotUI, GameObject root)
        {
            if (slotUI.button       == null) slotUI.button       = root.GetComponent<Button>();
            if (slotUI.iconImage    == null) slotUI.iconImage     = DeepFind<Image>(root.transform, "Icon");
            if (slotUI.nameText     == null) slotUI.nameText      = DeepFind<Text>(root.transform, "NameText");
            if (slotUI.cooldownMask == null) slotUI.cooldownMask  = DeepFind<Image>(root.transform, "CooldownMask");
            if (slotUI.cooldownText == null) slotUI.cooldownText  = DeepFind<Text>(root.transform, "CooldownText");
            if (slotUI.expBgImage   == null) slotUI.expBgImage    = DeepFind<Image>(root.transform, "ExpBg");
            if (slotUI.expFillImage == null) slotUI.expFillImage  = DeepFind<Image>(root.transform, "ExpFill");
            if (slotUI.levelText    == null) slotUI.levelText     = DeepFind<Text>(root.transform, "LevelText");
        }

        private static T DeepFind<T>(Transform parent, string objName) where T : Component
        {
            foreach (Transform child in parent)
            {
                if (child.name == objName)
                {
                    T comp = child.GetComponent<T>();
                    if (comp != null) return comp;
                }
                T found = DeepFind<T>(child, objName);
                if (found != null) return found;
            }
            return null;
        }

        // ── 代码模式：构建单个技能槽 ────────────────────────────────────────
        private GameObject BuildSlotGo(int i, int total)
        {
            float totalW = total * SLOT_W + (total - 1) * GAP;
            float startX = -totalW * 0.5f + SLOT_W * 0.5f;
            float posX   = startX + i * (SLOT_W + GAP);

            // 槽根
            GameObject slot = MakeRect("Slot_" + i, transform,
                new Vector2(SLOT_W, SLOT_H),
                new Vector2(0.5f, 0f),
                new Vector2(posX, 0f));

            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.95f);

            var btn = slot.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor     = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.disabledColor    = new Color(0.42f, 0.42f, 0.42f, 0.7f);
            btn.colors = cb;
            btn.targetGraphic = bg;

            // 图标
            GameObject iconGo = MakeRect("Icon", slot.transform,
                new Vector2(SLOT_W - 8f, ICON_H),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -PAD));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.cyan;

            // 技能名
            var nameGo = MakeRect("NameText", iconGo.transform,
                new Vector2(SLOT_W - 8f, 22f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 2f));
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize  = 11;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color     = Color.white;
            nameTxt.text      = "";

            // 冷却遮罩
            GameObject cdMaskGo = MakeRect("CooldownMask", iconGo.transform,
                new Vector2(SLOT_W - 8f, ICON_H),
                new Vector2(0.5f, 0.5f),
                Vector2.zero);
            var cdMask = cdMaskGo.AddComponent<Image>();
            cdMask.color         = new Color(0f, 0f, 0f, 0.75f);
            cdMask.type          = Image.Type.Filled;
            cdMask.fillMethod    = Image.FillMethod.Radial360;
            cdMask.fillOrigin    = (int)Image.Origin360.Top;
            cdMask.fillClockwise = true;
            cdMask.fillAmount    = 0f;
            cdMaskGo.SetActive(false);

            // 冷却数字
            var cdTxtGo = MakeRect("CooldownText", cdMaskGo.transform,
                new Vector2(SLOT_W - 8f, 28f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero);
            var cdTxt = cdTxtGo.AddComponent<Text>();
            cdTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdTxt.fontSize  = 20;
            cdTxt.fontStyle = FontStyle.Bold;
            cdTxt.alignment = TextAnchor.MiddleCenter;
            cdTxt.color     = Color.white;

            // 经验条背景
            float expOffY = -(PAD + ICON_H + 4f);
            GameObject expBgGo = MakeRect("ExpBg", slot.transform,
                new Vector2(SLOT_W - 8f, EXP_H),
                new Vector2(0.5f, 1f),
                new Vector2(0f, expOffY));
            expBgGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);

            // 经验条填充
            GameObject expFillGo = MakeRect("ExpFill", expBgGo.transform,
                new Vector2(SLOT_W - 8f, EXP_H),
                new Vector2(0f, 0.5f),
                Vector2.zero);
            var expFillRt = expFillGo.GetComponent<RectTransform>();
            expFillRt.anchorMin = Vector2.zero;
            expFillRt.anchorMax = Vector2.one;
            expFillRt.offsetMin = expFillRt.offsetMax = Vector2.zero;
            var expFill = expFillGo.AddComponent<Image>();
            expFill.color      = new Color(0.3f, 0.85f, 1f);
            expFill.type       = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillAmount = 0f;

            // 等级文字
            GameObject lvGo = MakeRect("LevelText", slot.transform,
                new Vector2(SLOT_W, LV_H),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 4f));
            var lvTxt = lvGo.AddComponent<Text>();
            lvTxt.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lvTxt.fontSize        = 13;
            lvTxt.fontStyle       = FontStyle.Bold;
            lvTxt.alignment       = TextAnchor.MiddleCenter;
            lvTxt.color           = new Color(0.85f, 0.85f, 1f);
            lvTxt.supportRichText = true;

            // 注入引用
            var slotUI = slot.AddComponent<SkillSlotUI>();
            slotUI.iconImage    = iconImg;
            slotUI.nameText     = nameTxt;
            slotUI.expFillImage = expFill;
            slotUI.expBgImage   = expBgGo.GetComponent<Image>();
            slotUI.levelText    = lvTxt;
            slotUI.cooldownMask = cdMask;
            slotUI.cooldownText = cdTxt;
            slotUI.button       = btn;

            return slot;
        }

        private static GameObject MakeRect(string name, Transform parent,
            Vector2 sizeDelta, Vector2 pivot, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = pivot;
            rt.anchorMax        = pivot;
            rt.sizeDelta        = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            go.transform.localScale = Vector3.one;
            return go;
        }
    }
}
