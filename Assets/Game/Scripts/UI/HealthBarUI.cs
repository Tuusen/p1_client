using UnityEngine;
using UnityEngine.UI;

namespace GeometryTD
{
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Single Bar Mode (Enemy)")]
        [SerializeField] private Image hpFill;

        [Header("Dual Bar Mode (Player)")]
        [SerializeField] private Image shieldFill;
        [SerializeField] private Image hpFillDual;

        private int maxHpValue;
        private int maxShieldValue;
        private bool isDualMode;

        public void SetupSingle(int maxHp)
        {
            isDualMode = false;
            maxHpValue = maxHp;

            if (hpFill != null)
                hpFill.fillAmount = 1f;

            if (shieldFill != null)
                shieldFill.gameObject.SetActive(false);
            if (hpFillDual != null)
                hpFillDual.gameObject.SetActive(false);
        }

        public void UpdateSingle(int currentHp)
        {
            if (hpFill != null && maxHpValue > 0)
            {
                hpFill.fillAmount = Mathf.Clamp01((float)currentHp / maxHpValue);
            }
        }

        public void SetupDual(int maxShield, int maxHp)
        {
            isDualMode = true;
            maxShieldValue = maxShield;
            maxHpValue = maxHp;

            if (hpFill != null)
                hpFill.gameObject.SetActive(false);

            if (shieldFill != null)
            {
                shieldFill.gameObject.SetActive(true);
                shieldFill.fillAmount = 1f;
            }
            if (hpFillDual != null)
            {
                hpFillDual.gameObject.SetActive(true);
                hpFillDual.fillAmount = 1f;
            }
        }

        public void UpdateDual(int currentShield, int currentHp)
        {
            if (shieldFill != null && maxShieldValue > 0)
            {
                shieldFill.fillAmount = Mathf.Clamp01((float)currentShield / maxShieldValue);
            }
            if (hpFillDual != null && maxHpValue > 0)
            {
                hpFillDual.fillAmount = Mathf.Clamp01((float)currentHp / maxHpValue);
            }
        }
    }
}
