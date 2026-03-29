using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class StoryCollectionWin : BaseWin
    {
        // List panel
        private Transform listContent;
        private readonly List<GameObject> listItems = new List<GameObject>();
        private readonly Dictionary<int, Image> itemBgMap = new Dictionary<int, Image>();

        // Detail panel
        private Image detailIcon;
        private Text detailName;
        private Text detailDesc;
        private Text detailProgress;
        private Text detailSaveStatus;
        private Button startButton;
        private Button continueButton;
        private GameObject continueButtonObj;

        // Confirm overlay
        private GameObject confirmOverlay;

        private Font cachedFont;
        private int selectedCollectionId;

        public override void Init()
        {
            base.Init();
            cachedFont = GameHelper.LoadFont();
            BuildUI();
        }

        public override void Show()
        {
            base.Show();
            if (confirmOverlay != null)
                confirmOverlay.SetActive(false);
            RefreshList();
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        // ===== UI Construction =====

        private void BuildUI()
        {
            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            // Background
            Image rootBg = gameObject.GetComponent<Image>();
            if (rootBg == null)
            {
                rootBg = gameObject.AddComponent<Image>();
                rootBg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            }

            // Header
            GameObject header = CreatePanel(rootRt, "Header",
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -50f), Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            RectTransform headerRt = header.GetComponent<RectTransform>();

            // Close button
            CreateTextButton(headerRt, "CloseButton",
                new Vector2(1f, 0f), Vector2.one,
                new Vector2(-50f, 2f), new Vector2(-5f, -2f),
                new Color(0.5f, 0.2f, 0.2f, 0.9f), 22, "X",
                () => WinManager.Instance.CloseWin<StoryCollectionWin>());

            // Title
            Text titleText = CreateText(headerRt, "Title",
                Vector2.zero, Vector2.one,
                new Vector2(15f, 0f), new Vector2(-60f, 0f),
                28, Color.white, TextAnchor.MiddleLeft);
            titleText.text = "故事集";

            // Content area (below header)
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(rootRt, false);
            RectTransform contentRt = contentArea.AddComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.offsetMin = new Vector2(10f, 10f);
            contentRt.offsetMax = new Vector2(-10f, -55f);

            // Left panel - List (40%)
            BuildListPanel(contentRt);

            // Right panel - Detail (60%)
            BuildDetailPanel(contentRt);

            // Confirm overlay (hidden by default)
            BuildConfirmOverlay(rootRt);
        }

        private void BuildListPanel(RectTransform parent)
        {
            GameObject listPanel = CreatePanel(parent, "ListPanel",
                Vector2.zero, new Vector2(0.38f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.08f, 0.15f, 0.8f));
            RectTransform listPanelRt = listPanel.GetComponent<RectTransform>();

            // ScrollRect
            GameObject scrollObj = new GameObject("ScrollRect");
            scrollObj.transform.SetParent(listPanelRt, false);
            RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(5f, 5f);
            scrollRt.offsetMax = new Vector2(-5f, -5f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            Image scrollMask = scrollObj.AddComponent<Image>();
            scrollMask.color = Color.clear;
            Mask mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(0, 0, 2, 2);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            listContent = contentRt;
        }

        private void BuildDetailPanel(RectTransform parent)
        {
            GameObject detailPanel = CreatePanel(parent, "DetailPanel",
                new Vector2(0.4f, 0f), Vector2.one,
                Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.08f, 0.15f, 0.5f));
            RectTransform detailRt = detailPanel.GetComponent<RectTransform>();

            // Icon (top center)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(detailRt, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -20f);
            iconRt.sizeDelta = new Vector2(120f, 120f);
            detailIcon = iconObj.AddComponent<Image>();
            detailIcon.preserveAspect = true;
            detailIcon.color = new Color(0.2f, 0.2f, 0.3f);

            // Name
            detailName = CreateText(detailRt, "DetailName",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(15f, -155f), new Vector2(-15f, -150f),
                26, Color.white, TextAnchor.MiddleCenter);
            RectTransform nameRt = detailName.GetComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x, 35f);

            // Description
            detailDesc = CreateText(detailRt, "DetailDesc",
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.75f),
                Vector2.zero, Vector2.zero,
                20, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft);

            // Progress
            detailProgress = CreateText(detailRt, "DetailProgress",
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.38f),
                Vector2.zero, Vector2.zero,
                22, new Color(0.9f, 0.8f, 0.3f), TextAnchor.MiddleLeft);

            // Save status
            detailSaveStatus = CreateText(detailRt, "DetailSaveStatus",
                new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.28f),
                Vector2.zero, Vector2.zero,
                18, new Color(0.5f, 0.8f, 0.5f), TextAnchor.MiddleLeft);

            // Buttons area
            // Start New button
            startButton = CreateTextButton(detailRt, "StartButton",
                new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f),
                new Vector2(-170f, 0f), new Vector2(-15f, 45f),
                new Color(0.2f, 0.35f, 0.55f, 0.95f), 22, "新冒险",
                OnStartNewClicked);

            // Continue button
            continueButtonObj = new GameObject("ContinueButtonWrapper");
            continueButtonObj.transform.SetParent(detailRt, false);
            RectTransform contWrapRt = continueButtonObj.AddComponent<RectTransform>();
            contWrapRt.anchorMin = new Vector2(0.5f, 0.06f);
            contWrapRt.anchorMax = new Vector2(0.5f, 0.06f);
            contWrapRt.offsetMin = new Vector2(15f, 0f);
            contWrapRt.offsetMax = new Vector2(170f, 45f);

            continueButton = CreateTextButton(contWrapRt, "ContinueButton",
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.5f, 0.25f, 0.95f), 22, "继续",
                OnContinueClicked);
        }

        private void BuildConfirmOverlay(RectTransform parent)
        {
            confirmOverlay = new GameObject("ConfirmOverlay");
            confirmOverlay.transform.SetParent(parent, false);
            RectTransform overlayRt = confirmOverlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            // Dim background
            Image dimBg = confirmOverlay.AddComponent<Image>();
            dimBg.color = new Color(0f, 0f, 0f, 0.7f);

            // Block clicks on underlying UI
            confirmOverlay.AddComponent<Button>().onClick.AddListener(() => { });

            // Center panel
            GameObject panel = CreatePanel(overlayRt, "ConfirmPanel",
                new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f),
                Vector2.zero, Vector2.zero,
                new Color(0.1f, 0.1f, 0.2f, 0.98f));
            RectTransform panelRt = panel.GetComponent<RectTransform>();

            // Message
            Text msgText = CreateText(panelRt, "Message",
                new Vector2(0f, 0.45f), Vector2.one,
                new Vector2(15f, 0f), new Vector2(-15f, -10f),
                22, Color.white, TextAnchor.MiddleCenter);
            msgText.text = "现有存档将被覆盖。\n确定开始新冒险吗？";

            // Yes button
            CreateTextButton(panelRt, "YesButton",
                new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
                new Vector2(-130f, 0f), new Vector2(-10f, 40f),
                new Color(0.5f, 0.25f, 0.2f, 0.95f), 22, "确定",
                OnConfirmOverwrite);

            // No button
            CreateTextButton(panelRt, "NoButton",
                new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
                new Vector2(10f, 0f), new Vector2(130f, 40f),
                new Color(0.3f, 0.3f, 0.4f, 0.95f), 22, "取消",
                OnCancelOverwrite);

            confirmOverlay.SetActive(false);
        }

        // ===== Data Refresh =====

        private void RefreshList()
        {
            // Clear old items
            foreach (var item in listItems)
            {
                if (item != null) Destroy(item);
            }
            listItems.Clear();
            itemBgMap.Clear();

            if (ConfigManager.Instance == null) return;

            var collections = ConfigManager.Instance.StoryCollectionConfigs;
            if (collections == null || collections.Count == 0) return;

            bool hasSelection = false;

            for (int i = 0; i < collections.Count; i++)
            {
                StoryCollectionConfig config = collections[i];
                GameObject itemObj = CreateListItem(config);
                listItems.Add(itemObj);

                if (!hasSelection)
                {
                    selectedCollectionId = config.id;
                    hasSelection = true;
                }
            }

            UpdateListSelection();
            RefreshDetail();
        }

        private GameObject CreateListItem(StoryCollectionConfig config)
        {
            GameObject itemObj = new GameObject($"Item_{config.id}");
            itemObj.transform.SetParent(listContent, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.25f, 0.9f);
            itemBgMap[config.id] = bg;

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 100;

            Button btn = itemObj.AddComponent<Button>();
            int capturedId = config.id;
            btn.onClick.AddListener(() => SelectCollection(capturedId));

            RectTransform itemRt = itemObj.GetComponent<RectTransform>();

            // Icon (left side)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.1f);
            iconRt.anchorMax = new Vector2(0f, 0.9f);
            iconRt.offsetMin = new Vector2(8f, 0f);
            iconRt.offsetMax = new Vector2(88f, 0f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            if (!string.IsNullOrEmpty(config.icon))
            {
                Sprite sp = GameHelper.LoadSprite(config.icon);
                if (sp != null)
                {
                    iconImg.sprite = sp;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = new Color(0.2f, 0.2f, 0.3f);
                }
            }
            else
            {
                iconImg.color = new Color(0.2f, 0.2f, 0.3f);
            }

            // Name
            Text nameText = CreateText(itemRt, "Name",
                new Vector2(0f, 0.5f), Vector2.one,
                new Vector2(95f, 0f), new Vector2(-8f, -5f),
                22, Color.white, TextAnchor.MiddleLeft);
            nameText.text = config.name ?? "";
            nameText.raycastTarget = false;

            // Completion + save status line
            float completionRate = StoryManager.Instance != null
                ? StoryManager.Instance.GetCompletionRate(config.id) : 0f;
            bool hasSave = StoryManager.Instance != null
                && StoryManager.Instance.HasSave(config.id);

            string statusLine = $"{Mathf.RoundToInt(completionRate * 100)}%";
            if (hasSave) statusLine += "  [存档]";

            Text statusText = CreateText(itemRt, "Status",
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(95f, 5f), new Vector2(-8f, 0f),
                17, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleLeft);
            statusText.text = statusLine;
            statusText.raycastTarget = false;

            return itemObj;
        }

        private void SelectCollection(int collectionId)
        {
            selectedCollectionId = collectionId;
            UpdateListSelection();
            RefreshDetail();

            if (confirmOverlay != null)
                confirmOverlay.SetActive(false);
        }

        private void UpdateListSelection()
        {
            foreach (var kvp in itemBgMap)
            {
                kvp.Value.color = kvp.Key == selectedCollectionId
                    ? new Color(0.18f, 0.3f, 0.45f, 0.95f)
                    : new Color(0.12f, 0.12f, 0.25f, 0.9f);
            }
        }

        private void RefreshDetail()
        {
            StoryCollectionConfig config = ConfigManager.Instance != null
                ? ConfigManager.Instance.GetStoryCollectionConfig(selectedCollectionId)
                : null;

            if (config == null)
            {
                if (detailName != null) detailName.text = "";
                if (detailDesc != null) detailDesc.text = "";
                if (detailProgress != null) detailProgress.text = "";
                if (detailSaveStatus != null) detailSaveStatus.text = "";
                if (startButton != null) startButton.interactable = false;
                if (continueButtonObj != null) continueButtonObj.SetActive(false);
                return;
            }

            // Icon
            if (detailIcon != null)
            {
                if (!string.IsNullOrEmpty(config.icon))
                {
                    Sprite sp = GameHelper.LoadSprite(config.icon);
                    if (sp != null)
                    {
                        detailIcon.sprite = sp;
                        detailIcon.color = Color.white;
                    }
                    else
                    {
                        detailIcon.sprite = null;
                        detailIcon.color = new Color(0.2f, 0.2f, 0.3f);
                    }
                }
                else
                {
                    detailIcon.sprite = null;
                    detailIcon.color = new Color(0.2f, 0.2f, 0.3f);
                }
            }

            // Name
            if (detailName != null)
                detailName.text = config.name ?? "";

            // Description
            if (detailDesc != null)
                detailDesc.text = config.description ?? "";

            // Progress
            if (detailProgress != null)
            {
                StoryProgressData progress = StoryManager.Instance != null
                    ? StoryManager.Instance.GetProgress(config.id) : null;
                int unlocked = progress != null ? progress.unlockedEndingIds.Count : 0;
                int total = config.endingNodeIds != null ? config.endingNodeIds.Length : 0;
                float rate = StoryManager.Instance != null
                    ? StoryManager.Instance.GetCompletionRate(config.id) : 0f;
                detailProgress.text = $"结局: {unlocked}/{total}  ({Mathf.RoundToInt(rate * 100)}%)";
            }

            // Save status
            bool hasSave = StoryManager.Instance != null
                && StoryManager.Instance.HasSave(config.id);
            if (detailSaveStatus != null)
            {
                detailSaveStatus.text = hasSave ? "存档已存在" : "";
                detailSaveStatus.color = hasSave
                    ? new Color(0.5f, 0.8f, 0.5f)
                    : Color.clear;
            }

            // Buttons
            if (startButton != null)
                startButton.interactable = true;
            if (continueButtonObj != null)
                continueButtonObj.SetActive(hasSave);
        }

        // ===== Actions =====

        private void OnStartNewClicked()
        {
            if (StoryManager.Instance == null) return;

            bool hasSave = StoryManager.Instance.HasSave(selectedCollectionId);
            if (hasSave)
            {
                if (confirmOverlay != null)
                    confirmOverlay.SetActive(true);
            }
            else
            {
                StartAdventure();
            }
        }

        private void OnContinueClicked()
        {
            if (StoryManager.Instance == null) return;

            if (StoryManager.Instance.ContinueAdventure(selectedCollectionId))
            {
                WinManager.Instance.CloseWin<StoryCollectionWin>();
                StoryManager.Instance.EnterStoryScene();
            }
        }

        private void OnConfirmOverwrite()
        {
            if (confirmOverlay != null)
                confirmOverlay.SetActive(false);
            StartAdventure();
        }

        private void OnCancelOverwrite()
        {
            if (confirmOverlay != null)
                confirmOverlay.SetActive(false);
        }

        private void StartAdventure()
        {
            if (StoryManager.Instance == null) {
                Debug.LogError("StoryManager is null");
                return;
            };

            StoryManager.Instance.StartNewAdventure(selectedCollectionId);
            WinManager.Instance.CloseWin<StoryCollectionWin>();
            StoryManager.Instance.EnterStoryScene();
        }

        // ===== UI Builders =====

        private GameObject CreatePanel(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private Text CreateText(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            int fontSize, Color color, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Text text = obj.AddComponent<Text>();
            text.font = cachedFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private Button CreateTextButton(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color bgColor, int fontSize, string label,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            Image img = obj.AddComponent<Image>();
            img.color = bgColor;
            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textObj.AddComponent<Text>();
            text.font = cachedFont;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.color = Color.white;

            return btn;
        }
    }
}
