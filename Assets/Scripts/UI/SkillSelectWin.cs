using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class SkillSelectWin : BaseWin
    {
        [SerializeField] private Transform skillListContent;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text countText;

        private const int RequiredCount = 8;
        private HashSet<int> selectedIds = new HashSet<int>();
        private List<GameObject> skillItems = new List<GameObject>();
        private Dictionary<int, Image> itemBgMap = new Dictionary<int, Image>();

        public override void Init()
        {
            base.Init();
            if (closeButton != null)
                closeButton.onClick.AddListener(() => WinManager.Instance.CloseWin<SkillSelectWin>());
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        public override void Show()
        {
            base.Show();
            RefreshList();
        }

        public override void OnClose()
        {
            base.OnClose();
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

            if (ConfigManager.Instance == null || ConfigManager.Instance.GameConfig == null) return;

            int[] equipped = GameManager.Instance != null
                ? GameManager.Instance.GetEquippedSkills()
                : ConfigManager.Instance.GameConfig.skill_slot_ids;
            if (equipped != null)
            {
                foreach (int id in equipped)
                    selectedIds.Add(id);
            }

            Font font = GameHelper.LoadFont();
            int[] allSkillIds = ConfigManager.Instance.GameConfig.skill_slot_ids;
            if (allSkillIds == null) return;

            foreach (int skillId in allSkillIds)
            {
                SkillConfig config = ConfigManager.Instance.GetSkillConfig(skillId, 0);
                if (config == null) continue;
                GameObject itemObj = CreateSkillItem(config, font);
                skillItems.Add(itemObj);
            }

            UpdateVisuals();
        }

        private GameObject CreateSkillItem(SkillConfig config, Font font)
        {
            GameObject itemObj = new GameObject($"SkillItem_{config.id}");
            itemObj.transform.SetParent(skillListContent, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);
            itemBgMap[config.id] = bg;

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 70;

            Button btn = itemObj.AddComponent<Button>();
            int skillId = config.id;
            btn.onClick.AddListener(() => OnSkillClicked(skillId));

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
            nameText.text = config.name;
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
            descText.text = config.desList != null && config.desList.Length > 0 ? config.desList[0] : "";
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

            if (countText != null)
                countText.text = $"\u5df2\u9009 {selectedIds.Count}/{RequiredCount}";

            if (confirmButton != null)
                confirmButton.interactable = selectedIds.Count == RequiredCount;
        }

        private void OnConfirmClicked()
        {
            if (selectedIds.Count != RequiredCount) return;

            List<int> ids = new List<int>(selectedIds);
            ids.Sort();
            if (GameManager.Instance != null)
                GameManager.Instance.SetEquippedSkills(ids.ToArray());

            WinManager.Instance.CloseWin<SkillSelectWin>();
        }
    }
}
