using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 预制体引用配置中心 - ScriptableObject
    /// 保存路径：Assets/Resources/PrefabRef.asset（运行时 Resources.Load 加载）
    ///
    /// 【设计说明】
    /// - 每类游戏对象只需一个预制体，外观/数值差异通过读取 JSON 配置实现
    /// - 修改预制体（结构、组件）→ 保存 → 运行即全局生效
    /// - 修改 JSON 配置（颜色、大小、伤害）→ 运行时读取
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRef", menuName = "GeometryTowerDefense/PrefabRef", order = 1)]
    public class PrefabRef : ScriptableObject
    {
        private static PrefabRef _instance;

        /// <summary>全局单例，从 Resources/PrefabRef 加载</summary>
        public static PrefabRef Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<PrefabRef>("PrefabRef");
                return _instance;
            }
        }

        [Header("敌人预制体（一个通用模板，差异由 enemies.json 配置驱动）")]
        public GameObject enemyPrefab;

        [Header("子弹预制体（一个通用模板，差异由 bullets.json 配置驱动）")]
        public GameObject bulletPrefab;

        [Header("玩家预制体")]
        public GameObject playerPrefab;

        [Header("技能槽预制体（一个通用模板，差异由 skills.json 配置驱动）")]
        public GameObject skillSlotPrefab;

        // ── 快速查找接口（保持调用方不变）────────────────────────────────────

        /// <summary>获取敌人预制体（所有敌人共用一个模板）</summary>
        public GameObject GetEnemyPrefab(string enemyId) => enemyPrefab;

        /// <summary>获取子弹预制体（所有子弹共用一个模板）</summary>
        public GameObject GetBulletPrefab(string bulletId) => bulletPrefab;

        /// <summary>获取技能槽预制体（所有技能槽共用一个模板）</summary>
        public GameObject GetSkillSlotPrefab(int index) => skillSlotPrefab;
    }
}
