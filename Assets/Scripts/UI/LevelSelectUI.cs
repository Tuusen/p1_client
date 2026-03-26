using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform levelListContent;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailDescText;
        [SerializeField] private Text detailEliteText;
        [SerializeField] private Text detailBossText;
        [SerializeField] private Text detailConditionText;
        [SerializeField] private Button challengeButton;
        [SerializeField] private Button closeDetailButton;
        [SerializeField] private Button closePanelButton;

        private int selectedLevelId;
        private List<GameObject> levelItems = new List<GameObject>();

        private void OnEnable()
        {
            RefreshLevelList();
            if (detailPanel != null)
                detailPanel.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (closeDetailButton != null)
                closeDetailButton.onClick.AddListener(() => detailPanel.SetActive(false));
            if (closePanelButton != null)
                closePanelButton.onClick.AddListener(Hide);
            if (challengeButton != null)
                challengeButton.onClick.AddListener(OnChallengeClicked);
        }

        private void RefreshLevelList()
        {
            foreach (var item in levelItems)
            {
                if (item != null) Destroy(item);
            }
            levelItems.Clear();

            if (ConfigManager.Instance == null || ConfigManager.Instance.LevelConfigs == null) return;

            Font font = GameHelper.LoadFont();

            foreach (LevelConfig level in ConfigManager.Instance.LevelConfigs)
            {
                bool unlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(level.id);
                bool completed = GameManager.Instance != null && GameManager.Instance.IsLevelCompleted(level.id);

                GameObject itemObj = CreateLevelItem(level, unlocked, completed, font);
                levelItems.Add(itemObj);
            }
        }

        private GameObject CreateLevelItem(LevelConfig config, bool unlocked, bool completed, Font font)
        {
            GameObject itemObj = new GameObject($"LevelItem_{config.id}");
            itemObj.transform.SetParent(levelListContent, false);

            RectTransform rt = itemObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 60);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = unlocked
                ? new Color(0.15f, 0.15f, 0.25f, 0.9f)
                : new Color(0.1f, 0.1f, 0.1f, 0.7f);

            Button btn = itemObj.AddComponent<Button>();
            int levelId = config.id;
            btn.onClick.AddListener(() => OnLevelItemClicked(levelId));
            btn.interactable = true;

            ColorBlock cb = btn.colors;
            cb.normalColor = bg.color;
            cb.highlightedColor = unlocked ? new Color(0.2f, 0.2f, 0.35f, 0.9f) : bg.color;
            cb.pressedColor = unlocked ? new Color(0.25f, 0.25f, 0.4f, 0.9f) : bg.color;
            cb.disabledColor = bg.color;
            btn.colors = cb;

            // 关卡名
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0.7f, 1);
            nameRt.offsetMin = new Vector2(15, 5);
            nameRt.offsetMax = new Vector2(0, -5);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 18;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = $"第{config.id}关  {config.name}";
            nameText.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);

            // 状态标记
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(itemObj.transform, false);
            RectTransform statusRt = statusObj.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.7f, 0);
            statusRt.anchorMax = new Vector2(1, 1);
            statusRt.offsetMin = new Vector2(0, 5);
            statusRt.offsetMax = new Vector2(-10, -5);

            Text statusText = statusObj.AddComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 14;
            statusText.alignment = TextAnchor.MiddleRight;
            if (completed)
            {
                statusText.text = "已通关";
                statusText.color = new Color(0.3f, 0.9f, 0.3f);
            }
            else if (unlocked)
            {
                statusText.text = "可挑战";
                statusText.color = new Color(0.9f, 0.9f, 0.3f);
            }
            else
            {
                statusText.text = "未解锁";
                statusText.color = new Color(0.5f, 0.5f, 0.5f);
            }

            return itemObj;
        }

        private void OnLevelItemClicked(int levelId)
        {
            selectedLevelId = levelId;
            ShowDetail(levelId);
        }

        private void ShowDetail(int levelId)
        {
            if (detailPanel == null) return;

            LevelConfig config = ConfigManager.Instance.GetLevelConfig(levelId);
            if (config == null) return;

            detailPanel.SetActive(true);

            if (detailNameText != null)
                detailNameText.text = $"第{config.id}关 - {config.name}";

            if (detailDescText != null)
                detailDescText.text = config.des;

            // 精英预览
            if (detailEliteText != null)
            {
                if (config.superMList != null && config.superMList.Length > 0)
                {
                    string eliteInfo = "";
                    foreach (var elite in config.superMList)
                    {
                        MonsterConfig mc = ConfigManager.Instance.GetMonsterConfig(elite.id);
                        string name = mc != null ? mc.name : $"ID:{elite.id}";
                        eliteInfo += $"  {name} (每{elite.num}只小怪出现{elite.generate}只)\n";
                    }
                    detailEliteText.text = eliteInfo.TrimEnd('\n');
                }
                else
                {
                    detailEliteText.text = "  无";
                }
            }

            // Boss预览
            if (detailBossText != null)
            {
                if (config.bossList != null && config.bossList.Length > 0)
                {
                    string bossInfo = "";
                    foreach (var boss in config.bossList)
                    {
                        MonsterConfig mc = ConfigManager.Instance.GetMonsterConfig(boss.id);
                        string name = mc != null ? mc.name : $"ID:{boss.id}";
                        bossInfo += $"  {name} (击杀{boss.num}只怪物后出现)\n";
                    }
                    detailBossText.text = bossInfo.TrimEnd('\n');
                }
                else
                {
                    detailBossText.text = "  无";
                }
            }

            // 条件
            bool unlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(levelId);
            if (detailConditionText != null)
            {
                if (config.conditions == null || config.conditions.Length == 0)
                {
                    detailConditionText.text = "";
                }
                else
                {
                    string condInfo = "";
                    foreach (int condId in config.conditions)
                    {
                        ConditionConfig cond = ConfigManager.Instance.GetConditionConfig(condId);
                        if (cond != null)
                        {
                            bool met = false;
                            if (cond.type == 1 && GameManager.Instance != null)
                                met = GameManager.Instance.IsLevelCompleted(cond.p1);
                            string mark = met ? "[OK] " : "[X] ";
                            condInfo += mark + cond.desc + "\n";
                        }
                    }
                    detailConditionText.text = condInfo.TrimEnd('\n');
                    detailConditionText.color = unlocked
                        ? new Color(0.3f, 0.9f, 0.3f)
                        : new Color(1f, 0.4f, 0.4f);
                }
            }

            // 挑战按钮
            if (challengeButton != null)
            {
                challengeButton.interactable = unlocked;
            }
        }

        private void OnChallengeClicked()
        {
            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.IsLevelUnlocked(selectedLevelId)) return;

            GameManager.Instance.SelectLevel(selectedLevelId);
            GameManager.Instance.StartSelectedLevel();
        }
    }
}
