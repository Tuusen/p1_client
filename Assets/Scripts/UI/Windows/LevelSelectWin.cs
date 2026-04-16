using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class LevelSelectWin : BaseWin
    {
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailDescText;
        [SerializeField] private Text detailEliteText;
        [SerializeField] private Text detailBossText;
        [SerializeField] private Text detailConditionText;
        [SerializeField] private Button challengeButton;
        [SerializeField] private Button closePanelButton;

        private int selectedLevelId;
        private bool isStoryMode;

        public override void Init()
        {
            base.Init();
            if (closePanelButton != null)
                closePanelButton.onClick.AddListener(() => {
                    OnClose();
                });
            if (challengeButton != null)
                challengeButton.onClick.AddListener(OnChallengeClicked);

        }

        public override void OnClose()
        {
            isStoryMode = false;
            base.OnClose();
        }

        public void ShowForStoryNode(int levelId)
        {
            isStoryMode = true;
            selectedLevelId = levelId;
            ShowDetail(levelId);
            if (challengeButton != null)
                challengeButton.interactable = true;
        }


        private void ShowDetail(int levelId)
        {

            LevelConfig config = Cfg.Level.Get(levelId);
            if (config == null) return;

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
                        MonsterConfig mc = Cfg.Monster.Get(elite.id);
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
                        MonsterConfig mc = Cfg.Monster.Get(boss.id);
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
                        ConditionConfig cond = Cfg.Condition.Get(condId);
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
