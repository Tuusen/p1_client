#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 预制体生成器 - 编辑器工具，自动生成所需的预制体
    /// </summary>
    public static class PrefabGenerator
    {
        private static string PrefabPath => "Assets/prefab";
        private static string MaterialPath => "Assets/Materials";

        /// <summary>
        /// 生成所有预制体
        /// </summary>
        [MenuItem("Tools/几何塔防/生成所有预制体", false, 1)]
        public static void GenerateAllPrefabs()
        {
            Debug.Log("[PrefabGenerator] 开始生成所有预制体...");

            // 确保目录存在
            EnsureDirectoryExists(PrefabPath);
            EnsureDirectoryExists(MaterialPath);

            // 加载配置
            ConfigLoader.LoadAllConfigs();

            // 生成玩家预制体
            GeneratePlayerPrefab();

            // 生成子弹预制体
            GenerateBulletPrefabs();

            // 生成怪物预制体
            GenerateEnemyPrefabs();

            // 生成其他预制体
            GenerateHealthBarPrefab();
            GenerateSpawnerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PrefabGenerator] 所有预制体生成完成！");
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        /// <summary>
        /// 生成玩家预制体
        /// </summary>
        [MenuItem("Tools/几何塔防/生成玩家预制体", false, 11)]
        public static void GeneratePlayerPrefab()
        {
            Debug.Log("[PrefabGenerator] 生成玩家预制体...");

            var config = ConfigLoader.GameConfig.player;
            Color color = ConfigLoader.ParseColor(config.color);

            // 创建根对象
            GameObject player = new GameObject("Player");

            // 创建几何体
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = Vector3.zero;

            MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();

            Mesh mesh = GeometryMeshGenerator.CreateMeshByShape(config.shape, config.scale);
            mesh.name = "PlayerMesh";
            meshFilter.mesh = mesh;

            Material mat = GeometryMeshGenerator.CreateMaterial(color, "PlayerMaterial");
            meshRenderer.material = mat;

            // 添加碰撞器
            MeshCollider collider = visual.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;

            // 添加玩家控制器
            PlayerController playerCtrl = player.AddComponent<PlayerController>();

            // 添加血条
            GameObject healthBar = CreateHealthBarObject(true);
            healthBar.transform.SetParent(player.transform);
            healthBar.transform.localPosition = new Vector3(0, config.scale * 1.5f, 0);
            healthBar.name = "HealthBar";

            // 保存预制体
            SavePrefab(player, "Player.prefab");
        }

        /// <summary>
        /// 生成子弹预制体
        /// </summary>
        [MenuItem("Tools/几何塔防/生成子弹预制体", false, 12)]
        public static void GenerateBulletPrefabs()
        {
            Debug.Log("[PrefabGenerator] 生成子弹预制体...");

            var configs = ConfigLoader.GetAllBulletConfigs();

            foreach (var config in configs)
            {
                GenerateBulletPrefab(config);
            }
        }

        /// <summary>
        /// 生成单个子弹预制体
        /// </summary>
        private static void GenerateBulletPrefab(BulletConfig config)
        {
            Color color = ConfigLoader.ParseColor(config.color);

            // 创建根对象
            GameObject bullet = new GameObject($"Bullet_{config.id}");

            // 创建几何体
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(bullet.transform);
            visual.transform.localPosition = Vector3.zero;

            MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();

            Mesh mesh = GeometryMeshGenerator.CreateMeshByShape(config.shape, config.scale);
            mesh.name = $"BulletMesh_{config.id}";
            meshFilter.mesh = mesh;

            Material mat = GeometryMeshGenerator.CreateMaterial(color, $"BulletMat_{config.id}");
            meshRenderer.material = mat;

            // 添加碰撞器
            SphereCollider collider = bullet.AddComponent<SphereCollider>();
            collider.radius = config.scale * 0.5f;
            collider.isTrigger = true;

            // 添加刚体
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // 添加子弹控制器
            BulletController bulletCtrl = bullet.AddComponent<BulletController>();

            // 添加拖尾效果
            if (config.hasTrail)
            {
                TrailRenderer trail = visual.AddComponent<TrailRenderer>();
                trail.time = 0.2f;
                trail.startWidth = config.scale * 0.3f;
                trail.endWidth = 0f;
                trail.material = GeometryMeshGenerator.CreateMaterial(
                    ConfigLoader.ParseColor(config.trailColor),
                    $"TrailMat_{config.id}"
                );
            }

            // 保存预制体
            SavePrefab(bullet, $"Bullet_{config.id}.prefab");
        }

        /// <summary>
        /// 生成怪物预制体
        /// </summary>
        [MenuItem("Tools/几何塔防/生成怪物预制体", false, 13)]
        public static void GenerateEnemyPrefabs()
        {
            Debug.Log("[PrefabGenerator] 生成怪物预制体...");

            var configs = ConfigLoader.GetAllEnemyConfigs();

            foreach (var config in configs)
            {
                GenerateEnemyPrefab(config);
            }
        }

        /// <summary>
        /// 生成单个怪物预制体
        /// </summary>
        private static void GenerateEnemyPrefab(EnemyConfig config)
        {
            Color color = ConfigLoader.ParseColor(config.color);

            // 创建根对象
            GameObject enemy = new GameObject($"Enemy_{config.id}");

            // 创建几何体
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(enemy.transform);
            visual.transform.localPosition = Vector3.zero;

            MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();

            Mesh mesh = GeometryMeshGenerator.CreateMeshByShape(config.shape, config.scale);
            mesh.name = $"EnemyMesh_{config.id}";
            meshFilter.mesh = mesh;

            Material mat = GeometryMeshGenerator.CreateMaterial(color, $"EnemyMat_{config.id}");
            meshRenderer.material = mat;

            // 添加碰撞器
            MeshCollider collider = visual.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;

            // 添加怪物控制器
            EnemyController enemyCtrl = enemy.AddComponent<EnemyController>();

            // 添加血条（不显示数值）
            GameObject healthBar = CreateHealthBarObject(false);
            healthBar.transform.SetParent(enemy.transform);
            healthBar.transform.localPosition = new Vector3(0, config.scale * 1.2f, 0);
            healthBar.name = "HealthBar";

            // 保存预制体
            SavePrefab(enemy, $"Enemy_{config.id}.prefab");
        }

        /// <summary>
        /// 生成血条预制体
        /// </summary>
        [MenuItem("Tools/几何塔防/生成血条预制体", false, 14)]
        public static void GenerateHealthBarPrefab()
        {
            Debug.Log("[PrefabGenerator] 生成血条预制体...");

            GameObject healthBar = CreateHealthBarObject(true);
            SavePrefab(healthBar, "HealthBar.prefab");
        }

        /// <summary>
        /// 创建血条对象
        /// </summary>
        private static GameObject CreateHealthBarObject(bool showValue)
        {
            GameObject healthBar = new GameObject("HealthBar");

            // 添加画布
            Canvas canvas = healthBar.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            healthBar.AddComponent<CanvasScaler>();
            healthBar.AddComponent<Billboard>();

            // 设置画布大小
            RectTransform canvasRect = healthBar.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 0.6f);
            canvasRect.localScale = Vector3.one * 0.3f;

            // 创建背景
            GameObject background = new GameObject("Background");
            background.transform.SetParent(healthBar.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = Vector3.one;

            UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(2f, 0.25f);
            bgRect.anchoredPosition = Vector2.zero;

            // 创建血条
            GameObject healthBarFill = new GameObject("HealthFill");
            healthBarFill.transform.SetParent(healthBar.transform);
            healthBarFill.transform.localPosition = Vector3.zero;
            healthBarFill.transform.localScale = Vector3.one;

            UnityEngine.UI.Image healthImage = healthBarFill.AddComponent<UnityEngine.UI.Image>();
            healthImage.color = Color.red;
            RectTransform healthRect = healthBarFill.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0.5f);
            healthRect.anchorMax = new Vector2(1, 0.5f);
            healthRect.sizeDelta = new Vector2(0f, 0.2f);
            healthRect.anchoredPosition = Vector2.zero;

            // 创建盾条
            GameObject shieldBarFill = new GameObject("ShieldFill");
            shieldBarFill.transform.SetParent(healthBar.transform);
            shieldBarFill.transform.localPosition = Vector3.zero;
            shieldBarFill.transform.localScale = Vector3.one;

            UnityEngine.UI.Image shieldImage = shieldBarFill.AddComponent<UnityEngine.UI.Image>();
            shieldImage.color = new Color(0.3f, 0.5f, 1f); // 蓝色盾值
            RectTransform shieldRect = shieldBarFill.GetComponent<RectTransform>();
            shieldRect.anchorMin = new Vector2(0, 0.5f);
            shieldRect.anchorMax = new Vector2(1, 0.5f);
            shieldRect.sizeDelta = new Vector2(0f, 0.15f);
            shieldRect.anchoredPosition = new Vector2(0, -0.2f);

            // 创建数值文本（如果需要）
            if (showValue)
            {
                GameObject valueText = new GameObject("ValueText");
                valueText.transform.SetParent(healthBar.transform);
                valueText.transform.localPosition = Vector3.zero;
                valueText.transform.localScale = Vector3.one;

                UnityEngine.UI.Text text = valueText.AddComponent<UnityEngine.UI.Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.text = "100/100";

                RectTransform textRect = valueText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
            }

            // 添加HealthBarController
            HealthBarController controller = healthBar.AddComponent<HealthBarController>();

            return healthBar;
        }

        /// <summary>
        /// 生成生成器预制体
        /// </summary>
        private static void GenerateSpawnerPrefab()
        {
            Debug.Log("[PrefabGenerator] 生成怪物生成器预制体...");

            GameObject spawner = new GameObject("EnemySpawner");
            spawner.AddComponent<EnemySpawner>();

            SavePrefab(spawner, "EnemySpawner.prefab");
        }

        /// <summary>
        /// 保存预制体
        /// </summary>
        private static void SavePrefab(GameObject obj, string name)
        {
            string path = $"{PrefabPath}/{name}";

            // 检查是否已存在
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // 更新现有预制体
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path, InteractionMode.AutomatedAction);
            }
            else
            {
                // 创建新预制体
                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }

            Object.DestroyImmediate(obj);
            Debug.Log($"[PrefabGenerator] 保存预制体: {path}");
        }
    }
}
#endif
