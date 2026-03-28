---
trigger: always_on
---

# Window System Rules

All new windows (panels, dialogs, popups) MUST follow the unified window management system.

## Creating a New Window

1. **Script**: Create `XxxWin.cs` in `Assets/Scripts/UI/`, inheriting from `BaseWin`.
2. **Prefab**: Create `XxxWin.prefab` in `Assets/Resources/UI/`. The prefab name MUST match the class name exactly.
3. **Component**: Attach the `XxxWin` script to the prefab root GameObject.

## Script Structure

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class XxxWin : BaseWin
    {
        // Use [SerializeField] for UI references
        [SerializeField] private Button closeButton;

        public override void Init()
        {
            base.Init();
            // Bindbutton listeners here
            if (closeButton != null)
                closeButton.onClick.AddListener(() => WinManager.Instance.CloseWin<XxxWin>());
        }

        public override void Show()
        {
            base.Show();
            // Refresh data here
        }

        public override void OnClose()
        {
            base.OnClose();
            // Cleanup if needed
        }
    }
}
```

## Opening and Closing Windows

- Open: `GameHelper.OpenWin<XxxWin>()` or `WinManager.Instance.OpenWin<XxxWin>()`
- Close: `GameHelper.CloseWin<XxxWin>()` or `WinManager.Instance.CloseWin<XxxWin>()`

## Rules

- NEVER use `gameObject.SetActive()` directly to show/hide windows. Always go through `WinManager`.
- NEVER instantiate window prefabs manually. Always use `WinManager.OpenWin<T>()`.
- Close buttons inside a window should call `WinManager.Instance.CloseWin<T>()`.
- Use `Init()` for one-time setup (button bindings). Use `Show()` for data refresh on each open.
- Set `sortOrder` in the Inspector to control window layering (higher values render on top).
- The prefab file name must exactly match the C# class name (e.g., `SkillSelectWin.cs` -> `SkillSelectWin.prefab`).
