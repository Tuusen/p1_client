#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace GeometryTD
{
    public class GameSetup : Editor
    {
        private static readonly string PrefabPath = "Assets/Game/Prefabs";
        private static readonly string ScenePath = "Assets/Game/Scenes";
        private static readonly string SpritePath = "Assets/Game/Sprites";
        private static readonly string ResourceSpritePath = "Assets/Game/Resources/Sprites";

        [MenuItem("Tools/游戏初始化 - 生成场景和Prefab")]
        public static void SetupGame()
        {
            if (!EditorUtility.DisplayDialog("游戏初始化",
                "将生成所有游戏场景和Prefab。\n已有文件将被覆盖。\n是否继续？",
                "确定", "取消"))
            {
                return;
            }

            EnsureDirectories();
            CreateSprites();
            AssetDatabase.Refresh();

            CreatePrefabs();
            AssetDatabase.Refresh();

            CreateMainMenuScene();
            CreateBattleScene();
            SetupBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成",
                "游戏初始化完成！\n\n" +
                "请从菜单 File > Build Settings 确认场景已添加。\n" +
                "然后双击 Assets/Game/Scenes/MainMenu 场景开始运行游戏。",
                "确定");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { PrefabPath, ScenePath, SpritePath, ResourceSpritePath };
            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        #region Sprite Creation

        private static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateDiamondTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size / 2f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = Mathf.Abs(x - half);
                    float dy = Mathf.Abs(y - half);
                    if (dx / half + dy / half <= 1f)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHexagonTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f * 0.9f;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 p = new Vector2(x - center.x, y - center.y);
                    float angle = Mathf.Atan2(p.y, p.x);
                    float r = p.magnitude;
                    float sector = Mathf.PI / 3f;
                    float a = Mathf.Abs(angle % sector);
                    if (a > sector / 2f) a = sector - a;
                    float hexR = radius * Mathf.Cos(Mathf.PI / 6f) / Mathf.Cos(a);
                    if (r <= hexR)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTriangleTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;
                    float left = 0.5f - ny * 0.5f;
                    float right = 0.5f + ny * 0.5f;
                    if (nx >= left && nx <= right && ny > 0.05f)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePentagonTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f * 0.85f;

            Vector2[] vertices = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI / 2f + i * 2f * Mathf.PI / 5f;
                vertices[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (IsPointInPolygon(new Vector2(x, y), vertices))
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        private static void SetTextureAsSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateSprites()
        {
            SaveTexture(CreatePentagonTexture(128, new Color(0.2f, 0.6f, 1f)), $"{SpritePath}/hero_shape.png");
            SaveTexture(CreateTriangleTexture(96, new Color(1f, 0.3f, 0.3f)), $"{SpritePath}/monster_shape.png");
            SaveTexture(CreateHexagonTexture(160, new Color(0.7f, 0.2f, 0.9f)), $"{SpritePath}/boss_shape.png");
            SaveTexture(CreateDiamondTexture(32, new Color(0f, 1f, 1f)), $"{SpritePath}/hero_bullet.png");
            SaveTexture(CreateDiamondTexture(32, new Color(1f, 0.2f, 0.2f)), $"{SpritePath}/boss_bullet.png");
            SaveTexture(CreateSolidTexture(4, 4, new Color(0.15f, 0.15f, 0.2f, 0.8f)), $"{SpritePath}/bar_bg.png");
            SaveTexture(CreateSolidTexture(4, 4, Color.white), $"{SpritePath}/bar_fill.png");

            AssetDatabase.Refresh();

            SetTextureAsSprite($"{SpritePath}/hero_shape.png");
            SetTextureAsSprite($"{SpritePath}/monster_shape.png");
            SetTextureAsSprite($"{SpritePath}/boss_shape.png");
            SetTextureAsSprite($"{SpritePath}/hero_bullet.png");
            SetTextureAsSprite($"{SpritePath}/boss_bullet.png");
            SetTextureAsSprite($"{SpritePath}/bar_bg.png");
            SetTextureAsSprite($"{SpritePath}/bar_fill.png");
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        #endregion

        #region Prefab Creation

        private static void CreatePrefabs()
        {
            CreateHeroPrefab();
            CreateMonsterPrefab();
            CreateBossPrefab();
            CreateBulletPrefab("HeroBullet", $"{SpritePath}/hero_bullet.png", new Color(0f, 1f, 1f));
            CreateBulletPrefab("BossBullet", $"{SpritePath}/boss_bullet.png", new Color(1f, 0.2f, 0.2f));
        }

        private static GameObject CreateHealthBarObject(string name, bool showText, Color fillColor, float width, float height)
        {
            Sprite barBg = LoadSprite($"{SpritePath}/bar_bg.png");
            Sprite barFill = LoadSprite($"{SpritePath}/bar_fill.png");

            GameObject barRoot = new GameObject(name);
            HealthBarUI healthBarUI = barRoot.AddComponent<HealthBarUI>();

            Canvas canvas = barRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            RectTransform canvasRT = barRoot.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(width * 100f, height * 100f);
            canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            CanvasScaler scaler = barRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(barRoot.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.anchorMin = Vector2.zero;
            sliderRT.anchorMax = Vector2.one;
            sliderRT.offsetMin = Vector2.zero;
            sliderRT.offsetMax = Vector2.zero;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = barBg;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = barFill;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;

            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;

            // Value Text
            Text valueText = null;
            if (showText)
            {
                GameObject textObj = new GameObject("ValueText");
                textObj.transform.SetParent(sliderObj.transform, false);
                valueText = textObj.AddComponent<Text>();
                valueText.text = "0/0";
                valueText.fontSize = 14;
                valueText.color = Color.white;
                valueText.alignment = TextAnchor.MiddleCenter;
                valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                valueText.verticalOverflow = VerticalWrapMode.Overflow;
                valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (valueText.font == null)
                    valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
            }

            // Wire up HealthBarUI
            SerializedObject so = new SerializedObject(healthBarUI);
            so.FindProperty("slider").objectReferenceValue = slider;
            so.FindProperty("showText").boolValue = showText;
            if (valueText != null)
                so.FindProperty("valueText").objectReferenceValue = valueText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return barRoot;
        }

        private static void CreateHeroPrefab()
        {
            Sprite heroSprite = LoadSprite($"{SpritePath}/hero_shape.png");

            GameObject hero = new GameObject("Hero");
            SpriteRenderer sr = hero.AddComponent<SpriteRenderer>();
            sr.sprite = heroSprite;
            sr.sortingOrder = 5;
            hero.AddComponent<HeroController>();

            // Shield Bar
            GameObject shieldBar = CreateHealthBarObject("ShieldBar", true, new Color(0.3f, 0.7f, 1f), 1.5f, 0.2f);
            shieldBar.transform.SetParent(hero.transform, false);
            shieldBar.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            // HP Bar
            GameObject hpBar = CreateHealthBarObject("HpBar", true, new Color(0.2f, 0.9f, 0.2f), 1.5f, 0.2f);
            hpBar.transform.SetParent(hero.transform, false);
            hpBar.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            // Wire HeroController bars
            HeroController hc = hero.GetComponent<HeroController>();
            SerializedObject so = new SerializedObject(hc);
            so.FindProperty("shieldBar").objectReferenceValue = shieldBar.GetComponent<HealthBarUI>();
            so.FindProperty("hpBar").objectReferenceValue = hpBar.GetComponent<HealthBarUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(hero, $"{PrefabPath}/Hero.prefab");
            DestroyImmediate(hero);
        }

        private static void CreateMonsterPrefab()
        {
            Sprite monsterSprite = LoadSprite($"{SpritePath}/monster_shape.png");

            GameObject monster = new GameObject("Monster");
            SpriteRenderer sr = monster.AddComponent<SpriteRenderer>();
            sr.sprite = monsterSprite;
            sr.sortingOrder = 5;
            monster.AddComponent<MonsterController>();

            // HP Bar (no text)
            GameObject hpBar = CreateHealthBarObject("HpBar", false, new Color(1f, 0.2f, 0.2f), 1.0f, 0.15f);
            hpBar.transform.SetParent(monster.transform, false);
            hpBar.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            MonsterController mc = monster.GetComponent<MonsterController>();
            SerializedObject so = new SerializedObject(mc);
            so.FindProperty("hpBar").objectReferenceValue = hpBar.GetComponent<HealthBarUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(monster, $"{PrefabPath}/Monster.prefab");
            DestroyImmediate(monster);
        }

        private static void CreateBossPrefab()
        {
            Sprite bossSprite = LoadSprite($"{SpritePath}/boss_shape.png");

            GameObject boss = new GameObject("Boss");
            SpriteRenderer sr = boss.AddComponent<SpriteRenderer>();
            sr.sprite = bossSprite;
            sr.sortingOrder = 5;
            boss.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            boss.AddComponent<BossController>();

            // HP Bar (with text)
            GameObject hpBar = CreateHealthBarObject("HpBar", true, new Color(0.7f, 0.2f, 0.9f), 2.0f, 0.2f);
            hpBar.transform.SetParent(boss.transform, false);
            hpBar.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            BossController bc = boss.GetComponent<BossController>();
            SerializedObject so = new SerializedObject(bc);
            so.FindProperty("hpBar").objectReferenceValue = hpBar.GetComponent<HealthBarUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(boss, $"{PrefabPath}/Boss.prefab");
            DestroyImmediate(boss);
        }

        private static void CreateBulletPrefab(string name, string spritePath, Color trailColor)
        {
            Sprite bulletSprite = LoadSprite(spritePath);

            GameObject bullet = new GameObject(name);
            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = bulletSprite;
            sr.sortingOrder = 8;
            bullet.AddComponent<BulletController>();

            // Trail Renderer
            TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            trail.sortingOrder = 7;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.minVertexDistance = 0.05f;

            PrefabUtility.SaveAsPrefabAsset(bullet, $"{PrefabPath}/{name}.prefab");
            DestroyImmediate(bullet);
        }

        #endregion

        #region Scene Creation

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static Text CreateUIText(GameObject parent, string name, string content,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter,
            FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.fontStyle = fontStyle;
            text.font = GetDefaultFont();
            return text;
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // ConfigManager
            GameObject cmObj = new GameObject("ConfigManager");
            cmObj.AddComponent<ConfigManager>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.15f, 1f);
            bgImage.raycastTarget = false;

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Background_Images/large/background-1-large.png");
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.color = Color.white;
            }

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Title Text
            Text titleText = CreateUIText(canvasObj, "TitleText", "几何塔防",
                72, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform titleRT = titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0, 150);
            titleRT.sizeDelta = new Vector2(600, 100);

            // Subtitle
            Text subtitleText = CreateUIText(canvasObj, "SubtitleText", "守卫星际家园",
                28, new Color(0.7f, 0.7f, 0.85f));
            RectTransform subtitleRT = subtitleText.GetComponent<RectTransform>();
            subtitleRT.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRT.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRT.anchoredPosition = new Vector2(0, 70);
            subtitleRT.sizeDelta = new Vector2(400, 50);

            // Start Button
            GameObject btnObj = new GameObject("StartButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.5f, 1f);

            Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-large.png");
            if (btnSprite != null)
            {
                btnImage.sprite = btnSprite;
                btnImage.type = Image.Type.Sliced;
                btnImage.color = Color.white;
            }

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.9f, 0.9f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.9f);
            btn.colors = cb;

            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = new Vector2(0, -50);
            btnRT.sizeDelta = new Vector2(300, 80);

            // Button Text
            Text btnText = CreateUIText(btnObj, "Text", "开始游戏", 36, Color.white);
            RectTransform btnTextRT = btnText.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;

            // Wire up button click
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                btn.onClick, menuUI.OnStartButtonClicked);

            EditorSceneManager.SaveScene(scene, $"{ScenePath}/MainMenu.unity");
        }

        private static void CreateBattleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Hero Spawn Point
            GameObject heroSpawnPoint = new GameObject("HeroSpawnPoint");
            heroSpawnPoint.transform.position = new Vector3(-6f, 0f, 0f);

            // BattleManager
            GameObject bmObj = new GameObject("BattleManager");
            BattleManager bm = bmObj.AddComponent<BattleManager>();
            bmObj.AddComponent<MonsterSpawner>();

            // Load prefabs
            GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Hero.prefab");
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Monster.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Boss.prefab");
            GameObject heroBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/HeroBullet.prefab");
            GameObject bossBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/BossBullet.prefab");

            // --- Canvas ---
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvasComp = canvasObj.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComp.sortingOrder = 100;

            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            BattleUI battleUI = canvasObj.AddComponent<BattleUI>();

            // --- Background (世界空间SpriteRenderer，不遮挡游戏元素) ---
            GameObject bgObj = new GameObject("Background");
            SpriteRenderer bgSR = bgObj.AddComponent<SpriteRenderer>();
            bgSR.sortingOrder = -100;

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Background_Images/large/background-2-large.png");
            if (bgSprite != null)
            {
                bgSR.sprite = bgSprite;
                // 缩放背景使其覆盖摄像机可视区域 (orthographicSize=5, aspect~16:9)
                float spriteW = bgSprite.bounds.size.x;
                float spriteH = bgSprite.bounds.size.y;
                float camH = 5f * 2f;
                float camW = camH * 16f / 9f;
                float scaleX = camW / spriteW;
                float scaleY = camH / spriteH;
                float scale = Mathf.Max(scaleX, scaleY);
                bgObj.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                bgSR.color = new Color(0.03f, 0.03f, 0.1f);
            }
            bgObj.transform.position = new Vector3(0, 0, 1f);

            // ========== Progress Panel (top) ==========
            GameObject progressPanel = new GameObject("ProgressPanel");
            progressPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform progressPanelRT = progressPanel.AddComponent<RectTransform>();
            progressPanelRT.anchorMin = new Vector2(0.2f, 1f);
            progressPanelRT.anchorMax = new Vector2(0.8f, 1f);
            progressPanelRT.pivot = new Vector2(0.5f, 1f);
            progressPanelRT.anchoredPosition = new Vector2(0, -20);
            progressPanelRT.sizeDelta = new Vector2(0, 60);

            // Progress Slider
            CreateUISlider(progressPanel, "ProgressSlider",
                new Color(0.15f, 0.15f, 0.2f, 0.8f),
                new Color(0.2f, 0.8f, 0.2f),
                out Slider progressSlider);

            // Progress Text
            Text progressText = CreateUIText(progressPanel, "ProgressText",
                "击杀进度: 0/100", 24, Color.white);
            progressText.raycastTarget = false;
            RectTransform progressTextRT = progressText.GetComponent<RectTransform>();
            progressTextRT.anchorMin = Vector2.zero;
            progressTextRT.anchorMax = Vector2.one;
            progressTextRT.offsetMin = Vector2.zero;
            progressTextRT.offsetMax = Vector2.zero;

            // ========== Result Panel (center, initially hidden) ==========
            GameObject resultPanel = new GameObject("ResultPanel");
            resultPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform resultPanelRT = resultPanel.AddComponent<RectTransform>();
            resultPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
            resultPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            resultPanelRT.anchoredPosition = Vector2.zero;
            resultPanelRT.sizeDelta = new Vector2(500, 350);

            // Result Background
            Image resultBg = resultPanel.AddComponent<Image>();
            resultBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

            Sprite containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Containers/Large/victory-defeat-container-large.png");
            if (containerSprite != null)
            {
                resultBg.sprite = containerSprite;
                resultBg.type = Image.Type.Sliced;
                resultBg.color = Color.white;
            }

            // Result Title
            Text resultTitle = CreateUIText(resultPanel, "ResultTitle", "胜 利",
                56, new Color(1f, 0.84f, 0f), TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform resultTitleRT = resultTitle.GetComponent<RectTransform>();
            resultTitleRT.anchorMin = new Vector2(0.5f, 0.5f);
            resultTitleRT.anchorMax = new Vector2(0.5f, 0.5f);
            resultTitleRT.anchoredPosition = new Vector2(0, 60);
            resultTitleRT.sizeDelta = new Vector2(400, 80);

            // Back Button
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(resultPanel.transform, false);
            Image backBtnImg = backBtnObj.AddComponent<Image>();
            backBtnImg.color = new Color(0.2f, 0.5f, 1f);

            Sprite backBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-large.png");
            if (backBtnSprite != null)
            {
                backBtnImg.sprite = backBtnSprite;
                backBtnImg.type = Image.Type.Sliced;
                backBtnImg.color = Color.white;
            }

            Button backBtn = backBtnObj.AddComponent<Button>();
            ColorBlock backCb = backBtn.colors;
            backCb.normalColor = Color.white;
            backCb.highlightedColor = new Color(0.9f, 0.9f, 1f);
            backCb.pressedColor = new Color(0.7f, 0.7f, 0.9f);
            backBtn.colors = backCb;

            RectTransform backBtnRT = backBtnObj.GetComponent<RectTransform>();
            backBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
            backBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
            backBtnRT.anchoredPosition = new Vector2(0, -60);
            backBtnRT.sizeDelta = new Vector2(260, 65);

            // Back Button Text
            Text backBtnText = CreateUIText(backBtnObj, "Text", "返回主界面",
                28, Color.white);
            RectTransform backBtnTextRT = backBtnText.GetComponent<RectTransform>();
            backBtnTextRT.anchorMin = Vector2.zero;
            backBtnTextRT.anchorMax = Vector2.one;
            backBtnTextRT.offsetMin = Vector2.zero;
            backBtnTextRT.offsetMax = Vector2.zero;

            resultPanel.SetActive(false);

            // ========== FloatingTextUI ==========
            FloatingTextUI floatingTextUI = bmObj.AddComponent<FloatingTextUI>();

            // ========== Skill Bar (bottom) ==========
            // 读取配置获取技能槽ID和图标
            string gameConfigJson = File.ReadAllText("Assets/Game/Resources/Configs/game_config.json");
            GameConfig gameConfigData = JsonUtility.FromJson<GameConfig>(gameConfigJson);
            string skillConfigJson = File.ReadAllText("Assets/Game/Resources/Configs/skill_config.json");
            SkillConfigList skillConfigList = JsonUtility.FromJson<SkillConfigList>(skillConfigJson);

            // 构建技能ID->图标名映射 (取level 0的配置)
            Dictionary<int, string> skillIconMap = new Dictionary<int, string>();
            Dictionary<int, string> skillNameMap = new Dictionary<int, string>();
            foreach (var sc in skillConfigList.skills)
            {
                if (sc.level == 0 && !string.IsNullOrEmpty(sc.icon))
                {
                    skillIconMap[sc.id] = sc.icon;
                    skillNameMap[sc.id] = sc.name;
                }
            }

            int slotCount = gameConfigData.skill_slot_ids != null ? gameConfigData.skill_slot_ids.Length : 0;

            GameObject skillBarPanel = new GameObject("SkillBarPanel");
            skillBarPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform skillBarPanelRT = skillBarPanel.AddComponent<RectTransform>();
            skillBarPanelRT.anchorMin = new Vector2(0.5f, 0f);
            skillBarPanelRT.anchorMax = new Vector2(0.5f, 0f);
            skillBarPanelRT.pivot = new Vector2(0.5f, 0f);
            skillBarPanelRT.anchoredPosition = new Vector2(0, 8);
            float slotWidth = 120f;
            float slotSpacing = 6f;
            float barWidth = slotCount * slotWidth + (slotCount - 1) * slotSpacing + 24f;
            float barHeight = 155f;
            skillBarPanelRT.sizeDelta = new Vector2(barWidth, barHeight);

            // 技能栏背景
            Image skillBarBg = skillBarPanel.AddComponent<Image>();
            skillBarBg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);
            skillBarBg.raycastTarget = false;

            SkillBarUI skillBarUI = skillBarPanel.AddComponent<SkillBarUI>();

            Sprite barBgSprite = LoadSprite($"{SpritePath}/bar_bg.png");
            Sprite barFillSprite = LoadSprite($"{SpritePath}/bar_fill.png");

            SkillSlotUI[] skillSlotUIs = new SkillSlotUI[slotCount];

            for (int s = 0; s < slotCount; s++)
            {
                int skillId = gameConfigData.skill_slot_ids[s];
                string iconName = skillIconMap.ContainsKey(skillId) ? skillIconMap[skillId] : "";
                string skillName = skillNameMap.ContainsKey(skillId) ? skillNameMap[skillId] : "";

                // 技能槽根对象
                GameObject slotObj = new GameObject($"SkillSlot_{s}");
                slotObj.transform.SetParent(skillBarPanel.transform, false);
                RectTransform slotRT = slotObj.AddComponent<RectTransform>();
                float slotX = -barWidth / 2f + 12f + s * (slotWidth + slotSpacing) + slotWidth / 2f;
                slotRT.anchorMin = new Vector2(0.5f, 0.5f);
                slotRT.anchorMax = new Vector2(0.5f, 0.5f);
                slotRT.anchoredPosition = new Vector2(slotX, 0);
                slotRT.sizeDelta = new Vector2(slotWidth, barHeight - 16f);

                SkillSlotUI slotUI = slotObj.AddComponent<SkillSlotUI>();

                // 技能槽背景框
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.08f, 0.08f, 0.2f, 0.9f);

                // 按钮组件
                Button slotBtn = slotObj.AddComponent<Button>();
                ColorBlock btnColors = slotBtn.colors;
                btnColors.normalColor = Color.white;
                btnColors.highlightedColor = new Color(0.85f, 0.9f, 1f);
                btnColors.pressedColor = new Color(0.6f, 0.7f, 1f);
                btnColors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
                slotBtn.colors = btnColors;
                slotBtn.targetGraphic = slotBg;

                // --- 技能名称（顶部） ---
                Text nameText = CreateUIText(slotObj, "NameText", skillName, 13,
                    new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
                nameText.raycastTarget = false;
                RectTransform nameRT = nameText.GetComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0f, 1f);
                nameRT.anchorMax = new Vector2(1f, 1f);
                nameRT.pivot = new Vector2(0.5f, 1f);
                nameRT.anchoredPosition = new Vector2(0, -2);
                nameRT.sizeDelta = new Vector2(0, 20);

                // --- 图标背景 ---
                GameObject iconBgObj = new GameObject("IconBg");
                iconBgObj.transform.SetParent(slotObj.transform, false);
                Image iconBgImg = iconBgObj.AddComponent<Image>();
                iconBgImg.color = new Color(0.06f, 0.06f, 0.15f, 1f);
                iconBgImg.raycastTarget = false;
                RectTransform iconBgRT = iconBgObj.GetComponent<RectTransform>();
                iconBgRT.anchorMin = new Vector2(0.5f, 1f);
                iconBgRT.anchorMax = new Vector2(0.5f, 1f);
                iconBgRT.pivot = new Vector2(0.5f, 1f);
                iconBgRT.anchoredPosition = new Vector2(0, -22);
                iconBgRT.sizeDelta = new Vector2(72, 72);

                // 图标边框
                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(iconBgObj.transform, false);
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(0.3f, 0.4f, 0.7f, 0.8f);
                borderImg.raycastTarget = false;
                RectTransform borderRT = borderObj.GetComponent<RectTransform>();
                borderRT.anchorMin = Vector2.zero;
                borderRT.anchorMax = Vector2.one;
                borderRT.offsetMin = new Vector2(-2, -2);
                borderRT.offsetMax = new Vector2(2, 2);
                borderObj.transform.SetAsFirstSibling();

                // 图标
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(iconBgObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.raycastTarget = false;
                iconImg.color = Color.white;

                if (!string.IsNullOrEmpty(iconName))
                {
                    Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        $"Assets/ui/Space_Exploration_GUI_Kit/Picto_Icons/White/{iconName}.png");
                    if (iconSprite != null)
                        iconImg.sprite = iconSprite;
                }

                RectTransform iconRT = iconObj.GetComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(6, 6);
                iconRT.offsetMax = new Vector2(-6, -6);

                // 冷却遮罩
                GameObject cdObj = new GameObject("CooldownOverlay");
                cdObj.transform.SetParent(iconBgObj.transform, false);
                Image cdImg = cdObj.AddComponent<Image>();
                cdImg.color = new Color(0, 0, 0, 0.7f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = 2; // Top
                cdImg.fillClockwise = true;
                cdImg.fillAmount = 0f;
                cdImg.raycastTarget = false;
                RectTransform cdRT = cdObj.GetComponent<RectTransform>();
                cdRT.anchorMin = Vector2.zero;
                cdRT.anchorMax = Vector2.one;
                cdRT.offsetMin = Vector2.zero;
                cdRT.offsetMax = Vector2.zero;
                cdObj.SetActive(false);

                // 冷却时间文本（覆盖在图标中央）
                Text cdTimeText = CreateUIText(iconBgObj, "CooldownText", "", 18,
                    Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                cdTimeText.raycastTarget = false;
                Outline cdOutline = cdTimeText.gameObject.AddComponent<Outline>();
                cdOutline.effectColor = new Color(0, 0, 0, 0.8f);
                cdOutline.effectDistance = new Vector2(1, -1);
                RectTransform cdTimeRT = cdTimeText.GetComponent<RectTransform>();
                cdTimeRT.anchorMin = Vector2.zero;
                cdTimeRT.anchorMax = Vector2.one;
                cdTimeRT.offsetMin = Vector2.zero;
                cdTimeRT.offsetMax = Vector2.zero;
                cdTimeText.gameObject.SetActive(false);

                // --- 等级文本 ---
                Text lvText = CreateUIText(slotObj, "LevelText", "Lv.0", 14,
                    new Color(0.9f, 0.9f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Normal);
                lvText.raycastTarget = false;
                RectTransform lvRT = lvText.GetComponent<RectTransform>();
                lvRT.anchorMin = new Vector2(0f, 0f);
                lvRT.anchorMax = new Vector2(1f, 0f);
                lvRT.pivot = new Vector2(0.5f, 0f);
                lvRT.anchoredPosition = new Vector2(0, 16);
                lvRT.sizeDelta = new Vector2(0, 18);

                // --- 经验条 ---
                GameObject xpSliderObj = new GameObject("XpSlider");
                xpSliderObj.transform.SetParent(slotObj.transform, false);
                Slider xpSlider = xpSliderObj.AddComponent<Slider>();
                xpSlider.interactable = false;
                xpSlider.transition = Selectable.Transition.None;
                xpSlider.minValue = 0;
                xpSlider.maxValue = 10;
                xpSlider.value = 0;

                RectTransform xpSliderRT = xpSliderObj.GetComponent<RectTransform>();
                xpSliderRT.anchorMin = new Vector2(0f, 0f);
                xpSliderRT.anchorMax = new Vector2(1f, 0f);
                xpSliderRT.pivot = new Vector2(0.5f, 0f);
                xpSliderRT.anchoredPosition = new Vector2(0, 2);
                xpSliderRT.sizeDelta = new Vector2(-16, 12);

                // XP条背景
                GameObject xpBgObj = new GameObject("Background");
                xpBgObj.transform.SetParent(xpSliderObj.transform, false);
                Image xpBgImg = xpBgObj.AddComponent<Image>();
                if (barBgSprite != null) xpBgImg.sprite = barBgSprite;
                xpBgImg.type = Image.Type.Sliced;
                xpBgImg.color = new Color(0.1f, 0.1f, 0.18f, 0.9f);
                RectTransform xpBgRT = xpBgObj.GetComponent<RectTransform>();
                xpBgRT.anchorMin = Vector2.zero;
                xpBgRT.anchorMax = Vector2.one;
                xpBgRT.offsetMin = Vector2.zero;
                xpBgRT.offsetMax = Vector2.zero;

                // XP条填充
                GameObject xpFillArea = new GameObject("Fill Area");
                xpFillArea.transform.SetParent(xpSliderObj.transform, false);
                RectTransform xpFillAreaRT = xpFillArea.AddComponent<RectTransform>();
                xpFillAreaRT.anchorMin = Vector2.zero;
                xpFillAreaRT.anchorMax = Vector2.one;
                xpFillAreaRT.offsetMin = Vector2.zero;
                xpFillAreaRT.offsetMax = Vector2.zero;

                GameObject xpFillObj = new GameObject("Fill");
                xpFillObj.transform.SetParent(xpFillArea.transform, false);
                Image xpFillImg = xpFillObj.AddComponent<Image>();
                if (barFillSprite != null) xpFillImg.sprite = barFillSprite;
                xpFillImg.type = Image.Type.Sliced;
                xpFillImg.color = new Color(0.2f, 0.7f, 1f);
                RectTransform xpFillRT = xpFillObj.GetComponent<RectTransform>();
                xpFillRT.anchorMin = Vector2.zero;
                xpFillRT.anchorMax = new Vector2(0, 1);
                xpFillRT.offsetMin = Vector2.zero;
                xpFillRT.offsetMax = Vector2.zero;

                xpSlider.fillRect = xpFillRT;

                // Wire SkillSlotUI
                SerializedObject slotSO = new SerializedObject(slotUI);
                slotSO.FindProperty("iconImage").objectReferenceValue = iconImg;
                slotSO.FindProperty("levelText").objectReferenceValue = lvText;
                slotSO.FindProperty("nameText").objectReferenceValue = nameText;
                slotSO.FindProperty("cooldownText").objectReferenceValue = cdTimeText;
                slotSO.FindProperty("xpSlider").objectReferenceValue = xpSlider;
                slotSO.FindProperty("cooldownOverlay").objectReferenceValue = cdImg;
                slotSO.FindProperty("slotButton").objectReferenceValue = slotBtn;
                slotSO.ApplyModifiedPropertiesWithoutUndo();

                skillSlotUIs[s] = slotUI;
            }

            // Wire SkillBarUI
            SerializedObject skillBarSO = new SerializedObject(skillBarUI);
            SerializedProperty slotsProperty = skillBarSO.FindProperty("slots");
            slotsProperty.arraySize = slotCount;
            for (int s = 0; s < slotCount; s++)
            {
                slotsProperty.GetArrayElementAtIndex(s).objectReferenceValue = skillSlotUIs[s];
            }
            skillBarSO.ApplyModifiedPropertiesWithoutUndo();

            // ========== Wire up BattleUI ==========
            SerializedObject battleUISO = new SerializedObject(battleUI);
            battleUISO.FindProperty("progressSlider").objectReferenceValue = progressSlider;
            battleUISO.FindProperty("progressText").objectReferenceValue = progressText;
            battleUISO.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            battleUISO.FindProperty("resultTitleText").objectReferenceValue = resultTitle;
            battleUISO.FindProperty("backButton").objectReferenceValue = backBtn;
            battleUISO.ApplyModifiedPropertiesWithoutUndo();

            // ========== Wire up BattleManager ==========
            SerializedObject bmSO = new SerializedObject(bm);
            bmSO.FindProperty("heroPrefab").objectReferenceValue = heroPrefab;
            bmSO.FindProperty("monsterPrefab").objectReferenceValue = monsterPrefab;
            bmSO.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
            bmSO.FindProperty("heroBulletPrefab").objectReferenceValue = heroBulletPrefab;
            bmSO.FindProperty("bossBulletPrefab").objectReferenceValue = bossBulletPrefab;
            bmSO.FindProperty("battleUI").objectReferenceValue = battleUI;
            bmSO.FindProperty("skillBarUI").objectReferenceValue = skillBarUI;
            bmSO.FindProperty("floatingTextUI").objectReferenceValue = floatingTextUI;
            bmSO.FindProperty("heroSpawnPoint").objectReferenceValue = heroSpawnPoint.transform;
            bmSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenePath}/Battle.unity");
        }

        private static void CreateUISlider(GameObject parent, string name,
            Color bgColor, Color fillColor, out Slider slider)
        {
            Sprite barBg = LoadSprite($"{SpritePath}/bar_bg.png");
            Sprite barFill = LoadSprite($"{SpritePath}/bar_fill.png");

            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent.transform, false);
            slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 0;

            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.anchorMin = Vector2.zero;
            sliderRT.anchorMax = Vector2.one;
            sliderRT.offsetMin = new Vector2(10, 10);
            sliderRT.offsetMax = new Vector2(-10, -10);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = barBg;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = bgColor;

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = barFill;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;

            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            slider.fillRect = fillRT;
        }

        #endregion

        #region Build Settings

        private static void SetupBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene($"{ScenePath}/MainMenu.unity", true));
            scenes.Add(new EditorBuildSettingsScene($"{ScenePath}/Battle.unity", true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion
    }
}
#endif
