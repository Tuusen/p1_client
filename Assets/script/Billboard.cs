using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 广告牌效果 - 让物体始终面向摄像机
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }
    }
}
