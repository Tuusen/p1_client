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
        [SerializeField] private WinType winType = WinType.Normal;
        [SerializeField] private WinPriority priority = WinPriority.Normal;

        public int SortOrder => sortOrder;
        public WinType WinType => winType;
        public WinPriority Priority => priority;
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// 自动根据命名规则绑定UI组件
        /// 命名规则：txt_xx代表文本，btn_xx代表按钮，toggle_xx代表复选框，node_xx代表节点等
        /// 字段名需与GameObject名一致（支持带前缀或不带前缀）
        /// </summary>
        private void AutoBindUIComponents()
        {
            // 获取当前类的所有字段（包括私有和公共）
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // 获取所有子对象
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            
            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;
                Transform match = null;
                
                // 查找匹配的子对象
                foreach (Transform child in allChildren)
                {
                    if (child == transform) continue;
                    
                    string _childName = child.name;
                    
                    // 匹配规则：
                    // 1. 完全匹配
                    // 2. 字段名匹配去掉前缀后的名称（如 btn_close 匹配 btn_close 或 close）
                    string strippedName = fieldName
                        .Replace("node_", "")
                        .Replace("btn_", "")
                        .Replace("txt_", "")
                        .Replace("sp_", "")
                        .Replace("toggle_", "");
                    
                    if (_childName == fieldName || _childName == strippedName || _childName.EndsWith("_" + fieldName))
                    {
                        match = child;
                        break;
                    }
                }
                
                if (match == null) continue;
                
                // 根据字段类型进行绑定
                string childName = match.name;
                
                if (field.FieldType == typeof(Transform))
                {
                    field.SetValue(this, match);
                }
                else if (field.FieldType == typeof(Button) && match.TryGetComponent<Button>(out Button btn))
                {
                    field.SetValue(this, btn);
                    if (childName.StartsWith("btn_"))
                    {
                        btn.onClick.AddListener(() => onBtnClick(btn, null));
                    }
                }
                else if (field.FieldType == typeof(Toggle) && match.TryGetComponent<Toggle>(out Toggle toggle))
                {
                    field.SetValue(this, toggle);
                    if (childName.StartsWith("toggle_"))
                    {
                        toggle.onValueChanged.AddListener((isOn) => onToggleClick(toggle, isOn));
                    }
                }
                else if (field.FieldType == typeof(Text) && match.TryGetComponent<Text>(out Text txt))
                {
                    field.SetValue(this, txt);
                }
                else if (field.FieldType == typeof(Image) && match.TryGetComponent<Image>(out Image img))
                {
                    field.SetValue(this, img);
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
