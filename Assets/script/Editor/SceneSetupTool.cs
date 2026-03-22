#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 2D 场景设置工具 - 一键生成完整游戏场景
    /// </summary>
    public static class SceneSetupTool
    {
        // ── 一键设置 ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/一键设置场景", false, 100)]
        public static void SetupCompleteScene()
        {
            Debug.Log("[SceneSetup] 开始构建 2D 场景...");

            // 先确保所有预制体已生成（若已存在则跳过）
            PrefabGenerator.GenerateAllPrefabs();

            SetupTags();         // 1. 标签
            SetupCamera();       // 2. 相机
            SetupBackground();   // 3. 背景
            CreatePlayer();      // 4. 玩家
            CreateSpawner();     // 5. 生成器
            CreateGameUI();      // 6. UI
            CreateGameManager(); // 7. 管理器

            Debug.Log("[SceneSetup] 完成！按 Play 开始游戏。");
        }

        // ── 1. 标签 ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/设置标签", false, 108)]
        public static void SetupTags()
        {
            AddTag("Enemy");
            AddTag("Bullet");
            Debug.Log("[SceneSetup] 标签已就绪");
        }

        private static void AddTag(string tagName)
        {
            SerializedObject tagMgr = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagMgr.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagMgr.ApplyModifiedProperties();
        }

        // ── 2. 2D 正交相机（手机横屏 16:9）────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建相机", false, 102)]
        public static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam    = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            // 手机横屏：16:9 的正交大小 = 宽度对应 32 个 Unity 单位
            // orthographicSize = height/2，库觙大小 = 16/9 * size×2 = ~18 Unity单位
            cam.orthographic     = true;
            cam.orthographicSize = 10.7f;     // 垂直 21.4 单位，16:9 横向 ~38 单位（能展现玩家x=-16到刷新点x=22）
            cam.transform.position  = new Vector3(3f, 0f, -10f);  // 居中偏右
            cam.backgroundColor  = new Color(0.08f, 0.08f, 0.12f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            Debug.Log("[SceneSetup] 相机就绪（横屏 16:9）");
        }

        // ── 3. 背景（简单深色 Sprite）────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建背景", false, 103)]
        public static void SetupBackground()
        {
            if (GameObject.Find("Background")) return;

            // 使用内置白色 Sprite 缩放填充
            GameObject bg = new GameObject("Background");
            bg.transform.position = Vector3.zero;

            SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite       = CreateSolidSprite(new Color(0.1f, 0.1f, 0.15f));
            sr.color        = new Color(0.1f, 0.1f, 0.15f);
            sr.sortingOrder = -10;
            bg.transform.localScale = new Vector3(60f, 25f, 1f);

            // 地面线（底部横线装饰）
            GameObject line = new GameObject("GroundLine");
            line.transform.position = new Vector3(0, -4.2f, 0);
            SpriteRenderer lr = line.AddComponent<SpriteRenderer>();
            lr.sprite       = CreateSolidSprite(new Color(0.3f, 0.35f, 0.4f));
            lr.color        = new Color(0.3f, 0.35f, 0.4f);
            lr.sortingOrder = -9;
            line.transform.localScale = new Vector3(60f, 0.06f, 1f);

            Debug.Log("[SceneSetup] 背景就绪");
        }

        private static Sprite CreateSolidSprite(Color c)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        // ── 4. 玩家 ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建玩家", false, 104)]
        public static void CreatePlayer()
        {
            if (GameObject.FindGameObjectWithTag("Player") != null)
            { Debug.Log("[SceneSetup] 玩家已存在"); return; }

            ConfigLoader.LoadAllConfigs();
            PlayerConfig cfg = ConfigLoader.GameConfig.player;
            Color col = GeometryMeshGenerator.ParseColor(cfg.color);

            // 根对象
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(-16f, 0f, 0f);

            // 2D 视觉
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(cfg.shape, cfg.scale, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            // 头顶血条（显示血量和盾量数字）
            CreateWorldSpaceHealthBar(player.transform, cfg.health, cfg.shield,
                cfg.scale * 0.9f + 0.4f, true);

            // 玩家逻辑
            player.AddComponent<PlayerController>();

            Debug.Log("[SceneSetup] 玩家就绪");
        }

        // ── 5. 生成器 ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建生成器", false, 105)]
        public static void CreateSpawner()
        {
            if (Object.FindObjectOfType<EnemySpawner>() != null)
            { Debug.Log("[SceneSetup] 生成器已存在"); return; }

            GameObject go = new GameObject("EnemySpawner");
            go.transform.position = Vector3.zero;
            var spawner = go.AddComponent<EnemySpawner>();
            spawner.spawnX = 22f;   // 玩家x=-16，生成点x=22，距离38单位
            Debug.Log("[SceneSetup] 生成器就绪");
        }

        // ── 6. HUD UI ────────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建游戏UI", false, 106)]
        public static void CreateGameUI()
        {
            if (Object.FindObjectOfType<GameUIController>() != null)
            { Debug.Log("[SceneSetup] UI 已存在"); return; }

            // ── 优先使用预制体 ─────────────────────────────────────────────
            string prefabPath = "Assets/prefab/GameUI.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
                // 确保 EventSystem 存在
                if (Object.FindObjectOfType<EventSystem>() == null)
                {
                    GameObject esGo = new GameObject("EventSystem");
                    esGo.AddComponent<EventSystem>();
                    esGo.AddComponent<StandaloneInputModule>();
                }
                Debug.Log("[SceneSetup] GameUI 预制体实例化完成，可在 Hierarchy 中直接编辑");
                return;
            }

            // ── 若预制体不存在，退回代码生成（兼容旧逻辑）────────────────
            // Canvas：基准分辨率改为 1280×720（手机横屏）
            GameObject cGo = new GameObject("GameUI");
            Canvas canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler cs = cGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
            cGo.AddComponent<GraphicRaycaster>();

            // EventSystem（按钮点击必须有）
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Debug.Log("[SceneSetup] EventSystem 已创建");
            }

            // 分数 / 波次 / 时间（屏幕左上角）
            MakeText("ScoreText",  cGo.transform, new Vector2(90, -30),  new Vector2(200, 36), "分数: 0", 22);
            MakeText("WaveText",   cGo.transform, new Vector2(90, -70),  new Vector2(200, 36), "波次: 1", 22);
            MakeText("TimeText",   cGo.transform, new Vector2(90, -110), new Vector2(200, 36), "时间: 00:00", 22);

            // 技能栏容器（屏幕底部居中）
            CreateSkillBarContainer(cGo.transform);

            // 游戏结束面板
            GameObject panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(cGo.transform, false);
            RectTransform pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
            pr.offsetMin = pr.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            panel.SetActive(false);

            MakeText("GameOverTitle", panel.transform, new Vector2(0, 80), new Vector2(400, 60), "游戏结束",
                     44, Color.red, TextAnchor.MiddleCenter, true);
            MakeText("FinalScoreText", panel.transform, new Vector2(0, 15),  new Vector2(350, 46), "最终分数: 0",
                     28, Color.white, TextAnchor.MiddleCenter, true);
            MakeButton("RestartButton",  panel.transform, new Vector2(0, -55),  "重新开始");
            MakeButton("MainMenuButton", panel.transform, new Vector2(0, -115), "返回主菜单");

            cGo.AddComponent<GameUIController>();
            Debug.Log("[SceneSetup] UI 就绪");
        }

        // ── 技能栏容器（独立 Canvas，确保按钮点击不受父容器裁剪影响）──────
        private static void CreateSkillBarContainer(Transform canvasParent)
        {
            // ---- 外层根节点（包含技能栏+能量栏）----
            GameObject root = new GameObject("SkillBarRoot");
            root.transform.SetParent(canvasParent, false);

            // 独立 Canvas
            Canvas barCanvas = root.AddComponent<Canvas>();
            barCanvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            barCanvas.sortingOrder  = 25;
            barCanvas.overrideSorting = true;

            CanvasScaler barCs = root.AddComponent<CanvasScaler>();
            barCs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            barCs.referenceResolution = new Vector2(1280, 720);
            barCs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            barCs.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin        = new Vector2(0.5f, 0f);
            rootRt.anchorMax        = new Vector2(0.5f, 0f);
            rootRt.pivot            = new Vector2(0.5f, 0f);
            rootRt.sizeDelta        = new Vector2(960f, 200f);  // 技能栏160 + 能量栏40
            rootRt.anchoredPosition = new Vector2(0f, 4f);

            // ---- 技能栏（上方）----
            GameObject bar = new GameObject("SkillBar");
            bar.transform.SetParent(root.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin        = new Vector2(0.5f, 0f);
            barRt.anchorMax        = new Vector2(0.5f, 0f);
            barRt.pivot            = new Vector2(0.5f, 0f);
            barRt.sizeDelta        = new Vector2(960f, 160f);
            barRt.anchoredPosition = new Vector2(0f, 36f);  // 坐落在能量栏上方（能量栏高约36px）
            bar.AddComponent<SkillBarUI>();

            // ---- 能量栏（下方）----
            CreateEnergyBarContainer(root.transform);
        }

        // ── 能量栏容器（技能栏下方，4列火/冰/电/风横排，紧凑布局）────────────
        private static void CreateEnergyBarContainer(Transform parent)
        {
            // 尺寸定义：4列横排（紧凑），每列 = 标签(16px) + 进度条(50px) + 超能数字(24px)
            const float COL_GAP  = 8f;   // 列间距
            const float BAR_W    = 50f;  // 进度条宽度（缩短）
            const float BAR_H    = 12f;  // 进度条高度
            const float LABEL_W  = 16f;  // 元素标签宽度
            const float SUPER_W  = 24f;  // 超能数字宽度
            const float COL_W    = LABEL_W + 3f + BAR_W + 3f + SUPER_W;  // 单列宽度
            const float PADDING  = 5f;
            const float TOTAL_W  = COL_W * 4 + COL_GAP * 3 + PADDING * 2;
            const float TOTAL_H  = BAR_H + PADDING * 2;
        
            GameObject energyRoot = new GameObject("EnergyBar");
            energyRoot.transform.SetParent(parent, false);
            RectTransform erRt = energyRoot.AddComponent<RectTransform>();
            erRt.anchorMin        = new Vector2(0.5f, 0f);
            erRt.anchorMax        = new Vector2(0.5f, 0f);
            erRt.pivot            = new Vector2(0.5f, 0f);
            erRt.sizeDelta        = new Vector2(TOTAL_W, TOTAL_H);
            erRt.anchoredPosition = new Vector2(0f, PADDING);
        
            // 半透明背景
            var bgImg = energyRoot.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.82f);
        
            // 元素定义：名称/标签/颜色
            var elements = new[]
            {
                (rowName:"FireRow",      label:"火", bgCol:new Color(0.3f,0.06f,0.04f,1f),   fillCol:new Color(1f,0.35f,0.1f)),
                (rowName:"IceRow",       label:"冰", bgCol:new Color(0.04f,0.14f,0.3f,1f),   fillCol:new Color(0.3f,0.75f,1f)),
                (rowName:"LightningRow", label:"电", bgCol:new Color(0.15f,0.1f,0.3f,1f),    fillCol:new Color(0.75f,0.4f,1f)),
                (rowName:"WindRow",      label:"风", bgCol:new Color(0.05f,0.22f,0.08f,1f),  fillCol:new Color(0.3f,1f,0.4f)),
            };
        
            // 第一列起始 X 坐标（居中）
            float startX = -(COL_W * 4 + COL_GAP * 3) * 0.5f;
        
            for (int i = 0; i < elements.Length; i++)
            {
                var   e    = elements[i];
                float colX = startX + i * (COL_W + COL_GAP);
        
                // 列容器
                GameObject row = new GameObject(e.rowName);
                row.transform.SetParent(energyRoot.transform, false);
                row.transform.localScale = Vector3.one;
                var rowRt = row.AddComponent<RectTransform>();
                rowRt.anchorMin        = new Vector2(0.5f, 0.5f);
                rowRt.anchorMax        = new Vector2(0.5f, 0.5f);
                rowRt.pivot            = new Vector2(0f, 0.5f);
                rowRt.sizeDelta        = new Vector2(COL_W, BAR_H);
                rowRt.anchoredPosition = new Vector2(colX, 0f);
        
                // 元素标签
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(row.transform, false);
                labelGo.transform.localScale = Vector3.one;
                var labelRt = labelGo.AddComponent<RectTransform>();
                labelRt.anchorMin        = new Vector2(0f, 0.5f);
                labelRt.anchorMax        = new Vector2(0f, 0.5f);
                labelRt.pivot            = new Vector2(0f, 0.5f);
                labelRt.sizeDelta        = new Vector2(LABEL_W, BAR_H);
                labelRt.anchoredPosition = new Vector2(0f, 0f);
                var labelTxt = labelGo.AddComponent<Text>();
                labelTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelTxt.fontSize  = 12;
                labelTxt.fontStyle = FontStyle.Bold;
                labelTxt.alignment = TextAnchor.MiddleCenter;
                labelTxt.color     = e.fillCol;
                labelTxt.text      = e.label;
        
                // 进度条背景
                GameObject barBg = new GameObject("BarBg");
                barBg.transform.SetParent(row.transform, false);
                barBg.transform.localScale = Vector3.one;
                var barBgRt = barBg.AddComponent<RectTransform>();
                barBgRt.anchorMin        = new Vector2(0f, 0.5f);
                barBgRt.anchorMax        = new Vector2(0f, 0.5f);
                barBgRt.pivot            = new Vector2(0f, 0.5f);
                barBgRt.sizeDelta        = new Vector2(BAR_W, BAR_H);
                barBgRt.anchoredPosition = new Vector2(LABEL_W + 3f, 0f);
                barBg.AddComponent<Image>().color = e.bgCol;
        
                // 进度条填充
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(barBg.transform, false);
                fill.transform.localScale = Vector3.one;
                var fillRt = fill.AddComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
                var fillImg = fill.AddComponent<Image>();
                fillImg.color      = e.fillCol;
                fillImg.type       = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImg.fillAmount = 0f;
        
                // 超能数字
                GameObject superGo = new GameObject("SuperText");
                superGo.transform.SetParent(row.transform, false);
                superGo.transform.localScale = Vector3.one;
                var superRt = superGo.AddComponent<RectTransform>();
                superRt.anchorMin        = new Vector2(0f, 0.5f);
                superRt.anchorMax        = new Vector2(0f, 0.5f);
                superRt.pivot            = new Vector2(0f, 0.5f);
                superRt.sizeDelta        = new Vector2(SUPER_W, BAR_H);
                superRt.anchoredPosition = new Vector2(LABEL_W + 3f + BAR_W + 3f, 0f);
                var superTxt = superGo.AddComponent<Text>();
                superTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                superTxt.fontSize  = 12;
                superTxt.fontStyle = FontStyle.Bold;
                superTxt.alignment = TextAnchor.MiddleLeft;
                superTxt.color     = new Color(0.6f, 0.6f, 0.6f);
                superTxt.text      = "0";
            }
        
            energyRoot.AddComponent<EnergyBarUI>();
        }

        // ── 7. GameManager ────────────────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建GameManager", false, 108)]
        public static void CreateGameManager()
        {
            if (Object.FindObjectOfType<GameManager>() != null)
            { Debug.Log("[SceneSetup] GameManager 已存在"); return; }

            // ── 优先使用预制体 ─────────────────────────────────────────────
            string prefabPath = "Assets/prefab/GameManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
                Debug.Log("[SceneSetup] GameManager 预制体实例化完成");
                return;
            }

            // 退回代码生成
            new GameObject("GameManager").AddComponent<GameManager>();
            Debug.Log("[SceneSetup] GameManager 就绪");
        }

        // ── 8. 创建技能槽预制体 ────────────────────────────────────────────
        [MenuItem("Tools/几何塔防/创建技能槽预制体", false, 120)]
        public static void CreateSkillSlotPrefab()
        {
            const float SLOT_W = 110f;
            const float SLOT_H = 140f;
            const float ICON_H = 80f;
            const float EXP_H  = 8f;
            const float LV_H   = 20f;
            const float PAD    = 8f;

            // ── 根节点 ──
            GameObject slot = new GameObject("SkillSlot");
            var rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(SLOT_W, SLOT_H);

            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.95f);

            var btn = slot.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor     = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.disabledColor    = new Color(0.42f, 0.42f, 0.42f, 0.7f);
            btn.colors = cb;
            btn.targetGraphic = bg;

            // ── 图标区域 ──
            GameObject iconGo = CreateChild(slot.transform, "Icon",
                new Vector2(SLOT_W - 8f, ICON_H), new Vector2(0.5f, 1f), new Vector2(0f, -PAD));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.3f, 0.6f, 1f);

            // 技能名
            GameObject nameGo = CreateChild(iconGo.transform, "NameText",
                new Vector2(SLOT_W - 8f, 22f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize = 11;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = Color.white;
            nameTxt.text = "技能名";

            // 冷却遮罩
            GameObject cdMaskGo = CreateChild(iconGo.transform, "CooldownMask",
                new Vector2(SLOT_W - 8f, ICON_H), new Vector2(0.5f, 0.5f), Vector2.zero);
            var cdMask = cdMaskGo.AddComponent<Image>();
            cdMask.color         = new Color(0f, 0f, 0f, 0.75f);
            cdMask.type          = Image.Type.Filled;
            cdMask.fillMethod    = Image.FillMethod.Radial360;
            cdMask.fillOrigin    = (int)Image.Origin360.Top;
            cdMask.fillClockwise = true;
            cdMask.fillAmount    = 0.6f; // 预览状态，运行时修改
            cdMaskGo.SetActive(false);

            // 冷却数字
            GameObject cdTxtGo = CreateChild(cdMaskGo.transform, "CooldownText",
                new Vector2(SLOT_W - 8f, 28f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var cdTxt = cdTxtGo.AddComponent<Text>();
            cdTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdTxt.fontSize  = 20;
            cdTxt.fontStyle = FontStyle.Bold;
            cdTxt.alignment = TextAnchor.MiddleCenter;
            cdTxt.color     = Color.white;
            cdTxt.text      = "8s";

            // ── 经验条 ──
            float expOffY = -(PAD + ICON_H + 4f);
            GameObject expBgGo = CreateChild(slot.transform, "ExpBg",
                new Vector2(SLOT_W - 8f, EXP_H), new Vector2(0.5f, 1f), new Vector2(0f, expOffY));
            expBgGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);

            GameObject expFillGo = new GameObject("ExpFill");
            expFillGo.transform.SetParent(expBgGo.transform, false);
            expFillGo.transform.localScale = Vector3.one;
            var expFillRt = expFillGo.AddComponent<RectTransform>();
            expFillRt.anchorMin = Vector2.zero;
            expFillRt.anchorMax = Vector2.one;
            expFillRt.offsetMin = expFillRt.offsetMax = Vector2.zero;
            var expFill = expFillGo.AddComponent<Image>();
            expFill.color      = new Color(0.3f, 0.85f, 1f);
            expFill.type       = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillAmount = 0.5f; // 预览，运行时修改

            // ── 等级文字 ──
            GameObject lvGo = CreateChild(slot.transform, "LevelText",
                new Vector2(SLOT_W, LV_H), new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            var lvTxt = lvGo.AddComponent<Text>();
            lvTxt.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lvTxt.fontSize        = 13;
            lvTxt.fontStyle       = FontStyle.Bold;
            lvTxt.alignment       = TextAnchor.MiddleCenter;
            lvTxt.color           = new Color(0.85f, 0.85f, 1f);
            lvTxt.supportRichText = true;
            lvTxt.text            = "Lv5";

            // ── 添加 SkillSlotUI 并绑定引用 ──
            var slotUI = slot.AddComponent<SkillSlotUI>();
            slotUI.button       = btn;
            slotUI.iconImage    = iconImg;
            slotUI.nameText     = nameTxt;
            slotUI.cooldownMask = cdMask;
            slotUI.cooldownText = cdTxt;
            slotUI.expBgImage   = expBgGo.GetComponent<Image>();
            slotUI.expFillImage = expFill;
            slotUI.levelText    = lvTxt;

            // ── 保存预制体 ──
            string dir = "Assets/prefab";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "prefab");

            string path = dir + "/SkillSlot.prefab";
            bool success;
            if (Selection.activeGameObject == slot) Selection.activeObject = null;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, path, out success);
            Object.DestroyImmediate(slot);

            if (success)
            {
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[SceneSetup] 技能槽预制体已保存到 {path}");
                Debug.Log("▶ 请将 SkillSlot.prefab 拖入 Hierarchy 中的 SkillBar 子节点绑定，\n" +
                          "   或将预制体拖入 SkillBarUI 的 Slot Prefab 字段！");
            }
            else
                Debug.LogError("[SceneSetup] 预制体保存失败！");
        }

        // 创建带 RectTransform 的子节点
        private static GameObject CreateChild(Transform parent, string name,
            Vector2 sizeDelta, Vector2 pivot, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            var rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = pivot;
            rt.anchorMax        = pivot;
            rt.sizeDelta        = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        // ── World Space 血条（玩家用）────────────────────────────────────────────
        private static void CreateWorldSpaceHealthBar(
            Transform parent, int maxHp, int maxSp, float offsetY, bool showValue)
        {
            bool  hasShield = maxSp > 0;
            float barH = showValue ? 18f : 12f;  // 显示数字时血条加高
            float gap  = 4f;
            float totalH = hasShield ? barH * 2 + gap : barH;

            GameObject hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(parent);
            hbObj.transform.localPosition = new Vector3(0, offsetY, 0);

            Canvas cv = hbObj.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.WorldSpace;
            cv.sortingOrder = 10;
            hbObj.AddComponent<CanvasScaler>();

            RectTransform cr = hbObj.GetComponent<RectTransform>();
            cr.sizeDelta  = new Vector2(180f, totalH);
            cr.localScale = Vector3.one * 0.01f;

            // 血条行（有盾时居上，无盾时居中）
            float hpRowY = hasShield ? (barH + gap) * 0.5f : 0f;
            MakeBarRow("HealthRow", hbObj.transform, barH, hpRowY,
                new Color(0.15f, 0.08f, 0.08f, 0.9f), "HealthFill", new Color(0.9f, 0.2f, 0.15f),
                showValue ? "HpText" : null);

            // 盾条行
            if (hasShield)
                MakeBarRow("ShieldRow", hbObj.transform, barH, -(barH + gap) * 0.5f,
                    new Color(0.08f, 0.12f, 0.2f, 0.9f), "ShieldFill", new Color(0.3f, 0.55f, 1f),
                    showValue ? "SpText" : null);

            HealthBarController hbc = hbObj.AddComponent<HealthBarController>();
            hbc.Initialize(maxHp, maxSp, showValue);
        }

        // 创建一行血条（带背景+填充+可选数字）
        private static void MakeBarRow(string rowName, Transform parent, float barH, float posY,
            Color bgColor, string fillName, Color fillColor, string valueName = null)
        {
            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent);
            row.transform.localScale = Vector3.one;
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 0.5f);
            rowRt.anchorMax = new Vector2(1, 0.5f);
            rowRt.pivot     = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(0, barH);
            rowRt.anchoredPosition = new Vector2(0, posY);
            row.AddComponent<Image>().color = bgColor;

            // 填充
            GameObject fill = new GameObject(fillName);
            fill.transform.SetParent(row.transform);
            fill.transform.localScale = Vector3.one;
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Image img = fill.AddComponent<Image>();
            img.color      = fillColor;
            img.type       = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            // 数字叠加层（居中显示在进度条中）
            if (!string.IsNullOrEmpty(valueName))
            {
                GameObject vGo = new GameObject(valueName);
                vGo.transform.SetParent(row.transform);
                vGo.transform.localScale = Vector3.one;
                var vRt = vGo.AddComponent<RectTransform>();
                vRt.anchorMin = Vector2.zero;
                vRt.anchorMax = Vector2.one;
                vRt.offsetMin = vRt.offsetMax = Vector2.zero;
                Text t = vGo.AddComponent<Text>();
                t.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize        = Mathf.RoundToInt(barH * 0.78f);  // 字体大小自适应血条高度
                t.fontStyle       = FontStyle.Bold;
                t.alignment       = TextAnchor.MiddleCenter;
                t.color           = new Color(1f, 1f, 1f, 0.95f);
                t.supportRichText = false;
                t.text            = "";
            }
        }

        // 创建 Filled 模式的血条子节点（兼容旧代码）
        private static Image MakeFilledBarRect(string name, Transform parent,
            Color color, float fillAmt = 1f, bool stretchFull = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localScale = Vector3.one;
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color      = color;
            if (!stretchFull)
            {
                img.type       = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.fillAmount = fillAmt;
            }
            return img;
        }

        // ── UI 辅助 ──────────────────────────────────────────────────────────────
        private static GameObject MakeText(string name, Transform parent,
            Vector2 anchoredPos, Vector2 size, string txt,
            int fontSize = 26, Color? color = null,
            TextAnchor align = TextAnchor.MiddleLeft,
            bool centered = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();

            if (centered)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
            }

            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text t   = obj.AddComponent<Text>();
            t.font   = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.alignment = align;
            t.color     = color ?? Color.white;
            t.text      = txt;
            return obj;
        }

        private static void MakeButton(string name, Transform parent, Vector2 pos, string label)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 50);
            obj.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.85f);
            obj.AddComponent<Button>();

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(obj.transform, false);
            RectTransform lr = lbl.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            Text t   = lbl.AddComponent<Text>();
            t.font   = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = 22;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = Color.white;
            t.text      = label;
        }

        // ════════════════════════════════════════════════════════════════════
        // ── 9. 创建主菜单场景 ─────────────────────────────────────────────
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/几何塔防/创建主菜单场景", false, 130)]
        public static void CreateMainMenuScene()
        {
            const string MAIN_MENU_SCENE = "MainMenu";
            const string GAME_SCENE      = "Game";

            // ── 确保 Scenes 目录存在 ───────────────────────────────────────
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            string menuPath = "Assets/Scenes/MainMenu.unity";

            // ── 若场景文件不存在，新建并保存 ──────────────────────────────
            if (!File.Exists(menuPath))
            {
                var newScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                // 设置背景色
                Camera cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.gameObject.tag      = "MainCamera";
                cam.orthographic        = true;
                cam.orthographicSize    = 5f;
                cam.backgroundColor     = new Color(0.06f, 0.06f, 0.1f);
                cam.clearFlags          = CameraClearFlags.SolidColor;
                cam.gameObject.AddComponent<AudioListener>();

                // EventSystem
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();

                // 主菜单控制器
                GameObject ctrl = new GameObject("MainMenuController");
                ctrl.AddComponent<MainMenuController>();

                EditorSceneManager.SaveScene(newScene, menuPath);
                EditorSceneManager.CloseScene(newScene, true);

                Debug.Log($"[SceneSetup] 主菜单场景已创建：{menuPath}");
            }
            else
            {
                Debug.Log($"[SceneSetup] 主菜单场景已存在：{menuPath}，跳过创建");
            }

            // ── 将两个场景加入 Build Settings ──────────────────────────────
            AddSceneToBuild(menuPath);

            // 按优先级查找游戏场景：SampleScene > Game（支持两种命名）
            string gameScenePath = null;
            foreach (string candidate in new[] { "SampleScene", "Game" })
            {
                string[] guids = AssetDatabase.FindAssets($"t:Scene {candidate}");
                if (guids.Length > 0)
                {
                    gameScenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    break;
                }
            }
            if (gameScenePath != null)
            {
                AddSceneToBuild(gameScenePath);
                Debug.Log($"[SceneSetup] 游戏场景已加入 Build Settings：{gameScenePath}");
            }
            else
                Debug.LogWarning("[SceneSetup] 未找到游戏场景（SampleScene/Game），请手动添加到 Build Settings");

            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] Build Settings 已更新，MainMenu 为索引 0（启动场景）");
        }

        private static void AddSceneToBuild(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == scenePath) return; // 已存在

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ════════════════════════════════════════════════════════════════════
        // ── 10. 创建 GameUI 预制体 ────────────────────────────────────────
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/几何塔防/创建游戏HUD预制体", false, 131)]
        public static void CreateGameUIPrefab()
        {
            string dir  = "Assets/prefab";
            string path = dir + "/GameUI.prefab";

            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "prefab");

            // ── Canvas 根节点 ─────────────────────────────────────────────
            GameObject cGo = new GameObject("GameUI");
            var canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var cs = cGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight  = 0.5f;
            cGo.AddComponent<GraphicRaycaster>();

            // ── 分数 / 波次 / 时间（左上角）───────────────────────────────
            MakeText("ScoreText",  cGo.transform, new Vector2(90, -30),  new Vector2(200, 36), "分数: 0",     22);
            MakeText("WaveText",   cGo.transform, new Vector2(90, -70),  new Vector2(200, 36), "波次: 1",     22);
            MakeText("TimeText",   cGo.transform, new Vector2(90, -110), new Vector2(200, 36), "时间: 00:00", 22);

            // ── 技能栏 + 能量栏 ────────────────────────────────────────────
            CreateSkillBarContainerOnParent(cGo.transform);

            // ── 游戏结束面板 ───────────────────────────────────────────────
            GameObject panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(cGo.transform, false);
            var pr = panel.AddComponent<RectTransform>();
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
            pr.offsetMin = pr.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            panel.SetActive(false);

            MakeText("GameOverTitle",  panel.transform, new Vector2(0, 80),  new Vector2(400, 60),
                     "游戏结束",    44, Color.red,   TextAnchor.MiddleCenter, true);
            MakeText("FinalScoreText", panel.transform, new Vector2(0, 15),  new Vector2(350, 46),
                     "最终分数: 0", 28, Color.white, TextAnchor.MiddleCenter, true);

            // 重新开始按钮
            MakeButton("RestartButton",   panel.transform, new Vector2(0, -55),  "重新开始");
            // 返回主菜单按钮
            MakeButton("MainMenuButton",  panel.transform, new Vector2(0, -115), "返回主菜单");

            // ── 挂载 GameUIController ──────────────────────────────────────
            cGo.AddComponent<GameUIController>();

            // ── 保存预制体 ────────────────────────────────────────────────
            bool success;
            if (Selection.activeGameObject == cGo) Selection.activeObject = null;
            var prefab = PrefabUtility.SaveAsPrefabAsset(cGo, path, out success);
            Object.DestroyImmediate(cGo);

            if (success)
            {
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[SceneSetup] GameUI 预制体已保存：{path}");
                Debug.Log("▶ 打开 Game 场景，将 GameUI.prefab 从 Project 拖入 Hierarchy 即可使用。");
            }
            else
                Debug.LogError("[SceneSetup] GameUI 预制体保存失败！");
        }

        // 独立版技能栏+能量栏容器（给预制体用，和 CreateSkillBarContainer 逻辑一致）
        private static void CreateSkillBarContainerOnParent(Transform canvasParent)
        {
            GameObject root = new GameObject("SkillBarRoot");
            root.transform.SetParent(canvasParent, false);

            Canvas barCanvas = root.AddComponent<Canvas>();
            barCanvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            barCanvas.sortingOrder    = 25;
            barCanvas.overrideSorting = true;

            CanvasScaler barCs = root.AddComponent<CanvasScaler>();
            barCs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            barCs.referenceResolution = new Vector2(1280, 720);
            barCs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            barCs.matchWidthOrHeight  = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin        = new Vector2(0.5f, 0f);
            rootRt.anchorMax        = new Vector2(0.5f, 0f);
            rootRt.pivot            = new Vector2(0.5f, 0f);
            rootRt.sizeDelta        = new Vector2(960f, 200f);
            rootRt.anchoredPosition = new Vector2(0f, 4f);

            // 技能栏
            GameObject bar = new GameObject("SkillBar");
            bar.transform.SetParent(root.transform, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin        = new Vector2(0.5f, 0f);
            barRt.anchorMax        = new Vector2(0.5f, 0f);
            barRt.pivot            = new Vector2(0.5f, 0f);
            barRt.sizeDelta        = new Vector2(960f, 160f);
            barRt.anchoredPosition = new Vector2(0f, 36f);
            bar.AddComponent<SkillBarUI>();

            // 能量栏
            CreateEnergyBarContainer(root.transform);
        }

        // ════════════════════════════════════════════════════════════════════
        // ── 11. 创建 GameManager 预制体 ───────────────────────────────────
        // ════════════════════════════════════════════════════════════════════
        [MenuItem("Tools/几何塔防/创建GameManager预制体", false, 132)]
        public static void CreateGameManagerPrefab()
        {
            string dir  = "Assets/prefab";
            string path = dir + "/GameManager.prefab";

            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "prefab");

            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();

            bool success;
            if (Selection.activeGameObject == go) Selection.activeObject = null;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out success);
            Object.DestroyImmediate(go);

            if (success)
            {
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[SceneSetup] GameManager 预制体已保存：{path}");
            }
            else
                Debug.LogError("[SceneSetup] GameManager 预制体保存失败！");
        }
    }
}
#endif
