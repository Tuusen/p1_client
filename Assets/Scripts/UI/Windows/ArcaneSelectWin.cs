using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    /// <summary>
    /// ArcaneSelectWin 的参数类
    /// </summary>
    public class ArcaneSelectWinParam
    {
    }

    public class ArcaneSelectWin : BaseWin
    {
        private ArcaneSelectWinParam data => Data as ArcaneSelectWinParam;

        [SerializeField] private Transform arcaneListContent;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text countText;

        private const int MaxCount = 4;
        private HashSet<int> selectedIds = new HashSet<int>();
        private List<GameObject> arcaneItems = new List<GameObject>();
        private Dictionary<int, Image> itemBgMap = new Dictionary<int, Image>();

        public override void load()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnClose());
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        public override void start()
        {
            RefreshList();
        }

        private void RefreshList()
        {
            foreach (var item in arcaneItems)
            {
                if (item != null) Destroy(item);
            }
            arcaneItems.Clear();
            itemBgMap.Clear();
            selectedIds.Clear();

            if (ConfigManager.Instance == null) return;

            // 加载已装备的奥术
            int[] equipped = GameManager.Instance != null
                ? GameManager.Instance.GetEquippedArcanes()
                : GameConsts.MetaConsts.ArcaneSlotIds;
            if (equipped != null)
            {
                foreach (int id in equipped)
                    selectedIds.Add(id);
            }

            Font font = GameHelper.LoadFont();
            int[] allArcaneIds = GameConsts.MetaConsts.ArcaneSlotIds;
            if (allArcaneIds == null) return;

            foreach (int arcaneId in allArcaneIds)
            {
                ArcaneConfig config = Cfg.Arcane.Get(arcaneId);
                if (config == null) continue;
                GameObject itemObj = CreateArcaneItem(config, font);
                arcaneItems.Add(itemObj);
            }

            UpdateVisuals();
        }

        private GameObject CreateArcaneItem(ArcaneConfig config, Font font)
        {
            GameObject itemObj = new GameObject($"ArcaneItem_{config.id}");
            itemObj.transform.SetParent(arcaneListContent, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);
            itemBgMap[config.id] = bg;

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 70;

            Button btn = itemObj.AddComponent<Button>();
            int arcaneId = config.id;
            btn.onClick.AddListener(() => OnArcaneClicked(arcaneId));

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
            descText.text = config.des;
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            return itemObj;
        }

        private void OnArcaneClicked(int arcaneId)
        {
            if (selectedIds.Contains(arcaneId))
                selectedIds.Remove(arcaneId);
            else if (selectedIds.Count < MaxCount)
                selectedIds.Add(arcaneId);

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
                countText.text = $"已选 {selectedIds.Count}/{MaxCount}";
        }

        private void OnConfirmClicked()
        {
            List<int> ids = new List<int>(selectedIds);
            ids.Sort();
            if (GameManager.Instance != null)
                GameManager.Instance.SetEquippedArcanes(ids.ToArray());

            OnClose();
        }
    }
}
