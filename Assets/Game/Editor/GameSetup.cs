using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace GeometryTD.Editor
{
    public class GameSetup
    {
        private const string GAME_PATH = "Assets/Game";
        private const string SCENE_PATH = GAME_PATH + "/Scenes";
        private const string PREFAB_PATH = GAME_PATH + "/Prefabs";

        [MenuItem("GeometryTD/Setup Game (Generate All)", false, 0)]
        public static void SetupAll()
        {
            CreateDirectories();
            CreateBulletPrefab();
            CreateEnemyPrefab();
            CreateBossPrefab();
            CreateMainMenuScene();
            CreateBattleScene();
            AddScenesToBuildSettings();
            Debug.Log("[GeometryTD] Game setup complete! Open MainMenuScene to play.");
        }

        [MenuItem("GeometryTD/1. Create Prefabs Only", false, 20)]
        public static void CreatePrefabsOnly()
        {
            CreateDirectories();
            CreateBulletPrefab();
            CreateEnemyPrefab();
            CreateBossPrefab();
            Debug.Log("[GeometryTD] Prefabs created.");
        }

        [MenuItem("GeometryTD/2. Create Scenes Only", false, 21)]
        public static void CreateScenesOnly()
        {
            CreateDirectories();
            CreateMainMenuScene();
            CreateBattleScene();
            AddScenesToBuildSettings();
            Debug.Log("[GeometryTD] Scenes created.");
        }

        private static void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder(SCENE_PATH))
                Directory.CreateDirectory(SCENE_PATH);
            if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
                Directory.CreateDirectory(PREFAB_PATH);
            AssetDatabase.Refresh();
        }

        // ===================== PREFABS =====================

        private static void CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("Bullet");

            var sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(8);
            sr.color = new Color(0.2f, 0.8f, 1f, 1f);
            sr.sortingOrder = 5;
            bullet.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            var trail = bullet.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(0.2f, 0.8f, 1f, 1f);
            trail.endColor = new Color(0.2f, 0.8f, 1f, 0f);
            trail.sortingOrder = 4;
            trail.numCornerVertices = 3;
            trail.numCapVertices = 3;

            var col = bullet.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            col.isTrigger = true;

            var rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var bc = bullet.AddComponent<BulletController>();
            // Wire trail via SerializedObject
            SerializedObject so = new SerializedObject(bc);
            so.FindProperty("trailRenderer").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = PREFAB_PATH + "/Bullet.prefab";
            PrefabUtility.SaveAsPrefabAsset(bullet, path);
            Object.DestroyImmediate(bullet);
        }

        private static void CreateEnemyPrefab()
        {
            GameObject enemy = new GameObject("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDiamondSprite();
            sr.color = new Color(1f, 0.3f, 0.5f, 1f);
            sr.sortingOrder = 3;
            enemy.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            enemy.AddComponent<EnemyController>();

            // Create health bar above enemy
            CreateWorldSpaceHealthBar(enemy, false, 0.8f);

            string path = PREFAB_PATH + "/Enemy.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, path);
            Object.DestroyImmediate(enemy);

            // Wire health bar reference
            WireEnemyHealthBar(prefab);
        }

        private static void CreateBossPrefab()
        {
            GameObject boss = new GameObject("Boss");

            var sr = boss.AddComponent<SpriteRenderer>();
            sr.sprite = CreateHexagonSprite();
            sr.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            sr.sortingOrder = 3;
            boss.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var col = boss.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;
            col.isTrigger = true;

            var rb = boss.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Fire point
            GameObject firePointGo = new GameObject("FirePoint");
            firePointGo.transform.SetParent(boss.transform);
            firePointGo.transform.localPosition = new Vector3(-0.8f, 0, 0);

            var bossCtrl = boss.AddComponent<BossController>();
            SerializedObject so = new SerializedObject(bossCtrl);
            so.FindProperty("firePoint").objectReferenceValue = firePointGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = PREFAB_PATH + "/Boss.prefab";
            PrefabUtility.SaveAsPrefabAsset(boss, path);
            Object.DestroyImmediate(boss);
        }

        private static void WireEnemyHealthBar(GameObject prefab)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            var enemyCtrl = instance.GetComponent<EnemyController>();
            var healthBar = instance.GetComponentInChildren<HealthBarUI>();

            if (enemyCtrl != null && healthBar != null)
            {
                SerializedObject so = new SerializedObject(enemyCtrl);
                so.FindProperty("healthBar").objectReferenceValue = healthBar;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH + "/Enemy.prefab");
            Object.DestroyImmediate(instance);
        }

        // ===================== SCENES =====================

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera setup
            Camera.main.orthographic = true;
            Camera.main.orthographicSize = 5;
            Camera.main.backgroundColor = new Color(0.05f, 0.02f, 0.1f);

            // GameManager
            GameObject gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            // Canvas
            GameObject canvas = CreateCanvas("MainMenuCanvas");

            // Background
            GameObject bg = CreateUIImage(canvas, "Background", Vector2.zero,
                new Vector2(1920, 1080), new Color(0.08f, 0.04f, 0.15f, 1f));
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Load background image if available
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Background_Images/medium/home-background-medium.png");
            if (bgSprite != null)
            {
                bg.GetComponent<Image>().sprite = bgSprite;
                bg.GetComponent<Image>().color = Color.white;
            }

            // Title
            GameObject title = CreateUIText(canvas, "Title", new Vector2(0, 150),
                "GEOMETRY DEFENSE", 48, Color.white, TextAnchor.MiddleCenter);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(800, 100);

            // Subtitle
            CreateUIText(canvas, "Subtitle", new Vector2(0, 80),
                "A 2D Tower Defense Game", 24, new Color(0.7f, 0.5f, 1f), TextAnchor.MiddleCenter);

            // Start Button
            GameObject startBtn = CreateUIButton(canvas, "StartButton",
                new Vector2(0, -50), new Vector2(300, 70), "START GAME",
                new Color(0.3f, 0.6f, 1f, 1f));

            // Quit Button
            GameObject quitBtn = CreateUIButton(canvas, "QuitButton",
                new Vector2(0, -150), new Vector2(300, 70), "QUIT",
                new Color(0.8f, 0.3f, 0.4f, 1f));

            // MainMenuUI component
            var menuUI = canvas.AddComponent<MainMenuUI>();

            // Wire button events
            var startButton = startBtn.GetComponent<Button>();
            var quitButton = quitBtn.GetComponent<Button>();

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                startButton.onClick, menuUI.OnStartGameClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                quitButton.onClick, menuUI.OnQuitClicked);

            // Event System
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "/MainMenuScene.unity");
        }

        private static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera.main.orthographic = true;
            Camera.main.orthographicSize = 5;
            Camera.main.backgroundColor = new Color(0.03f, 0.01f, 0.08f);

            // Battle Initializer
            GameObject initGo = new GameObject("BattleInitializer");
            initGo.AddComponent<BattleInitializer>();

            // Enemy Manager
            GameObject emGo = new GameObject("EnemyManager");
            emGo.AddComponent<EnemyManager>();

            // Bullet Pool
            GameObject bulletPoolGo = new GameObject("BulletPool");
            var bulletPool = bulletPoolGo.AddComponent<BulletPool>();

            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + "/Bullet.prefab");
            if (bulletPrefab != null)
            {
                SerializedObject bpSo = new SerializedObject(bulletPool);
                bpSo.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
                bpSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Player
            GameObject player = CreatePlayer();

            // Spawn Manager
            GameObject spawnGo = new GameObject("SpawnManager");
            var spawnMgr = spawnGo.AddComponent<SpawnManager>();
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + "/Enemy.prefab");
            {
                SerializedObject smSo = new SerializedObject(spawnMgr);
                smSo.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
                smSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
                smSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Boss Spawner
            GameObject bossSpawnGo = new GameObject("BossSpawner");
            var bossSpawner = bossSpawnGo.AddComponent<BossSpawner>();
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + "/Boss.prefab");
            {
                SerializedObject bsSo = new SerializedObject(bossSpawner);
                bsSo.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
                bsSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
                bsSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Canvas - Battle UI
            GameObject canvas = CreateCanvas("BattleCanvas");

            // --- Kill Progress Bar (Top) ---
            GameObject killProgressGroup = new GameObject("KillProgressGroup");
            killProgressGroup.transform.SetParent(canvas.transform, false);
            var kpRect = killProgressGroup.AddComponent<RectTransform>();
            kpRect.anchorMin = new Vector2(0.2f, 1f);
            kpRect.anchorMax = new Vector2(0.8f, 1f);
            kpRect.pivot = new Vector2(0.5f, 1f);
            kpRect.anchoredPosition = new Vector2(0, -20);
            kpRect.sizeDelta = new Vector2(0, 40);

            // Progress Label
            CreateUIText(killProgressGroup, "KillLabel", new Vector2(0, 20),
                "Kill Progress", 16, Color.white, TextAnchor.MiddleCenter);

            // Progress bar background
            GameObject killBarBg = CreateUIImage(killProgressGroup, "KillBarBg",
                Vector2.zero, new Vector2(0, 25), new Color(0.15f, 0.1f, 0.25f, 0.9f));
            var killBarBgRect = killBarBg.GetComponent<RectTransform>();
            killBarBgRect.anchorMin = new Vector2(0, 0.5f);
            killBarBgRect.anchorMax = new Vector2(1, 0.5f);
            killBarBgRect.sizeDelta = new Vector2(0, 25);

            // Progress bar fill
            GameObject killBarFill = CreateUIImage(killProgressGroup, "KillBarFill",
                Vector2.zero, new Vector2(0, 25), new Color(0.3f, 0.8f, 0.3f, 1f));
            var killBarFillRect = killBarFill.GetComponent<RectTransform>();
            killBarFillRect.anchorMin = new Vector2(0, 0.5f);
            killBarFillRect.anchorMax = new Vector2(1, 0.5f);
            killBarFillRect.sizeDelta = new Vector2(0, 25);
            killBarFillRect.pivot = new Vector2(0, 0.5f);
            var killFillImage = killBarFill.GetComponent<Image>();
            killFillImage.type = Image.Type.Filled;
            killFillImage.fillMethod = Image.FillMethod.Horizontal;

            // Kill progress text
            GameObject killText = CreateUIText(killProgressGroup, "KillText",
                Vector2.zero, "0 / 100", 14, Color.white, TextAnchor.MiddleCenter);
            var killTextRect = killText.GetComponent<RectTransform>();
            killTextRect.anchorMin = new Vector2(0, 0.5f);
            killTextRect.anchorMax = new Vector2(1, 0.5f);
            killTextRect.sizeDelta = new Vector2(0, 25);

            // --- Boss Health Group (hidden by default) ---
            GameObject bossHealthGroup = new GameObject("BossHealthGroup");
            bossHealthGroup.transform.SetParent(canvas.transform, false);
            var bhRect = bossHealthGroup.AddComponent<RectTransform>();
            bhRect.anchorMin = new Vector2(0.15f, 1f);
            bhRect.anchorMax = new Vector2(0.85f, 1f);
            bhRect.pivot = new Vector2(0.5f, 1f);
            bhRect.anchoredPosition = new Vector2(0, -20);
            bhRect.sizeDelta = new Vector2(0, 40);

            CreateUIText(bossHealthGroup, "BossLabel", new Vector2(0, 20),
                "BOSS", 18, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);

            GameObject bossBarBg = CreateUIImage(bossHealthGroup, "BossBarBg",
                Vector2.zero, new Vector2(0, 30), new Color(0.2f, 0.05f, 0.05f, 0.9f));
            var bossBarBgRect = bossBarBg.GetComponent<RectTransform>();
            bossBarBgRect.anchorMin = new Vector2(0, 0.5f);
            bossBarBgRect.anchorMax = new Vector2(1, 0.5f);
            bossBarBgRect.sizeDelta = new Vector2(0, 30);

            GameObject bossBarFill = CreateUIImage(bossHealthGroup, "BossBarFill",
                Vector2.zero, new Vector2(0, 30), new Color(1f, 0.2f, 0.2f, 1f));
            var bossBarFillRect = bossBarFill.GetComponent<RectTransform>();
            bossBarFillRect.anchorMin = new Vector2(0, 0.5f);
            bossBarFillRect.anchorMax = new Vector2(1, 0.5f);
            bossBarFillRect.sizeDelta = new Vector2(0, 30);
            bossBarFillRect.pivot = new Vector2(0, 0.5f);
            var bossFillImage = bossBarFill.GetComponent<Image>();
            bossFillImage.type = Image.Type.Filled;
            bossFillImage.fillMethod = Image.FillMethod.Horizontal;

            GameObject bossHpText = CreateUIText(bossHealthGroup, "BossHpText",
                Vector2.zero, "2000 / 2000", 16, Color.white, TextAnchor.MiddleCenter);
            var bossHpTextRect = bossHpText.GetComponent<RectTransform>();
            bossHpTextRect.anchorMin = new Vector2(0, 0.5f);
            bossHpTextRect.anchorMax = new Vector2(1, 0.5f);
            bossHpTextRect.sizeDelta = new Vector2(0, 30);

            bossHealthGroup.SetActive(false);

            // --- Result Panel ---
            GameObject resultPanel = new GameObject("ResultPanel");
            resultPanel.transform.SetParent(canvas.transform, false);
            var rpRect = resultPanel.AddComponent<RectTransform>();
            rpRect.anchorMin = Vector2.zero;
            rpRect.anchorMax = Vector2.one;
            rpRect.offsetMin = Vector2.zero;
            rpRect.offsetMax = Vector2.zero;

            // Semi-transparent overlay
            var overlayImage = resultPanel.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);

            // Container
            Sprite containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Containers/Medium/victory-defeat-container-medium.png");
            GameObject container = CreateUIImage(resultPanel, "Container",
                Vector2.zero, new Vector2(500, 350),
                containerSprite != null ? Color.white : new Color(0.1f, 0.05f, 0.2f, 0.95f));
            if (containerSprite != null)
                container.GetComponent<Image>().sprite = containerSprite;

            GameObject resultTitle = CreateUIText(container, "ResultTitle",
                new Vector2(0, 80), "VICTORY", 42, Color.white, TextAnchor.MiddleCenter);
            var rtRect = resultTitle.GetComponent<RectTransform>();
            rtRect.sizeDelta = new Vector2(400, 60);

            GameObject resultDesc = CreateUIText(container, "ResultDesc",
                new Vector2(0, 10), "Boss has been defeated!", 22,
                new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);
            var rdRect = resultDesc.GetComponent<RectTransform>();
            rdRect.sizeDelta = new Vector2(400, 40);

            GameObject returnBtn = CreateUIButton(container, "ReturnButton",
                new Vector2(0, -80), new Vector2(250, 60), "RETURN TO MENU",
                new Color(0.3f, 0.6f, 1f, 1f));

            resultPanel.SetActive(false);

            // --- BattleUI Component ---
            var battleUI = canvas.AddComponent<BattleUI>();
            SerializedObject buiSo = new SerializedObject(battleUI);
            buiSo.FindProperty("killProgressFill").objectReferenceValue = killFillImage;
            buiSo.FindProperty("killProgressText").objectReferenceValue = killText.GetComponent<Text>();
            buiSo.FindProperty("bossHealthGroup").objectReferenceValue = bossHealthGroup;
            buiSo.FindProperty("bossHpFill").objectReferenceValue = bossFillImage;
            buiSo.FindProperty("bossHpText").objectReferenceValue = bossHpText.GetComponent<Text>();
            buiSo.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            buiSo.FindProperty("resultTitleText").objectReferenceValue = resultTitle.GetComponent<Text>();
            buiSo.FindProperty("resultDescText").objectReferenceValue = resultDesc.GetComponent<Text>();
            buiSo.FindProperty("returnButton").objectReferenceValue = returnBtn.GetComponent<Button>();
            buiSo.FindProperty("bossSpawner").objectReferenceValue = bossSpawner;
            buiSo.ApplyModifiedPropertiesWithoutUndo();

            // Wire return button event
            var returnButton = returnBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                returnButton.onClick, battleUI.OnReturnClicked);

            // Event System
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Background starfield effect (simple)
            CreateBattleBackground(scene);

            EditorSceneManager.SaveScene(scene, SCENE_PATH + "/BattleScene.unity");
        }

        // ===================== PLAYER CREATION =====================

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(-6f, 0, 0);

            // Main body - triangle shape
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTriangleSprite();
            sr.color = new Color(0.2f, 0.7f, 1f, 1f);
            sr.sortingOrder = 3;
            player.transform.localScale = new Vector3(1f, 1f, 1f);

            var col = player.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Fire point
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform);
            firePoint.transform.localPosition = new Vector3(0.7f, 0, 0);

            var playerCtrl = player.AddComponent<PlayerController>();

            // Player health bar (world space canvas above player)
            CreateWorldSpaceHealthBar(player, true, 1.2f);

            // Wire references
            SerializedObject so = new SerializedObject(playerCtrl);
            so.FindProperty("firePoint").objectReferenceValue = firePoint.transform;

            var healthBar = player.GetComponentInChildren<HealthBarUI>();
            if (healthBar != null)
            {
                so.FindProperty("healthBar").objectReferenceValue = healthBar;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        // ===================== WORLD SPACE HEALTH BAR =====================

        private static void CreateWorldSpaceHealthBar(GameObject parent, bool isDual, float yOffset)
        {
            GameObject canvasGo = new GameObject("HealthBarCanvas");
            canvasGo.transform.SetParent(parent.transform, false);
            canvasGo.transform.localPosition = new Vector3(0, yOffset, 0);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 30);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            var healthBarUI = canvasGo.AddComponent<HealthBarUI>();

            if (isDual)
            {
                // Shield bar
                GameObject shieldBg = CreateUIImage(canvasGo, "ShieldBarBg",
                    new Vector2(0, 8), new Vector2(100, 10),
                    new Color(0.15f, 0.15f, 0.3f, 0.8f));

                GameObject shieldFill = CreateUIImage(canvasGo, "ShieldBarFill",
                    new Vector2(0, 8), new Vector2(100, 10),
                    new Color(0.3f, 0.6f, 1f, 1f));
                var sfImg = shieldFill.GetComponent<Image>();
                sfImg.type = Image.Type.Filled;
                sfImg.fillMethod = Image.FillMethod.Horizontal;

                // HP bar
                GameObject hpBg = CreateUIImage(canvasGo, "HpBarBg",
                    new Vector2(0, -4), new Vector2(100, 10),
                    new Color(0.3f, 0.1f, 0.1f, 0.8f));

                GameObject hpFill = CreateUIImage(canvasGo, "HpBarFill",
                    new Vector2(0, -4), new Vector2(100, 10),
                    new Color(0.2f, 0.9f, 0.2f, 1f));
                var hfImg = hpFill.GetComponent<Image>();
                hfImg.type = Image.Type.Filled;
                hfImg.fillMethod = Image.FillMethod.Horizontal;

                SerializedObject so = new SerializedObject(healthBarUI);
                so.FindProperty("shieldFill").objectReferenceValue = sfImg;
                so.FindProperty("hpFillDual").objectReferenceValue = hfImg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                // Single HP bar
                GameObject hpBg = CreateUIImage(canvasGo, "HpBarBg",
                    Vector2.zero, new Vector2(80, 8),
                    new Color(0.3f, 0.1f, 0.1f, 0.8f));

                GameObject hpFill = CreateUIImage(canvasGo, "HpBarFill",
                    Vector2.zero, new Vector2(80, 8),
                    new Color(1f, 0.3f, 0.3f, 1f));
                var hfImg = hpFill.GetComponent<Image>();
                hfImg.type = Image.Type.Filled;
                hfImg.fillMethod = Image.FillMethod.Horizontal;

                SerializedObject so = new SerializedObject(healthBarUI);
                so.FindProperty("hpFill").objectReferenceValue = hfImg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ===================== BACKGROUND =====================

        private static void CreateBattleBackground(Scene scene)
        {
            // Use existing background sprite if available
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Background_Images/medium/background-1-medium.png");

            GameObject bg = new GameObject("Background");
            var sr = bg.AddComponent<SpriteRenderer>();
            if (bgSprite != null)
            {
                sr.sprite = bgSprite;
                sr.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            else
            {
                sr.color = new Color(0.03f, 0.01f, 0.08f);
            }
            sr.sortingOrder = -10;
            bg.transform.localScale = new Vector3(2f, 2f, 1f);
        }

        // ===================== UI HELPERS =====================

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            return canvasGo;
        }

        private static GameObject CreateUIImage(GameObject parent, string name,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private static GameObject CreateUIText(GameObject parent, string name,
            Vector2 position, string text, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 50);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return go;
        }

        private static GameObject CreateUIButton(GameObject parent, string name,
            Vector2 position, Vector2 size, string label, Color color)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent.transform, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var img = btnGo.AddComponent<Image>();
            img.color = color;

            // Try to use existing button sprite
            Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/ui/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/medium/large-blue-medium.png");
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;

            // Label text
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);

            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var txt = labelGo.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btnGo;
        }

        // ===================== SPRITE GENERATION =====================

        private static Sprite CreateCircleSprite(int radius)
        {
            int size = radius * 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius - 1)
                        pixels[y * size + x] = Color.white;
                    else if (dist < radius)
                        pixels[y * size + x] = new Color(1, 1, 1, radius - dist);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string path = PREFAB_PATH + "/Sprites";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string assetPath = path + "/circle_sprite.png";
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite CreateTriangleSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw a right-pointing triangle
            Vector2 p0 = new Vector2(size - 4, size / 2);     // right tip
            Vector2 p1 = new Vector2(4, size - 4);             // top-left
            Vector2 p2 = new Vector2(4, 4);                     // bottom-left

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (PointInTriangle(new Vector2(x, y), p0, p1, p2))
                        pixels[y * size + x] = Color.white;
                }
            }

            // Draw border
            DrawLine(tex, pixels, size, p0, p1, Color.white, 2);
            DrawLine(tex, pixels, size, p1, p2, Color.white, 2);
            DrawLine(tex, pixels, size, p2, p0, Color.white, 2);

            tex.SetPixels(pixels);
            tex.Apply();

            string path = PREFAB_PATH + "/Sprites";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string assetPath = path + "/triangle_sprite.png";
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite CreateDiamondSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - half);
                    int dy = Mathf.Abs(y - half);
                    if (dx + dy < half - 2)
                        pixels[y * size + x] = Color.white;
                    else if (dx + dy < half)
                        pixels[y * size + x] = new Color(1, 1, 1, 0.8f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string path = PREFAB_PATH + "/Sprites";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string assetPath = path + "/diamond_sprite.png";
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite CreateHexagonSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 3;

            // 6 vertices of hexagon
            Vector2[] verts = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (PointInPolygon(new Vector2(x, y), verts))
                        pixels[y * size + x] = Color.white;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string path = PREFAB_PATH + "/Sprites";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string assetPath = path + "/hexagon_sprite.png";
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // ===================== GEOMETRY HELPERS =====================

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static void DrawLine(Texture2D tex, Color[] pixels, int size,
            Vector2 from, Vector2 to, Color color, int thickness)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to));
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                Vector2 p = Vector2.Lerp(from, to, t);
                for (int dx = -thickness; dx <= thickness; dx++)
                {
                    for (int dy = -thickness; dy <= thickness; dy++)
                    {
                        int px = Mathf.Clamp((int)p.x + dx, 0, size - 1);
                        int py = Mathf.Clamp((int)p.y + dy, 0, size - 1);
                        pixels[py * size + px] = color;
                    }
                }
            }
        }

        // ===================== BUILD SETTINGS =====================

        private static void AddScenesToBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(SCENE_PATH + "/MainMenuScene.unity", true),
                new EditorBuildSettingsScene(SCENE_PATH + "/BattleScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[GeometryTD] Build settings updated with MainMenuScene and BattleScene.");
        }
    }
}
