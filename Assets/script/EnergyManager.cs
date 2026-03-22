using UnityEngine;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 能量管理器 - 管理4种元素能量（火/冰/电/风）和超能量
    /// 规则：10点基础能量 = 1点超能量（自动累积）
    /// 挂在 Player 上，由 SkillExecutor 在技能命中时调用 AddEnergy
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        // ── 常量 ──────────────────────────────────────────────────────────────
        public const int MAX_BASE_ENERGY  = 10;  // 每种能量满格为10，转换为1超能
        public const int MAX_SUPER_ENERGY = 99;  // 超能量上限

        // ── 基础能量（0~10，满10自动转超能）──────────────────────────────────
        private int fireEnergy;
        private int iceEnergy;
        private int lightningEnergy;
        private int windEnergy;

        // ── 超能量（0~99）────────────────────────────────────────────────────
        private int fireSuperEnergy;
        private int iceSuperEnergy;
        private int lightningSuperEnergy;
        private int windSuperEnergy;

        // ── 属性（只读）──────────────────────────────────────────────────────
        public int FireEnergy      => fireEnergy;
        public int IceEnergy       => iceEnergy;
        public int LightningEnergy => lightningEnergy;
        public int WindEnergy      => windEnergy;

        public int FireSuper      => fireSuperEnergy;
        public int IceSuper       => iceSuperEnergy;
        public int LightningSuper => lightningSuperEnergy;
        public int WindSuper      => windSuperEnergy;

        // ── 事件：能量变化时通知 UI ────────────────────────────────────────
        public System.Action OnEnergyChanged;

        // ── 全局单例（方便 SkillExecutor 调用）────────────────────────────
        public static EnergyManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── 添加能量 ──────────────────────────────────────────────────────────
        public void AddEnergy(string element, int amount)
        {
            if (amount <= 0) return;

            switch (element)
            {
                case "fire":
                    fireEnergy += amount;
                    while (fireEnergy >= MAX_BASE_ENERGY)
                    {
                        fireEnergy -= MAX_BASE_ENERGY;
                        fireSuperEnergy = Mathf.Min(MAX_SUPER_ENERGY, fireSuperEnergy + 1);
                    }
                    break;
                case "ice":
                    iceEnergy += amount;
                    while (iceEnergy >= MAX_BASE_ENERGY)
                    {
                        iceEnergy -= MAX_BASE_ENERGY;
                        iceSuperEnergy = Mathf.Min(MAX_SUPER_ENERGY, iceSuperEnergy + 1);
                    }
                    break;
                case "lightning":
                    lightningEnergy += amount;
                    while (lightningEnergy >= MAX_BASE_ENERGY)
                    {
                        lightningEnergy -= MAX_BASE_ENERGY;
                        lightningSuperEnergy = Mathf.Min(MAX_SUPER_ENERGY, lightningSuperEnergy + 1);
                    }
                    break;
                case "wind":
                    windEnergy += amount;
                    while (windEnergy >= MAX_BASE_ENERGY)
                    {
                        windEnergy -= MAX_BASE_ENERGY;
                        windSuperEnergy = Mathf.Min(MAX_SUPER_ENERGY, windSuperEnergy + 1);
                    }
                    break;
            }

            OnEnergyChanged?.Invoke();
        }

        // ── 消耗超能量 ────────────────────────────────────────────────────────
        public bool ConsumeSuper(string element, int amount = 1)
        {
            switch (element)
            {
                case "fire":
                    if (fireSuperEnergy < amount) return false;
                    fireSuperEnergy -= amount;
                    break;
                case "ice":
                    if (iceSuperEnergy < amount) return false;
                    iceSuperEnergy -= amount;
                    break;
                case "lightning":
                    if (lightningSuperEnergy < amount) return false;
                    lightningSuperEnergy -= amount;
                    break;
                case "wind":
                    if (windSuperEnergy < amount) return false;
                    windSuperEnergy -= amount;
                    break;
                default:
                    return false;
            }
            OnEnergyChanged?.Invoke();
            return true;
        }

        // ── 重置 ──────────────────────────────────────────────────────────────
        public void Reset()
        {
            fireEnergy = iceEnergy = lightningEnergy = windEnergy = 0;
            fireSuperEnergy = iceSuperEnergy = lightningSuperEnergy = windSuperEnergy = 0;
            OnEnergyChanged?.Invoke();
        }

        // ── 调试：直接设置值 ──────────────────────────────────────────────────
        public void DebugAddSuper(string element, int amount)
        {
            AddEnergy(element, amount * MAX_BASE_ENERGY);
        }
    }
}
