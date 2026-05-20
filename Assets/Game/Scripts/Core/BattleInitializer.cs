using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometryTD
{
    public class BattleInitializer : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBattleSceneLoaded();
            }
        }
    }
}
