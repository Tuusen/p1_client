using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// ChoiceWin 的参数类
    /// </summary>
    public class ChoiceWinParam
    {
        public ChoiceGroupConfig config;
        public Action<int, ChoiceConfig> onSelected;
    }

    public class ChoiceWin : BaseWin
    {
        private ChoiceWinParam data => Data as ChoiceWinParam;

        private Text txt_title;
        private Transform node_content;

        private ChoiceGroupConfig currentConfig;
        private Action<int, ChoiceConfig> onSelected;
        private List<GameObject> optionItems = new List<GameObject>();

        public override void start()
        {
            ShowChoices(data.config, data.onSelected);
        }

        public override void closeWin()
        {
            // 如果关闭界面时候没有选，就帮他选第一个
            if (currentConfig != null)
            {
                RestoreTimeScale();
                currentConfig = null;
                onSelected?.Invoke(0, null);
            }
        }

        /// <summary>
        /// 显示选项组。玩家选择后回调返回 (1-based索引, 选中的ChoiceConfig)。
        /// 外部关闭窗口时回调返回 (0, null)。
        /// </summary>
        public void ShowChoices(ChoiceGroupConfig config, Action<int, ChoiceConfig> onSelected)
        {
            if (config == null || config.choices == null || config.choices.Length == 0)
            {
                RestoreTimeScale();
                onSelected?.Invoke(0, null);
                return;
            }

            currentConfig = config;
            this.onSelected = onSelected;

            // 通过 GameManager 统一管理 TimeScale
            GameManager.Instance.PauseGame();

            RefreshOptions();
        }

        private void RefreshOptions()
        {
            ClearOptionItems();

            txt_title.text = currentConfig.title ?? "";

            Font font = GameHelper.LoadFont();

            for (int i = 0; i < currentConfig.choices.Length; i++)
            {
                int choiceId = currentConfig.choices[i];
                ChoiceConfig choice = Cfg.Choice.Get(choiceId);
                if (choice == null)
                {
                    continue;
                }
                
                int optionIndex = i + 1; // 1-based
                GameObject item = CreateOptionItem(choice, optionIndex, font);
                optionItems.Add(item);
            }
        }

        private GameObject CreateOptionItem(ChoiceConfig choice, int optionIndex, Font font)
        {
            GameObject itemObj = new GameObject($"node_option_{choice.id}");
            itemObj.transform.SetParent(node_content, false);

            // Background
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 90;

            // Button
            Button btn = itemObj.AddComponent<Button>();
            btn.name = $"btn_option_{choice.id}"; // Set button name for event handling
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.35f, 0.25f, 0.95f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.2f, 0.95f);
            btn.colors = colors;

            ChoiceConfig capturedChoice = choice;
            int capturedIndex = optionIndex;
            btn.onClick.AddListener(() => OnOptionClicked(capturedIndex, capturedChoice));

            // Option text (top area)
            GameObject nameObj = new GameObject("txt_optionText");
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
            nameText.text = choice.text ?? "";
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // Description (middle area)
            if (!string.IsNullOrEmpty(choice.des))
            {
                GameObject descObj = new GameObject("txt_optionDesc");
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
                descText.text = choice.des;
                descText.color = new Color(0.75f, 0.75f, 0.75f);
                descText.raycastTarget = false;
            }

            // Reward hints (bottom-right area)
            string rewardHint = BuildRewardHint(choice);
            if (!string.IsNullOrEmpty(rewardHint))
            {
                GameObject rewardObj = new GameObject("txt_optionReward");
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

        private string BuildRewardHint(ChoiceConfig choice)
        {
            List<string> hints = new List<string>();

            if (choice.effectId > 0)
            {
                PassiveEffectConfig effect = Cfg.PassiveEffect.Get(choice.effectId);
                if (effect != null)
                    hints.Add(effect.name);
            }

            if (choice.goldReward > 0)
                hints.Add($"+{choice.goldReward} 金币");

            if (choice.triggerBattle)
                hints.Add("触发战斗");

            return hints.Count > 0 ? string.Join("  ", hints) : null;
        }

        private void OnOptionClicked(int optionIndex, ChoiceConfig choice)
        {
            if (currentConfig == null) return;

            // 已经选择了
            RestoreTimeScale();
            currentConfig = null;
            onSelected?.Invoke(optionIndex, choice);

            OnClose();
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
            GameManager.Instance.ResetTimeScale();
        }
    }
}
