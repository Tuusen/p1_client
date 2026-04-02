// using System;
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
        [SerializeField] private GameObject summonPrefab;

        [Header("UI引用")]
        [SerializeField] private BattleUI battleUI;
        [SerializeField] private SkillBarUI skillBarUI;
        [SerializeField] private FloatingTextUI floatingTextUI;
        [SerializeField] private ArcaneBarUI arcaneBarUI;
        [SerializeField] private RuneBarUI runeBarUI;
        [SerializeField] private ArcaneActiveIconUI arcaneActiveIconUI;
        [SerializeField] private SkillXpTimerUI skillXpTimerUI;

        [Header("生成点")]
        [SerializeField] private Transform heroSpawnPoint;

        private MonsterSpawner monsterSpawner;
        private HeroController heroController;
        private BossController bossController;
        private SkillManager skillManager;
        private ArcaneManager arcaneManager;
        private EventEffectManager eventEffectManager;

        private List<Transform> aliveEnemies = new List<Transform>();
        private bool gameEnded;
        private int skillXpMin;
        private int skillXpMax;
        private float skillXpTimer;
        private float skillXpInterval;
        private int pendingXpSlotIndex = -1;

        private LevelConfig currentLevelConfig;
        private float hardMultiplier;
        private int currentLevelId;

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

        private void Update()
        {
            if (gameEnded || skillManager == null) return;

            skillXpTimer -= Time.deltaTime;

            // 每帧检查预选槽位是否仍然有效（可能被GrantXpToSlots升至满级或被其他逻辑改变）
            if (pendingXpSlotIndex >= 0)
            {
                var pendingSlot = skillManager.GetSlot(pendingXpSlotIndex);
                if (pendingSlot == null || pendingSlot.level >= 10)
                {
                    pendingXpSlotIndex = skillManager.PickRandomEligibleSlot();
                    UpdatePendingSlotUI();
                }
            }

            if (skillXpTimerUI != null)
                skillXpTimerUI.UpdateTimer(skillXpTimer, skillXpInterval);

            if (skillXpTimer <= 0f)
            {
                if (pendingXpSlotIndex >= 0)
                {
                    skillManager.AddXpToRandomSlot(pendingXpSlotIndex, skillXpMin, skillXpMax);

                    // 获取目标技能槽UI位置，播放粒子特效
                    if (skillXpTimerUI != null && skillBarUI != null)
                    {
                        var targetSlotUI = skillBarUI.GetSlotUI(pendingXpSlotIndex);
                        if (targetSlotUI != null)
                        {
                            RectTransform targetRect = targetSlotUI.GetIconRect();
                            if (targetRect != null)
                                skillXpTimerUI.PlayXpParticle(targetRect);
                        }
                    }
                }
                ResetSkillXpTimer();
            }
        }

        private void ResetSkillXpTimer()
        {
            skillXpTimer = skillXpInterval;

            // 预选下次获得经验的技能槽
            pendingXpSlotIndex = skillManager.PickRandomEligibleSlot();
            UpdatePendingSlotUI();
        }

        private void UpdatePendingSlotUI()
        {
            if (skillXpTimerUI == null) return;

            if (pendingXpSlotIndex >= 0)
            {
                var slot = skillManager.GetSlot(pendingXpSlotIndex);
                if (slot != null)
                {
                    var config = ConfigManager.Instance.GetSkillConfig(slot.skillId, 0);
                    string iconPath = config != null ? config.icon : null;
                    skillXpTimerUI.SetTargetSkill(slot.skillName, iconPath);
                }
                else
                {
                    skillXpTimerUI.SetTargetSkill(null, null);
                }
            }
            else
            {
                skillXpTimerUI.SetTargetSkill(null, null);
            }
        }

        private void InitBattle()
        {
            if (ConfigManager.Instance == null)
            {
                Debug.LogError("[BattleManager] ConfigManager未初始化");
                return;
            }

            GameConfig gameConfig = ConfigManager.Instance.GameConfig;

            // 获取玩家选择的英雄
            int heroId = GameManager.Instance != null
                ? GameManager.Instance.GetSelectedHeroId()
                : gameConfig.default_hero_id;
            HeroConfig heroConfig = ConfigManager.Instance.GetHeroConfig(heroId);
            if (heroConfig == null)
            {
                Debug.LogError($"[BattleManager] 未找到英雄配置, id: {heroId}");
                return;
            }

            // 获取关卡配置
            currentLevelId = GameManager.Instance != null ? GameManager.Instance.GetSelectedLevelId() : 1;
            if (currentLevelId <= 0) currentLevelId = 1;
            currentLevelConfig = ConfigManager.Instance.GetLevelConfig(currentLevelId);
            if (currentLevelConfig == null)
            {
                Debug.LogError($"[BattleManager] 未找到关卡配置, id: {currentLevelId}");
                return;
            }

            hardMultiplier = currentLevelConfig.hard / 10000f;
            gameEnded = false;
            skillXpMin = heroConfig.skill_xp_min;
            skillXpMax = heroConfig.skill_xp_max;
            skillXpInterval = heroConfig.skill_xp_interval;

            // 预制体 fallback：通过配置加载子弹预制体
            if (heroBulletPrefab == null)
                heroBulletPrefab = ConfigManager.Instance.GetBulletPrefab(1);
            if (bossBulletPrefab == null)
                bossBulletPrefab = ConfigManager.Instance.GetBulletPrefab(201);

            // 加载关卡背景
            if (!string.IsNullOrEmpty(currentLevelConfig.bg))
            {
                GameObject bgPrefab = GameHelper.LoadPrefab(currentLevelConfig.bg);
                if (bgPrefab != null)
                    Instantiate(bgPrefab, new Vector3(0,0,100), Quaternion.identity);
            }

            // 生成英雄（通过role查找prefab）
            Vector3 heroPos = heroSpawnPoint != null ? heroSpawnPoint.position : new Vector3(-8f, 0f, 0f);
            GameObject heroPrefabToUse = ConfigManager.Instance.GetRolePrefab(heroConfig.role);
            if (heroPrefabToUse == null) heroPrefabToUse = heroPrefab;
            GameObject heroObj = Instantiate(heroPrefabToUse, heroPos, Quaternion.identity);
            heroController = heroObj.GetComponent<HeroController>();
            heroController.Init(heroConfig, this);

            // 初始化事件特效管理器
            eventEffectManager = GetComponent<EventEffectManager>();
            if (eventEffectManager == null)
                eventEffectManager = gameObject.AddComponent<EventEffectManager>();

            // 初始化怪物生成器（使用关卡配置）
            monsterSpawner = GetComponent<MonsterSpawner>();
            if (monsterSpawner == null)
                monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
            monsterSpawner.Init(this, currentLevelConfig, hardMultiplier);

            // 初始化技能管理器（使用玩家装备的技能）
            int[] equippedSkills = GameManager.Instance != null
                ? GameManager.Instance.GetEquippedSkills()
                : gameConfig.skill_slot_ids;
            if (equippedSkills != null && equippedSkills.Length > 0)
            {
                skillManager = gameObject.AddComponent<SkillManager>();
                skillManager.Init(equippedSkills, heroController, this, floatingTextUI);

                if (skillBarUI != null)
                {
                    skillBarUI.SetSkillManager(skillManager);
                }

                ResetSkillXpTimer();
            }

            // 初始化UI（使用第一个Boss的击杀阈值）
            if (battleUI != null)
            {
                int firstBossThreshold = monsterSpawner.GetNextBossKillThreshold();
                battleUI.InitProgressBar(firstBossThreshold);
                battleUI.UpdateKillProgress(0, firstBossThreshold);
            }

            // 初始化奥术管理器（使用玩家装备的奥术）
            int[] equippedArcanes = GameManager.Instance != null
                ? GameManager.Instance.GetEquippedArcanes()
                : gameConfig.arcane_slot_ids;
            if (equippedArcanes != null && equippedArcanes.Length > 0)
            {
                arcaneManager = gameObject.AddComponent<ArcaneManager>();
                arcaneManager.Init(equippedArcanes, heroController, this);

                if (arcaneBarUI != null)
                {
                    arcaneBarUI.SetArcaneManager(arcaneManager);
                    var arcaneSlots = arcaneBarUI.GetSlots();
                    if (arcaneSlots != null)
                    {
                        for (int i = 0; i < arcaneSlots.Length; i++)
                        {
                            if (arcaneSlots[i] == null) continue;
                            if (i < arcaneManager.SlotCount)
                            {
                                arcaneSlots[i].gameObject.SetActive(true);
                                arcaneSlots[i].Init(i, arcaneManager);
                            }
                            else
                            {
                                arcaneSlots[i].gameObject.SetActive(false);
                            }
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

        public List<Transform> GetNearestEnemiesUnique(Vector3 from, float maxRange, int count)
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
            int max = Mathf.Min(count, inRange.Count);
            for (int i = 0; i < max; i++)
                result.Add(inRange[i].t);

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
        public void SpawnMonster(MonsterConfig config, Vector3 position, float hardMult)
        {
            if (gameEnded) return;

            GameObject prefabToUse = ConfigManager.Instance.GetRolePrefab(config.role);
            if (prefabToUse == null) prefabToUse = monsterPrefab;
            GameObject monsterObj = Instantiate(prefabToUse, position, Quaternion.identity);
            MonsterController monster = monsterObj.GetComponent<MonsterController>();
            monster.Init(config, heroController.transform, this, hardMult);
            aliveEnemies.Add(monsterObj.transform);
        }

        public void SpawnLevelBoss(int bossId, float hardMult)
        {
            if (gameEnded) return;

            MonsterConfig bossConfig = ConfigManager.Instance.GetMonsterConfig(bossId);
            if (bossConfig == null) return;

            float spawnX = 12f;
            Vector3 spawnPos = new Vector3(spawnX, 0f, 0f);

            float heroX = heroController.transform.position.x;
            float bossTargetX = -heroX;
            Vector3 bossTargetPos = new Vector3(bossTargetX, 0f, 0f);

            GameObject prefabToUse = ConfigManager.Instance.GetRolePrefab(bossConfig.role);
            if (prefabToUse == null) prefabToUse = bossPrefab;
            GameObject bossObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            bossController = bossObj.GetComponent<BossController>();
            bossController.Init(bossConfig, heroController.transform, this, bossTargetPos, hardMult);
            aliveEnemies.Add(bossObj.transform);

            if (battleUI != null)
            {
                battleUI.SwitchToBossHpMode(bossController.CurrentHp, bossController.MaxHp);
            }
        }

        public void SpawnHeroBullet(Vector3 from, Transform target, float damage, float speed)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(heroBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.Init(target, speed, damage, false, this);
        }

        public void SpawnSkillBullet(Vector3 from, Transform target, float damage,
                                      float speed, BulletModifiers mods, int bulletStyleId = 0, float attackRange = 50f)
        {
            if (gameEnded) return;

            GameObject prefab = heroBulletPrefab;
            if (bulletStyleId > 0)
            {
                GameObject stylePrefab = ConfigManager.Instance.GetBulletPrefab(bulletStyleId);
                if (stylePrefab != null)
                    prefab = stylePrefab;
            }

            GameObject bulletObj = Instantiate(prefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.InitSkillBullet(target, speed, damage, this, mods, attackRange);
        }

        public void SpawnBossBullet(Vector3 from, Transform target, float damage, float speed)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(bossBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.Init(target, speed, damage, true, this);
        }

        public void SpawnMonsterBullet(Vector3 from, Transform target, float damage, float speed, float attackRange = 15f)
        {
            if (gameEnded) return;

            GameObject bulletObj = Instantiate(bossBulletPrefab, from, Quaternion.identity);
            BulletController bullet = bulletObj.GetComponent<BulletController>();
            bullet.Init(target, speed, damage, true, this, attackRange);
        }

        public void SpawnSummon(Vector3 position, float duration, float attrRatio, int monsterId, bool homing)
        {
            if (gameEnded) return;

            MonsterConfig monsterConfig = ConfigManager.Instance.GetMonsterConfig(monsterId);
            if (monsterConfig == null)
            {
                Debug.LogWarning($"[BattleManager] 召唤物找不到怪物配置, monsterId: {monsterId}");
                return;
            }

            GameObject prefab = ConfigManager.Instance.GetRolePrefab(monsterConfig.role);
            GameObject summonObj;
            if (prefab != null)
            {
                summonObj = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                summonObj = new GameObject($"Summon_{monsterId}");
                summonObj.transform.position = position;
            }

            SummonMonsterController controller = summonObj.GetComponent<SummonMonsterController>();
            if (controller == null)
                controller = summonObj.AddComponent<SummonMonsterController>();
            controller.Init(monsterConfig, attrRatio, duration, homing, this);
        }

        // ===== 技能经验 =====
        public void OnHeroNormalAttack(Vector3 heroPos)
        {
            // 经验值现在通过计时器自动获取，不再通过普通攻击
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
            monsterSpawner.OnMonsterKilled();

            if (!monsterSpawner.IsBossActive())
            {
                int nextThreshold = monsterSpawner.GetNextBossKillThreshold();
                if (battleUI != null && nextThreshold > 0)
                {
                    battleUI.UpdateKillProgress(monsterSpawner.KillCount, nextThreshold);
                }
            }

            // Story: grant kill gold
            if (StoryManager.Instance != null && StoryManager.Instance.IsInAdventure)
            {
                int coin = monster.IsElite
                    ? currentLevelConfig.coinEliteKill
                    : currentLevelConfig.coinNormalKill;
                if (coin > 0)
                    StoryManager.Instance.AddBattleGold(coin);
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

            if (bossController != null)
            {
                aliveEnemies.Remove(bossController.transform);
            }
            bossController = null;

            // Story: grant boss kill gold
            if (StoryManager.Instance != null && StoryManager.Instance.IsInAdventure
                && currentLevelConfig.coinBossKill > 0)
            {
                StoryManager.Instance.AddBattleGold(currentLevelConfig.coinBossKill);
            }

            // Story: check boss death event (dialogue/choices overlay)
            if (StoryManager.Instance != null && StoryManager.Instance.IsInAdventure)
            {
                BossEventEntry bossEvent = StoryManager.Instance.GetCurrentBossEvent();
                if (bossEvent != null)
                {
                    HandleBossEvent(bossEvent);
                    return;
                }
            }

            monsterSpawner.OnBossKilled();
        }

        // ===== Boss Death Event Chain =====

        private void HandleBossEvent(BossEventEntry bossEvent)
        {
            if (bossEvent.dialogueId > 0)
            {
                DialogueConfig config = ConfigManager.Instance.GetDialogueConfig(bossEvent.dialogueId);
                if (config != null && config.lines != null && config.lines.Length > 0)
                {
                    DialogueWin win = GameHelper.OpenWin<DialogueWin>();
                    win.ShowDialogue(config, () => OnBossDialogueComplete(bossEvent));
                    return;
                }
            }

            if (bossEvent.choiceGroupId > 0)
            {
                ShowBossChoices(bossEvent.choiceGroupId);
                return;
            }

            monsterSpawner.OnBossKilled();
        }

        private void OnBossDialogueComplete(BossEventEntry bossEvent)
        {
            if (bossEvent.choiceGroupId > 0)
            {
                ShowBossChoices(bossEvent.choiceGroupId);
            }
            else
            {
                monsterSpawner.OnBossKilled();
            }
        }

        private void ShowBossChoices(int choiceGroupId)
        {
            ChoiceGroupConfig config = ConfigManager.Instance.GetChoiceGroupConfig(choiceGroupId);
            if (config != null && config.options != null && config.options.Length > 0)
            {
                ChoiceWin win = GameHelper.OpenWin<ChoiceWin>();
                win.ShowChoices(config, OnBossChoiceSelected);
                return;
            }

            monsterSpawner.OnBossKilled();
        }

        private void OnBossChoiceSelected(int index, ChoiceOption option)
        {
            if (option != null && StoryManager.Instance != null)
                StoryManager.Instance.ProcessChoice(index, option);

            monsterSpawner.OnBossKilled();
        }

        public void OnBossDefeatedContinue(int nextBossThreshold)
        {
            if (battleUI != null)
            {
                battleUI.SwitchToKillMode(monsterSpawner.KillCount, nextBossThreshold);
            }
        }

        public void OnLevelComplete()
        {
            if (gameEnded) return;
            gameEnded = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MarkLevelCompleted(currentLevelId);
            }

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

            // Story: handle battle failure before showing UI
            if (StoryManager.Instance != null && StoryManager.Instance.IsInAdventure)
                StoryManager.Instance.HandleBattleFailed();

            Time.timeScale = 0f;
            if (battleUI != null)
            {
                battleUI.ShowResult(false);
            }
        }
    }
}
