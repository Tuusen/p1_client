using UnityEngine;

namespace GeometryTD
{
    public abstract class BaseWin : MonoBehaviour
    {
        [SerializeField] private int sortOrder = 0;

        public int SortOrder => sortOrder;
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
