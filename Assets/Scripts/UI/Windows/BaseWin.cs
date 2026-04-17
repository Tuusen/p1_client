using UnityEngine;

namespace GeometryTD
{
    public abstract class BaseWin : MonoBehaviour
    {
        public object Data = null;
        [SerializeField] private int sortOrder = 0;
        [SerializeField] private WinType winType = WinType.Normal;
        [SerializeField] private WinPriority priority = WinPriority.Normal;

        public int SortOrder => sortOrder;
        public WinType WinType => winType;
        public WinPriority Priority => priority;
        public bool IsVisible => gameObject.activeSelf;


        public virtual void ResetOpen(object param)
        {
            Data = param;
            resetOpen();
        }

        public virtual void resetOpen()
        {
            start();
        }

        public virtual void Init(object param)
        {
            Data = param;
            load();
        }

        public virtual void load() {

        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            start();
        }

        public virtual void start() {

        }

        public virtual void Hide()
        {
            OnClose();
        }

        public virtual void OnClose()
        {
            closeWin();
            WinManager.Instance.CloseWin(this.name);
        }

        public virtual void closeWin()
        {
            
        }

        public virtual void waitCallBack(string callName, float interval)
        {
            InvokeRepeating(callName, 0f, interval);
        }
    }
}
