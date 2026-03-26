using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class HeroSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform heroListContent;
        [SerializeField] private Button closeButton;

        private int selectedHeroId;
        private List<GameObject> heroItems = new List<GameObject>();
        private Dictionary<int, Image> itemBgMap = new Dictionary<int, Image>();

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            RefreshList();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RefreshList()
        {
            foreach (var item in heroItems)
            {
                if (item != null) Destroy(item);
            }
            heroItems.Clear();
            itemBgMap.Clear();

            if (ConfigManager.Instance == null || ConfigManager.Instance.HeroConfigs == null) return;

            selectedHeroId = GameManager.Instance != null
                ? GameManager.Instance.GetSelectedHeroId()
                : 1;

            Font font = GameHelper.LoadFont();

            foreach (HeroConfig hero in ConfigManager.Instance.HeroConfigs)
            {
                GameObject itemObj = CreateHeroItem(hero, font);
                heroItems.Add(itemObj);
            }

            UpdateSelection();
        }

        private GameObject CreateHeroItem(HeroConfig config, Font font)
        {
            GameObject itemObj = new GameObject($"HeroItem_{config.id}");
            itemObj.transform.SetParent(heroListContent, false);

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.3f, 0.9f);
            itemBgMap[config.id] = bg;

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 80;

            Button btn = itemObj.AddComponent<Button>();
            int heroId = config.id;
            btn.onClick.AddListener(() => OnHeroItemClicked(heroId));

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
            nameText.fontSize = 28;
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
            descText.fontSize = 20;
            descText.alignment = TextAnchor.MiddleLeft;
            descText.text = config.description;
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            return itemObj;
        }

        private void OnHeroItemClicked(int heroId)
        {
            selectedHeroId = heroId;
            if (GameManager.Instance != null)
                GameManager.Instance.SelectHero(heroId);
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            foreach (var kvp in itemBgMap)
            {
                kvp.Value.color = kvp.Key == selectedHeroId
                    ? new Color(0.2f, 0.4f, 0.2f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.3f, 0.9f);
            }
        }
    }
}
