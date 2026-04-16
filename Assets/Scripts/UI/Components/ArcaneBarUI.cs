using UnityEngine;

namespace GeometryTD
{
    public class ArcaneBarUI : MonoBehaviour
    {
        [SerializeField] private ArcaneSlotUI[] slots;

        private ArcaneManager arcaneManager;

        public void SetArcaneManager(ArcaneManager manager)
        {
            arcaneManager = manager;
        }

        public ArcaneSlotUI[] GetSlots() => slots;

        private void Update()
        {
            if (arcaneManager == null || slots == null) return;

            for (int i = 0; i < slots.Length && i < arcaneManager.SlotCount; i++)
            {
                if (slots[i] != null)
                    slots[i].UpdateSlot(arcaneManager.GetSlot(i));
            }
        }
    }
}
