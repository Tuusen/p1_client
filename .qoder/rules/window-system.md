# Unity窗口管理系统规范

## 窗口类型Prefab命名规则

### 命名约定
- Prefab名称必须与C#类名完全一致
- 例如：`SkillSelectWin.cs` 对应 `SkillSelectWin.prefab`
- Prefab必须放在 `Assets/Resources/UI/` 目录下
- 脚本必须放在 `Assets/Scripts/UI/Windows/` 目录下

### UI组件命名规则
在Prefab中的UI组件必须遵循以下命名约定，以便自动绑定：

| 前缀 | 组件类型 | 示例 |
|------|----------|------|
| `btn_` | Button（按钮） | `btn_close`, `btn_confirm`, `btn_skill_1` |
| `toggle_` | Toggle（复选框） | `toggle_speed`, `toggle_sound`, `toggle_option` |
| `txt_` | Text（文本） | `txt_title`, `txt_content`, `txt_score` |
| `sp_` | Image/Sprite（图片） | `sp_icon`, `sp_bg`, `sp_avatar` |
| `node_` | Transform（节点） | `node_content`, `node_list`, `node_panel` |

### 字段匹配规则
在C#脚本中声明的字段名需要与Prefab中的GameObject名称匹配：
1. 完全匹配：字段名 `btn_close` 匹配 GameObject名 `btn_close`
2. 去前缀匹配：字段名 `btn_close` 也可以匹配 GameObject名 `close`
3. 后缀匹配：字段名 `close` 可以匹配 GameObject名 `xxx_close`

## 事件注册规范

### 按钮事件处理
所有按钮的点击事件会自动注册到 `onBtnClick` 函数，子类必须重写此方法来处理具体的按钮逻辑：

```csharp
public override void onBtnClick(Button btn, object param)
{
    string name = btn.name;
    switch (name)
    {
        case "btn_close":
            OnClose();
            break;
        case "btn_confirm":
            // 处理确认按钮逻辑
            break;
        case "btn_cancel":
            // 处理取消按钮逻辑
            break;
        default:
            break;
    }
}
```

### 复选框事件处理
所有Toggle的值变化事件会自动注册到 `onToggleClick` 函数，子类必须重写此方法来处理具体的Toggle逻辑：

```csharp
public override void onToggleClick(Toggle toggle, object param)
{
    string name = toggle.name;
    switch (name)
    {
        case "toggle_sound":
            bool isOn = (bool)param;
            // 处理声音开关逻辑
            break;
        case "toggle_fullscreen":
            bool isOn = (bool)param;
            // 处理全屏切换逻辑
            break;
        default:
            break;
    }
}
```

## 窗口脚本结构

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class XxxWin : BaseWin
    {
        // 使用 [SerializeField] 声明UI引用，字段名需遵循命名规则
        [SerializeField] private Button btn_close;
        [SerializeField] private Button btn_confirm;
        [SerializeField] private Toggle toggle_sound;
        [SerializeField] private Text txt_title;
        [SerializeField] private Image sp_icon;
        [SerializeField] private Transform node_content;

        public override void Init(object param)
        {
            base.Init(param);
            // 额外的初始化逻辑（如果需要）
        }

        public override void load()
        {
            // 数据加载逻辑
        }

        public override void start()
        {
            // 窗口显示时的逻辑
        }

        public override void onBtnClick(Button btn, object param)
        {
            string name = btn.name;
            switch (name)
            {
                case "btn_close":
                    OnClose();
                    break;
                case "btn_confirm":
                    // 处理确认逻辑
                    break;
                default:
                    base.onBtnClick(btn, param);
                    break;
            }
        }

        public override void onToggleClick(Toggle toggle, object param)
        {
            string name = toggle.name;
            switch (name)
            {
                case "toggle_sound":
                    bool isOn = (bool)param;
                    // 处理Toggle逻辑
                    break;
                default:
                    base.onToggleClick(toggle, param);
                    break;
            }
        }

        public override void closeWin()
        {
            // 清理逻辑
        }
    }
}
```

## 打开和关闭窗口

- 打开窗口：`GameHelper.OpenWin<XxxWin>()` 或 `WinManager.Instance.OpenWin<XxxWin>()`
- 关闭窗口：`GameHelper.CloseWin<XxxWin>()` 或 `WinManager.Instance.CloseWin<XxxWin>()`

## 重要规则

1. **禁止直接使用** `gameObject.SetActive()` 来显示/隐藏窗口，必须通过 `WinManager`
2. **禁止手动实例化** 窗口预制体，必须使用 `WinManager.OpenWin<T>()`
3. 窗口内的关闭按钮应调用 `WinManager.Instance.CloseWin<T>()`
4. 在 `Init()` 中进行一次性设置（如按钮绑定）
5. 在 `Show()` 中刷新数据（每次打开时执行）
6. 在Inspector中设置 `sortOrder` 来控制窗口层级（值越大渲染越靠前）
