#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 预制体一键生成工具
    /// 菜单：Tools/几何塔防/★ 一键生成所有预制体
    ///
    /// 每类对象只生成一个通用预制体：
    ///   - Enemy.prefab      → 所有敌人共用，差异由 enemies.json 配置驱动
    ///   - Bullet.prefab     → 所有子弹共用，差异由 bullets.json 配置驱动
    ///   - Player.prefab     → 玩家
    ///   - SkillSlot.prefab  → 所有技能槽共用，差异由 skills.json 配置驱动
    ///   - PrefabRef.asset   → 引用配置（放 Resources 目录）
    /// </summary>
    public static class PrefabGenerator
    {
        private const string PREFAB_DIR   = "Assets/prefab";
        private const string RESOURCE_DIR = "Assets/Resources";

        [MenuItem("Tools/几何塔防/★ 一键生成所有预制体", false, 10)]
        public static void GenerateAllPrefabs()
        {
            ConfigLoader.LoadAllConfigs();

            EnsureDir(PREFAB_DIR);
            EnsureDir(RESOURCE_DIR);

            int count = 0;
            count += GenerateEnemyPrefab();
            count += GenerateBulletPrefab();
            count += GeneratePlayerPrefab();
            count += GenerateSkillSlotPrefab();
            PrefabRef pref = GeneratePrefabRef();

            AssetDatabase.Refresh();
            if (pref != null)
            {
                Selection.activeObject = pref;
                EditorGUIUtility.PingObject(pref);
            }

            Debug.Log($"[PrefabGenerator] 完成！共生成/更新 {count} 个预制体 + PrefabRef.asset");
            Debug.Log("▶ 在 Project/prefab 目录找到所有预制体，双击编辑！");
            Debug.Log("▶ 预制体结构/组件决定外观框架，JSON 配置决定每种怪/子弹/技能的数值差异。");
        }

        // ════════════════════════════════════════════════════════════════════
        // 敌人预制体（通用模板，配置驱动差异）
        // ════════════════════════════════════════════════════════════════════
        private static int GenerateEnemyPrefab()
        {
            string path = $"{PREFAB_DIR}/Enemy.prefab";
            if (File.Exists(path))
            {
                Debug.Log($"[PrefabGenerator] 跳过（已存在）：{path}");
                return 0;
            }

            // 用第一个敌人配置作为模板外观
            var cfgs = ConfigLoader.GameConfig?.enemies?.enemies;
            var cfg  = (cfgs != null && cfgs.Count > 0) ? cfgs[0] : null;

            Color col   = cfg != null ? ConfigLoader.ParseColor(cfg.color) : Color.red;
            string shape = cfg?.shape ?? "circle";
            float  scale = cfg?.scale ?? 0.6f;

            var root = new GameObject("Enemy");
            root.tag = "Enemy";

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(shape, scale, col);
            sr.color        = col;
            sr.sortingOrder = 3;

            root.AddComponent<EnemyController>();

            // 头顶血条
            BuildEnemyHealthBar(root.transform, scale, cfg?.shield > 0);

            SavePrefab(root, path);
            return 1;
        }

        private static void BuildEnemyHealthBar(Transform parent, float scale, bool hasShield)
        {
            float barH   = 10f;
            float gap    = 2f;
            float totalH = hasShield ? barH * 2 + gap : barH;
            float offsetY = scale * 0.9f + 0.3f;

            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(parent);
            hbObj.transform.localPosition = new Vector3(0, offsetY, 0);

            var canvas = hbObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            hbObj.AddComponent<CanvasScaler>();

            var cr = hbObj.GetComponent<RectTransform>();
            cr.sizeDelta  = new Vector2(120f, totalH);
            cr.localScale = Vector3.one * 0.01f;

            float hpY = hasShield ? (barH + gap) * 0.5f : 0f;
            CreateBarRow("HealthRow", hbObj.transform, barH, hpY,
                new Color(0.15f, 0.08f, 0.08f, 0.9f), "HealthFill", new Color(0.9f, 0.2f, 0.15f));

            if (hasShield)
                CreateBarRow("ShieldRow", hbObj.transform, barH, -(barH + gap) * 0.5f,
                    new Color(0.08f, 0.12f, 0.2f, 0.9f), "ShieldFill", new Color(0.3f, 0.55f, 1f));

            hbObj.AddComponent<HealthBarController>();
        }

        // ════════════════════════════════════════════════════════════════════
        // 子弹预制体（通用模板）
        // ════════════════════════════════════════════════════════════════════
        private static int GenerateBulletPrefab()
        {
            string path = $"{PREFAB_DIR}/Bullet.prefab";
            if (File.Exists(path))
            {
                Debug.Log($"[PrefabGenerator] 跳过（已存在）：{path}");
                return 0;
            }

            var cfgs = ConfigLoader.GameConfig?.bullets?.bullets;
            var cfg  = (cfgs != null && cfgs.Count > 0) ? cfgs[0] : null;

            Color  col   = cfg != null ? ConfigLoader.ParseColor(cfg.color) : Color.yellow;
            string shape = cfg?.shape ?? "circle";
            float  scale = cfg?.scale ?? 0.25f;

            var root = new GameObject("Bullet");
            root.tag = "Bullet";

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(shape, scale, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            // 尾迹（可在预制体上开关）
            var tr = root.AddComponent<TrailRenderer>();
            tr.time         = 0.12f;
            tr.startWidth   = scale * 0.3f;
            tr.endWidth     = 0f;
            tr.material     = GeometryMeshGenerator.CreateMaterial(new Color(1f, 0.9f, 0.4f, 0.6f));
            tr.sortingOrder = 4;
            tr.enabled      = false; // 默认关闭，由 BulletController 按配置开启

            root.AddComponent<BulletController>();

            SavePrefab(root, path);
            return 1;
        }

        // ════════════════════════════════════════════════════════════════════
        // 玩家预制体
        // ════════════════════════════════════════════════════════════════════
        private static int GeneratePlayerPrefab()
        {
            string path = $"{PREFAB_DIR}/Player.prefab";
            if (File.Exists(path))
            {
                Debug.Log($"[PrefabGenerator] 跳过（已存在）：{path}");
                return 0;
            }

            ConfigLoader.LoadAllConfigs();
            var cfg   = ConfigLoader.GameConfig.player;
            Color col = ConfigLoader.ParseColor(cfg.color);

            var root = new GameObject("Player");
            root.tag = "Player";

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite       = GeometryMeshGenerator.CreateSprite(cfg.shape, cfg.scale, col);
            sr.color        = col;
            sr.sortingOrder = 5;

            BuildPlayerHealthBar(root.transform, cfg);
            root.AddComponent<PlayerController>();

            SavePrefab(root, path);
            return 1;
        }

        private static void BuildPlayerHealthBar(Transform parent, PlayerConfig cfg)
        {
            bool  hasShield = cfg.shield > 0;
            float barH      = 18f;
            float gap       = 4f;
            float totalH    = hasShield ? barH * 2 + gap : barH;
            float offsetY   = cfg.scale * 0.9f + 0.4f;

            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(parent);
            hbObj.transform.localPosition = new Vector3(0, offsetY, 0);

            var canvas = hbObj.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            hbObj.AddComponent<CanvasScaler>();

            var cr = hbObj.GetComponent<RectTransform>();
            cr.sizeDelta  = new Vector2(180f, totalH);
            cr.localScale = Vector3.one * 0.01f;

            float hpY = hasShield ? (barH + gap) * 0.5f : 0f;
            CreateBarRow("HealthRow", hbObj.transform, barH, hpY,
                new Color(0.15f, 0.08f, 0.08f, 0.9f), "HealthFill", new Color(0.9f, 0.2f, 0.15f),
                "HpText");

            if (hasShield)
                CreateBarRow("ShieldRow", hbObj.transform, barH, -(barH + gap) * 0.5f,
                    new Color(0.08f, 0.12f, 0.2f, 0.9f), "ShieldFill", new Color(0.3f, 0.55f, 1f),
                    "SpText");

            hbObj.AddComponent<HealthBarController>();
        }

        // ════════════════════════════════════════════════════════════════════
        // 技能槽预制体（通用模板，每个技能槽共用同一结构）
        // ════════════════════════════════════════════════════════════════════
        private static int GenerateSkillSlotPrefab()
        {
            string path = $"{PREFAB_DIR}/SkillSlot.prefab";
            if (File.Exists(path))
            {
                Debug.Log($"[PrefabGenerator] 跳过（已存在）：{path}");
                return 0;
            }

            const float SLOT_W = 110f;
            const float SLOT_H = 140f;
            const float ICON_H = 80f;
            const float EXP_H  = 8f;
            const float LV_H   = 20f;
            const float PAD    = 8f;

            var slot = new GameObject("SkillSlot");
            var rt   = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(SLOT_W, SLOT_H);

            var bg  = slot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.95f);

            var btn = slot.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor     = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.disabledColor    = new Color(0.42f, 0.42f, 0.42f, 0.7f);
            btn.colors       = cb;
            btn.targetGraphic = bg;

            // 图标区域
            var iconGo  = CreateUIChild(slot.transform, "Icon",
                new Vector2(SLOT_W - 8f, ICON_H), new Vector2(0.5f, 1f), new Vector2(0f, -PAD));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.3f, 0.6f, 1f);

            // 技能名
            var nameGo = CreateUIChild(iconGo.transform, "NameText",
                new Vector2(SLOT_W - 8f, 22f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.fontSize  = 11;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color     = Color.white;
            nameTxt.text      = "";

            // 冷却遮罩
            var cdMaskGo = CreateUIChild(iconGo.transform, "CooldownMask",
                new Vector2(SLOT_W - 8f, ICON_H), new Vector2(0.5f, 0.5f), Vector2.zero);
            var cdMask = cdMaskGo.AddComponent<Image>();
            cdMask.color         = new Color(0f, 0f, 0f, 0.75f);
            cdMask.type          = Image.Type.Filled;
            cdMask.fillMethod    = Image.FillMethod.Radial360;
            cdMask.fillOrigin    = (int)Image.Origin360.Top;
            cdMask.fillClockwise = true;
            cdMask.fillAmount    = 0f;
            cdMaskGo.SetActive(false);

            // 冷却数字
            var cdTxtGo = CreateUIChild(cdMaskGo.transform, "CooldownText",
                new Vector2(SLOT_W - 8f, 28f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var cdTxt = cdTxtGo.AddComponent<Text>();
            cdTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdTxt.fontSize  = 20;
            cdTxt.fontStyle = FontStyle.Bold;
            cdTxt.alignment = TextAnchor.MiddleCenter;
            cdTxt.color     = Color.white;

            // 经验条背景
            float expOffY = -(PAD + ICON_H + 4f);
            var expBgGo = CreateUIChild(slot.transform, "ExpBg",
                new Vector2(SLOT_W - 8f, EXP_H), new Vector2(0.5f, 1f), new Vector2(0f, expOffY));
            expBgGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);

            // 经验条填充
            var expFillGo = new GameObject("ExpFill");
            expFillGo.transform.SetParent(expBgGo.transform, false);
            expFillGo.transform.localScale = Vector3.one;
            var efRt = expFillGo.AddComponent<RectTransform>();
            efRt.anchorMin = Vector2.zero;
            efRt.anchorMax = Vector2.one;
            efRt.offsetMin = efRt.offsetMax = Vector2.zero;
            var expFill = expFillGo.AddComponent<Image>();
            expFill.color      = new Color(0.3f, 0.85f, 1f);
            expFill.type       = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillAmount = 0f;

            // 等级文字
            var lvGo = CreateUIChild(slot.transform, "LevelText",
                new Vector2(SLOT_W, LV_H), new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            var lvTxt = lvGo.AddComponent<Text>();
            lvTxt.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lvTxt.fontSize        = 13;
            lvTxt.fontStyle       = FontStyle.Bold;
            lvTxt.alignment       = TextAnchor.MiddleCenter;
            lvTxt.color           = new Color(0.85f, 0.85f, 1f);
            lvTxt.supportRichText = true;

            // 绑定 SkillSlotUI
            var slotUI          = slot.AddComponent<SkillSlotUI>();
            slotUI.button       = btn;
            slotUI.iconImage    = iconImg;
            slotUI.nameText     = nameTxt;
            slotUI.cooldownMask = cdMask;
            slotUI.cooldownText = cdTxt;
            slotUI.expBgImage   = expBgGo.GetComponent<Image>();
            slotUI.expFillImage = expFill;
            slotUI.levelText    = lvTxt;

            SavePrefab(slot, path);
            return 1;
        }

        // ════════════════════════════════════════════════════════════════════
        // 生成 PrefabRef.asset
        // ════════════════════════════════════════════════════════════════════
        private static PrefabRef GeneratePrefabRef()
        {
            string assetPath = $"{RESOURCE_DIR}/PrefabRef.asset";
            PrefabRef pref = AssetDatabase.LoadAssetAtPath<PrefabRef>(assetPath);
            if (pref == null)
            {
                pref = ScriptableObject.CreateInstance<PrefabRef>();
                AssetDatabase.CreateAsset(pref, assetPath);
            }

            pref.enemyPrefab     = Load($"{PREFAB_DIR}/Enemy.prefab");
            pref.bulletPrefab    = Load($"{PREFAB_DIR}/Bullet.prefab");
            pref.playerPrefab    = Load($"{PREFAB_DIR}/Player.prefab");
            pref.skillSlotPrefab = Load($"{PREFAB_DIR}/SkillSlot.prefab");

            EditorUtility.SetDirty(pref);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PrefabGenerator] PrefabRef.asset 已保存到 {assetPath}");
            return pref;
        }

        private static GameObject Load(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        // ════════════════════════════════════════════════════════════════════
        // 通用辅助
        // ════════════════════════════════════════════════════════════════════
        private static void SavePrefab(GameObject go, string path)
        {
            if (Selection.activeGameObject == go)
                Selection.activeObject = null;

            bool ok;
            PrefabUtility.SaveAsPrefabAsset(go, path, out ok);
            Object.DestroyImmediate(go);
            if (ok) Debug.Log($"[PrefabGenerator] 已生成：{path}");
            else    Debug.LogError($"[PrefabGenerator] 生成失败：{path}");
        }

        private static void EnsureDir(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent     = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static GameObject CreateUIChild(Transform parent, string name,
            Vector2 size, Vector2 pivot, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            var rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = pivot;
            rt.anchorMax        = pivot;
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        private static void CreateBarRow(string rowName, Transform parent, float barH, float posY,
            Color bgColor, string fillName, Color fillColor, string valueName = null)
        {
            var row = new GameObject(rowName);
            row.transform.SetParent(parent);
            row.transform.localScale = Vector3.one;
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchorMin        = new Vector2(0, 0.5f);
            rowRt.anchorMax        = new Vector2(1, 0.5f);
            rowRt.pivot            = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta        = new Vector2(0, barH);
            rowRt.anchoredPosition = new Vector2(0, posY);
            row.AddComponent<Image>().color = bgColor;

            var fill = new GameObject(fillName);
            fill.transform.SetParent(row.transform);
            fill.transform.localScale = Vector3.one;
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var img = fill.AddComponent<Image>();
            img.color      = fillColor;
            img.type       = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            if (!string.IsNullOrEmpty(valueName))
            {
                var vGo = new GameObject(valueName);
                vGo.transform.SetParent(row.transform);
                vGo.transform.localScale = Vector3.one;
                var vRt = vGo.AddComponent<RectTransform>();
                vRt.anchorMin = Vector2.zero;
                vRt.anchorMax = Vector2.one;
                vRt.offsetMin = vRt.offsetMax = Vector2.zero;
                var t = vGo.AddComponent<Text>();
                t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize  = Mathf.RoundToInt(barH * 0.78f);
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.color     = new Color(1f, 1f, 1f, 0.95f);
                t.supportRichText = false;
            }
        }
    }
}
#endif
