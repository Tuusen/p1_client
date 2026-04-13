using UnityEngine;

namespace GeometryTD
{
    public abstract class BaseWin : MonoBehaviour
    {
        [SerializeField] private int sortOrder = 0;
        [SerializeField] private WinType winType = WinType.Normal;
        [SerializeField] private WinPriority priority = WinPriority.Normal;

        public int SortOrder => sortOrder;
        public WinType WinType => winType;
        public WinPriority Priority => priority;
        public bool IsVisible => gameObject.activeSelf;

        public virtual void Init()
        {
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnClose()
        {
            Hide();
        }
    }
}
