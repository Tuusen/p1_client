using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class TabSwitchUI : MonoBehaviour
    {
        [SerializeField] private GameObject skillBarPanel;
        [SerializeField] private GameObject arcaneBarPanel;
        [SerializeField] private Button tabButton;
        [SerializeField] private Text tabButtonText;

        private bool showingSkills = true;

        private void Start()
        {
            if (tabButton != null)
                tabButton.onClick.AddListener(Toggle);
            ApplyState();
        }

        private void OnDestroy()
        {
            if (tabButton != null)
                tabButton.onClick.RemoveListener(Toggle);
        }

        private void Toggle()
        {
            showingSkills = !showingSkills;
            ApplyState();
        }

        private void ApplyState()
        {
            if (skillBarPanel != null)
                skillBarPanel.SetActive(showingSkills);
            if (arcaneBarPanel != null)
                arcaneBarPanel.SetActive(!showingSkills);
            if (tabButtonText != null)
                tabButtonText.text = showingSkills ? "奥术" : "技能";
        }
    }
}
