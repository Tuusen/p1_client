using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// CollectionBagWin 的参数类
    /// </summary>
    public class CollectionBagWinParam
    {
    }

    public class CollectionBagWin : BaseWin
    {
        private CollectionBagWinParam data => Data as CollectionBagWinParam;
        private Text txt_title;
        private Transform node_content;
        private Button btn_close;
        private Text txt_count;

        private List<GameObject> itemObjects = new List<GameObject>();

        public override void start()
        {
            RefreshItems();
        }

        public override void closeWin()
        {
            ClearItems();
        }

        private void RefreshItems()
        {
            ClearItems();

            txt_title.text = "Collections";

            List<PassiveEffectConfig> ownedEffects = StoryManager.Instance.GetOwnedEffects();

            txt_count.text = $"Total: {ownedEffects.Count}";

            for (int i = 0; i < ownedEffects.Count; i++)
            {
                CreateCollectionItem(ownedEffects[i], i);
            }
        }

        private void CreateCollectionItem(PassiveEffectConfig config, int index)
        {
            Font cachedFont = GameHelper.LoadFont();
            if (config == null) return;

            GameObject itemObj = new GameObject($"CollectionItem_{config.id}");
            itemObj.transform.SetParent(node_content, false);

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
            itemBtn.name = $"btn_item_{config.id}"; // Set button name for event handling
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

            GameHelper.OpenWin<CollectionDetailWin>(param: new CollectionDetailWinParam
            {
                config = config,
            });
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
    }
}
