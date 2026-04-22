using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GeometryTD
{
    /// <summary>
    /// 技能/奥术详情面板参数类
    /// </summary>
    public class SkillArcaneDetailWinParam
    {
        public int id;                    // 技能poolId或奥术id
        public bool isSkill;              // true=技能, false=奥术
        public int currentLevel;          // 技能当前等级（仅技能需要）
    }

    /// <summary>
    /// 技能/奥术详情面板窗口
    /// 点击屏幕任意区域可关闭
    /// UI通过预制体或代码动态构建
    /// </summary>
    public class SkillArcaneDetailWin : BaseWin
    {
        private SkillArcaneDetailWinParam data => Data as SkillArcaneDetailWinParam;

        // UI组件（支持预制体自动绑定）
        [SerializeField] private Image sp_iconImage;
        [SerializeField] private Text txt_nameText;
        [SerializeField] private Text txt_descText;
        [SerializeField] private Text txt_energyText;
        [SerializeField] private Text txt_cdText;
        [SerializeField] private Text txt_closeHintText;
        [SerializeField] private Image sp_backgroundImage;

        // 属性类型颜色映射
        private static readonly Dictionary<int, Color> AttributeColors = new Dictionary<int, Color>
        {
            { 0, new Color(1f, 1f, 1f) },       // 无属性 - 白色
            { 1, new Color(1f, 0.4f, 0.2f) },   // 火 - 橙红色
            { 2, new Color(0.2f, 0.6f, 1f) },   // 冰 - 蓝色
            { 3, new Color(1f, 0.9f, 0.2f) },   // 雷 - 黄色
            { 4, new Color(0.3f, 1f, 0.3f) }    // 风 - 绿色
        };

        // 属性类型名称映射
        private static readonly string[] AttributeNames = { "无属性", "火", "冰", "雷", "风" };

        public override void load()
        {
            // 点击背景关闭
            if (sp_backgroundImage != null)
            {
                sp_backgroundImage.raycastTarget = true;
                Button bgButton = sp_backgroundImage.GetComponent<Button>();
                if (bgButton == null)
                {
                    bgButton = sp_backgroundImage.gameObject.AddComponent<Button>();
                }
                bgButton.targetGraphic = sp_backgroundImage;
                bgButton.transition = Selectable.Transition.None;
                bgButton.onClick.AddListener(() => OnClose());
            }
        }

        public override void start()
        {
            if (data == null)
            {
                Debug.LogError("[SkillArcaneDetailWin] Data is null");
                OnClose();
                return;
            }

            RefreshContent();
        }

        private void RefreshContent()
        {
            if (data.isSkill)
            {
                ShowSkillDetail();
            }
            else
            {
                ShowArcaneDetail();
            }
        }

        /// <summary>
        /// 显示技能详情
        /// </summary>
        private void ShowSkillDetail()
        {
            var poolConfig = Cfg.SkillPool.Get(data.id);
            if (poolConfig == null)
            {
                Debug.LogError($"[SkillArcaneDetailWin] SkillPool config not found: {data.id}");
                OnClose();
                return;
            }

            // 获取当前等级的技能配置
            SkillConfig skillConfig = null;
            if (data.currentLevel > 0)
            {
                skillConfig = ConfigManager.Instance.GetSkillConfigByPool(data.id, data.currentLevel);
            }

            // 图标
            if (sp_iconImage != null && !string.IsNullOrEmpty(poolConfig.icon))
            {
                Sprite sprite = GameHelper.LoadSprite(poolConfig.icon);
                if (sprite != null)
                {
                    sp_iconImage.sprite = sprite;
                    sp_iconImage.color = Color.white;
                }
                else
                {
                    sp_iconImage.sprite = null;
                    sp_iconImage.color = new Color(0.2f, 0.2f, 0.3f);
                }
            }

            // 名称
            if (txt_nameText != null)
            {
                txt_nameText.text = poolConfig.name;
            }

            // 描述（支持多行和换行符）
            if (txt_descText != null)
            {
                txt_descText.supportRichText = true;
                string desc = poolConfig.des ?? "";
                // 处理升级描述
                if (!string.IsNullOrEmpty(poolConfig.upDes))
                {
                    desc += "\n\n<color=#FFFF00>[升级效果]</color>" + poolConfig.upDes;
                }
                // 添加各级描述
                if (poolConfig.levelDes != null && poolConfig.levelDes.Length > 0)
                {
                    desc += "\n";
                    foreach (var levelDes in poolConfig.levelDes)
                    {
                        bool unlocked = data.currentLevel >= levelDes.level;
                        string colorTag = unlocked ? "<color=#00FF00>" : "<color=#808080>";
                        desc += $"\n{colorTag}[{levelDes.level}级]</color>{levelDes.des}";
                    }
                }
                txt_descText.text = desc;
            }

            // 能量恢复信息
            if (txt_energyText != null && skillConfig != null && skillConfig.events != null)
            {
                string energyInfo = ParseSkillEnergyInfo(skillConfig.events);
                txt_energyText.text = energyInfo;
                txt_energyText.gameObject.SetActive(!string.IsNullOrEmpty(energyInfo));
            }
            else if (txt_energyText != null)
            {
                txt_energyText.gameObject.SetActive(false);
            }

            // 冷却时间
            if (txt_cdText != null)
            {
                float cd = skillConfig != null ? skillConfig.cd : 0f;
                txt_cdText.text = cd > 0 ? $"<color=#B0B0FF>冷却时间:</color> {cd:F1}s" : "<color=#B0B0FF>冷却时间:</color> -";
            }

            // 关闭提示
            if (txt_closeHintText != null)
            {
                txt_closeHintText.text = "点击任意区域关闭";
            }
        }

        /// <summary>
        /// 显示奥术详情
        /// </summary>
        private void ShowArcaneDetail()
        {
            var config = Cfg.Arcane.Get(data.id);
            if (config == null)
            {
                Debug.LogError($"[SkillArcaneDetailWin] Arcane config not found: {data.id}");
                OnClose();
                return;
            }

            // 图标
            if (sp_iconImage != null && !string.IsNullOrEmpty(config.icon))
            {
                Sprite sprite = GameHelper.LoadSprite(config.icon);
                if (sprite != null)
                {
                    sp_iconImage.sprite = sprite;
                    sp_iconImage.color = Color.white;
                }
                else
                {
                    sp_iconImage.sprite = null;
                    sp_iconImage.color = new Color(0.2f, 0.2f, 0.3f);
                }
            }

            // 名称
            if (txt_nameText != null)
            {
                txt_nameText.text = config.name;
            }

            // 描述（支持多行和换行符）
            if (txt_descText != null)
            {
                txt_descText.supportRichText = true;
                txt_descText.text = config.des ?? "";
            }

            // 能量消耗信息
            if (txt_energyText != null)
            {
                string energyTypeName = config.runeType >= 1 && config.runeType <= 4
                    ? AttributeNames[config.runeType]
                    : "未知";
                Color energyColor = GetAttributeColor(config.runeType);
                string colorHex = ColorUtility.ToHtmlStringRGB(energyColor);
                txt_energyText.text = $"<color={colorHex}>消耗能量:</color> {config.runeCost} {energyTypeName}符能";
            }

            // 冷却时间
            if (txt_cdText != null)
            {
                txt_cdText.text = $"<color=#B0B0FF>冷却时间:</color> {config.cd:F1}s";
            }

            // 关闭提示
            if (txt_closeHintText != null)
            {
                txt_closeHintText.text = "点击任意区域关闭";
            }
        }

        /// <summary>
        /// 解析技能能量恢复信息
        /// </summary>
        private string ParseSkillEnergyInfo(int[] events)
        {
            if (events == null || events.Length == 0) return "";

            List<string> energyParts = new List<string>();

            foreach (int eventId in events)
            {
                var eventEffect = Cfg.EventEffect.Get(eventId);
                if (eventEffect == null) continue;

                // EventType 6 = 获得能量
                if (eventEffect.eventType == 6)
                {
                    // 从 offset 中提取参数：[能量值, 能量类型]
                    if (eventEffect.offset != null && eventEffect.offset.Length >= 2)
                    {
                        int energyValue = eventEffect.offset[0].y; // y轴存储数值
                        int energyType = eventEffect.offset[1].y;  // y轴存储类型

                        // 如果 energyType = -1 表示全部类型
                        if (energyType == -1)
                        {
                            energyParts.Add("<color=#FFFFFF>全部能量</color> +" + energyValue);
                        }
                        else
                        {
                            string typeName = energyType >= 0 && energyType < AttributeNames.Length
                                ? AttributeNames[energyType]
                                : "未知";
                            Color typeColor = GetAttributeColor(energyType);
                            string colorHex = ColorUtility.ToHtmlStringRGB(typeColor);
                            energyParts.Add($"<color={colorHex}>{typeName}能量</color> +{energyValue}");
                        }
                    }
                }
            }

            if (energyParts.Count == 0) return "";

            return "<color=#B0B0FF>能量恢复:</color> " + string.Join(", ", energyParts);
        }

        /// <summary>
        /// 获取属性类型对应的颜色
        /// </summary>
        private Color GetAttributeColor(int dmgType)
        {
            if (AttributeColors.TryGetValue(dmgType, out Color color))
            {
                return color;
            }
            return Color.white;
        }
    }
}
