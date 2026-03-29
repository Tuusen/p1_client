using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class ChoiceWin : BaseWin
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Transform optionListContent;

        private ChoiceGroupConfig currentConfig;
        private Action<int, ChoiceOption> onSelected;
        private List<GameObject> optionItems = new List<GameObject>();

        private float savedTimeScale;
        private bool hasPausedTime;

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

            if (currentConfig != null)
            {
                RestoreTimeScale();
                currentConfig = null;

                Action<int, ChoiceOption> callback = onSelected;
                onSelected = null;
                callback?.Invoke(0, null);
            }
        }

        /// <summary>
        /// 显示选项组。玩家选择后回调返回 (1-based索引, 选中的ChoiceOption)。
        /// 外部关闭窗口时回调返回 (0, null)。
        /// </summary>
        public void ShowChoices(ChoiceGroupConfig config, Action<int, ChoiceOption> onSelected)
        {
            if (config == null || config.options == null || config.options.Length == 0)
            {
                onSelected?.Invoke(0, null);
                return;
            }

            currentConfig = config;
            this.onSelected = onSelected;

            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedTime = true;

            RefreshOptions();
        }

        private void RefreshOptions()
        {
            ClearOptionItems();

            if (titleText != null)
                titleText.text = currentConfig.title ?? "";

            Font font = GameHelper.LoadFont();

            for (int i = 0; i < currentConfig.options.Length; i++)
            {
                ChoiceOption option = currentConfig.options[i];
                int optionIndex = i + 1; // 1-based
                GameObject item = CreateOptionItem(option, optionIndex, font);
                optionItems.Add(item);
            }
        }

        private GameObject CreateOptionItem(ChoiceOption option, int optionIndex, Font font)
        {
            GameObject itemObj = new GameObject($"Option_{option.id}");
            itemObj.transform.SetParent(optionListContent, false);

            // Background
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 90;

            // Button
            Button btn = itemObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.35f, 0.25f, 0.95f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.2f, 0.95f);
            btn.colors = colors;

            ChoiceOption capturedOption = option;
            int capturedIndex = optionIndex;
            btn.onClick.AddListener(() => OnOptionClicked(capturedIndex, capturedOption));

            // Option text (top area)
            GameObject nameObj = new GameObject("Text");
            nameObj.transform.SetParent(itemObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.55f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(15f, 0f);
            nameRt.offsetMax = new Vector2(-15f, -5f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 24;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = option.text ?? "";
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // Description (middle area)
            if (!string.IsNullOrEmpty(option.description))
            {
                GameObject descObj = new GameObject("Desc");
                descObj.transform.SetParent(itemObj.transform, false);
                RectTransform descRt = descObj.AddComponent<RectTransform>();
                descRt.anchorMin = new Vector2(0f, 0.1f);
                descRt.anchorMax = new Vector2(1f, 0.55f);
                descRt.offsetMin = new Vector2(15f, 0f);
                descRt.offsetMax = new Vector2(-15f, 0f);
                Text descText = descObj.AddComponent<Text>();
                descText.font = font;
                descText.fontSize = 18;
                descText.alignment = TextAnchor.MiddleLeft;
                descText.text = option.description;
                descText.color = new Color(0.75f, 0.75f, 0.75f);
                descText.raycastTarget = false;
            }

            // Reward hints (bottom-right area)
            string rewardHint = BuildRewardHint(option);
            if (!string.IsNullOrEmpty(rewardHint))
            {
                GameObject rewardObj = new GameObject("Reward");
                rewardObj.transform.SetParent(itemObj.transform, false);
                RectTransform rewardRt = rewardObj.AddComponent<RectTransform>();
                rewardRt.anchorMin = new Vector2(0.5f, 0f);
                rewardRt.anchorMax = new Vector2(1f, 0.45f);
                rewardRt.offsetMin = new Vector2(0f, 2f);
                rewardRt.offsetMax = new Vector2(-15f, 0f);
                Text rewardText = rewardObj.AddComponent<Text>();
                rewardText.font = font;
                rewardText.fontSize = 16;
                rewardText.alignment = TextAnchor.MiddleRight;
                rewardText.text = rewardHint;
                rewardText.color = new Color(1f, 0.85f, 0.3f);
                rewardText.raycastTarget = false;
            }

            return itemObj;
        }

        private string BuildRewardHint(ChoiceOption option)
        {
            List<string> hints = new List<string>();

            if (option.effectId > 0)
            {
                PassiveEffectConfig effect = ConfigManager.Instance.GetPassiveEffectConfig(option.effectId);
                if (effect != null)
                    hints.Add(effect.name);
            }

            if (option.goldReward > 0)
                hints.Add($"+{option.goldReward} 金币");

            if (option.triggerBattle)
                hints.Add("触发战斗");

            return hints.Count > 0 ? string.Join("  ", hints) : null;
        }

        private void OnOptionClicked(int optionIndex, ChoiceOption option)
        {
            if (currentConfig == null) return;

            RestoreTimeScale();
            currentConfig = null;

            Action<int, ChoiceOption> callback = onSelected;
            onSelected = null;

            WinManager.Instance.CloseWin<ChoiceWin>();
            callback?.Invoke(optionIndex, option);
        }

        private void ClearOptionItems()
        {
            for (int i = 0; i < optionItems.Count; i++)
            {
                if (optionItems[i] != null)
                    Destroy(optionItems[i]);
            }
            optionItems.Clear();
        }

        private void RestoreTimeScale()
        {
            if (hasPausedTime)
            {
                Time.timeScale = savedTimeScale;
                hasPausedTime = false;
            }
        }

        // ===== Dynamic UI Build =====

        private void BuildUI()
        {
            Font font = GameHelper.LoadFont();
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Dim background
            Image dimBg = gameObject.AddComponent<Image>();
            dimBg.color = new Color(0f, 0f, 0f, 0.5f);

            // Center panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(root, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.15f, 0.15f);
            panelRt.anchorMax = new Vector2(0.85f, 0.85f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.15f, 0.95f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelRt, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -10f);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // Scroll area for options
            GameObject scrollObj = new GameObject("OptionList");
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
            contentRt.offsetMin = new Vector2(0f, 0f);
            contentRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            optionListContent = contentRt;
        }
    }
}
