using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class BattleManager : MonoBehaviour
    {
        [Header("Prefab引用")]
        [SerializeField] private GameObject heroPrefab;
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject heroBulletPrefab;
        [SerializeField] private GameObject bossBulletPrefab;

        [Header("UI引用")]
        [SerializeField] private BattleUI battleUI;
        [SerializeField] private SkillBarUI skillBarUI;
        [SerializeField] private FloatingTextUI floatingTextUI;
        [SerializeField] private ArcaneBarUI arcaneBarUI;
        [SerializeField] private RuneBarUI runeBarUI;
        [SerializeField] private ArcaneActiveIconUI arcaneActiveIconUI;

        [Header("生成点")]
        [SerializeField] private Transform heroSpawnPoint;

        private MonsterSpawner monsterSpawner;
        private HeroController heroController;
        private BossController bossController;
        private SkillManager skillManager;
        private ArcaneManager arcaneManager;
        private EventEffectManager eventEffectManager;

        private List<Transform> aliveEnemies = new List<Transform>();
        private int killCount;
        private int killCountForBoss;
        private bool bossPhase;
        private bool gameEnded;
        private int skillXpMin;
        private int skillXpMax;

        public Transform HeroTransform => heroController != null ? heroController.transform : null;
        public ArcaneManager ArcaneManager => arcaneManager;
        public EventEffectManager EventEffectManager => eventEffectManager;

        public void ShowDamageText(Vector3 worldPos, float amount, bool isHeal)
        {
            if (floatingTextUI == null) return;
            int display = Mathf.RoundToInt(amount);
            if (display <= 0) return;
            string text = isHeal ? $"+{display}" : display.ToString();
            Color color = isHeal
                ? new Color(0.2f, 0.9f, 0.3f)
                : new Color(1f, 0.4f, 0.1f);
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0f);
            floatingTextUI.Show(text, worldPos + offset, color);
        }

        private void Start()
        {
            InitBattle();
        }

        private void InitBattle()
        {
            if (ConfigManager.Instance == null)
            {
                Debug.LogError("[BattleManager] ConfigManager未初始化");
                return;
            }

            GameConfig gameConfig = ConfigManager.Instance.GameConfig;
            HeroConfig heroConfig = ConfigManager.Instance.HeroConfig;

            killCountForBoss = gameConfig.kill_count_for_boss;
            killCount = 0;
            bossPhase = false;
            gameEnded = false;
            skillXpMin = heroConfig.skill_xp_min;
            skillXpMax = heroConfig.skill_xp_max;

            // 生成英雄
            Vector3 heroPos = heroSpawnPoint != null ? heroSpawnPoint.position : new Vector3(-6f, 0f, 0f);
            GameObject heroObj = Instantiate(heroPrefab, heroPos, Quaternion.identity);
            heroController = heroObj.GetComponent<HeroController>();
            heroController.Init(heroConfig, this);

            // 初始化事件特效管理器
            eventEffectManager = GetComponent<EventEffectManager>();
            if (eventEffectManager == null)
                eventEffectManager = gameObject.AddComponent<EventEffectManager>();

            // 初始化怪物生成器
            monsterSpawner = GetComponent<MonsterSpawner>();
            if (monsterSpawner == null)
            {
                monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
            }
            List<MonsterConfig> normalConfigs = ConfigManager.Instance.GetNormalMonsterConfigs();
            monsterSpawner.Init(this, gameConfig.monster_spawn_interval, normalConfigs);

            // 初始化技能管理器
            if (gameConfig.skill_slot_ids != null && gameConfig.skill_slot_ids.Length > 0)
            {
                skillManager = gameObject.AddComponent<SkillManager>();
                skillManager.Init(gameConfig.skill_slot_ids, heroController, this, floatingTextUI);

                if (skillBarUI != null)
                {
                    skillBarUI.SetSkillManager(skillManager);
                }
            }

            // 初始化UI
            if (battleUI != null)
            {
                battleUI.InitProgressBar(killCountForBoss);
                battleUI.UpdateKillProgress(killCount, killCountForBoss);
            }

            // 初始化奥术管理器
            if (gameConfig.arcane_slot_ids != null && gameConfig.arcane_slot_ids.Length > 0)
            {
                arcaneManager = gameObject.AddComponent<ArcaneManager>();
                arcaneManager.Init(gameConfig.arcane_slot_ids, heroController, this);

                if (arcaneBarUI != null)
                {
                    arcaneBarUI.SetArcaneManager(arcaneManager);
                    var arcaneSlots = arcaneBarUI.GetSlots();
                    if (arcaneSlots != null)
                    {
                        for (int i = 0; i < arcaneSlots.Length && i < arcaneManager.SlotCount; i++)
                        {
                            if (arcaneSlots[i] != null)
                                arcaneSlots[i].Init(i, arcaneManager);
                        }
                    }
                }
                if (runeBarUI != null)
                    runeBarUI.SetArcaneManager(arcaneManager);
                if (arcaneActiveIconUI != null)
                    arcaneActiveIconUI.SetArcaneManager(arcaneManager);
            }
        }

        // ===== 敌人查询 =====
        public Transform GetNearestEnemy(Vector3 from, float maxRange)
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }

                float dist = Vector3.Distance(from, aliveEnemies[i].position);
                if (dist <= maxRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = aliveEnemies[i];
                }
            }

            return nearest;
        }

        public List<Transform> GetNearestEnemies(Vector3 from, float maxRange, int count)
        {
            List<(Transform t, float d)> inRange = new List<(Transform, float)>();

            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }
                float dist = Vector3.Distance(from, aliveEnemies[i].position);
                if (dist <= maxRange)
                    inRange.Add((aliveEnemies[i], dist));
            }

            inRange.Sort((a, b) => a.d.CompareTo(b.d));

            List<Transform> result = new List<Transform>();
            if (inRange.Count == 0) return result;

            for (int i = 0; i < count; i++)
                result.Add(inRange[i % inRange.Count].t);

            return result;
        }

        public Transform GetNearestEnemyExcluding(Vector3 from, float maxRange, HashSet<Transform> exclude)
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }
                if (exclude != null && exclude.Contains(aliveEnemies[i])) continue;

                float dist = Vector3.Distance(from, aliveEnemies[i].position);
                if (dist <= maxRange && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = aliveEnemies[i];
                }
            }

            return nearest;
        }

        // ===== 范围查询 =====
        public List<Transform> GetEnemiesInRadius(Vector3 center, float radius)
        {
            List<Transform> result = new List<Transform>();
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }
                if (Vector3.Distance(center, aliveEnemies[i].position) <= radius)
                    result.Add(aliveEnemies[i]);
            }
            return result;
        }

        // ===== AoE 伤害 =====
        public void DealAoeDamage(Vector3 center, float radius, float damage)
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }
                float dist = Vector3.Distance(center, aliveEnemies[i].position);
                if (dist <= radius)
                {
                    MonsterController mc = aliveEnemies[i].GetComponent<MonsterController>();
                    if (mc != null) { mc.TakeDamage(damage); continue; }

                    BossController bc = aliveEnemies[i].GetComponent<BossController>();
                    if (bc != null) bc.TakeDamage(damage);
                }
            }
        }

        // 全屏AoE（超暴风）：伤害 + 击退 + 减速 + 易伤
        public void DealFullScreenAoe(Vector3 heroPos, float damage,
            float knockbackForce, float slowDuration, float slowRatio,
            float vulnRatio, float vulnDuration)
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                    continue;
                }

                Transform enemy = aliveEnemies[i];

                MonsterController mc = enemy.GetComponent<MonsterController>();
                if (mc != null)
                {
                    mc.TakeDamage(damage);
                    if (knockbackForce > 0) mc.ApplyKnockback(heroPos, knockbackForce);
                    if (slowDuration > 0) mc.ApplySlow(slowDuration, slowRatio);
                    if (vulnDuration > 0) mc.ApplyVulnerability(vulnDuration, vulnRatio);
                    continue;
                }

                BossController bc = enemy.GetComponent<BossController>();
                if (bc != null)
                {
                    bc.TakeDamage(damage);
                    if (knockbackForce > 0) bc.ApplyKnockback(heroPos, knockbackForce);
                    if (slowDuration > 0) bc.ApplySlow(slowDuration, slowRatio);
                    if (vulnDuration > 0) bc.ApplyVulnerability(vulnDuration, vulnRatio);
                }
            }
        }

        // ===== 生成 =====
        public void SpawnMonster(MonsterConfig config, Vector3 position)
        {
            if (gameEnded) return;

            GameObject monsterObj = Instantiate(monsterPrefab, position, Quaternion.identity);
            MonsterController monster = monsterObj.GetComponent<MonsterController>();
            monster.Init(config, heroController.transform, this);
            aliveEnemies.Add(monsterObj.transform);
        }

        public void SpawnHeroBullet(Vector3 from, Transform target, float damage, float speed)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(heroBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.Init(target, speed, damage, false, this);
        }

        public void SpawnSkillBullet(Vector3 from, Transform target, float damage,
                                      float speed, BulletModifiers mods, int bulletStyleId = 0)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(heroBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.InitSkillBullet(target, speed, damage, this, mods);
            if (bulletStyleId > 0)
                bullet.ApplyStyle(bulletStyleId);
        }

        public void SpawnBossBullet(Vector3 from, Transform target, float damage, float speed)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(bossBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.Init(target, speed, damage, true, this);
        }

        public void SpawnSummon(Vector3 position, float damage, float atkInterval,
                                float duration, bool homing)
        {
            if (gameEnded) return;

            GameObject summonObj = new GameObject("Summon");
            summonObj.transform.position = position;

            SummonController summon = summonObj.AddComponent<SummonController>();
            summon.Init(damage, atkInterval, duration, homing, this);
        }

        // ===== 技能经验 =====
        public void OnHeroNormalAttack(Vector3 heroPos)
        {
            if (skillManager != null)
            {
                skillManager.AddXpToRandomSlot(skillXpMin, skillXpMax);
            }
        }

        public void GrantSkillXp(int xpAmount, int slotCount)
        {
            if (skillManager != null)
            {
                skillManager.GrantXpToSlots(xpAmount, slotCount);
            }
        }

        // ===== 事件回调 =====
        public void OnMonsterKilled(MonsterController monster)
        {
            if (gameEnded) return;

            aliveEnemies.Remove(monster.transform);
            killCount++;

            if (!bossPhase)
            {
                if (battleUI != null)
                {
                    battleUI.UpdateKillProgress(killCount, killCountForBoss);
                }

                if (killCount >= killCountForBoss)
                {
                    SpawnBoss();
                }
            }
        }

        private void SpawnBoss()
        {
            bossPhase = true;
            monsterSpawner.StopSpawning();

            MonsterConfig bossConfig = ConfigManager.Instance.GetBossConfig();
            if (bossConfig == null) return;

            float spawnX = 12f;
            Vector3 spawnPos = new Vector3(spawnX, 0f, 0f);

            float heroX = heroController.transform.position.x;
            float bossTargetX = -heroX;
            Vector3 bossTargetPos = new Vector3(bossTargetX, 0f, 0f);

            GameObject bossObj = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            bossController = bossObj.GetComponent<BossController>();
            bossController.Init(bossConfig, heroController.transform, this, bossTargetPos);
            aliveEnemies.Add(bossObj.transform);

            if (battleUI != null)
            {
                battleUI.SwitchToBossHpMode(bossConfig.hp, bossConfig.hp);
            }
        }

        public void UpdateBossHpUI(float currentHp, float maxHp)
        {
            if (battleUI != null)
            {
                battleUI.UpdateBossHp(currentHp, maxHp);
            }
        }

        public void OnBossKilled()
        {
            if (gameEnded) return;
            gameEnded = true;

            if (bossController != null)
            {
                aliveEnemies.Remove(bossController.transform);
            }
            bossController = null;

            Time.timeScale = 0f;
            if (battleUI != null)
            {
                battleUI.ShowResult(true);
            }
        }

        public void OnHeroDead()
        {
            if (gameEnded) return;
            gameEnded = true;

            Time.timeScale = 0f;
            if (battleUI != null)
            {
                battleUI.ShowResult(false);
            }
        }
    }
}
