---
name: window-generator
description: 根据用户描述的窗口功能和UI样貌，自动生成Unity窗口预制体构建器（类似SkillArcaneDetailWinBuilder.cs）和对应的窗口脚本（继承BaseWin.cs）。使用场景：当用户需要创建新的UI窗口并描述其功能和外观时，自动生成对应的Editor构建工具和窗口脚本。
---

# Unity窗口生成器

根据用户描述的窗口功能和UI样貌，自动生成完整的窗口系统代码，包括：
1. Editor预制体构建工具（类似SkillArcaneDetailWinBuilder.cs）
2. 窗口脚本（继承BaseWin.cs，遵循项目命名规范）

## 工作流程

当用户描述需要创建的窗口时，按以下步骤生成代码：

### 1. 分析用户需求
- 窗口功能（显示什么数据、交互方式）
- UI布局（面板尺寸、元素排列方式）
- UI组件（文本、按钮、图片、Toggle等）
- 特殊需求（点击关闭、拖拽等）

### 2. 生成窗口脚本（XxxWin.cs）
遵循BaseWin.cs的命名规范和生命周期：

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    // 参数类（如需要）
    public class XxxWinParam
    {
        public int id;
        // 其他参数...
    }

    public class XxxWin : BaseWin
    {
        private XxxWinParam data => Data as XxxWinParam;

        // UI组件（使用序列化字段支持预制体自动绑定）
        // 命名规范：
        // - sp_xxxImage: Image组件
        // - txt_xxxText: Text组件
        // - btn_xxxButton: Button组件（自动绑定点击事件）
        // - toggle_xxxToggle: Toggle组件（自动绑定值变化事件）
        // - node_xxx: Transform节点
        private Image sp_icon;
        private Text txt_name;
        private Text txt_desc;
        private Button btn_close;

        public override void load()
        {
            // 初始化逻辑（事件绑定等）
        }

        public override void start()
        {
            // 显示时的逻辑（数据刷新等）
            if (data == null)
            {
                Debug.LogError("[XxxWin] Data is null");
                OnClose();
                return;
            }
            
            updateView();
        }

        private void updateView()
        {
            // 根据data刷新UI
        }
    }
}
```

### 3. 生成Editor构建工具（XxxWinBuilder.cs）
参考SkillArcaneDetailWinBuilder.cs的模式：

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

namespace GeometryTD
{
    public class XxxWinBuilder : ScriptableWizard
    {
        [Header("输出设置")]
        [Tooltip("预制体保存路径（相对于Resources文件夹）")]
        public string outputPath = "UI/Windows";

        [Header("面板设置")]
        [Tooltip("面板宽度")]
        public float panelWidth = 500f;
        [Tooltip("面板高度")]
        public float panelHeight = 600f;
        // 其他可配置参数...

        [MenuItem("Tools/UI窗口预制体构建/生成 XxxWin")]
        static void CreateWizard()
        {
            XxxWinBuilder wizard = DisplayWizard<XxxWinBuilder>(
                "生成 XxxWin 预制体",
                "生成",
                "应用设置");
        }

        void OnWizardCreate()
        {
            BuildPrefab();
        }

        void OnWizardOtherButton()
        {
            OnWizardCreate();
        }

        private void BuildPrefab()
        {
            // 创建根对象
            GameObject root = new GameObject("XxxWin");
            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            // 添加窗口组件
            XxxWin winComponent = root.AddComponent<XxxWin>();

            // 构建UI层次结构
            BuildUIHierarchy(root.transform, winComponent);

            // 保存预制体
            string fullPath = $"Assets/Resources/{outputPath}/XxxWin.prefab";
            string directory = Path.GetDirectoryName(fullPath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 删除旧预制体
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fullPath) != null)
            {
                AssetDatabase.DeleteAsset(fullPath);
            }

            // 创建预制体
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, fullPath);
            
            if (prefab != null)
            {
                Debug.Log($"[XxxWinBuilder] ✓ 预制体生成成功: {fullPath}");
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogError($"[XxxWinBuilder] ✗ 预制体生成失败");
            }

            // 清理临时对象
            DestroyImmediate(root);
        }

        private void BuildUIHierarchy(Transform parent, XxxWin winComponent)
        {
            // 背景（用于点击关闭）
            GameObject bgObj = new GameObject("sp_background");
            bgObj.transform.SetParent(parent, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Image backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.5f);
            backgroundImage.raycastTarget = true;

            // 设置私有字段
            SetPrivateField(winComponent, "sp_background", backgroundImage);

            // 主面板
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(bgRt, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.15f, 0.95f);

            // 垂直布局
            VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.spacing = 15f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = panelObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 添加UI元素...
        }

        private Text CreateText(GameObject parent, string name, int fontSize, FontStyle style, Color color, TextAnchor alignment, float height, bool richText = false)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(300f, height > 0 ? height : 25f);

            Text txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.supportRichText = richText;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return txt;
        }

        private GameObject CreatePanel(GameObject parent, string name, Color color, float height)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent.transform, false);
            RectTransform rt = panelObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, height);

            Image img = panelObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            return panelObj;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[XxxWinBuilder] 未找到字段: {fieldName}");
            }
        }
    }
}
```

## UI组件命名规范

严格遵循BaseWin.cs的自动绑定规则：

| 前缀 | 组件类型 | 示例 |
|------|----------|------|
| `sp_` | Image | `sp_icon`, `sp_background` |
| `txt_` | Text | `txt_name`, `txt_desc` |
| `btn_` | Button | `btn_close`, `btn_confirm` |
| `toggle_` | Toggle | `toggle_mute`, `toggle_fullscreen` |
| `node_` | Transform | `node_content`, `node_list` |

**重要：**
- Button命名必须以`btn_`开头，会自动绑定到`onBtnClick`方法
- Toggle命名必须以`toggle_`开头，会自动绑定到`onToggleClick`方法
- 字段名需与GameObject名一致（支持带前缀或不带前缀）

## 窗口脚本规范

1. **继承BaseWin**：所有窗口必须继承`BaseWin`
2. **参数类**：如果需要传递参数，创建`XxxWinParam`类
3. **Data转换**：使用属性转换Data：`private XxxWinParam data => Data as XxxWinParam;`
4. **UI字段**：使用`[SerializeField] private`声明UI组件
5. **生命周期**：
   - `load()`: 初始化逻辑（事件绑定等）
   - `start()`: 显示时的逻辑（数据刷新等）
   - `closeWin()`: 关闭时的清理逻辑（可选）

## 使用方式

当用户说类似以下内容时，使用此技能：
- "创建一个技能详情窗口，显示技能图标、名称、描述、能量消耗"
- "生成一个设置面板，包含音量滑块、画质选项Toggle、关闭按钮"
- "我需要一个角色选择窗口，显示角色列表和确认按钮"

根据用户描述：
1. 分析需要的UI组件和布局
2. 生成对应的XxxWin.cs窗口脚本
3. 生成对应的XxxWinBuilder.cs预制体构建工具
4. 提供使用说明

## 注意事项

- 预制体保存在`Assets/Resources/UI/Windows/`目录
- Editor工具菜单路径：`Tools/UI窗口预制体构建/生成 XxxWin`
- 窗口通过`GameHelper.OpenWin<XxxWin>(param: new XxxWinParam{...})`打开
- 所有UI组件支持BaseWin的自动绑定机制
- 确保生成的代码符合项目的命名规范和架构设计