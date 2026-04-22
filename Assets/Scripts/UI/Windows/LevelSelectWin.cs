using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// LevelSelectWin 的参数类
    /// </summary>
    public class LevelSelectWinParam
    {
        public int levelId;
    }

    public class LevelSelectWin : BaseWin
    {
        private LevelSelectWinParam data => Data as LevelSelectWinParam;
        private Text txt_name;
        private Text txt_desc;
        private Text txt_elite;
        private Text txt_boss;
        private Text txt_condition;
        private Button btn_challenge;

        private int selectedLevelId;
        private bool isStoryMode;

        public override void start()
        {
            ShowForStoryNode(data.levelId);
        }

        public override void onBtnClick(Button btn, object param)
        {
            string name = btn.name;
            switch (name)
            {
                case "btn_close":
                    OnClose();
                    break;
                case "btn_challenge":
                    OnChallengeClicked();
                    break;
                default:
                    break;
            }
        }


        public void ShowForStoryNode(int levelId)
        {
            isStoryMode = true;
            selectedLevelId = levelId;
            ShowDetail(levelId);
            btn_challenge.interactable = true;
        }


        private void ShowDetail(int levelId)
        {

            LevelConfig config = Cfg.Level.Get(levelId);
            if (config == null) return;

            txt_name.text = $"第{config.id}关 - {config.name}";
            txt_desc.text = config.des;

            // 精英预览
            if (config.superMList != null && config.superMList.Length > 0)
            {
                string eliteInfo = "";
                foreach (var elite in config.superMList)
                {
                    MonsterConfig mc = Cfg.Monster.Get(elite.id);
                    string name = mc != null ? mc.name : $"ID:{elite.id}";
                    eliteInfo += $"  {name} (每{elite.num}只小怪出现{elite.generate}只)\n";
                }
                txt_elite.text = eliteInfo.TrimEnd('\n');
            }
            else
            {
                txt_elite.text = "  无";
            }

            // Boss预览
            if (config.bossList != null && config.bossList.Length > 0)
            {
                string bossInfo = "";
                foreach (var boss in config.bossList)
                {
                    MonsterConfig mc = Cfg.Monster.Get(boss.id);
                    string name = mc != null ? mc.name : $"ID:{boss.id}";
                    bossInfo += $"  {name} (击杀{boss.num}只怪物后出现)\n";
                }
                txt_boss.text = bossInfo.TrimEnd('\n');
            }
            else
            {
                txt_boss.text = "  无";
            }

            // 条件
            bool unlocked = GameManager.Instance.IsLevelUnlocked(levelId);
            if (config.conditions == null || config.conditions.Length == 0)
            {
                txt_condition.text = "";
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
                        if (cond.type == 1)
                            met = GameManager.Instance.IsLevelCompleted(cond.p1);
                        string mark = met ? "[OK] " : "[X] ";
                        condInfo += mark + cond.desc + "\n";
                    }
                }
                txt_condition.text = condInfo.TrimEnd('\n');
                txt_condition.color = unlocked
                    ? new Color(0.3f, 0.9f, 0.3f)
                    : new Color(1f, 0.4f, 0.4f);
            }
           

            // 挑战按钮
            btn_challenge.interactable = unlocked;
        }

        private void OnChallengeClicked()
        {
            if (isStoryMode)
            {
                StoryManager.Instance.ExecuteCurrentNode();
                OnClose();
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
