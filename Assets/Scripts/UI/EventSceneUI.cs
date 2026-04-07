using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class EventSceneUI : MonoBehaviour
    {
        // Shop UI
        private GameObject shopPanel;
        private Text shopNameText;
        private Text shopGoldText;
        private Transform shopItemContent;
        private readonly List<GameObject> shopItemObjs = new List<GameObject>();
        private readonly List<ShopItemState> shopItemStates = new List<ShopItemState>();

        // Ending UI
        private GameObject endingPanel;
        private Image endingCgImage;
        private Text endingNameText;
        private Text endingTypeText;
        private Button retryButton;

        private Font cachedFont;
        private StoryNodeConfig currentNode;

        private class ShopItemState
        {
            public int effectId;
            public int price;
            public Button buyButton;
            public Text buyButtonText;
            public bool sold;
        }

        private void Start()
        {
            if (StoryManager.Instance == null || !StoryManager.Instance.IsInAdventure)
            {
                GameHelper.LoadScene("MainMenu");
                return;
            }

            currentNode = StoryManager.Instance.CurrentNode;
            if (currentNode == null)
            {
                GameHelper.LoadScene("MainMenu");
                return;
            }

            cachedFont = GameHelper.LoadFont();

            switch (currentNode.type)
            {
                case StoryNodeType.Event:
                    HandleEventNode();
                    break;
                case StoryNodeType.Shop:
                    HandleShopNode();
                    break;
                case StoryNodeType.Ending:
                    HandleEndingNode();
                    break;
                default:
                    StoryManager.Instance.EnterStoryScene();
                    break;
            }
        }

        // ===== Event Node =====

        private void HandleEventNode()
        {
            SetupCanvas();

            if (currentNode.dialogueId > 0)
            {
                ShowDialogue(currentNode.dialogueId, OnEventDialogueComplete);
            }
            else if (currentNode.choiceGroupId > 0)
            {
                ShowChoices(currentNode.choiceGroupId, OnEventChoicesComplete);
            }
            else
            {
                FinishEventNode();
            }
        }

        private void OnEventDialogueComplete()
        {
            if (currentNode.choiceGroupId > 0)
            {
                ShowChoices(currentNode.choiceGroupId, OnEventChoicesComplete);
            }
            else
            {
                FinishEventNode();
            }
        }

        private void OnEventChoicesComplete(int index, ChoiceGroupConfig.OptionsItem option)
        {
            if (option != null)
                StoryManager.Instance.ProcessChoice(index, option);

            FinishEventNode();
        }

        private void FinishEventNode()
        {
            StoryManager.Instance.AdvanceToNextNode();
            StoryManager.Instance.EnterStoryScene();
        }

        // ===== Shop Node =====

        private void HandleShopNode()
        {
            if (currentNode.shopId <= 0)
            {
                FinishShopNode();
                return;
            }

            EventShopConfig shopConfig = Cfg.EventShop.Get(currentNode.shopId);
            if (shopConfig == null)
            {
                FinishShopNode();
                return;
            }

            BuildShopUI(shopConfig);
        }

        private void BuildShopUI(EventShopConfig shopConfig)
        {
            SetupCanvas();
            RectTransform rootRt = GetComponent<RectTransform>();

            // Background
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f);

            // Shop panel
            shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(rootRt, false);
            RectTransform panelRt = shopPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // Header
            GameObject header = CreatePanel(panelRt, "Header",
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -60f), Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f));
            RectTransform headerRt = header.GetComponent<RectTransform>();

            // Leave button
            Button leaveBtn = CreateTextButton(headerRt, "LeaveButton",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(10f, 5f), new Vector2(100f, -5f),
                new Color(0.4f, 0.2f, 0.2f, 0.9f), 22, "Leave");
            leaveBtn.onClick.AddListener(FinishShopNode);

            // Shop name
            shopNameText = CreateText(headerRt, "ShopName",
                new Vector2(0.2f, 0f), new Vector2(0.7f, 1f),
                Vector2.zero, Vector2.zero,
                28, Color.white, TextAnchor.MiddleCenter);
            shopNameText.text = shopConfig.name ?? "";

            // Gold
            shopGoldText = CreateText(headerRt, "Gold",
                new Vector2(0.7f, 0f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(-15f, 0f),
                24, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleRight);
            UpdateShopGold();

            // Item list area
            GameObject listArea = new GameObject("ListArea");
            listArea.transform.SetParent(panelRt, false);
            RectTransform listRt = listArea.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.1f, 0.05f);
            listRt.anchorMax = new Vector2(0.9f, 0.9f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;

            // Content with vertical layout
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(listRt, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            shopItemContent = contentRt;

            // Select items from pool (weighted random, up to refreshCount)
            List<EventShopConfig.ItemsItem> selectedItems = SelectShopItems(shopConfig);

            for (int i = 0; i < selectedItems.Count; i++)
            {
                CreateShopItemUI(selectedItems[i]);
            }

            StoryManager.Instance.OnGoldChanged += HandleShopGoldChanged;
        }

        private List<EventShopConfig.ItemsItem> SelectShopItems(EventShopConfig config)
        {
            List<EventShopConfig.ItemsItem> pool = new List<EventShopConfig.ItemsItem>();
            if (config.items != null)
            {
                for (int i = 0; i < config.items.Length; i++)
                    pool.Add(config.items[i]);
            }

            int count = Mathf.Min(config.refreshCount, pool.Count);
            List<EventShopConfig.ItemsItem> selected = new List<EventShopConfig.ItemsItem>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int totalWeight = 0;
                for (int j = 0; j < pool.Count; j++)
                    totalWeight += pool[j].weight;

                int roll = Random.Range(0, totalWeight);
                int cumulative = 0;
                int pick = 0;
                for (int j = 0; j < pool.Count; j++)
                {
                    cumulative += pool[j].weight;
                    if (roll < cumulative)
                    {
                        pick = j;
                        break;
                    }
                }

                selected.Add(pool[pick]);
                pool.RemoveAt(pick);
            }

            return selected;
        }

        private void CreateShopItemUI(EventShopConfig.ItemsItem item)
        {
            PassiveEffectConfig effect = Cfg.PassiveEffect.Get(item.effectId);
            if (effect == null) return;

            GameObject itemObj = new GameObject($"ShopItem_{item.effectId}");
            itemObj.transform.SetParent(shopItemContent, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.25f, 0.9f);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 90;

            // Effect name (top-left)
            Text nameText = CreateText(itemObj.GetComponent<RectTransform>(), "Name",
                new Vector2(0f, 0.5f), Vector2.one,
                new Vector2(15f, 0f), new Vector2(-150f, -5f),
                24, Color.white, TextAnchor.MiddleLeft);
            nameText.text = effect.name;
            nameText.raycastTarget = false;

            // Description (bottom-left)
            Text descText = CreateText(itemObj.GetComponent<RectTransform>(), "Desc",
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(15f, 5f), new Vector2(-150f, 0f),
                18, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);
            descText.text = effect.description ?? "";
            descText.raycastTarget = false;

            // Price + Buy button (right side)
            Button buyBtn = CreateTextButton(itemObj.GetComponent<RectTransform>(), "BuyButton",
                new Vector2(1f, 0.15f), new Vector2(1f, 0.85f),
                new Vector2(-135f, 0f), new Vector2(-10f, 0f),
                new Color(0.2f, 0.45f, 0.2f, 0.95f), 20, $"{item.price}G");

            ShopItemState state = new ShopItemState
            {
                effectId = item.effectId,
                price = item.price,
                buyButton = buyBtn,
                buyButtonText = buyBtn.GetComponentInChildren<Text>(),
                sold = false
            };

            int capturedIndex = shopItemStates.Count;
            buyBtn.onClick.AddListener(() => OnBuyClicked(capturedIndex));

            shopItemStates.Add(state);
            shopItemObjs.Add(itemObj);
        }

        private void OnBuyClicked(int stateIndex)
        {
            if (stateIndex < 0 || stateIndex >= shopItemStates.Count) return;
            ShopItemState state = shopItemStates[stateIndex];
            if (state.sold) return;

            bool success = StoryManager.Instance.PurchaseEffect(state.effectId, state.price);
            if (success)
            {
                state.sold = true;
                UpdateShopItemVisual(state);
                RefreshBuyButtons();
            }
        }

        private void UpdateShopItemVisual(ShopItemState state)
        {
            if (state.buyButton != null)
                state.buyButton.interactable = false;
            if (state.buyButtonText != null)
                state.buyButtonText.text = "Sold";
        }

        private void RefreshBuyButtons()
        {
            int gold = StoryManager.Instance.GetGold();
            for (int i = 0; i < shopItemStates.Count; i++)
            {
                ShopItemState s = shopItemStates[i];
                if (s.sold || s.buyButton == null) continue;
                s.buyButton.interactable = gold >= s.price;
            }
        }

        private void UpdateShopGold()
        {
            if (shopGoldText != null)
                shopGoldText.text = $"Gold: {StoryManager.Instance.GetGold()}";
        }

        private void HandleShopGoldChanged(int gold)
        {
            if (shopGoldText != null)
                shopGoldText.text = $"Gold: {gold}";
            RefreshBuyButtons();
        }

        private void FinishShopNode()
        {
            if (StoryManager.Instance == null) return;

            StoryManager.Instance.OnGoldChanged -= HandleShopGoldChanged;
            StoryManager.Instance.AdvanceToNextNode();
            StoryManager.Instance.EnterStoryScene();
        }

        // ===== Ending Node =====

        private void HandleEndingNode()
        {
            SetupCanvas();

            if (currentNode.dialogueId > 0)
            {
                ShowDialogue(currentNode.dialogueId, OnEndingDialogueComplete);
            }
            else
            {
                ShowEndingPanel();
            }
        }

        private void OnEndingDialogueComplete()
        {
            ShowEndingPanel();
        }

        private void ShowEndingPanel()
        {
            RectTransform rootRt = GetComponent<RectTransform>();

            // Background
            if (GetComponent<Image>() == null)
            {
                Image bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.02f, 0.05f);
            }

            endingPanel = new GameObject("EndingPanel");
            endingPanel.transform.SetParent(rootRt, false);
            RectTransform panelRt = endingPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // CG Image (center, large)
            if (!string.IsNullOrEmpty(currentNode.endingCg))
            {
                GameObject cgObj = new GameObject("EndingCG");
                cgObj.transform.SetParent(panelRt, false);
                RectTransform cgRt = cgObj.AddComponent<RectTransform>();
                cgRt.anchorMin = new Vector2(0.1f, 0.3f);
                cgRt.anchorMax = new Vector2(0.9f, 0.9f);
                cgRt.offsetMin = Vector2.zero;
                cgRt.offsetMax = Vector2.zero;
                endingCgImage = cgObj.AddComponent<Image>();
                endingCgImage.preserveAspect = true;

                Sprite cgSprite = GameHelper.LoadSprite(currentNode.endingCg);
                if (cgSprite != null)
                {
                    endingCgImage.sprite = cgSprite;
                    endingCgImage.color = Color.white;
                }
                else
                {
                    endingCgImage.color = new Color(0.1f, 0.1f, 0.15f);
                }
            }

            // Ending name
            endingNameText = CreateText(panelRt, "EndingName",
                new Vector2(0f, 0.18f), new Vector2(1f, 0.28f),
                new Vector2(20f, 0f), new Vector2(-20f, 0f),
                30, Color.white, TextAnchor.MiddleCenter);
            endingNameText.text = currentNode.name ?? "";

            // Ending type label
            endingTypeText = CreateText(panelRt, "EndingType",
                new Vector2(0f, 0.12f), new Vector2(1f, 0.18f),
                new Vector2(20f, 0f), new Vector2(-20f, 0f),
                22, GetEndingTypeColor(currentNode.endingType), TextAnchor.MiddleCenter);
            endingTypeText.text = GetEndingTypeLabel(currentNode.endingType);

            // Buttons
            float buttonY = 0.04f;

            if (currentNode.endingType == EndingType.Fail)
            {
                // Retry button (left)
                retryButton = CreateTextButton(panelRt, "RetryButton",
                    new Vector2(0.5f, buttonY), new Vector2(0.5f, buttonY),
                    new Vector2(-170f, 0f), new Vector2(-20f, 50f),
                    new Color(0.5f, 0.4f, 0.15f, 0.95f), 24, "回到失败之前");
                retryButton.onClick.AddListener(OnRetryClicked);

                // Return button (right)
                Button returnBtn = CreateTextButton(panelRt, "ReturnButton",
                    new Vector2(0.5f, buttonY), new Vector2(0.5f, buttonY),
                    new Vector2(20f, 0f), new Vector2(170f, 50f),
                    new Color(0.4f, 0.2f, 0.2f, 0.95f), 24, "退出");
                returnBtn.onClick.AddListener(OnReturnClicked);
            }
            else
            {
                // Single return button (centered)
                Button returnBtn = CreateTextButton(panelRt, "ReturnButton",
                    new Vector2(0.5f, buttonY), new Vector2(0.5f, buttonY),
                    new Vector2(-80f, 0f), new Vector2(80f, 50f),
                    new Color(0.2f, 0.3f, 0.5f, 0.95f), 24, "退出");
                returnBtn.onClick.AddListener(OnReturnClicked);
            }
        }

        private void OnRetryClicked()
        {
            if (StoryManager.Instance != null && StoryManager.Instance.RetryFromFailure())
            {
                StoryManager.Instance.EnterStoryScene();
            }
        }

        private void OnReturnClicked()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.EndAdventure(currentNode.id);
                StoryManager.Instance.BackToMainMenu();
            }
        }

        // ===== Shared Helpers =====

        private void ShowDialogue(int dialogueId, System.Action onComplete)
        {
            DialogueConfig config = Cfg.Dialogue.Get(dialogueId);
            if (config == null || config.lines == null || config.lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            DialogueWin win = GameHelper.OpenWin<DialogueWin>();
            win.ShowDialogue(config, onComplete);
        }

        private void ShowChoices(int choiceGroupId, System.Action<int, ChoiceGroupConfig.OptionsItem> onSelected)
        {
            ChoiceGroupConfig config = Cfg.ChoiceGroup.Get(choiceGroupId);
            if (config == null || config.options == null || config.options.Length == 0)
            {
                onSelected?.Invoke(0, null);
                return;
            }

            ChoiceWin win = GameHelper.OpenWin<ChoiceWin>();
            win.ShowChoices(config, onSelected);
        }

        private string GetEndingTypeLabel(int type)
        {
            switch (type)
            {
                case EndingType.Normal: return "- Normal Ending -";
                case EndingType.True: return "- True Ending -";
                case EndingType.Hidden: return "- Hidden Ending -";
                case EndingType.Fail: return "- Failed -";
                default: return "";
            }
        }

        private Color GetEndingTypeColor(int type)
        {
            switch (type)
            {
                case EndingType.Normal: return new Color(0.7f, 0.7f, 0.7f);
                case EndingType.True: return new Color(1f, 0.85f, 0.3f);
                case EndingType.Hidden: return new Color(0.7f, 0.5f, 0.9f);
                case EndingType.Fail: return new Color(1f, 0.3f, 0.3f);
                default: return Color.gray;
            }
        }

        // ===== Canvas Setup =====

        private void SetupCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
            }
            if (GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
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
            Color bgColor, int fontSize, string label)
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

        private void OnDestroy()
        {
            if (StoryManager.Instance != null)
                StoryManager.Instance.OnGoldChanged -= HandleShopGoldChanged;
        }
    }
}
