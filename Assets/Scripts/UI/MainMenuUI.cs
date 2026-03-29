using UnityEngine;

namespace GeometryTD
{
    public class MainMenuUI : MonoBehaviour
    {
        private void Start()
        {
        }

        public void OnHeroButtonClicked()
        {
            GameHelper.OpenWin<HeroSelectWin>();
        }

        public void OnSkillButtonClicked()
        {
            GameHelper.OpenWin<SkillSelectWin>();
        }

        public void OnArcaneButtonClicked()
        {
            GameHelper.OpenWin<ArcaneSelectWin>();
        }

        public void OnStoryCollectionButtonClicked()
        {
            GameHelper.OpenWin<StoryCollectionWin>();
        }
    }
}
