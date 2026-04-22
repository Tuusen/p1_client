using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// SkillSelectWin 的参数类
    /// </summary>
    public class SkillSelectWinParam
    {
    }

    public class SkillSelectWin : BaseWin
    {
        private SkillSelectWinParam data => Data as SkillSelectWinParam;
        private Transform node_content;
        private Button btn_enter;
        private Text txt_count;

        private const int RequiredCount = 8;
        private HashSet<int> selectedIds = new HashSet<int>();
        private List<GameObject> skillItems = new List<GameObject>();
        private Dictionary<int, Image> itemBgMap = new Dictionary<int, Image>();

        public override void start()
        {
            RefreshList();
        }

        public override void onBtnClick(Button btn, object param)
        {
            string name = btn.name;
            switch (name)
            {
                case "btn_close":
                    OnClose();
                    break;
                case "btn_enter":
                    OnConfirmClicked();
                    break;
                default:
                    break;
            }
        }

        private void RefreshList()
        {
            foreach (var item in skillItems)
            {
                if (item != null) Destroy(item);
            }
            skillItems.Clear();
            itemBgMap.Clear();
            selectedIds.Clear();

            if (ConfigManager.Instance == null) return;

            int[] equipped = GameManager.Instance != null
                ? GameManager.Instance.GetEquippedSkills()
                : GameConsts.SkillSlotIds;
            if (equipped != null)
            {
                foreach (int id in equipped)
                    selectedIds.Add(id);
            }

            Font font = GameHelper.LoadFont();
            int[] allSkillPoolIds = GameConsts.SkillSlotIds;
            if (allSkillPoolIds == null) return;

            foreach (int poolId in allSkillPoolIds)
            {
                SkillPoolConfig poolConfig = Cfg.SkillPool.Get(poolId);
                if (poolConfig == null) continue;
                GameObject itemObj = CreateSkillItem(poolConfig, font);
                skillItems.Add(itemObj);
            }

            UpdateVisuals();
        }

        private GameObject CreateSkillItem(SkillPoolConfig poolConfig, Font font)
        {
            GameObject itemObj = new GameObject($"SkillItem_{poolConfig.id}");
            
            // 必须添加 RectTransform 才能作为 LayoutGroup 的子对象
            RectTransform rectTransform = itemObj.AddComponent<RectTransform>();
            
            itemObj.transform.SetParent(node_content, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);
            itemBgMap[poolConfig.id] = bg;

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            le.flexibleWidth = 0; // 禁止宽度弹性扩展

            Button btn = itemObj.AddComponent<Button>();
            int poolId = poolConfig.id;
            btn.onClick.AddListener(() => OnSkillClicked(poolId));

            // 名称
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(10, 0);
            nameRt.offsetMax = new Vector2(-10, 0);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 26;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = poolConfig.name;
            nameText.color = Color.white;

            // 描述
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(itemObj.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = Vector2.zero;
            descRt.anchorMax = new Vector2(1, 0.5f);
            descRt.offsetMin = new Vector2(10, 0);
            descRt.offsetMax = new Vector2(-10, 0);

            Text descText = descObj.AddComponent<Text>();
            descText.font = font;
            descText.fontSize = 18;
            descText.alignment = TextAnchor.MiddleLeft;
            descText.text = poolConfig.des;
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            return itemObj;
        }

        private void OnSkillClicked(int skillId)
        {
            if (selectedIds.Contains(skillId))
                selectedIds.Remove(skillId);
            else if (selectedIds.Count < RequiredCount)
                selectedIds.Add(skillId);

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            foreach (var kvp in itemBgMap)
            {
                kvp.Value.color = selectedIds.Contains(kvp.Key)
                    ? new Color(0.2f, 0.4f, 0.2f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.3f, 0.9f);
            }

            txt_count.text = $"\u5df2\u9009 {selectedIds.Count}/{RequiredCount}";

            btn_enter.interactable = selectedIds.Count == RequiredCount;
        }

        private void OnConfirmClicked()
        {
            if (selectedIds.Count != RequiredCount) return;

            List<int> ids = new List<int>(selectedIds);
            ids.Sort();
            if (GameManager.Instance != null)
                GameManager.Instance.SetEquippedSkills(ids.ToArray());

            OnClose();
        }
    }
}
