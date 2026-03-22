using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 主菜单控制器 - 代码生成主菜单 UI
    /// 挂载在 MainMenu 场景的空 GameObject 上，Start 时自动构建界面
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("场景名称（可在 Inspector 修改）")]
        [SerializeField] private string gameSceneName = "SampleScene";

        [Header("主菜单文字")]
        [SerializeField] private string titleText   = "几何塔防";
        [SerializeField] private string startText   = "开始游戏";
        [SerializeField] private string versionText = "v0.1";

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Canvas ──────────────────────────────────────────────────────
            GameObject cGo = new GameObject("MainMenuCanvas");
            Canvas canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler cs = cGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight  = 0.5f;
            cGo.AddComponent<GraphicRaycaster>();

            // ── 背景 ────────────────────────────────────────────────────────
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(cGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.06f, 0.10f, 1f);

            // ── 中央容器 ────────────────────────────────────────────────────
            GameObject centerGo = new GameObject("CenterPanel");
            centerGo.transform.SetParent(cGo.transform, false);
            var centerRt = centerGo.AddComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.5f, 0.5f);
            centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.pivot     = new Vector2(0.5f, 0.5f);
            centerRt.sizeDelta = new Vector2(480f, 380f);
            centerRt.anchoredPosition = Vector2.zero;

            // ── 标题 ────────────────────────────────────────────────────────
            CreateText("TitleText", centerGo.transform,
                new Vector2(0f, 110f), new Vector2(420f, 100f),
                titleText, 64, new Color(0.95f, 0.88f, 0.3f),
                FontStyle.Bold, TextAnchor.MiddleCenter);

            // ── 副标题装饰线 ────────────────────────────────────────────────
            GameObject lineGo = new GameObject("TitleLine");
            lineGo.transform.SetParent(centerGo.transform, false);
            var lineRt = lineGo.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.pivot     = new Vector2(0.5f, 0.5f);
            lineRt.sizeDelta = new Vector2(300f, 2f);
            lineRt.anchoredPosition = new Vector2(0f, 64f);
            lineGo.AddComponent<Image>().color = new Color(0.95f, 0.88f, 0.3f, 0.5f);

            // ── 开始游戏按钮 ─────────────────────────────────────────────────
            GameObject btnGo = CreateButton("StartButton", centerGo.transform,
                new Vector2(0f, -10f), new Vector2(260f, 60f),
                startText, 28,
                new Color(0.15f, 0.45f, 0.85f),
                new Color(0.22f, 0.58f, 1f),
                new Color(0.1f, 0.32f, 0.65f));

            btnGo.GetComponent<Button>().onClick.AddListener(OnStartClicked);

            // ── 版本号 ──────────────────────────────────────────────────────
            CreateText("VersionText", cGo.transform,
                Vector2.zero, new Vector2(200f, 28f),
                versionText, 16, new Color(0.5f, 0.5f, 0.5f),
                FontStyle.Normal, TextAnchor.MiddleCenter,
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPosOverride: new Vector2(-10f, 10f));
        }

        private void OnStartClicked()
        {
            // 优先加载配置的场景名，找不到则依次尝试备选名
            string[] candidates = { gameSceneName, "SampleScene", "Game" };
            foreach (var sceneName in candidates)
            {
                if (IsSceneInBuild(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                    return;
                }
            }
            Debug.LogError("[MainMenu] 未找到可用的游戏场景！请执行菜单：Tools → 几何塔防 → 创建主菜单场景，或手动将场景加入 Build Settings。");
        }

        /// <summary>检查场景是否已加入 Build Settings</summary>
        private static bool IsSceneInBuild(string sceneName)
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                // path 格式：Assets/Scenes/SampleScene.unity
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return true;
            }
            return false;
        }

        // ── UI 辅助方法 ──────────────────────────────────────────────────────

        private static GameObject CreateText(
            string name, Transform parent,
            Vector2 anchoredPos, Vector2 size,
            string text, int fontSize,
            Color color,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2? pivot = null, Vector2? anchoredPosOverride = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            Vector2 am = anchorMin ?? new Vector2(0.5f, 0.5f);
            Vector2 ax = anchorMax ?? new Vector2(0.5f, 0.5f);
            Vector2 pv = pivot    ?? new Vector2(0.5f, 0.5f);

            rt.anchorMin        = am;
            rt.anchorMax        = ax;
            rt.pivot            = pv;
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPosOverride ?? anchoredPos;

            var t = go.AddComponent<Text>();
            t.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text            = text;
            t.fontSize        = fontSize;
            t.fontStyle       = fontStyle;
            t.alignment       = alignment;
            t.color           = color;
            t.supportRichText = false;
            return go;
        }

        private static GameObject CreateButton(
            string name, Transform parent,
            Vector2 anchoredPos, Vector2 size,
            string label, int fontSize,
            Color normalColor, Color highlightColor, Color pressedColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = normalColor;

            var btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = normalColor;
            cb.highlightedColor = highlightColor;
            cb.pressedColor     = pressedColor;
            cb.colorMultiplier  = 1f;
            btn.colors          = cb;
            btn.targetGraphic   = img;

            // 标签文字
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var txt = lblGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = Color.white;
            return go;
        }
    }
}
