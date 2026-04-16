using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class CollectionDetailWin : BaseWin
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text qualityText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text passiveListText;
        [SerializeField] private Text goldBonusText;
        [SerializeField] private Text stackTypeText;
        [SerializeField] private Text maxStackText;
        [SerializeField] private Button closeButton;

        private PassiveEffectConfig currentConfig;
        private Font cachedFont;

        public override void Init()
        {
            base.Init();

            if (titleText == null)
                BuildUI();
        }

        public override void Show()
        {
            base.Show();
        }

        public override void OnClose()
        {
            base.OnClose();
            currentConfig = null;
        }

        public void ShowDetail(PassiveEffectConfig config)
        {
            currentConfig = config;
            cachedFont = GameHelper.LoadFont();
            gameObject.SetActive(true);
            RefreshDetail();
        }

        private void RefreshDetail()
        {
            if (currentConfig == null) return;

            if (titleText != null)
                titleText.text = currentConfig.name ?? "";

            // Icon
            if (iconImage != null)
            {
                iconImage.color = new Color(1f, 1f, 1f, 0.3f);
                if (!string.IsNullOrEmpty(currentConfig.icon))
                {
                    Sprite iconSprite = GameHelper.LoadSprite(currentConfig.icon);
                    if (iconSprite != null)
                    {
                        iconImage.sprite = iconSprite;
                        iconImage.color = Color.white;
                    }
                }
            }

            // Quality
            if (qualityText != null)
            {
                qualityText.text = GetQualityName(currentConfig.color);
                qualityText.color = GetQualityTextColor(currentConfig.color);
            }

            // Level
            if (levelText != null)
            {
                levelText.text = $"Level: {currentConfig.level}";
            }

            // Description
            if (descriptionText != null)
            {
                descriptionText.text = currentConfig.des ?? "";
            }

            // Passive list
            if (passiveListText != null)
            {
                string passiveInfo = BuildPassiveInfo();
                passiveListText.text = string.IsNullOrEmpty(passiveInfo) ? "No passive skills" : passiveInfo;
            }

            // Gold bonus
            if (goldBonusText != null)
            {
                if (currentConfig.addGold > 0)
                {
                    goldBonusText.text = $"+{currentConfig.addGold / 100f}% Gold Bonus";
                    goldBonusText.color = new Color(1f, 0.85f, 0.3f);
                }
                else
                {
                    goldBonusText.text = "No gold bonus";
                    goldBonusText.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }

            // Stack type
            if (stackTypeText != null)
            {
                stackTypeText.text = $"Type: {GetStackTypeName(currentConfig.stackType)}";
            }

            // Max stack
            if (maxStackText != null)
            {
                if (currentConfig.maxStack == -1)
                {
                    maxStackText.text = "无生效限制";
                } else {
                    maxStackText.text = $"最大生效上限: {currentConfig.maxStack}";
                }
            }
        }

        private string BuildPassiveInfo()
        {
            if (currentConfig.passives == null || currentConfig.passives.Length == 0)
                return "";

            List<string> passiveNames = new List<string>();
            for (int i = 0; i < currentConfig.passives.Length; i++)
            {
                PassiveConfig passiveConfig = Cfg.Passive.Get(currentConfig.passives[i]);
                if (passiveConfig != null)
                {
                    passiveNames.Add(passiveConfig.name ?? $"Passive {currentConfig.passives[i]}");
                }
                else
                {
                    passiveNames.Add($"Passive {currentConfig.passives[i]} (Not Found)");
                }
            }

            return string.Join("\n", passiveNames);
        }

        private string GetQualityName(int color)
        {
            switch (color)
            {
                case 1: return "Common";
                case 2: return "Rare";
                case 3: return "Epic";
                default:
                    if (color >= 4) return "Legendary";
                    return "Normal";
            }
        }

        private Color GetQualityTextColor(int color)
        {
            switch (color)
            {
                case 1: return new Color(0.6f, 1f, 0.6f); // Green
                case 2: return new Color(0.6f, 0.8f, 1f); // Blue
                case 3: return new Color(0.9f, 0.7f, 1f); // Purple
                default:
                    if (color >= 4) return new Color(1f, 0.9f, 0.5f); // Gold
                    return Color.white;
            }
        }

        private Color GetQualityBgColor(int color)
        {
            switch (color)
            {
                case 1: return new Color(0.2f, 0.5f, 0.2f, 0.8f); // Green
                case 2: return new Color(0.2f, 0.4f, 0.6f, 0.8f); // Blue
                case 3: return new Color(0.5f, 0.3f, 0.6f, 0.8f); // Purple
                default:
                    if (color >= 4) return new Color(0.8f, 0.6f, 0.2f, 0.8f); // Gold
                    return new Color(0.3f, 0.3f, 0.35f, 0.8f); // Gray
            }
        }

        private string GetStackTypeName(int stackType)
        {
            switch (stackType)
            {
                case 0: return "General";
                default: return $"Type {stackType}";
            }
        }

        private void BuildUI()
        {
            cachedFont = GameHelper.LoadFont();
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Dim background
            Image dimBg = gameObject.GetComponent<Image>();
            if (dimBg == null)
                dimBg = gameObject.AddComponent<Image>();
            dimBg.color = new Color(0f, 0f, 0f, 0.7f);
            dimBg.raycastTarget = true;

            // Center panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(root, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.2f, 0.15f);
            panelRt.anchorMax = new Vector2(0.8f, 0.85f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.12f, 0.98f);

            // Header bar
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRt, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = Vector2.one;
            headerRt.offsetMin = new Vector2(0f, -50f);
            headerRt.offsetMax = Vector2.zero;
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0f, 0f, 0f, 0.4f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerRt, false);
            RectTransform titleRt2 = titleObj.AddComponent<RectTransform>();
            titleRt2.anchorMin = new Vector2(0f, 0f);
            titleRt2.anchorMax = new Vector2(0.8f, 1f);
            titleRt2.offsetMin = Vector2.zero;
            titleRt2.offsetMax = Vector2.zero;
            titleText = titleObj.AddComponent<Text>();
            titleText.font = cachedFont;
            titleText.fontSize = 26;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;

            // Close button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(headerRt, false);
            RectTransform closeRt = closeObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.9f, 0.1f);
            closeRt.anchorMax = new Vector2(1f, 0.9f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.6f, 0.2f, 0.2f, 0.9f);
            closeButton = closeObj.AddComponent<Button>();
            closeButton.onClick.AddListener(OnCloseClicked);

            GameObject closeTextObj = new GameObject("Text");
            closeTextObj.transform.SetParent(closeRt, false);
            RectTransform closeTextRt = closeTextObj.AddComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = Vector2.zero;
            closeTextRt.offsetMax = Vector2.zero;
            Text closeText = closeTextObj.AddComponent<Text>();
            closeText.font = cachedFont;
            closeText.fontSize = 16;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.text = "X";
            closeText.color = Color.white;

            // Icon area (left side)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(panelRt, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.offsetMin = new Vector2(20f, -80f);
            iconRt.offsetMax = new Vector2(120f, 80f);
            iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.color = new Color(1f, 1f, 1f, 0.3f);

            // Quality text (below icon)
            GameObject qualityObj = new GameObject("Quality");
            qualityObj.transform.SetParent(panelRt, false);
            RectTransform qualityRt = qualityObj.AddComponent<RectTransform>();
            qualityRt.anchorMin = new Vector2(0f, 0.5f);
            qualityRt.anchorMax = new Vector2(0f, 0.5f);
            qualityRt.offsetMin = new Vector2(20f, -110f);
            qualityRt.offsetMax = new Vector2(120f, -80f);
            qualityText = qualityObj.AddComponent<Text>();
            qualityText.font = cachedFont;
            qualityText.fontSize = 16;
            qualityText.alignment = TextAnchor.MiddleCenter;
            qualityText.color = Color.white;

            // Level text (below quality)
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(panelRt, false);
            RectTransform levelRt = levelObj.AddComponent<RectTransform>();
            levelRt.anchorMin = new Vector2(0f, 0.5f);
            levelRt.anchorMax = new Vector2(0f, 0.5f);
            levelRt.offsetMin = new Vector2(20f, -140f);
            levelRt.offsetMax = new Vector2(120f, -110f);
            levelText = levelObj.AddComponent<Text>();
            levelText.font = cachedFont;
            levelText.fontSize = 14;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = new Color(0.7f, 0.7f, 0.7f);

            // Description (right side, top)
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(panelRt, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.15f, 0.55f);
            descRt.anchorMax = new Vector2(1f, 0.85f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = new Vector2(-20f, 0f);
            descriptionText = descObj.AddComponent<Text>();
            descriptionText.font = cachedFont;
            descriptionText.fontSize = 18;
            descriptionText.alignment = TextAnchor.UpperLeft;
            descriptionText.color = new Color(0.8f, 0.8f, 0.8f);

            // Passive skills section
            GameObject passiveHeaderObj = new GameObject("PassiveHeader");
            passiveHeaderObj.transform.SetParent(panelRt, false);
            RectTransform passiveHeaderRt = passiveHeaderObj.AddComponent<RectTransform>();
            passiveHeaderRt.anchorMin = new Vector2(0.15f, 0.4f);
            passiveHeaderRt.anchorMax = new Vector2(1f, 0.5f);
            passiveHeaderRt.offsetMin = Vector2.zero;
            passiveHeaderRt.offsetMax = new Vector2(-20f, 0f);
            Text passiveHeaderText = passiveHeaderObj.AddComponent<Text>();
            passiveHeaderText.font = cachedFont;
            passiveHeaderText.fontSize = 16;
            passiveHeaderText.alignment = TextAnchor.UpperLeft;
            passiveHeaderText.text = "Passive Skills:";
            passiveHeaderText.color = new Color(0.6f, 0.8f, 1f);

            GameObject passiveListObj = new GameObject("PassiveList");
            passiveListObj.transform.SetParent(panelRt, false);
            RectTransform passiveListRt = passiveListObj.AddComponent<RectTransform>();
            passiveListRt.anchorMin = new Vector2(0.15f, 0.2f);
            passiveListRt.anchorMax = new Vector2(1f, 0.4f);
            passiveListRt.offsetMin = Vector2.zero;
            passiveListRt.offsetMax = new Vector2(-20f, 0f);
            passiveListText = passiveListObj.AddComponent<Text>();
            passiveListText.font = cachedFont;
            passiveListText.fontSize = 14;
            passiveListText.alignment = TextAnchor.UpperLeft;
            passiveListText.color = new Color(0.7f, 0.7f, 0.7f);

            // Stats section (bottom)
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(panelRt, false);
            RectTransform statsRt = statsObj.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 0f);
            statsRt.anchorMax = new Vector2(1f, 0.18f);
            statsRt.offsetMin = new Vector2(20f, 5f);
            statsRt.offsetMax = new Vector2(-20f, 0f);

            // Gold bonus
            GameObject goldObj = new GameObject("GoldBonus");
            goldObj.transform.SetParent(statsRt, false);
            RectTransform goldRt = goldObj.AddComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0f, 0.5f);
            goldRt.anchorMax = new Vector2(0.5f, 1f);
            goldRt.offsetMin = Vector2.zero;
            goldRt.offsetMax = Vector2.zero;
            goldBonusText = goldObj.AddComponent<Text>();
            goldBonusText.font = cachedFont;
            goldBonusText.fontSize = 14;
            goldBonusText.alignment = TextAnchor.MiddleLeft;
            goldBonusText.color = new Color(1f, 0.85f, 0.3f);

            // Stack type
            GameObject stackTypeObj = new GameObject("StackType");
            stackTypeObj.transform.SetParent(statsRt, false);
            RectTransform stackTypeRt = stackTypeObj.AddComponent<RectTransform>();
            stackTypeRt.anchorMin = new Vector2(0f, 0f);
            stackTypeRt.anchorMax = new Vector2(0.5f, 0.5f);
            stackTypeRt.offsetMin = Vector2.zero;
            stackTypeRt.offsetMax = Vector2.zero;
            stackTypeText = stackTypeObj.AddComponent<Text>();
            stackTypeText.font = cachedFont;
            stackTypeText.fontSize = 14;
            stackTypeText.alignment = TextAnchor.MiddleLeft;
            stackTypeText.color = new Color(0.7f, 0.7f, 0.7f);

            // Max stack
            GameObject maxStackObj = new GameObject("MaxStack");
            maxStackObj.transform.SetParent(statsRt, false);
            RectTransform maxStackRt = maxStackObj.AddComponent<RectTransform>();
            maxStackRt.anchorMin = new Vector2(0.5f, 0f);
            maxStackRt.anchorMax = new Vector2(1f, 0.5f);
            maxStackRt.offsetMin = Vector2.zero;
            maxStackRt.offsetMax = Vector2.zero;
            maxStackText = maxStackObj.AddComponent<Text>();
            maxStackText.font = cachedFont;
            maxStackText.fontSize = 14;
            maxStackText.alignment = TextAnchor.MiddleLeft;
            maxStackText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void OnCloseClicked()
        {
            WinManager.Instance.CloseWin<CollectionDetailWin>();
        }
    }
}
