using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class LevelSelectWin : BaseWin
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
        private bool isStoryMode;
        private List<GameObject> levelItems = new List<GameObject>();

        public override void Init()
        {
            base.Init();
            if (closeDetailButton != null)
                closeDetailButton.onClick.AddListener(() => {
                    if (isStoryMode)
                        WinManager.Instance.CloseWin<LevelSelectWin>();
                    else
                        detailPanel.SetActive(false);
                });
            if (closePanelButton != null)
                closePanelButton.onClick.AddListener(() => {
                    OnClose();
                });
            if (challengeButton != null)
                challengeButton.onClick.AddListener(OnChallengeClicked);

            if (detailPanel != null)
                detailPanel.SetActive(false);
        }

        public override void Show()
        {
            base.Show();
            RefreshLevelList();
        }

        public override void OnClose()
        {
            isStoryMode = false;
            base.OnClose();
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

            Image bg = itemObj.AddComponent<Image>();
            if (completed)
                bg.color = new Color(0.15f, 0.3f, 0.15f, 0.9f);
            else if (unlocked)
                bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);
            else
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            Button btn = itemObj.AddComponent<Button>();
            int levelId = config.id;
            btn.onClick.AddListener(() => OnLevelItemClicked(levelId));

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = unlocked ? new Color(0.9f, 0.9f, 1f) : Color.white;
            cb.pressedColor = unlocked ? new Color(0.7f, 0.7f, 0.9f) : Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            btn.colors = cb;

            // 关卡编号
            GameObject numObj = new GameObject("Number");
            numObj.transform.SetParent(itemObj.transform, false);
            RectTransform numRt = numObj.AddComponent<RectTransform>();
            numRt.anchorMin = Vector2.zero;
            numRt.anchorMax = Vector2.one;
            numRt.offsetMin = Vector2.zero;
            numRt.offsetMax = Vector2.zero;

            Text numText = numObj.AddComponent<Text>();
            numText.font = font;
            numText.fontSize = 36;
            numText.alignment = TextAnchor.MiddleCenter;
            numText.text = config.id.ToString();
            numText.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);

            return itemObj;
        }

        public void ShowForStoryNode(int levelId)
        {
            isStoryMode = true;
            selectedLevelId = levelId;
            ShowDetail(levelId);
            if (challengeButton != null)
                challengeButton.interactable = true;
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
            if (isStoryMode)
            {
                WinManager.Instance.CloseWin<LevelSelectWin>();
                if (StoryManager.Instance != null)
                    StoryManager.Instance.ExecuteCurrentNode();
                return;
            }

            if (GameManager.Instance == null) return;
            if (!GameManager.Instance.IsLevelUnlocked(selectedLevelId)) return;

            GameManager.Instance.SelectLevel(selectedLevelId);
            GameManager.Instance.StartSelectedLevel();
            OnClose();
        }
    }
}
