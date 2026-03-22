using UnityEngine;
using UnityEngine.UI;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 能量栏 UI - 显示4种元素能量进度条和超能数值
    /// 结构：每行 = [超能文字] [进度条背景][进度条填充]
    /// 由 SceneSetupTool 在技能栏下方创建，运行时监听 EnergyManager.OnEnergyChanged
    /// </summary>
    public class EnergyBarUI : MonoBehaviour
    {
        // 4种元素的进度条填充（由 SceneSetupTool 创建后自动绑定）
        [HideInInspector] public Image fireFill;
        [HideInInspector] public Image iceFill;
        [HideInInspector] public Image lightningFill;
        [HideInInspector] public Image windFill;

        // 超能数值文字
        [HideInInspector] public Text fireSuperText;
        [HideInInspector] public Text iceSuperText;
        [HideInInspector] public Text lightningSuperText;
        [HideInInspector] public Text windSuperText;

        private void Start()
        {
            // 尝试自动绑定子节点引用
            AutoBind();

            // 订阅能量变化事件
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.OnEnergyChanged += Refresh;
                Refresh();
            }
        }

        private void OnEnable()
        {
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.OnEnergyChanged -= Refresh;
                EnergyManager.Instance.OnEnergyChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (EnergyManager.Instance != null)
                EnergyManager.Instance.OnEnergyChanged -= Refresh;
        }

        // ── 刷新显示 ────────────────────────────────────────────────────
        public void Refresh()
        {
            var mgr = EnergyManager.Instance;
            if (mgr == null) return;

            SetBar(fireFill,      mgr.FireEnergy);
            SetBar(iceFill,       mgr.IceEnergy);
            SetBar(lightningFill, mgr.LightningEnergy);
            SetBar(windFill,      mgr.WindEnergy);

            SetSuper(fireSuperText,      mgr.FireSuper);
            SetSuper(iceSuperText,       mgr.IceSuper);
            SetSuper(lightningSuperText, mgr.LightningSuper);
            SetSuper(windSuperText,      mgr.WindSuper);
        }

        private static void SetBar(Image fill, int current)
        {
            if (fill == null) return;
            fill.fillAmount = (float)current / EnergyManager.MAX_BASE_ENERGY;
        }

        private static void SetSuper(Text txt, int superVal)
        {
            if (txt == null) return;
            txt.text = superVal.ToString();
            // 有超能时高亮
            txt.color = superVal > 0
                ? new Color(1f, 0.92f, 0.2f)   // 金色
                : new Color(0.6f, 0.6f, 0.6f);  // 灰色
        }

        // ── 自动查找子节点引用（SceneSetupTool 创建的标准命名）────────
        private void AutoBind()
        {
            fireFill      = FindFill("FireRow");
            iceFill       = FindFill("IceRow");
            lightningFill = FindFill("LightningRow");
            windFill      = FindFill("WindRow");

            fireSuperText      = FindSuperText("FireRow");
            iceSuperText       = FindSuperText("IceRow");
            lightningSuperText = FindSuperText("LightningRow");
            windSuperText      = FindSuperText("WindRow");
        }

        private Image FindFill(string rowName)
        {
            var row = transform.Find(rowName);
            if (row == null) return null;
            var fill = row.Find("Fill");
            return fill != null ? fill.GetComponent<Image>() : null;
        }

        private Text FindSuperText(string rowName)
        {
            var row = transform.Find(rowName);
            if (row == null) return null;
            var t = row.Find("SuperText");
            return t != null ? t.GetComponent<Text>() : null;
        }
    }
}
