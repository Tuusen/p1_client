using UnityEngine;
using System.Collections.Generic;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 状态效果类型
    /// </summary>
    public enum StatusType
    {
        Burn,            // 灼伤：每秒伤害
        Freeze,          // 冰冻：停止移动
        Slow,            // 减速：降低移动速度
        Vulnerable,      // 易伤：受到更多伤害
        HoT,             // 持续治愈（用于玩家）
        DamageReduction, // 伤害减免（用于玩家）
    }

    /// <summary>
    /// 单个状态效果数据
    /// </summary>
    public class StatusEffect
    {
        public StatusType type;
        public float      duration;    // 剩余持续时间（秒）
        public float      value;       // 效果数值（含义因类型而异）
        // Burn:            value = 每秒伤害量
        // Freeze:          value 无意义（停止移动）
        // Slow:            value = 移动速度乘数（如 0.5 = 减速50%）
        // Vulnerable:      value = 额外伤害倍率（如 0.5 = 额外受50%伤害）
        // HoT:             value = 每秒治愈百分比（基于已损失生命值）
        // DamageReduction: value = 减免比例（如 0.5 = 减免50%伤害）

        public StatusEffect(StatusType t, float dur, float val)
        {
            type = t; duration = dur; value = val;
        }
    }

    /// <summary>
    /// 状态效果组件 - 挂在 EnemyController 同 GameObject 上，由 EnemyController 驱动
    /// 对外提供只读属性给 EnemyController 使用
    /// </summary>
    public class StatusEffectController : MonoBehaviour
    {
        // ── 活跃状态列表 ─────────────────────────────────────────────────────────
        private List<StatusEffect> activeEffects = new List<StatusEffect>();

        // ── 敌人引用（用于 Burn 造成伤害）────────────────────────────────────────
        private EnemyController enemy;

        // ── 状态图标（头顶显示）──────────────────────────────────────────────────
        private Transform         iconRoot;
        private GameObject        burnIconGo;

        // ── Burn 计时 ────────────────────────────────────────────────────────────
        private float burnTick = 0f;

        public void Init(EnemyController ec, Transform root)
        {
            enemy    = ec;
            iconRoot = root;
        }

        // ── 只读属性 ──────────────────────────────────────────────────────────────
        public bool  IsFrozen            => HasEffect(StatusType.Freeze);
        public float SpeedMultiplier     => GetSpeedMultiplier();
        public float DamageMultiplier    => GetDamageMultiplier();
        public bool  HasDamageReduction  => HasEffect(StatusType.DamageReduction);
        public float DamageReductionVal  => GetEffectValue(StatusType.DamageReduction);

        // ── 应用状态效果 ──────────────────────────────────────────────────────────
        public void ApplyStatus(StatusEffect effect)
        {
            // 冰冻与减速不叠加，刷新时长
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].type == effect.type)
                {
                    // 刷新时长（取较长值），灼伤刷新伤害值
                    activeEffects[i].duration = Mathf.Max(activeEffects[i].duration, effect.duration);
                    if (effect.type == StatusType.Burn)
                        activeEffects[i].value = Mathf.Max(activeEffects[i].value, effect.value);
                    return;
                }
            }
            activeEffects.Add(effect);
            UpdateIcons();
        }

        // ── 每帧更新 ──────────────────────────────────────────────────────────────
        private void Update()
        {
            if (enemy == null || enemy.IsDead) return;

            float dt = Time.deltaTime;

            // Burn 计时
            burnTick += dt;
            if (burnTick >= 1f)
            {
                burnTick -= 1f;
                foreach (var eff in activeEffects)
                {
                    if (eff.type == StatusType.Burn && eff.duration > 0f)
                        enemy.TakeDamage(Mathf.RoundToInt(eff.value));
                }
            }

            // 倒计时
            bool changed = false;
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].duration -= dt;
                if (activeEffects[i].duration <= 0f)
                {
                    activeEffects.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) UpdateIcons();
        }

        // ── 内部工具 ──────────────────────────────────────────────────────────────
        private bool HasEffect(StatusType t)
        {
            foreach (var e in activeEffects)
                if (e.type == t) return true;
            return false;
        }

        private float GetEffectValue(StatusType t)
        {
            foreach (var e in activeEffects)
                if (e.type == t) return e.value;
            return 0f;
        }

        private float GetSpeedMultiplier()
        {
            if (HasEffect(StatusType.Freeze)) return 0f;
            float mult = 1f;
            foreach (var e in activeEffects)
                if (e.type == StatusType.Slow) mult *= (1f - e.value);
            return Mathf.Max(0.05f, mult);
        }

        private float GetDamageMultiplier()
        {
            float mult = 1f;
            foreach (var e in activeEffects)
                if (e.type == StatusType.Vulnerable) mult += e.value;
            return mult;
        }

        // ── 状态图标（灼伤显示橙色小方块）────────────────────────────────────────
        private void UpdateIcons()
        {
            bool hasBurn = HasEffect(StatusType.Burn);
            if (hasBurn && burnIconGo == null && iconRoot != null)
            {
                burnIconGo = new GameObject("BurnIcon");
                burnIconGo.transform.SetParent(iconRoot, false);
                burnIconGo.transform.localPosition = new Vector3(0.35f, 0f, 0f);
                burnIconGo.transform.localScale     = Vector3.one * 0.15f;
                var sr = burnIconGo.AddComponent<SpriteRenderer>();
                sr.color        = new Color(1f, 0.45f, 0.1f);
                sr.sortingOrder = 12;
                sr.sprite       = GeometryMeshGenerator.CreateSprite("cube", 1f,
                    new Color(1f, 0.45f, 0.1f));
            }
            else if (!hasBurn && burnIconGo != null)
            {
                Destroy(burnIconGo);
                burnIconGo = null;
            }
        }

        public void ClearAll()
        {
            activeEffects.Clear();
            UpdateIcons();
        }
    }
}
