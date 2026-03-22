using UnityEngine;
using UnityEngine.UI;

namespace GeometryTowerDefense
{
    /// <summary>
    /// 血条控制器 - 头顶显示血量 / 盾値
    /// 布局：血条在上，盾条在下，各自进度条中间叠加数值文字
    /// 血条 / 盾条使用 Image.Type.Filled + FillMethod.Horizontal 控制宽度
    /// </summary>
    public class HealthBarController : MonoBehaviour
    {
        private Image hpFill;
        private Image spFill;
        private Text  hpText;   // 血量数值，叠加在血条中间
        private Text  spText;   // 盾量数值，叠加在盾条中间

        private int  maxHp, maxSp;
        private bool showValue;

        // ── 初始化（由 SceneSetupTool 或 EnemyController 调用）────────────────
        public void Initialize(int hp, int sp, bool showVal)
        {
            maxHp     = Mathf.Max(1, hp);
            maxSp     = sp;
            showValue = showVal;

            // 每次都重新查找，防止场景重建后引用丢失
            FindComponents();

            if (hpText != null) hpText.gameObject.SetActive(showVal);
            if (spText != null) spText.gameObject.SetActive(showVal && maxSp > 0);
            if (spFill != null) spFill.transform.parent.gameObject.SetActive(maxSp > 0);

            UpdateDisplay(hp, sp);
        }

        // ── 运行时更新 ────────────────────────────────────────────
        public void UpdateHealth(int hp, int sp)
        {
            if (hpFill == null) FindComponents();
            UpdateDisplay(hp, sp);
        }

        // ── 查找子节点组件（支持两层嵌套）────────────────────────────────
        private void FindComponents()
        {
            hpFill = FindDeep("HealthFill");
            spFill = FindDeep("ShieldFill");
            hpText = FindDeepText("HpText");
            spText = FindDeepText("SpText");
        }

        private Image FindDeep(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t.GetComponent<Image>();
            foreach (Transform child in transform)
            {
                var t2 = child.Find(childName);
                if (t2 != null) return t2.GetComponent<Image>();
            }
            return null;
        }

        private Text FindDeepText(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t.GetComponent<Text>();
            foreach (Transform child in transform)
            {
                var t2 = child.Find(childName);
                if (t2 != null) return t2.GetComponent<Text>();
            }
            return null;
        }

        // ── 刷新显示 ────────────────────────────────────────────
        private void UpdateDisplay(int hp, int sp)
        {
            hp = Mathf.Max(0, hp);
            sp = Mathf.Max(0, sp);

            // fillAmount 控制宽度（Image 设为 Filled + Horizontal）
            if (hpFill != null)
                hpFill.fillAmount = maxHp > 0 ? (float)hp / maxHp : 0f;

            if (spFill != null && maxSp > 0)
                spFill.fillAmount = (float)sp / maxSp;

            if (!showValue) return;

            if (hpText != null)
                hpText.text = $"{hp}/{maxHp}";

            if (spText != null && maxSp > 0)
                spText.text = $"{sp}/{maxSp}";
        }
    }
}
