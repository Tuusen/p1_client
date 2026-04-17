using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// CollectionDetailWin 的参数类
    /// </summary>
    public class CollectionDetailWinParam
    {
        public PassiveEffectConfig config;
    }

    public class CollectionDetailWin : BaseWin
    {
        // 通过属性访问基类的 data 字段
        private CollectionDetailWinParam data => Data as CollectionDetailWinParam;

        private Text txt_title;
        private Image sp_icon;
        private Text txt_quality;
        private Text txt_level;
        private Text txt_description;
        private Text txt_passiveList;
        private Text txt_goldBonus;
        private Text txt_stackType;
        private Text txt_maxStack;
        private Button btn_close;

        private PassiveEffectConfig currentConfig;
        private Font cachedFont;


        public override void start()
        {
            ShowDetail(data.config);
        }

        public override void closeWin()
        {
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

            if (txt_title != null)
                txt_title.text = currentConfig.name ?? "";

            // Icon
            if (sp_icon != null)
            {
                sp_icon.color = new Color(1f, 1f, 1f, 0.3f);
                if (!string.IsNullOrEmpty(currentConfig.icon))
                {
                    Sprite iconSprite = GameHelper.LoadSprite(currentConfig.icon);
                    if (iconSprite != null)
                    {
                        sp_icon.sprite = iconSprite;
                        sp_icon.color = Color.white;
                    }
                }
            }

            // Quality
            if (txt_quality != null)
            {
                txt_quality.text = GetQualityName(currentConfig.color);
                txt_quality.color = GetQualityTextColor(currentConfig.color);
            }

            // Level
            if (txt_level != null)
            {
                txt_level.text = $"Level: {currentConfig.level}";
            }

            // Description
            if (txt_description != null)
            {
                txt_description.text = currentConfig.des ?? "";
            }

            // Passive list
            if (txt_passiveList != null)
            {
                string passiveInfo = BuildPassiveInfo();
                txt_passiveList.text = string.IsNullOrEmpty(passiveInfo) ? "No passive skills" : passiveInfo;
            }

            // Gold bonus
            if (txt_goldBonus != null)
            {
                if (currentConfig.addGold > 0)
                {
                    txt_goldBonus.text = $"+{currentConfig.addGold / 100f}% Gold Bonus";
                    txt_goldBonus.color = new Color(1f, 0.85f, 0.3f);
                }
                else
                {
                    txt_goldBonus.text = "No gold bonus";
                    txt_goldBonus.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }

            // Stack type
            if (txt_stackType != null)
            {
                txt_stackType.text = $"Type: {GetStackTypeName(currentConfig.stackType)}";
            }

            // Max stack
            if (txt_maxStack != null)
            {
                if (currentConfig.maxStack == -1)
                {
                    txt_maxStack.text = "无生效限制";
                } else {
                    txt_maxStack.text = $"最大生效上限: {currentConfig.maxStack}";
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


    }
}
