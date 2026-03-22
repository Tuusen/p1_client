using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 单个技能运行时状态
    /// </summary>
    public class SkillState
    {
        public SkillConfig Config;

        public int   Level   { get; private set; } = 0;   // 0~10
        public int   Exp     { get; private set; } = 0;   // 当前等级经验 0~expPerLevel-1
        public float Cooldown { get; private set; } = 0f; // 剩余冷却
        public bool  IsMaxLevel  => Level >= Config.maxLevel;
        public bool  IsReady     => Level > 0 && Cooldown <= 0f;

        // 事件：等级/经验/冷却变化
        public System.Action<SkillState> OnStateChanged;

        public SkillState(SkillConfig cfg) { Config = cfg; }

        /// <summary>添加经验，自动升级（已满级不接受经验）</summary>
        public bool AddExp(int amount)
        {
            if (IsMaxLevel) return false;

            Exp += amount;
            bool leveledUp = false;
            while (Exp >= Config.expPerLevel && Level < Config.maxLevel)
            {
                Exp -= Config.expPerLevel;
                Level++;
                leveledUp = true;
                if (IsMaxLevel) Exp = 0; // 满级后清零多余经验
            }
            OnStateChanged?.Invoke(this);
            return leveledUp;
        }

        /// <summary>记录冷却减少（由 SkillManager.Update 每帧调用）</summary>
        public void TickCooldown(float dt)
        {
            if (Cooldown <= 0f) return;
            Cooldown = Mathf.Max(0f, Cooldown - dt);
            OnStateChanged?.Invoke(this);
        }

        /// <summary>使用技能：等级归零，开始冷却</summary>
        public void Activate(float selfCooldown)
        {
            Level = 0;
            Exp   = 0;
            Cooldown = selfCooldown;
            OnStateChanged?.Invoke(this);
        }

        /// <summary>被其他技能激活导致的公共冷却</summary>
        public void ApplySharedCooldown(float cd)
        {
            // 共享冷却只在自身冷却更短时覆盖
            if (cd > Cooldown)
            {
                Cooldown = cd;
                OnStateChanged?.Invoke(this);
            }
        }

        public void Reset()
        {
            Level = 0; Exp = 0; Cooldown = 0f;
            OnStateChanged?.Invoke(this);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 技能管理器 - 挂在 Player 上
    /// 负责：经验分配、升级、技能使用、冷却倒计时
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static float SELF_COOLDOWN   = 10f;
        public static float SHARED_COOLDOWN = 2f;

        // ── 状态 ──────────────────────────────────────────────────────────────
        public List<SkillState> Skills { get; private set; } = new List<SkillState>();

        // 可以获得经验的技能池（未满级的）
        private List<int> expCandidates = new List<int>();

        // 发射子弹委托（由 PlayerController 注入）
        public System.Action<SkillConfig, Transform> OnFireSkillBullet;

        // 对外事件
        public System.Action<SkillState, bool, int> OnSkillExpChanged;   // (skill, leveledUp, expAmount)
        public System.Action<SkillState>       OnSkillActivated;
        public System.Action<SkillState>       OnSkillCooldownTick;

        // ── 初始化 ─────────────────────────────────────────────────────────────
        public void Initialize()
        {
            Skills.Clear();
            var cfgs = ConfigLoader.GetAllSkillConfigs();

            // 取前8个（或全部）
            int count = Mathf.Min(8, cfgs.Count);
            for (int i = 0; i < count; i++)
            {
                var state = new SkillState(cfgs[i]);
                state.OnStateChanged += _ => { };  // 占位
                Skills.Add(state);
            }

            // 如果不足8个，用默认补全（理论上不会）
            Debug.Log($"[SkillManager] 初始化 {Skills.Count} 个技能");
        }

        // ── 每帧冷却更新 ──────────────────────────────────────────────────────
        private void Update()
        {
            for (int i = 0; i < Skills.Count; i++)
                Skills[i].TickCooldown(Time.deltaTime);
        }

        // ── 每次攻击随机给一个技能加经验 ─────────────────────────────────────
        /// <summary>由 PlayerController 的 FireBullet 每次调用</summary>
        public void GainRandomExp()
        {
            // 收集未满级的技能索引
            expCandidates.Clear();
            for (int i = 0; i < Skills.Count; i++)
                if (!Skills[i].IsMaxLevel) expCandidates.Add(i);

            if (expCandidates.Count == 0) return;  // 全部满级

            int idx    = expCandidates[Random.Range(0, expCandidates.Count)];
            int amount = Random.Range(1, 11);  // 1~10

            bool leveledUp = Skills[idx].AddExp(amount);
            OnSkillExpChanged?.Invoke(Skills[idx], leveledUp, amount);

            if (leveledUp)
                Debug.Log($"[SkillManager] {Skills[idx].Config.name} 升级至 Lv{Skills[idx].Level}");
        }

        // ── 使用技能（玩家点击 / 自动触发） ──────────────────────────────
        /// <summary>
        /// 返回 null = 成功；返回字符串 = 失败原因（用于 UI 提示）
        /// </summary>
        public string TryUseSkill(int index, Transform target = null)
        {
            if (index < 0 || index >= Skills.Count) return "无效的技能槽";
            SkillState s = Skills[index];

            if (s.Level == 0)
                return $"{s.Config.name}\n尚未解锁\n（攻击可获得经验）";
            if (s.Cooldown > 0f)
                return $"{s.Config.name}\n冷却中 {s.Cooldown:F1} 秒";
            if (!s.IsReady)
                return $"{s.Config.name}\n无法使用";

            // 能量交换郢血量校验
            if (s.Config.isEnergyExchange)
            {
                var player = Object.FindObjectOfType<PlayerController>();
                if (player != null && !player.CanAffordHpCost(s.Config.hpCostPct))
                    return $"{s.Config.name}\n生命不足\n（需要血量>{s.Config.minHpPct:F0}%）";
            }

            // 找目标
            if (target == null) target = FindNearestEnemy();

            // 使用 SkillExecutor 执行技能
            var pc = Object.FindObjectOfType<PlayerController>();
            SkillExecutor.Execute(s.Config, s, pc, target);

            // 自身冷却
            float cd = s.Config.cooldown > 0 ? s.Config.cooldown : SELF_COOLDOWN;
            s.Activate(cd);
            OnSkillActivated?.Invoke(s);

            // 其他技能共享冷却
            for (int i = 0; i < Skills.Count; i++)
                if (i != index) Skills[i].ApplySharedCooldown(SHARED_COOLDOWN);

            Debug.Log($"[SkillManager] 使用技能 [{s.Config.name}]  自冷却={cd}s  共享={SHARED_COOLDOWN}s");
            return null; // 成功
        }

        /// <summary>兼容旧接口</summary>
        public bool UseSkill(int index, Transform target = null)
            => TryUseSkill(index, target) == null;

        // ── 重置 ──────────────────────────────────────────────────────────────
        public void ResetAll()
        {
            foreach (var s in Skills) s.Reset();
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────
        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var ec = e.GetComponent<EnemyController>();
                if (ec == null || ec.IsDead) continue; // 过滤未初始化或已死目标
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e.transform; }
            }
            return best;
        }
    }
}
