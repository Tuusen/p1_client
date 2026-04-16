using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class CollectionBagWin : BaseWin
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Transform itemListContent;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text countText;

        private List<GameObject> itemObjects = new List<GameObject>();
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
            RefreshItems();
        }

        public override void OnClose()
        {
            base.OnClose();
            ClearItems();
        }

        private void RefreshItems()
        {
            ClearItems();

            if (titleText != null)
                titleText.text = "Collections";

            List<PassiveEffectConfig> ownedEffects = StoryManager.Instance != null
                ? StoryManager.Instance.GetOwnedEffects()
                : new List<PassiveEffectConfig>();

            if (countText != null)
                countText.text = $"Total: {ownedEffects.Count}";

            for (int i = 0; i < ownedEffects.Count; i++)
            {
                CreateCollectionItem(ownedEffects[i], i);
            }
        }

        private void CreateCollectionItem(PassiveEffectConfig config, int index)
        {
            if (config == null) return;

            GameObject itemObj = new GameObject($"CollectionItem_{config.id}");
            itemObj.transform.SetParent(itemListContent, false);

            // Background with color border based on quality
            Image bg = itemObj.AddComponent<Image>();
            bg.color = GetQualityColor(config.color);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 80;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.2f);
            iconRt.anchorMax = new Vector2(0f, 0.8f);
            iconRt.offsetMin = new Vector2(10f, 0f);
            iconRt.offsetMax = new Vector2(70f, 0f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1f, 1f, 1f, 0.3f);

            // Try load icon sprite
            if (!string.IsNullOrEmpty(config.icon))
            {
                Sprite iconSprite = GameHelper.LoadSprite(config.icon);
                if (iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                    iconImg.color = Color.white;
                }
            }

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.55f);
            nameRt.anchorMax = new Vector2(1f, 0.9f);
            nameRt.offsetMin = new Vector2(80f, 0f);
            nameRt.offsetMax = new Vector2(-80f, 0f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = cachedFont;
            nameText.fontSize = 22;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = config.name ?? "";
            nameText.color = GetQualityTextColor(config.color);
            nameText.raycastTarget = false;

            // Level badge
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(itemObj.transform, false);
            RectTransform levelRt = levelObj.AddComponent<RectTransform>();
            levelRt.anchorMin = new Vector2(0f, 0.1f);
            levelRt.anchorMax = new Vector2(0f, 0.45f);
            levelRt.offsetMin = new Vector2(10f, 0f);
            levelRt.offsetMax = new Vector2(70f, 0f);
            Text levelText = levelObj.AddComponent<Text>();
            levelText.font = cachedFont;
            levelText.fontSize = 14;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.text = $"Lv.{config.level}";
            levelText.color = new Color(0.8f, 0.8f, 0.8f);
            levelText.raycastTarget = false;

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(itemObj.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.5f);
            descRt.offsetMin = new Vector2(80f, 5f);
            descRt.offsetMax = new Vector2(-15f, 0f);
            Text descText = descObj.AddComponent<Text>();
            descText.font = cachedFont;
            descText.fontSize = 16;
            descText.alignment = TextAnchor.UpperLeft;
            descText.text = config.des ?? "";
            descText.color = new Color(0.7f, 0.7f, 0.7f);
            descText.raycastTarget = false;

            // Click to open detail
            Button itemBtn = itemObj.AddComponent<Button>();
            ColorBlock colors = itemBtn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.15f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            itemBtn.colors = colors;

            PassiveEffectConfig capturedConfig = config;
            itemBtn.onClick.AddListener(() => OnItemClicked(capturedConfig));

            itemObjects.Add(itemObj);
        }

        private void OnItemClicked(PassiveEffectConfig config)
        {
            if (config == null) return;

            CollectionDetailWin detailWin = GameHelper.OpenWin<CollectionDetailWin>();
            if (detailWin != null)
            {
                detailWin.ShowDetail(config);
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < itemObjects.Count; i++)
            {
                if (itemObjects[i] != null)
                    Destroy(itemObjects[i]);
            }
            itemObjects.Clear();
        }

        private Color GetQualityColor(int color)
        {
            // color: 1=common(green), 2=rare(blue), 3=epic(purple), higher=legendary(gold)
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
            dimBg.color = new Color(0f, 0f, 0f, 0.6f);
            dimBg.raycastTarget = true;

            // Center panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(root, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.1f, 0.1f);
            panelRt.anchorMax = new Vector2(0.9f, 0.9f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.12f, 0.98f);

            // Header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRt, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = Vector2.one;
            headerRt.offsetMin = new Vector2(0f, -60f);
            headerRt.offsetMax = Vector2.zero;
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0f, 0f, 0f, 0.4f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerRt, false);
            RectTransform titleRt2 = titleObj.AddComponent<RectTransform>();
            titleRt2.anchorMin = new Vector2(0f, 0f);
            titleRt2.anchorMax = new Vector2(0.7f, 1f);
            titleRt2.offsetMin = Vector2.zero;
            titleRt2.offsetMax = Vector2.zero;
            titleText = titleObj.AddComponent<Text>();
            titleText.font = cachedFont;
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = "Collections";
            titleText.color = Color.white;

            // Count text
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(headerRt, false);
            RectTransform countRt = countObj.AddComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0.7f, 0f);
            countRt.anchorMax = new Vector2(0.85f, 1f);
            countRt.offsetMin = Vector2.zero;
            countRt.offsetMax = Vector2.zero;
            countText = countObj.AddComponent<Text>();
            countText.font = cachedFont;
            countText.fontSize = 18;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.text = "Total: 0";
            countText.color = new Color(0.7f, 0.85f, 1f);

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

            // Scroll area for items
            GameObject scrollObj = new GameObject("ItemList");
            scrollObj.transform.SetParent(panelRt, false);
            RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(15f, 15f);
            scrollRt.offsetMax = new Vector2(-15f, -70f);

            // Content container with vertical layout
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollRt, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            itemListContent = contentRt;

            // Scroll rect
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 30f;

            Mask scrollMask = scrollObj.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.2f);
        }

        private void OnCloseClicked()
        {
            WinManager.Instance.CloseWin<CollectionBagWin>();
        }
    }
}
