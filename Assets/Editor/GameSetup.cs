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
        private static readonly string PrefabPath = "Assets/Prefabs";
        private static readonly string ScenePath = "Assets/Scenes";
        private static readonly string SpritePath = "Assets/Sprites";
        private static readonly string ResourceSpritePath = "Assets/Resources/Sprites";
        private static readonly string ResourceBulletsPath = "Assets/Resources/Bullets";
        private static readonly string ResourceEffectsPath = "Assets/Resources/Effects";
        private static readonly string CharacterPath = "Assets/Characters";

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
                "然后双击 Assets/Scenes/MainMenu 场景开始运行游戏。",
                "确定");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { PrefabPath, ScenePath, SpritePath, ResourceSpritePath, ResourceBulletsPath, ResourceEffectsPath };
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

        private static Texture2D CreateCircleBulletTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateArrowBulletTexture(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size / 2f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;
                    float distFromCenter = Mathf.Abs(ny - 0.5f);
                    float maxDist = 0.5f * (1f - nx);
                    bool inArrow = nx < 0.85f && distFromCenter <= maxDist;
                    tex.SetPixel(x, y, inArrow ? color : Color.clear);
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

            // 子弹形状（白色，运行时着色）保存到 Resources 以便运行时加载
            SaveTexture(CreateDiamondTexture(32, Color.white), $"{ResourceSpritePath}/bullet_diamond.png");
            SaveTexture(CreateCircleBulletTexture(32, Color.white), $"{ResourceSpritePath}/bullet_circle.png");
            SaveTexture(CreateArrowBulletTexture(32, Color.white), $"{ResourceSpritePath}/bullet_arrow.png");
            SaveTexture(CreateCircleBulletTexture(128, Color.white), $"{ResourceSpritePath}/range_circle.png");

            AssetDatabase.Refresh();

            SetTextureAsSprite($"{SpritePath}/hero_shape.png");
            SetTextureAsSprite($"{SpritePath}/monster_shape.png");
            SetTextureAsSprite($"{SpritePath}/boss_shape.png");
            SetTextureAsSprite($"{SpritePath}/hero_bullet.png");
            SetTextureAsSprite($"{SpritePath}/boss_bullet.png");
            SetTextureAsSprite($"{SpritePath}/bar_bg.png");
            SetTextureAsSprite($"{SpritePath}/bar_fill.png");
            SetTextureAsSprite($"{ResourceSpritePath}/bullet_diamond.png");
            SetTextureAsSprite($"{ResourceSpritePath}/bullet_circle.png");
            SetTextureAsSprite($"{ResourceSpritePath}/bullet_arrow.png");
            SetTextureAsSprite($"{ResourceSpritePath}/range_circle.png");
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        #endregion

        #region Prefab Creation

        private static void CreatePrefabs()
        {
            CreateSummonPrefab();
            CreateStyleBulletPrefabs();
            CreateEffectPrefabs();
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
                valueText.font = GetDefaultFont();

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
            GameObject hero = new GameObject("Hero");
            hero.AddComponent<HeroController>();
            CharacterFacing facing = hero.AddComponent<CharacterFacing>();

            // Load artist prefab as Visual child (contains Animator + bone sprites)
            GameObject heroPrefabSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPath}/Hero/sword_man.prefab");
            if (heroPrefabSrc != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefabSrc);
                visual.name = "Visual";
                visual.transform.SetParent(hero.transform, false);
                visual.transform.position = Vector3.zero;

                // Wire CharacterFacing.visualRoot
                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                // Fallback: geometric shape
                Sprite heroSprite = LoadSprite($"{SpritePath}/hero_shape.png");
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(hero.transform, false);
                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = heroSprite;
                sr.sortingOrder = 5;

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Shield Bar (stays on root to avoid flip)
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
            GameObject monster = new GameObject("Monster");
            monster.AddComponent<MonsterController>();
            CharacterFacing facing = monster.AddComponent<CharacterFacing>();

            // Load artist prefab as Visual child
            GameObject monsterPrefabSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPath}/Monster/goblin.prefab");
            if (monsterPrefabSrc != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefabSrc);
                visual.name = "Visual";
                visual.transform.position = Vector3.zero;
                visual.transform.SetParent(monster.transform, false);

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Sprite monsterSprite = LoadSprite($"{SpritePath}/monster_shape.png");
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(monster.transform, false);
                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = monsterSprite;
                sr.sortingOrder = 5;

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // HP Bar (no text, stays on root)
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
            GameObject boss = new GameObject("Boss");
            boss.AddComponent<BossController>();
            CharacterFacing facing = boss.AddComponent<CharacterFacing>();

            // Load artist prefab as Visual child
            GameObject bossPrefabSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPath}/Boss/skull_warrior.prefab");
            if (bossPrefabSrc != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefabSrc);
                visual.name = "Visual";
                visual.transform.SetParent(boss.transform, false);
                visual.transform.position = Vector3.zero;
                visual.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Sprite bossSprite = LoadSprite($"{SpritePath}/boss_shape.png");
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(boss.transform, false);
                visual.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = bossSprite;
                sr.sortingOrder = 5;

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // HP Bar (with text, stays on root)
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

        private static void CreateSummonPrefab()
        {
            GameObject summon = new GameObject("Summon");
            summon.AddComponent<SummonController>();
            CharacterFacing facing = summon.AddComponent<CharacterFacing>();

            // Load artist prefab as Visual child
            GameObject summonPrefabSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterPath}/Summon/green_slime.prefab");
            if (summonPrefabSrc != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(summonPrefabSrc);
                visual.name = "Visual";
                visual.transform.position = Vector3.zero;
                visual.transform.SetParent(summon.transform, false);

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Sprite summonSprite = LoadSprite($"{SpritePath}/summon_shape.png");
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(summon.transform, false);
                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = summonSprite;
                sr.sortingOrder = 5;

                SerializedObject facingSO = new SerializedObject(facing);
                facingSO.FindProperty("visualRoot").objectReferenceValue = visual.transform;
                facingSO.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(summon, $"{PrefabPath}/Summon.prefab");
            DestroyImmediate(summon);
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

        // ===== Bullet Style Prefab Generation =====

        private struct BulletStyleData
        {
            public int id;
            public string shape;
            public float size;
            public Color color;
            public Color trailColor;
            public float trailWidth;
            public float trailTime;
        }

        private static void CreateStyleBulletPrefabs()
        {
            BulletStyleData[] styles = new BulletStyleData[]
            {
                new BulletStyleData { id = 1,   shape = "diamond", size = 1.0f,
                    color = new Color(0f, 1f, 1f), trailColor = new Color(0f, 1f, 1f),
                    trailWidth = 0.15f, trailTime = 0.3f },
                new BulletStyleData { id = 101, shape = "circle",  size = 1.2f,
                    color = new Color(1f, 0.5f, 0f), trailColor = new Color(1f, 0.6f, 0f),
                    trailWidth = 0.2f, trailTime = 0.4f },
                new BulletStyleData { id = 102, shape = "diamond", size = 1.0f,
                    color = new Color(0.3f, 0.7f, 1f), trailColor = new Color(0.5f, 0.8f, 1f),
                    trailWidth = 0.15f, trailTime = 0.3f },
                new BulletStyleData { id = 103, shape = "arrow",   size = 1.0f,
                    color = new Color(0.6f, 0.3f, 1f), trailColor = new Color(0.7f, 0.4f, 1f),
                    trailWidth = 0.18f, trailTime = 0.35f },
                new BulletStyleData { id = 104, shape = "circle",  size = 0.8f,
                    color = new Color(0.2f, 1f, 0.4f), trailColor = new Color(0.3f, 1f, 0.5f),
                    trailWidth = 0.12f, trailTime = 0.25f },
                new BulletStyleData { id = 201, shape = "diamond", size = 1.0f,
                    color = new Color(1f, 0.2f, 0.2f), trailColor = new Color(1f, 0.2f, 0.2f),
                    trailWidth = 0.15f, trailTime = 0.3f },
            };

            foreach (var style in styles)
            {
                CreateStyledBulletPrefab(style);
            }
        }

        private static void CreateStyledBulletPrefab(BulletStyleData style)
        {
            Sprite bulletSprite = LoadSprite($"{ResourceSpritePath}/bullet_{style.shape}.png");

            GameObject bullet = new GameObject($"Bullet_Style{style.id}");
            bullet.transform.localScale = Vector3.one * style.size;

            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = bulletSprite;
            sr.color = style.color;
            sr.sortingOrder = 8;

            bullet.AddComponent<BulletController>();

            TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
            trail.time = style.trailTime;
            trail.startWidth = style.trailWidth;
            trail.endWidth = style.trailWidth * 0.13f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = style.trailColor;
            trail.endColor = new Color(style.trailColor.r, style.trailColor.g, style.trailColor.b, 0f);
            trail.sortingOrder = 7;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.minVertexDistance = 0.05f;

            PrefabUtility.SaveAsPrefabAsset(bullet, $"{ResourceBulletsPath}/Bullet_Style{style.id}.prefab");
            DestroyImmediate(bullet);
        }

        // ===== Event Effect Prefab Generation =====

        private struct EffectData
        {
            public int eventType;
            public string name;
            public Color color;
            public string shape;    // "circle" or "ring"
            public float size;
            public float duration;
            public bool isInstant;  // true = EffectBurstAnim, false = EffectFadeAnim
        }

        private static void CreateEffectPrefabs()
        {
            EffectData[] effects = new EffectData[]
            {
                new EffectData { eventType = 1,  name = "Pierce",          color = new Color(1f, 0.9f, 0.3f, 0.6f),   shape = "ring",   size = 0.8f,  duration = 0.3f, isInstant = true },
                new EffectData { eventType = 2,  name = "Explosion",       color = new Color(1f, 0.5f, 0.1f, 0.7f),   shape = "circle", size = 2.0f,  duration = 0.5f, isInstant = true },
                new EffectData { eventType = 3,  name = "Freeze",          color = new Color(0.3f, 0.7f, 1f, 0.6f),   shape = "circle", size = 1.0f,  duration = 0.8f, isInstant = false },
                new EffectData { eventType = 4,  name = "Burn",            color = new Color(1f, 0.4f, 0.1f, 0.5f),   shape = "ring",   size = 0.6f,  duration = 0.6f, isInstant = false },
                new EffectData { eventType = 7,  name = "Slow",            color = new Color(0.5f, 0.5f, 1f, 0.4f),   shape = "circle", size = 0.8f,  duration = 0.5f, isInstant = false },
                new EffectData { eventType = 8,  name = "Heal",            color = new Color(0.2f, 0.9f, 0.3f, 0.6f), shape = "circle", size = 1.5f,  duration = 0.5f, isInstant = true },
                new EffectData { eventType = 9,  name = "HealOverTime",    color = new Color(0.3f, 1f, 0.5f, 0.4f),   shape = "ring",   size = 1.2f,  duration = 1.0f, isInstant = false },
                new EffectData { eventType = 10, name = "DamageReduction", color = new Color(0.8f, 0.8f, 0.2f, 0.5f), shape = "ring",   size = 1.5f,  duration = 0.6f, isInstant = true },
                new EffectData { eventType = 13, name = "Shield",          color = new Color(0.3f, 0.7f, 1f, 0.5f),   shape = "ring",   size = 2.0f,  duration = 0.6f, isInstant = true },
                new EffectData { eventType = 14, name = "Retaliation",     color = new Color(0.9f, 0.9f, 0.2f, 0.6f), shape = "ring",   size = 1.0f,  duration = 0.3f, isInstant = true },
                new EffectData { eventType = 15, name = "Knockback",       color = new Color(0.6f, 0.9f, 0.6f, 0.5f), shape = "circle", size = 3.0f,  duration = 0.4f, isInstant = true },
                new EffectData { eventType = 16, name = "Vulnerability",   color = new Color(0.9f, 0.3f, 0.9f, 0.5f), shape = "ring",   size = 0.8f,  duration = 0.8f, isInstant = false },
                new EffectData { eventType = 17, name = "Summon",          color = new Color(0.9f, 0.7f, 0.2f, 0.5f), shape = "circle", size = 1.5f,  duration = 0.5f, isInstant = true },
                new EffectData { eventType = 19, name = "ShieldBreak",     color = new Color(1f, 0.3f, 0.3f, 0.7f),   shape = "circle", size = 2.5f,  duration = 0.5f, isInstant = true },
            };

            // Generate procedural textures for effects
            Texture2D circleTex = CreateEffectCircleTexture();
            Texture2D ringTex = CreateEffectRingTexture();

            string circleTexPath = $"{ResourceEffectsPath}/effect_circle_tex.png";
            string ringTexPath = $"{ResourceEffectsPath}/effect_ring_tex.png";

            SaveTexture(circleTex, circleTexPath);
            SaveTexture(ringTex, ringTexPath);
            AssetDatabase.Refresh();
            SetTextureAsSprite(circleTexPath);
            SetTextureAsSprite(ringTexPath);

            foreach (var effect in effects)
            {
                CreateSingleEffectPrefab(effect, circleTexPath, ringTexPath);
            }
        }

        private static readonly int EffectTexSize = 64;

        private static Texture2D CreateEffectCircleTexture()
        {
            Texture2D tex = new Texture2D(EffectTexSize, EffectTexSize, TextureFormat.RGBA32, false);
            float c = EffectTexSize / 2f;
            float rSq = c * c;
            for (int x = 0; x < EffectTexSize; x++)
            {
                for (int y = 0; y < EffectTexSize; y++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= rSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEffectRingTexture()
        {
            Texture2D tex = new Texture2D(EffectTexSize, EffectTexSize, TextureFormat.RGBA32, false);
            float c = EffectTexSize / 2f;
            float outerSq = c * c;
            float inner = c * 0.7f;
            float innerSq = inner * inner;
            for (int x = 0; x < EffectTexSize; x++)
            {
                for (int y = 0; y < EffectTexSize; y++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float dSq = dx * dx + dy * dy;
                    tex.SetPixel(x, y, dSq <= outerSq && dSq >= innerSq ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void CreateSingleEffectPrefab(EffectData effect, string circleTexPath, string ringTexPath)
        {
            string texPath = effect.shape == "ring" ? ringTexPath : circleTexPath;
            Sprite texSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);

            GameObject go = new GameObject($"Effect_{effect.name}");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 90;
            sr.color = effect.color;
            sr.sprite = texSprite;

            // Scale to achieve the desired world-space size
            // The sprite at 100 PPU with 64px texture = 0.64 world units diameter
            // We want effect.size diameter, so scale = effect.size / 0.64
            float spriteWorldSize = EffectTexSize / 100f; // default PPU = 100
            float scale = effect.size / spriteWorldSize;
            go.transform.localScale = Vector3.one * scale;

            if (effect.isInstant)
            {
                EffectBurstAnim anim = go.AddComponent<EffectBurstAnim>();
                SerializedObject so = new SerializedObject(anim);
                so.FindProperty("duration").floatValue = effect.duration;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                EffectFadeAnim anim = go.AddComponent<EffectFadeAnim>();
                SerializedObject so = new SerializedObject(anim);
                so.FindProperty("duration").floatValue = effect.duration;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(go, $"{ResourceEffectsPath}/Effect_{effect.name}.prefab");
            DestroyImmediate(go);
        }

        #endregion

        #region Scene Creation

        private static Font GetDefaultFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/AnFont.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                "Assets/Resources/Bg/large/background-1-large.png");
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
            Text titleText = CreateUIText(canvasObj, "TitleText", "小小塔防",
                72, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform titleRT = titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0, 150);
            titleRT.sizeDelta = new Vector2(600, 100);

            // Subtitle
            Text subtitleText = CreateUIText(canvasObj, "SubtitleText", "你是一个勇敢的探险家",
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
                "按钮路径");
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

            // ===== 角色/技能/奥术 Button Row =====
            string[] menuLabels = { "角色", "技能", "奥术" };
            UnityEngine.Events.UnityAction[] menuActions = {
                new UnityEngine.Events.UnityAction(menuUI.OnHeroButtonClicked),
                new UnityEngine.Events.UnityAction(menuUI.OnSkillButtonClicked),
                new UnityEngine.Events.UnityAction(menuUI.OnArcaneButtonClicked)
            };
            float menuBtnWidth = 160f;
            float menuBtnSpacing = 30f;
            float menuBtnY = -160f;
            for (int i = 0; i < 3; i++)
            {
                float xOffset = (i - 1) * (menuBtnWidth + menuBtnSpacing);
                GameObject mBtnObj = new GameObject($"{menuLabels[i]}Button");
                mBtnObj.transform.SetParent(canvasObj.transform, false);
                Image mBtnImg = mBtnObj.AddComponent<Image>();
                mBtnImg.color = new Color(0.15f, 0.3f, 0.5f);
                Button mBtn = mBtnObj.AddComponent<Button>();
                ColorBlock mCb = mBtn.colors;
                mCb.normalColor = Color.white;
                mCb.highlightedColor = new Color(0.9f, 0.9f, 1f);
                mCb.pressedColor = new Color(0.7f, 0.7f, 0.9f);
                mBtn.colors = mCb;

                RectTransform mBtnRT = mBtnObj.GetComponent<RectTransform>();
                mBtnRT.anchorMin = new Vector2(0.5f, 0.5f);
                mBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
                mBtnRT.anchoredPosition = new Vector2(xOffset, menuBtnY);
                mBtnRT.sizeDelta = new Vector2(menuBtnWidth, 60);

                Text mBtnText = CreateUIText(mBtnObj, "Text", menuLabels[i], 28, Color.white);
                RectTransform mBtnTextRT = mBtnText.GetComponent<RectTransform>();
                mBtnTextRT.anchorMin = Vector2.zero;
                mBtnTextRT.anchorMax = Vector2.one;
                mBtnTextRT.offsetMin = Vector2.zero;
                mBtnTextRT.offsetMax = Vector2.zero;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    mBtn.onClick, menuActions[i]);
            }

            // ===== LevelSelectUI Panel =====
            GameObject levelSelectObj = new GameObject("LevelSelectPanel");
            levelSelectObj.transform.SetParent(canvasObj.transform, false);
            RectTransform lsRT = levelSelectObj.AddComponent<RectTransform>();
            lsRT.anchorMin = Vector2.zero;
            lsRT.anchorMax = Vector2.one;
            lsRT.offsetMin = Vector2.zero;
            lsRT.offsetMax = Vector2.zero;

            Image lsBg = levelSelectObj.AddComponent<Image>();
            lsBg.color = new Color(0.03f, 0.03f, 0.1f, 0.95f);

            LevelSelectUI levelSelectUI = levelSelectObj.AddComponent<LevelSelectUI>();
            levelSelectObj.SetActive(false);

            // 关闭按钮(右上角)
            GameObject lsCloseBtnObj = new GameObject("CloseButton");
            lsCloseBtnObj.transform.SetParent(levelSelectObj.transform, false);
            Image lsCloseBtnImg = lsCloseBtnObj.AddComponent<Image>();
            lsCloseBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
            Button lsCloseBtn = lsCloseBtnObj.AddComponent<Button>();
            RectTransform lsCloseBtnRT = lsCloseBtnObj.GetComponent<RectTransform>();
            lsCloseBtnRT.anchorMin = new Vector2(1, 1);
            lsCloseBtnRT.anchorMax = new Vector2(1, 1);
            lsCloseBtnRT.anchoredPosition = new Vector2(-40, -40);
            lsCloseBtnRT.sizeDelta = new Vector2(50, 50);
            Text lsCloseBtnText = CreateUIText(lsCloseBtnObj, "Text", "X", 24, Color.white);
            RectTransform lsCloseBtnTextRT = lsCloseBtnText.GetComponent<RectTransform>();
            lsCloseBtnTextRT.anchorMin = Vector2.zero;
            lsCloseBtnTextRT.anchorMax = Vector2.one;
            lsCloseBtnTextRT.offsetMin = Vector2.zero;
            lsCloseBtnTextRT.offsetMax = Vector2.zero;

            // 标题
            Text lsTitleText = CreateUIText(levelSelectObj, "Title", "关卡选择",
                36, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform lsTitleRT = lsTitleText.GetComponent<RectTransform>();
            lsTitleRT.anchorMin = new Vector2(0.5f, 1);
            lsTitleRT.anchorMax = new Vector2(0.5f, 1);
            lsTitleRT.anchoredPosition = new Vector2(0, -50);
            lsTitleRT.sizeDelta = new Vector2(400, 60);

            // ScrollView 关卡列表
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(levelSelectObj.transform, false);
            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.1f, 0.15f);
            scrollRT.anchorMax = new Vector2(0.9f, 0.85f);
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.18f, 0.8f);
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportObj.AddComponent<Image>().color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportRT;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRT = contentObj.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.offsetMin = new Vector2(10, 0);
            contentRT.offsetMax = new Vector2(-10, 0);
            contentRT.sizeDelta = new Vector2(0, 0);

            GridLayoutGroup glg = contentObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(120, 120);
            glg.spacing = new Vector2(15, 15);
            glg.padding = new RectOffset(15, 15, 15, 15);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            // ===== Detail Panel =====
            GameObject detailObj = new GameObject("DetailPanel");
            detailObj.transform.SetParent(levelSelectObj.transform, false);
            RectTransform detailRT = detailObj.AddComponent<RectTransform>();
            detailRT.anchorMin = new Vector2(0.15f, 0.1f);
            detailRT.anchorMax = new Vector2(0.85f, 0.9f);
            detailRT.offsetMin = Vector2.zero;
            detailRT.offsetMax = Vector2.zero;

            Image detailBg = detailObj.AddComponent<Image>();
            detailBg.color = new Color(0.08f, 0.08f, 0.2f, 0.97f);
            detailObj.SetActive(false);

            // Detail: 关卡名
            Text detailNameText = CreateUIText(detailObj, "NameText", "",
                28, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform dnRT = detailNameText.GetComponent<RectTransform>();
            dnRT.anchorMin = new Vector2(0.05f, 0.85f);
            dnRT.anchorMax = new Vector2(0.95f, 0.95f);
            dnRT.offsetMin = Vector2.zero;
            dnRT.offsetMax = Vector2.zero;

            // Detail: 描述
            Text detailDescText = CreateUIText(detailObj, "DescText", "",
                18, new Color(0.8f, 0.8f, 0.9f), TextAnchor.UpperLeft);
            RectTransform ddRT = detailDescText.GetComponent<RectTransform>();
            ddRT.anchorMin = new Vector2(0.05f, 0.72f);
            ddRT.anchorMax = new Vector2(0.95f, 0.84f);
            ddRT.offsetMin = Vector2.zero;
            ddRT.offsetMax = Vector2.zero;

            // Detail: 精英标签
            Text eliteLabelText = CreateUIText(detailObj, "EliteLabel", "精英怪物:",
                18, new Color(0.9f, 0.6f, 0.2f), TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform elRT = eliteLabelText.GetComponent<RectTransform>();
            elRT.anchorMin = new Vector2(0.05f, 0.62f);
            elRT.anchorMax = new Vector2(0.95f, 0.72f);
            elRT.offsetMin = Vector2.zero;
            elRT.offsetMax = Vector2.zero;

            Text detailEliteText = CreateUIText(detailObj, "EliteText", "",
                16, new Color(0.7f, 0.7f, 0.8f), TextAnchor.UpperLeft);
            RectTransform deRT = detailEliteText.GetComponent<RectTransform>();
            deRT.anchorMin = new Vector2(0.05f, 0.48f);
            deRT.anchorMax = new Vector2(0.95f, 0.62f);
            deRT.offsetMin = Vector2.zero;
            deRT.offsetMax = Vector2.zero;

            // Detail: Boss标签
            Text bossLabelText = CreateUIText(detailObj, "BossLabel", "Boss:",
                18, new Color(0.9f, 0.2f, 0.2f), TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform blRT = bossLabelText.GetComponent<RectTransform>();
            blRT.anchorMin = new Vector2(0.05f, 0.38f);
            blRT.anchorMax = new Vector2(0.95f, 0.48f);
            blRT.offsetMin = Vector2.zero;
            blRT.offsetMax = Vector2.zero;

            Text detailBossText = CreateUIText(detailObj, "BossText", "",
                16, new Color(0.7f, 0.7f, 0.8f), TextAnchor.UpperLeft);
            RectTransform dbRT = detailBossText.GetComponent<RectTransform>();
            dbRT.anchorMin = new Vector2(0.05f, 0.24f);
            dbRT.anchorMax = new Vector2(0.95f, 0.38f);
            dbRT.offsetMin = Vector2.zero;
            dbRT.offsetMax = Vector2.zero;

            // Detail: 条件
            Text detailCondText = CreateUIText(detailObj, "ConditionText", "",
                16, new Color(0.7f, 0.7f, 0.8f), TextAnchor.UpperLeft);
            RectTransform dcRT = detailCondText.GetComponent<RectTransform>();
            dcRT.anchorMin = new Vector2(0.05f, 0.14f);
            dcRT.anchorMax = new Vector2(0.95f, 0.24f);
            dcRT.offsetMin = Vector2.zero;
            dcRT.offsetMax = Vector2.zero;

            // Detail: 挑战按钮
            GameObject challengeBtnObj = new GameObject("ChallengeButton");
            challengeBtnObj.transform.SetParent(detailObj.transform, false);
            Image chBtnImg = challengeBtnObj.AddComponent<Image>();
            chBtnImg.color = new Color(0.2f, 0.6f, 0.2f);
            Button challengeBtn = challengeBtnObj.AddComponent<Button>();
            ColorBlock chCb = challengeBtn.colors;
            chCb.normalColor = Color.white;
            chCb.highlightedColor = new Color(0.9f, 1f, 0.9f);
            chCb.pressedColor = new Color(0.7f, 0.9f, 0.7f);
            chCb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            challengeBtn.colors = chCb;
            RectTransform chBtnRT = challengeBtnObj.GetComponent<RectTransform>();
            chBtnRT.anchorMin = new Vector2(0.3f, 0.02f);
            chBtnRT.anchorMax = new Vector2(0.7f, 0.12f);
            chBtnRT.offsetMin = Vector2.zero;
            chBtnRT.offsetMax = Vector2.zero;
            Text chBtnText = CreateUIText(challengeBtnObj, "Text", "挑战", 24, Color.white);
            RectTransform chBtnTextRT = chBtnText.GetComponent<RectTransform>();
            chBtnTextRT.anchorMin = Vector2.zero;
            chBtnTextRT.anchorMax = Vector2.one;
            chBtnTextRT.offsetMin = Vector2.zero;
            chBtnTextRT.offsetMax = Vector2.zero;

            // Detail: 关闭按钮
            GameObject detailCloseBtnObj = new GameObject("CloseDetailButton");
            detailCloseBtnObj.transform.SetParent(detailObj.transform, false);
            Image dcBtnImg = detailCloseBtnObj.AddComponent<Image>();
            dcBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
            Button detailCloseBtn = detailCloseBtnObj.AddComponent<Button>();
            RectTransform dcBtnRT = detailCloseBtnObj.GetComponent<RectTransform>();
            dcBtnRT.anchorMin = new Vector2(1, 1);
            dcBtnRT.anchorMax = new Vector2(1, 1);
            dcBtnRT.anchoredPosition = new Vector2(-25, -25);
            dcBtnRT.sizeDelta = new Vector2(40, 40);
            Text dcBtnText = CreateUIText(detailCloseBtnObj, "Text", "X", 20, Color.white);
            RectTransform dcBtnTextRT = dcBtnText.GetComponent<RectTransform>();
            dcBtnTextRT.anchorMin = Vector2.zero;
            dcBtnTextRT.anchorMax = Vector2.one;
            dcBtnTextRT.offsetMin = Vector2.zero;
            dcBtnTextRT.offsetMax = Vector2.zero;

            // Wire LevelSelectUI serialized fields
            SerializedObject lsSO = new SerializedObject(levelSelectUI);
            lsSO.FindProperty("levelListContent").objectReferenceValue = contentRT;
            lsSO.FindProperty("detailPanel").objectReferenceValue = detailObj;
            lsSO.FindProperty("detailNameText").objectReferenceValue = detailNameText;
            lsSO.FindProperty("detailDescText").objectReferenceValue = detailDescText;
            lsSO.FindProperty("detailEliteText").objectReferenceValue = detailEliteText;
            lsSO.FindProperty("detailBossText").objectReferenceValue = detailBossText;
            lsSO.FindProperty("detailConditionText").objectReferenceValue = detailCondText;
            lsSO.FindProperty("challengeButton").objectReferenceValue = challengeBtn;
            lsSO.FindProperty("closeDetailButton").objectReferenceValue = detailCloseBtn;
            lsSO.FindProperty("closePanelButton").objectReferenceValue = lsCloseBtn;
            lsSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire MainMenuUI -> LevelSelectUI
            SerializedObject menuSO = new SerializedObject(menuUI);
            menuSO.FindProperty("levelSelectUI").objectReferenceValue = levelSelectUI;

            // ===== HeroSelectUI Panel =====
            HeroSelectUI heroSelectUI;
            {
                GameObject panelObj = new GameObject("HeroSelectPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                RectTransform panelRT = panelObj.AddComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;
                Image panelBg = panelObj.AddComponent<Image>();
                panelBg.color = new Color(0.03f, 0.03f, 0.1f, 0.95f);
                heroSelectUI = panelObj.AddComponent<HeroSelectUI>();
                panelObj.SetActive(false);

                // 关闭按钮
                GameObject closeBtnObj = new GameObject("CloseButton");
                closeBtnObj.transform.SetParent(panelObj.transform, false);
                Image closeBtnImg = closeBtnObj.AddComponent<Image>();
                closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
                Button closeBtn = closeBtnObj.AddComponent<Button>();
                RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
                closeBtnRT.anchorMin = new Vector2(1, 1);
                closeBtnRT.anchorMax = new Vector2(1, 1);
                closeBtnRT.anchoredPosition = new Vector2(-40, -40);
                closeBtnRT.sizeDelta = new Vector2(50, 50);
                Text closeBtnText = CreateUIText(closeBtnObj, "Text", "X", 24, Color.white);
                RectTransform closeBtnTextRT = closeBtnText.GetComponent<RectTransform>();
                closeBtnTextRT.anchorMin = Vector2.zero;
                closeBtnTextRT.anchorMax = Vector2.one;
                closeBtnTextRT.offsetMin = Vector2.zero;
                closeBtnTextRT.offsetMax = Vector2.zero;

                // 标题
                Text titleT = CreateUIText(panelObj, "Title", "角色选择",
                    36, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
                RectTransform titleRT2 = titleT.GetComponent<RectTransform>();
                titleRT2.anchorMin = new Vector2(0.5f, 1);
                titleRT2.anchorMax = new Vector2(0.5f, 1);
                titleRT2.anchoredPosition = new Vector2(0, -50);
                titleRT2.sizeDelta = new Vector2(400, 60);

                // ScrollView
                GameObject scrollObj = new GameObject("ScrollView");
                scrollObj.transform.SetParent(panelObj.transform, false);
                RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
                scrollRT.anchorMin = new Vector2(0.15f, 0.15f);
                scrollRT.anchorMax = new Vector2(0.85f, 0.85f);
                scrollRT.offsetMin = Vector2.zero;
                scrollRT.offsetMax = Vector2.zero;
                Image scrollBgH = scrollObj.AddComponent<Image>();
                scrollBgH.color = new Color(0.08f, 0.08f, 0.18f, 0.8f);
                ScrollRect scrollRectH = scrollObj.AddComponent<ScrollRect>();
                scrollRectH.horizontal = false;

                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollObj.transform, false);
                RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.offsetMin = Vector2.zero;
                viewportRT.offsetMax = Vector2.zero;
                viewportObj.AddComponent<Image>().color = Color.white;
                Mask maskH = viewportObj.AddComponent<Mask>();
                maskH.showMaskGraphic = false;
                scrollRectH.viewport = viewportRT;

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform, false);
                RectTransform contentRTH = contentObj.AddComponent<RectTransform>();
                contentRTH.anchorMin = new Vector2(0, 1);
                contentRTH.anchorMax = new Vector2(1, 1);
                contentRTH.pivot = new Vector2(0.5f, 1);
                contentRTH.offsetMin = new Vector2(10, 0);
                contentRTH.offsetMax = new Vector2(-10, 0);
                contentRTH.sizeDelta = new Vector2(0, 0);

                VerticalLayoutGroup vlgH = contentObj.AddComponent<VerticalLayoutGroup>();
                vlgH.spacing = 8;
                vlgH.padding = new RectOffset(10, 10, 10, 10);
                vlgH.childForceExpandWidth = true;
                vlgH.childForceExpandHeight = false;
                vlgH.childControlWidth = true;
                vlgH.childControlHeight = false;

                ContentSizeFitter csfH = contentObj.AddComponent<ContentSizeFitter>();
                csfH.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRectH.content = contentRTH;

                // add LayoutElement to items created at runtime: handled by HeroSelectUI itself
                // set preferred height via LayoutElement on each item

                SerializedObject heroSO = new SerializedObject(heroSelectUI);
                heroSO.FindProperty("heroListContent").objectReferenceValue = contentRTH;
                heroSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
                heroSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // ===== SkillSelectUI Panel =====
            SkillSelectUI skillSelectUI;
            {
                GameObject panelObj = new GameObject("SkillSelectPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                RectTransform panelRT = panelObj.AddComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;
                Image panelBg = panelObj.AddComponent<Image>();
                panelBg.color = new Color(0.03f, 0.03f, 0.1f, 0.95f);
                skillSelectUI = panelObj.AddComponent<SkillSelectUI>();
                panelObj.SetActive(false);

                // 关闭按钮
                GameObject closeBtnObj = new GameObject("CloseButton");
                closeBtnObj.transform.SetParent(panelObj.transform, false);
                Image closeBtnImg = closeBtnObj.AddComponent<Image>();
                closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
                Button closeBtn = closeBtnObj.AddComponent<Button>();
                RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
                closeBtnRT.anchorMin = new Vector2(1, 1);
                closeBtnRT.anchorMax = new Vector2(1, 1);
                closeBtnRT.anchoredPosition = new Vector2(-40, -40);
                closeBtnRT.sizeDelta = new Vector2(50, 50);
                Text closeBtnText = CreateUIText(closeBtnObj, "Text", "X", 24, Color.white);
                RectTransform closeBtnTextRT = closeBtnText.GetComponent<RectTransform>();
                closeBtnTextRT.anchorMin = Vector2.zero;
                closeBtnTextRT.anchorMax = Vector2.one;
                closeBtnTextRT.offsetMin = Vector2.zero;
                closeBtnTextRT.offsetMax = Vector2.zero;

                // 标题
                Text titleT = CreateUIText(panelObj, "Title", "技能选择",
                    36, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
                RectTransform titleRT2 = titleT.GetComponent<RectTransform>();
                titleRT2.anchorMin = new Vector2(0.5f, 1);
                titleRT2.anchorMax = new Vector2(0.5f, 1);
                titleRT2.anchoredPosition = new Vector2(0, -50);
                titleRT2.sizeDelta = new Vector2(400, 60);

                // 计数文本
                Text cntText = CreateUIText(panelObj, "CountText", "已选 0/8",
                    24, new Color(0.8f, 0.8f, 0.9f));
                RectTransform cntRT = cntText.GetComponent<RectTransform>();
                cntRT.anchorMin = new Vector2(0.5f, 1);
                cntRT.anchorMax = new Vector2(0.5f, 1);
                cntRT.anchoredPosition = new Vector2(0, -90);
                cntRT.sizeDelta = new Vector2(200, 30);

                // ScrollView
                GameObject scrollObj = new GameObject("ScrollView");
                scrollObj.transform.SetParent(panelObj.transform, false);
                RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
                scrollRT.anchorMin = new Vector2(0.15f, 0.18f);
                scrollRT.anchorMax = new Vector2(0.85f, 0.82f);
                scrollRT.offsetMin = Vector2.zero;
                scrollRT.offsetMax = Vector2.zero;
                Image scrollBgS = scrollObj.AddComponent<Image>();
                scrollBgS.color = new Color(0.08f, 0.08f, 0.18f, 0.8f);
                ScrollRect scrollRectS = scrollObj.AddComponent<ScrollRect>();
                scrollRectS.horizontal = false;

                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollObj.transform, false);
                RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.offsetMin = Vector2.zero;
                viewportRT.offsetMax = Vector2.zero;
                viewportObj.AddComponent<Image>().color = Color.white;
                Mask maskS = viewportObj.AddComponent<Mask>();
                maskS.showMaskGraphic = false;
                scrollRectS.viewport = viewportRT;

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform, false);
                RectTransform contentRTS = contentObj.AddComponent<RectTransform>();
                contentRTS.anchorMin = new Vector2(0, 1);
                contentRTS.anchorMax = new Vector2(1, 1);
                contentRTS.pivot = new Vector2(0.5f, 1);
                contentRTS.offsetMin = new Vector2(10, 0);
                contentRTS.offsetMax = new Vector2(-10, 0);
                contentRTS.sizeDelta = new Vector2(0, 0);

                VerticalLayoutGroup vlgS = contentObj.AddComponent<VerticalLayoutGroup>();
                vlgS.spacing = 8;
                vlgS.padding = new RectOffset(10, 10, 10, 10);
                vlgS.childForceExpandWidth = true;
                vlgS.childForceExpandHeight = false;
                vlgS.childControlWidth = true;
                vlgS.childControlHeight = false;

                ContentSizeFitter csfS = contentObj.AddComponent<ContentSizeFitter>();
                csfS.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRectS.content = contentRTS;

                // 确认按钮
                GameObject confirmBtnObj = new GameObject("ConfirmButton");
                confirmBtnObj.transform.SetParent(panelObj.transform, false);
                Image confirmBtnImg = confirmBtnObj.AddComponent<Image>();
                confirmBtnImg.color = new Color(0.2f, 0.6f, 0.2f);
                Button confirmBtn = confirmBtnObj.AddComponent<Button>();
                ColorBlock confirmCb = confirmBtn.colors;
                confirmCb.normalColor = Color.white;
                confirmCb.highlightedColor = new Color(0.9f, 1f, 0.9f);
                confirmCb.pressedColor = new Color(0.7f, 0.9f, 0.7f);
                confirmCb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
                confirmBtn.colors = confirmCb;
                RectTransform confirmBtnRT = confirmBtnObj.GetComponent<RectTransform>();
                confirmBtnRT.anchorMin = new Vector2(0.35f, 0.03f);
                confirmBtnRT.anchorMax = new Vector2(0.65f, 0.1f);
                confirmBtnRT.offsetMin = Vector2.zero;
                confirmBtnRT.offsetMax = Vector2.zero;
                Text confirmBtnText = CreateUIText(confirmBtnObj, "Text", "确认", 28, Color.white);
                RectTransform confirmBtnTextRT = confirmBtnText.GetComponent<RectTransform>();
                confirmBtnTextRT.anchorMin = Vector2.zero;
                confirmBtnTextRT.anchorMax = Vector2.one;
                confirmBtnTextRT.offsetMin = Vector2.zero;
                confirmBtnTextRT.offsetMax = Vector2.zero;

                SerializedObject skillSO = new SerializedObject(skillSelectUI);
                skillSO.FindProperty("skillListContent").objectReferenceValue = contentRTS;
                skillSO.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
                skillSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
                skillSO.FindProperty("countText").objectReferenceValue = cntText;
                skillSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // ===== ArcaneSelectUI Panel =====
            ArcaneSelectUI arcaneSelectUI;
            {
                GameObject panelObj = new GameObject("ArcaneSelectPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                RectTransform panelRT = panelObj.AddComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;
                Image panelBg = panelObj.AddComponent<Image>();
                panelBg.color = new Color(0.03f, 0.03f, 0.1f, 0.95f);
                arcaneSelectUI = panelObj.AddComponent<ArcaneSelectUI>();
                panelObj.SetActive(false);

                // 关闭按钮
                GameObject closeBtnObj = new GameObject("CloseButton");
                closeBtnObj.transform.SetParent(panelObj.transform, false);
                Image closeBtnImg = closeBtnObj.AddComponent<Image>();
                closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
                Button closeBtn = closeBtnObj.AddComponent<Button>();
                RectTransform closeBtnRT = closeBtnObj.GetComponent<RectTransform>();
                closeBtnRT.anchorMin = new Vector2(1, 1);
                closeBtnRT.anchorMax = new Vector2(1, 1);
                closeBtnRT.anchoredPosition = new Vector2(-40, -40);
                closeBtnRT.sizeDelta = new Vector2(50, 50);
                Text closeBtnText = CreateUIText(closeBtnObj, "Text", "X", 24, Color.white);
                RectTransform closeBtnTextRT = closeBtnText.GetComponent<RectTransform>();
                closeBtnTextRT.anchorMin = Vector2.zero;
                closeBtnTextRT.anchorMax = Vector2.one;
                closeBtnTextRT.offsetMin = Vector2.zero;
                closeBtnTextRT.offsetMax = Vector2.zero;

                // 标题
                Text titleT = CreateUIText(panelObj, "Title", "奥术选择",
                    36, new Color(0.98f, 0.9f, 0.62f), TextAnchor.MiddleCenter, FontStyle.Bold);
                RectTransform titleRT2 = titleT.GetComponent<RectTransform>();
                titleRT2.anchorMin = new Vector2(0.5f, 1);
                titleRT2.anchorMax = new Vector2(0.5f, 1);
                titleRT2.anchoredPosition = new Vector2(0, -50);
                titleRT2.sizeDelta = new Vector2(400, 60);

                // 计数文本
                Text cntText = CreateUIText(panelObj, "CountText", "已选 0/4",
                    24, new Color(0.8f, 0.8f, 0.9f));
                RectTransform cntRT = cntText.GetComponent<RectTransform>();
                cntRT.anchorMin = new Vector2(0.5f, 1);
                cntRT.anchorMax = new Vector2(0.5f, 1);
                cntRT.anchoredPosition = new Vector2(0, -90);
                cntRT.sizeDelta = new Vector2(200, 30);

                // ScrollView
                GameObject scrollObj = new GameObject("ScrollView");
                scrollObj.transform.SetParent(panelObj.transform, false);
                RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
                scrollRT.anchorMin = new Vector2(0.15f, 0.18f);
                scrollRT.anchorMax = new Vector2(0.85f, 0.82f);
                scrollRT.offsetMin = Vector2.zero;
                scrollRT.offsetMax = Vector2.zero;
                Image scrollBgA = scrollObj.AddComponent<Image>();
                scrollBgA.color = new Color(0.08f, 0.08f, 0.18f, 0.8f);
                ScrollRect scrollRectA = scrollObj.AddComponent<ScrollRect>();
                scrollRectA.horizontal = false;

                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollObj.transform, false);
                RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.offsetMin = Vector2.zero;
                viewportRT.offsetMax = Vector2.zero;
                viewportObj.AddComponent<Image>().color = Color.white;
                Mask maskA = viewportObj.AddComponent<Mask>();
                maskA.showMaskGraphic = false;
                scrollRectA.viewport = viewportRT;

                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(viewportObj.transform, false);
                RectTransform contentRTA = contentObj.AddComponent<RectTransform>();
                contentRTA.anchorMin = new Vector2(0, 1);
                contentRTA.anchorMax = new Vector2(1, 1);
                contentRTA.pivot = new Vector2(0.5f, 1);
                contentRTA.offsetMin = new Vector2(10, 0);
                contentRTA.offsetMax = new Vector2(-10, 0);
                contentRTA.sizeDelta = new Vector2(0, 0);

                VerticalLayoutGroup vlgA = contentObj.AddComponent<VerticalLayoutGroup>();
                vlgA.spacing = 8;
                vlgA.padding = new RectOffset(10, 10, 10, 10);
                vlgA.childForceExpandWidth = true;
                vlgA.childForceExpandHeight = false;
                vlgA.childControlWidth = true;
                vlgA.childControlHeight = false;

                ContentSizeFitter csfA = contentObj.AddComponent<ContentSizeFitter>();
                csfA.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRectA.content = contentRTA;

                // 确认按钮
                GameObject confirmBtnObj = new GameObject("ConfirmButton");
                confirmBtnObj.transform.SetParent(panelObj.transform, false);
                Image confirmBtnImg = confirmBtnObj.AddComponent<Image>();
                confirmBtnImg.color = new Color(0.2f, 0.6f, 0.2f);
                Button confirmBtn = confirmBtnObj.AddComponent<Button>();
                ColorBlock confirmCb = confirmBtn.colors;
                confirmCb.normalColor = Color.white;
                confirmCb.highlightedColor = new Color(0.9f, 1f, 0.9f);
                confirmCb.pressedColor = new Color(0.7f, 0.9f, 0.7f);
                confirmCb.disabledColor = new Color(0.5f, 0.5f, 0.5f);
                confirmBtn.colors = confirmCb;
                RectTransform confirmBtnRT = confirmBtnObj.GetComponent<RectTransform>();
                confirmBtnRT.anchorMin = new Vector2(0.35f, 0.03f);
                confirmBtnRT.anchorMax = new Vector2(0.65f, 0.1f);
                confirmBtnRT.offsetMin = Vector2.zero;
                confirmBtnRT.offsetMax = Vector2.zero;
                Text confirmBtnText = CreateUIText(confirmBtnObj, "Text", "确认", 28, Color.white);
                RectTransform confirmBtnTextRT = confirmBtnText.GetComponent<RectTransform>();
                confirmBtnTextRT.anchorMin = Vector2.zero;
                confirmBtnTextRT.anchorMax = Vector2.one;
                confirmBtnTextRT.offsetMin = Vector2.zero;
                confirmBtnTextRT.offsetMax = Vector2.zero;

                SerializedObject arcaneSO = new SerializedObject(arcaneSelectUI);
                arcaneSO.FindProperty("arcaneListContent").objectReferenceValue = contentRTA;
                arcaneSO.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
                arcaneSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
                arcaneSO.FindProperty("countText").objectReferenceValue = cntText;
                arcaneSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire MainMenuUI -> all panels
            menuSO.FindProperty("heroSelectUI").objectReferenceValue = heroSelectUI;
            menuSO.FindProperty("skillSelectUI").objectReferenceValue = skillSelectUI;
            menuSO.FindProperty("arcaneSelectUI").objectReferenceValue = arcaneSelectUI;
            menuSO.ApplyModifiedPropertiesWithoutUndo();

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
            DragVisualManager dragVisualMgr = bmObj.AddComponent<DragVisualManager>();

            // Load prefabs
            GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Hero.prefab");
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Monster.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Boss.prefab");
            GameObject heroBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/HeroBullet.prefab");
            GameObject bossBulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/BossBullet.prefab");
            GameObject summonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/Summon.prefab");

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
                "游戏场景");
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
                "按钮路径");
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
                "返回按钮路径");
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
            string gameConfigJson = File.ReadAllText("Assets/Resources/Configs/game_config.json");
            GameConfig gameConfigData = JsonUtility.FromJson<GameConfig>(gameConfigJson);
            string skillConfigJson = File.ReadAllText("Assets/Resources/Configs/skill_config.json");
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
            skillBarPanelRT.anchoredPosition = new Vector2(0, 44);
            float slotWidth = 120f;
            float slotSpacing = 6f;
            float barWidth = slotCount * slotWidth + (slotCount - 1) * slotSpacing + 24f;
            float barHeight = 160f;
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
                slotRT.anchoredPosition = new Vector2(slotX, -5);
                slotRT.sizeDelta = new Vector2(slotWidth, barHeight - 16f);

                SkillSlotUI slotUI = slotObj.AddComponent<SkillSlotUI>();
                CanvasGroup slotCanvasGroup = slotObj.AddComponent<CanvasGroup>();

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
                Text nameText = CreateUIText(slotObj, "NameText", skillName, 18,
                    new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
                nameText.raycastTarget = false;
                Outline nameOutline = nameText.gameObject.AddComponent<Outline>();
                nameOutline.effectColor = new Color(0, 0, 0, 0.8f);
                nameOutline.effectDistance = new Vector2(1, -1);
                RectTransform nameRT = nameText.GetComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0f, 1f);
                nameRT.anchorMax = new Vector2(1f, 1f);
                nameRT.pivot = new Vector2(0.5f, 1f);
                nameRT.anchoredPosition = new Vector2(0, 5);
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
                Text cdTimeText = CreateUIText(iconBgObj, "CooldownText", "", 20,
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
                Text lvText = CreateUIText(slotObj, "LevelText", "Lv.0", 24,
                    new Color(0.9f, 0.9f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Normal);
                lvText.raycastTarget = false;
                Outline lvOutline = lvText.gameObject.AddComponent<Outline>();
                lvOutline.effectColor = new Color(0, 0, 0, 0.8f);
                lvOutline.effectDistance = new Vector2(1, -1);
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
                slotSO.FindProperty("slotCanvasGroup").objectReferenceValue = slotCanvasGroup;
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
            skillBarSO.FindProperty("floatingTextUI").objectReferenceValue = floatingTextUI;
            skillBarSO.ApplyModifiedPropertiesWithoutUndo();

            // ========== Arcane Bar (above skill bar) ==========
            string arcaneConfigJson = File.ReadAllText("Assets/Resources/Configs/arcane_config.json");
            ArcaneConfigList arcaneConfigList = JsonUtility.FromJson<ArcaneConfigList>(arcaneConfigJson);

            Dictionary<int, string> arcaneIconMap = new Dictionary<int, string>();
            Dictionary<int, string> arcaneNameMap = new Dictionary<int, string>();
            Dictionary<int, ArcaneConfig> arcaneConfigMap = new Dictionary<int, ArcaneConfig>();
            if (arcaneConfigList != null && arcaneConfigList.arcanes != null)
            {
                foreach (var ac in arcaneConfigList.arcanes)
                {
                    arcaneIconMap[ac.id] = ac.icon;
                    arcaneNameMap[ac.id] = ac.name;
                    arcaneConfigMap[ac.id] = ac;
                }
            }

            int arcaneSlotCount = gameConfigData.arcane_slot_ids != null ? gameConfigData.arcane_slot_ids.Length : 0;

            // --- Rune Bar Panel (bottom, full width horizontal 4-column) ---
            GameObject runeBarPanel = new GameObject("RuneBarPanel");
            runeBarPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform runeBarRT = runeBarPanel.AddComponent<RectTransform>();
            runeBarRT.anchorMin = new Vector2(0.5f, 0f);
            runeBarRT.anchorMax = new Vector2(0.5f, 0f);
            runeBarRT.pivot = new Vector2(0.5f, 0f);
            runeBarRT.anchoredPosition = new Vector2(0, 8);
            runeBarRT.sizeDelta = new Vector2(barWidth, 30);

            Image runeBarBg = runeBarPanel.AddComponent<Image>();
            runeBarBg.color = new Color(0.02f, 0.02f, 0.08f, 0.8f);
            runeBarBg.raycastTarget = false;

            RuneBarUI runeBarUI = runeBarPanel.AddComponent<RuneBarUI>();

            string[] runeLabels = { "火", "冰", "雷", "风" };
            Color[] runeColors = {
                new Color(1f, 0.4f, 0.2f),
                new Color(0.3f, 0.7f, 1f),
                new Color(0.8f, 0.8f, 0.2f),
                new Color(0.3f, 0.9f, 0.5f)
            };

            Text[] runeCountTexts = new Text[4];
            Text[] runeValueTexts = new Text[4];
            Slider[] energySliders = new Slider[4];

            for (int r = 0; r < 4; r++)
            {
                float colLeft = r * 0.25f;
                float colRight = (r + 1) * 0.25f;

                // Rune type label
                Text labelText = CreateUIText(runeBarPanel, $"RuneLabel_{r}", runeLabels[r], 24,
                    runeColors[r], TextAnchor.MiddleCenter, FontStyle.Bold);
                labelText.raycastTarget = false;
                Outline labelOutline = labelText.gameObject.AddComponent<Outline>();
                labelOutline.effectColor = new Color(0, 0, 0, 0.8f);
                labelOutline.effectDistance = new Vector2(1, -1);
                RectTransform labelRT = labelText.GetComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(colLeft, 0);
                labelRT.anchorMax = new Vector2(colLeft, 1);
                labelRT.pivot = new Vector2(0, 0.5f);
                labelRT.anchoredPosition = new Vector2(8, 0);
                labelRT.sizeDelta = new Vector2(24, 0);

                // Rune count
                Text countText = CreateUIText(runeBarPanel, $"RuneCount_{r}", "0", 24,
                    runeColors[r], TextAnchor.MiddleCenter, FontStyle.Bold);
                countText.raycastTarget = false;
                Outline countOutline = countText.gameObject.AddComponent<Outline>();
                countOutline.effectColor = new Color(0, 0, 0, 0.8f);
                countOutline.effectDistance = new Vector2(1, -1);
                RectTransform countRT = countText.GetComponent<RectTransform>();
                countRT.anchorMin = new Vector2(colLeft, 0);
                countRT.anchorMax = new Vector2(colLeft, 1);
                countRT.pivot = new Vector2(0, 0.5f);
                countRT.anchoredPosition = new Vector2(34, 0);
                countRT.sizeDelta = new Vector2(30, 0);
                runeCountTexts[r] = countText;

                // Energy slider
                GameObject esliderObj = new GameObject($"EnergySlider_{r}");
                esliderObj.transform.SetParent(runeBarPanel.transform, false);
                Slider eslider = esliderObj.AddComponent<Slider>();
                eslider.interactable = false;
                eslider.transition = Selectable.Transition.None;
                eslider.minValue = 0;
                eslider.maxValue = 10;
                eslider.value = 0;

                RectTransform esliderRT = esliderObj.GetComponent<RectTransform>();
                esliderRT.anchorMin = new Vector2(colLeft, 0.15f);
                esliderRT.anchorMax = new Vector2(colRight, 0.85f);
                esliderRT.offsetMin = new Vector2(66, 0);
                esliderRT.offsetMax = new Vector2(-6, 0);

                GameObject eslBg = new GameObject("Background");
                eslBg.transform.SetParent(esliderObj.transform, false);
                Image eslBgImg = eslBg.AddComponent<Image>();
                if (barBgSprite != null) eslBgImg.sprite = barBgSprite;
                eslBgImg.type = Image.Type.Sliced;
                eslBgImg.color = new Color(0.1f, 0.1f, 0.18f, 0.9f);
                RectTransform eslBgRT = eslBg.GetComponent<RectTransform>();
                eslBgRT.anchorMin = Vector2.zero;
                eslBgRT.anchorMax = Vector2.one;
                eslBgRT.offsetMin = Vector2.zero;
                eslBgRT.offsetMax = Vector2.zero;

                GameObject eslFillArea = new GameObject("Fill Area");
                eslFillArea.transform.SetParent(esliderObj.transform, false);
                RectTransform eslFillAreaRT = eslFillArea.AddComponent<RectTransform>();
                eslFillAreaRT.anchorMin = Vector2.zero;
                eslFillAreaRT.anchorMax = Vector2.one;
                eslFillAreaRT.offsetMin = Vector2.zero;
                eslFillAreaRT.offsetMax = Vector2.zero;

                GameObject eslFill = new GameObject("Fill");
                eslFill.transform.SetParent(eslFillArea.transform, false);
                Image eslFillImg = eslFill.AddComponent<Image>();
                if (barFillSprite != null) eslFillImg.sprite = barFillSprite;
                eslFillImg.type = Image.Type.Sliced;
                eslFillImg.color = runeColors[r];
                RectTransform eslFillRT = eslFill.GetComponent<RectTransform>();
                eslFillRT.anchorMin = Vector2.zero;
                eslFillRT.anchorMax = new Vector2(0, 1);
                eslFillRT.offsetMin = Vector2.zero;
                eslFillRT.offsetMax = Vector2.zero;

                eslider.fillRect = eslFillRT;
                energySliders[r] = eslider;

                // Rune count value
                Text countValueText = CreateUIText(runeBarPanel, $"RuneCountValue_{r}", "0", 16,
                    Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                countValueText.raycastTarget = false;
                Outline countValueOutline = countValueText.gameObject.AddComponent<Outline>();
                countValueOutline.effectColor = new Color(0, 0, 0, 0.8f);
                countValueOutline.effectDistance = new Vector2(1, -1);
                RectTransform countValueRT = countValueText.GetComponent<RectTransform>();
                countValueRT.anchorMin = new Vector2(colLeft, 0);
                countValueRT.anchorMax = new Vector2(colLeft, 1);
                countValueRT.pivot = new Vector2(0, 0.5f);
                countValueRT.anchoredPosition = new Vector2(150, 0);
                countValueRT.sizeDelta = new Vector2(30, 0);
                runeValueTexts[r] = countValueText;
            }

            // Wire RuneBarUI
            SerializedObject runeBarSO = new SerializedObject(runeBarUI);
            SerializedProperty runeValueTextsP = runeBarSO.FindProperty("runeValueTexts");
            runeValueTextsP.arraySize = 4;
            SerializedProperty runeTextsP = runeBarSO.FindProperty("runeCountTexts");
            runeTextsP.arraySize = 4;
            SerializedProperty energySlidersP = runeBarSO.FindProperty("energySliders");
            energySlidersP.arraySize = 4;
            for (int r = 0; r < 4; r++) {
                runeValueTextsP.GetArrayElementAtIndex(r).objectReferenceValue = runeValueTexts[r];
                runeTextsP.GetArrayElementAtIndex(r).objectReferenceValue = runeCountTexts[r];
                energySlidersP.GetArrayElementAtIndex(r).objectReferenceValue = energySliders[r];
            }
            runeBarSO.ApplyModifiedPropertiesWithoutUndo();

            // --- Arcane Bar Panel (same position as skill bar, toggled) ---
            float arcaneSlotWidth = 100f;
            float arcaneSlotSpacing = 6f;
            float arcaneBarWidth = arcaneSlotCount * arcaneSlotWidth + (arcaneSlotCount - 1) * arcaneSlotSpacing + 20f;
            float arcaneBarHeight = 120f;

            GameObject arcaneBarPanel = new GameObject("ArcaneBarPanel");
            arcaneBarPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform arcaneBarPanelRT = arcaneBarPanel.AddComponent<RectTransform>();
            arcaneBarPanelRT.anchorMin = new Vector2(0.5f, 0f);
            arcaneBarPanelRT.anchorMax = new Vector2(0.5f, 0f);
            arcaneBarPanelRT.pivot = new Vector2(0.5f, 0f);
            arcaneBarPanelRT.anchoredPosition = new Vector2(0, 44);
            arcaneBarPanelRT.sizeDelta = new Vector2(arcaneBarWidth, arcaneBarHeight);

            Image arcaneBarBg = arcaneBarPanel.AddComponent<Image>();
            arcaneBarBg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);
            arcaneBarBg.raycastTarget = false;

            ArcaneBarUI arcaneBarUI = arcaneBarPanel.AddComponent<ArcaneBarUI>();

            ArcaneSlotUI[] arcaneSlotUIs = new ArcaneSlotUI[arcaneSlotCount];

            for (int a = 0; a < arcaneSlotCount; a++)
            {
                int arcaneId = gameConfigData.arcane_slot_ids[a];
                string aIconName = arcaneIconMap.ContainsKey(arcaneId) ? arcaneIconMap[arcaneId] : "";
                string aName = arcaneNameMap.ContainsKey(arcaneId) ? arcaneNameMap[arcaneId] : "";
                ArcaneConfig aCfg = arcaneConfigMap.ContainsKey(arcaneId) ? arcaneConfigMap[arcaneId] : null;

                int runeCost = aCfg != null ? aCfg.runeCost : 0;
                int runeType = aCfg != null ? aCfg.runeType : 1;
                string costLabel = $"{runeCost}{runeLabels[Mathf.Clamp(runeType - 1, 0, 3)]}";

                GameObject aSlotObj = new GameObject($"ArcaneSlot_{a}");
                aSlotObj.transform.SetParent(arcaneBarPanel.transform, false);
                RectTransform aSlotRT = aSlotObj.AddComponent<RectTransform>();
                float aSlotX = -arcaneBarWidth / 2f + 10f + a * (arcaneSlotWidth + arcaneSlotSpacing) + arcaneSlotWidth / 2f;
                aSlotRT.anchorMin = new Vector2(0.5f, 0.5f);
                aSlotRT.anchorMax = new Vector2(0.5f, 0.5f);
                aSlotRT.anchoredPosition = new Vector2(aSlotX, 0);
                aSlotRT.sizeDelta = new Vector2(arcaneSlotWidth, arcaneBarHeight - 12f);

                ArcaneSlotUI aSlotUI = aSlotObj.AddComponent<ArcaneSlotUI>();
                CanvasGroup aSlotCG = aSlotObj.AddComponent<CanvasGroup>();

                Image aSlotBg = aSlotObj.AddComponent<Image>();
                aSlotBg.color = new Color(0.06f, 0.04f, 0.15f, 0.9f);

                Text aNameText = CreateUIText(aSlotObj, "NameText", aName, 14,
                    new Color(0.85f, 0.7f, 1f), TextAnchor.MiddleCenter);
                aNameText.raycastTarget = false;
                Outline aNameOutline = aNameText.gameObject.AddComponent<Outline>();
                aNameOutline.effectColor = new Color(0, 0, 0, 0.8f);
                aNameOutline.effectDistance = new Vector2(1, -1);
                RectTransform aNameRT = aNameText.GetComponent<RectTransform>();
                aNameRT.anchorMin = new Vector2(0f, 1f);
                aNameRT.anchorMax = new Vector2(1f, 1f);
                aNameRT.pivot = new Vector2(0.5f, 1f);
                aNameRT.anchoredPosition = new Vector2(0, -2);
                aNameRT.sizeDelta = new Vector2(0, 18);

                GameObject aIconBg = new GameObject("IconBg");
                aIconBg.transform.SetParent(aSlotObj.transform, false);
                Image aIconBgImg = aIconBg.AddComponent<Image>();
                aIconBgImg.color = new Color(0.04f, 0.04f, 0.12f, 1f);
                aIconBgImg.raycastTarget = false;
                RectTransform aIconBgRT = aIconBg.GetComponent<RectTransform>();
                aIconBgRT.anchorMin = new Vector2(0.5f, 1f);
                aIconBgRT.anchorMax = new Vector2(0.5f, 1f);
                aIconBgRT.pivot = new Vector2(0.5f, 1f);
                aIconBgRT.anchoredPosition = new Vector2(0, -20);
                aIconBgRT.sizeDelta = new Vector2(56, 56);

                GameObject aBorder = new GameObject("Border");
                aBorder.transform.SetParent(aIconBg.transform, false);
                Image aBorderImg = aBorder.AddComponent<Image>();
                aBorderImg.color = new Color(0.5f, 0.3f, 0.7f, 0.8f);
                aBorderImg.raycastTarget = false;
                RectTransform aBorderRT = aBorder.GetComponent<RectTransform>();
                aBorderRT.anchorMin = Vector2.zero;
                aBorderRT.anchorMax = Vector2.one;
                aBorderRT.offsetMin = new Vector2(-2, -2);
                aBorderRT.offsetMax = new Vector2(2, 2);
                aBorder.transform.SetAsFirstSibling();

                GameObject aIconObj = new GameObject("Icon");
                aIconObj.transform.SetParent(aIconBg.transform, false);
                Image aIconImg = aIconObj.AddComponent<Image>();
                aIconImg.raycastTarget = false;
                aIconImg.color = Color.white;

                RectTransform aIconRT = aIconObj.GetComponent<RectTransform>();
                aIconRT.anchorMin = Vector2.zero;
                aIconRT.anchorMax = Vector2.one;
                aIconRT.offsetMin = new Vector2(4, 4);
                aIconRT.offsetMax = new Vector2(-4, -4);

                GameObject aCdObj = new GameObject("CooldownOverlay");
                aCdObj.transform.SetParent(aIconBg.transform, false);
                Image aCdImg = aCdObj.AddComponent<Image>();
                aCdImg.color = new Color(0, 0, 0, 0.7f);
                aCdImg.type = Image.Type.Filled;
                aCdImg.fillMethod = Image.FillMethod.Radial360;
                aCdImg.fillOrigin = 2;
                aCdImg.fillClockwise = true;
                aCdImg.fillAmount = 0f;
                aCdImg.raycastTarget = false;
                RectTransform aCdRT = aCdObj.GetComponent<RectTransform>();
                aCdRT.anchorMin = Vector2.zero;
                aCdRT.anchorMax = Vector2.one;
                aCdRT.offsetMin = Vector2.zero;
                aCdRT.offsetMax = Vector2.zero;
                aCdObj.SetActive(false);

                Text aCdText = CreateUIText(aIconBg, "CooldownText", "", 18,
                    Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                aCdText.raycastTarget = false;
                Outline aCdOutline = aCdText.gameObject.AddComponent<Outline>();
                aCdOutline.effectColor = new Color(0, 0, 0, 0.8f);
                aCdOutline.effectDistance = new Vector2(1, -1);
                RectTransform aCdTextRT = aCdText.GetComponent<RectTransform>();
                aCdTextRT.anchorMin = Vector2.zero;
                aCdTextRT.anchorMax = Vector2.one;
                aCdTextRT.offsetMin = Vector2.zero;
                aCdTextRT.offsetMax = Vector2.zero;
                aCdText.gameObject.SetActive(false);

                Text aCostText = CreateUIText(aSlotObj, "CostText", costLabel, 14,
                    new Color(0.6f, 0.8f, 1f), TextAnchor.MiddleCenter);
                aCostText.raycastTarget = false;
                Outline aCostOutline = aCostText.gameObject.AddComponent<Outline>();
                aCostOutline.effectColor = new Color(0, 0, 0, 0.8f);
                aCostOutline.effectDistance = new Vector2(1, -1);
                RectTransform aCostRT = aCostText.GetComponent<RectTransform>();
                aCostRT.anchorMin = new Vector2(0f, 0f);
                aCostRT.anchorMax = new Vector2(1f, 0f);
                aCostRT.pivot = new Vector2(0.5f, 0f);
                aCostRT.anchoredPosition = new Vector2(0, 2);
                aCostRT.sizeDelta = new Vector2(0, 18);

                SerializedObject aSlotSO = new SerializedObject(aSlotUI);
                aSlotSO.FindProperty("iconImage").objectReferenceValue = aIconImg;
                aSlotSO.FindProperty("nameText").objectReferenceValue = aNameText;
                aSlotSO.FindProperty("costText").objectReferenceValue = aCostText;
                aSlotSO.FindProperty("cooldownText").objectReferenceValue = aCdText;
                aSlotSO.FindProperty("cooldownOverlay").objectReferenceValue = aCdImg;
                aSlotSO.FindProperty("slotCanvasGroup").objectReferenceValue = aSlotCG;
                aSlotSO.ApplyModifiedPropertiesWithoutUndo();

                arcaneSlotUIs[a] = aSlotUI;
            }

            // Wire ArcaneBarUI
            SerializedObject arcaneBarSO = new SerializedObject(arcaneBarUI);
            SerializedProperty arcaneSlotsP = arcaneBarSO.FindProperty("slots");
            arcaneSlotsP.arraySize = arcaneSlotCount;
            for (int a = 0; a < arcaneSlotCount; a++)
                arcaneSlotsP.GetArrayElementAtIndex(a).objectReferenceValue = arcaneSlotUIs[a];
            arcaneBarSO.ApplyModifiedPropertiesWithoutUndo();

            // --- Tab Switch Button (left of skill/arcane bar) ---
            float maxBarWidth = Mathf.Max(barWidth, arcaneBarWidth);
            GameObject tabSwitchObj = new GameObject("TabSwitchButton");
            tabSwitchObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tabSwitchRT = tabSwitchObj.AddComponent<RectTransform>();
            tabSwitchRT.anchorMin = new Vector2(0.5f, 0f);
            tabSwitchRT.anchorMax = new Vector2(0.5f, 0f);
            tabSwitchRT.pivot = new Vector2(1f, 0.5f);
            tabSwitchRT.anchoredPosition = new Vector2(-maxBarWidth / 2f - 30f, 44 + barHeight / 2f);
            tabSwitchRT.sizeDelta = new Vector2(120, 112);

            Image tabBtnBg = tabSwitchObj.AddComponent<Image>();
            tabBtnBg.color = new Color(0.15f, 0.12f, 0.3f, 0.9f);

            Button tabBtn = tabSwitchObj.AddComponent<Button>();
            ColorBlock tabBtnColors = tabBtn.colors;
            tabBtnColors.normalColor = Color.white;
            tabBtnColors.highlightedColor = new Color(0.85f, 0.8f, 1f);
            tabBtnColors.pressedColor = new Color(0.6f, 0.5f, 0.9f);
            tabBtn.colors = tabBtnColors;
            tabBtn.targetGraphic = tabBtnBg;

            Text tabBtnText = CreateUIText(tabSwitchObj, "Text", "奥术", 32,
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform tabBtnTextRT = tabBtnText.GetComponent<RectTransform>();
            tabBtnTextRT.anchorMin = Vector2.zero;
            tabBtnTextRT.anchorMax = Vector2.one;
            tabBtnTextRT.offsetMin = Vector2.zero;
            tabBtnTextRT.offsetMax = Vector2.zero;
            Outline tabBtnOutline = tabBtnText.gameObject.AddComponent<Outline>();
            tabBtnOutline.effectColor = new Color(0, 0, 0, 0.8f);
            tabBtnOutline.effectDistance = new Vector2(1, -1);

            // Wire TabSwitchUI
            TabSwitchUI tabSwitchUI = tabSwitchObj.AddComponent<TabSwitchUI>();
            SerializedObject tabSwitchSO = new SerializedObject(tabSwitchUI);
            tabSwitchSO.FindProperty("skillBarPanel").objectReferenceValue = skillBarPanel;
            tabSwitchSO.FindProperty("arcaneBarPanel").objectReferenceValue = arcaneBarPanel;
            tabSwitchSO.FindProperty("tabButton").objectReferenceValue = tabBtn;
            tabSwitchSO.FindProperty("tabButtonText").objectReferenceValue = tabBtnText;
            tabSwitchSO.ApplyModifiedPropertiesWithoutUndo();

            arcaneBarPanel.SetActive(false);

            // --- Active Arcane Icons (top right) ---
            GameObject activeArcanePanel = new GameObject("ActiveArcanePanel");
            activeArcanePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform activeArcanePanelRT = activeArcanePanel.AddComponent<RectTransform>();
            activeArcanePanelRT.anchorMin = new Vector2(1f, 1f);
            activeArcanePanelRT.anchorMax = new Vector2(1f, 1f);
            activeArcanePanelRT.pivot = new Vector2(1f, 1f);
            activeArcanePanelRT.anchoredPosition = new Vector2(-10, -90);
            activeArcanePanelRT.sizeDelta = new Vector2(220, 48);

            ArcaneActiveIconUI activeIconUI = activeArcanePanel.AddComponent<ArcaneActiveIconUI>();
            SerializedObject activeIconSO = new SerializedObject(activeIconUI);
            activeIconSO.FindProperty("container").objectReferenceValue = activeArcanePanelRT;
            activeIconSO.ApplyModifiedPropertiesWithoutUndo();

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
            bmSO.FindProperty("summonPrefab").objectReferenceValue = summonPrefab;
            bmSO.FindProperty("battleUI").objectReferenceValue = battleUI;
            bmSO.FindProperty("skillBarUI").objectReferenceValue = skillBarUI;
            bmSO.FindProperty("floatingTextUI").objectReferenceValue = floatingTextUI;
            bmSO.FindProperty("heroSpawnPoint").objectReferenceValue = heroSpawnPoint.transform;
            bmSO.FindProperty("arcaneBarUI").objectReferenceValue = arcaneBarUI;
            bmSO.FindProperty("runeBarUI").objectReferenceValue = runeBarUI;
            bmSO.FindProperty("arcaneActiveIconUI").objectReferenceValue = activeIconUI;
            bmSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire DragVisualManager
            SerializedObject dragVisualSO = new SerializedObject(dragVisualMgr);
            dragVisualSO.FindProperty("battleManager").objectReferenceValue = bm;
            dragVisualSO.ApplyModifiedPropertiesWithoutUndo();

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
