using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;

namespace GeometryTD
{
    public abstract class BaseWin : MonoBehaviour
    {
        public object Data = null;
        [SerializeField] private int sortOrder = 0;
        [SerializeField] private GameConsts.WinType winType = GameConsts.WinType.Normal;
        [SerializeField] private GameConsts.WinPriority priority = GameConsts.WinPriority.Normal;

        public int SortOrder => sortOrder;
        public GameConsts.WinType WinType => winType;
        public GameConsts.WinPriority Priority => priority;
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// 自动根据命名规则绑定UI组件
        /// 命名规则：txt_xx代表文本，btn_xx代表按钮，toggle_xx代表复选框，sp_xx代表图片，node_xx代表节点等
        /// 字段名需与GameObject名一致（必须包含下划线前缀）
        /// </summary>
        private void AutoBindUIComponents()
        {
            // 获取所有子对象
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            
            foreach (Transform child in allChildren)
            {
                // 只处理名称中包含下划线的字段
                if (!child.name.Contains("_")) continue;
                
                // 根据字段前缀尝试获取对应类型的组件
                string prefix = child.name.Substring(0, child.name.IndexOf('_') + 1);
                FieldInfo field = GetType().GetField(child.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                switch (prefix)
                {
                    case "txt_":
                        if (child.TryGetComponent<Text>(out Text txt))
                        {
                            if (field != null){
                                field.SetValue(this, txt);
                            }
                        }
                        break;
                        
                    case "btn_":
                        if (child.TryGetComponent<Button>(out Button btn))
                        {
                            if (field != null){
                                field.SetValue(this, btn);
                            }
                            btn.onClick.AddListener(() => onBtnClick(btn, null));
                        }
                        break;
                        
                    case "toggle_":
                        if (child.TryGetComponent<Toggle>(out Toggle toggle))
                        {
                            if (field != null){
                                field.SetValue(this, toggle);
                            }
                            toggle.onValueChanged.AddListener((isOn) => onToggleClick(toggle, isOn));
                        }
                        break;
                        
                    case "sp_":
                        if (child.TryGetComponent<Image>(out Image img))
                        {
                            if (field != null){
                                field.SetValue(this, img);
                            }
                        }
                        break;
                        
                    case "node_":
                        if (field != null){
                            field.SetValue(this, child);
                        }
                        break;
                }
            }
        }

        public virtual void Init(object param)
        {
            Data = param;
            AutoBindUIComponents();
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

        public virtual void onBtnClick(Button btn,object param)
        {
            string name = btn.name;
            switch (name)
            {
                case "btn_close":
                    OnClose();
                    break;
                default:
                    break;
            }
        }
        public virtual void onToggleClick(Toggle toggle,object param)
        {
            string name = toggle.name;
            switch (name)
            {
                case "":
                    break;
                default:
                    break;
            }
        }
        public virtual void ResetOpen(object param)
        {
            Data = param;
            resetOpen();
        }

        public virtual void resetOpen()
        {
            start();
        }
    }
}
