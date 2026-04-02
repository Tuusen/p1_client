using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class AttrComponent : MonoBehaviour
    {
        private Dictionary<int, int> baseAttrs = new Dictionary<int, int>();
        private Dictionary<int, int> bonusAttrs = new Dictionary<int, int>();

        public void Init(AttrEntry[] configAttrs)
        {
            baseAttrs.Clear();
            bonusAttrs.Clear();

            if (configAttrs != null)
            {
                for (int i = 0; i < configAttrs.Length; i++)
                    baseAttrs[configAttrs[i].id] = configAttrs[i].value;
            }
        }

        public int GetBase(int attrId)
        {
            return baseAttrs.TryGetValue(attrId, out int val) ? val : 0;
        }

        public int GetBonus(int attrId)
        {
            return bonusAttrs.TryGetValue(attrId, out int val) ? val : 0;
        }

        public int GetRaw(int attrId)
        {
            return GetBase(attrId) + GetBonus(attrId);
        }

        public int GetFinal(int attrId)
        {
            int raw = GetRaw(attrId);
            var meta = ConfigManager.Instance != null ? ConfigManager.Instance.GetAttrMeta(attrId) : null;
            if (meta != null)
            {
                if (meta.downLimit != 0 || meta.upLimit != 0)
                {
                    if (meta.downLimit != 0 && raw < meta.downLimit)
                        raw = meta.downLimit;
                    if (meta.upLimit != 0 && raw > meta.upLimit)
                        raw = meta.upLimit;
                }
            }
            return raw;
        }

        public float GetFinalFloat(int attrId)
        {
            int val = GetFinal(attrId);
            var meta = ConfigManager.Instance != null ? ConfigManager.Instance.GetAttrMeta(attrId) : null;
            if (meta != null && meta.powerType == 1)
                return val / 10000f;
            return val;
        }

        public void SetBase(int attrId, int val)
        {
            baseAttrs[attrId] = val;
        }

        public void AddBonus(int attrId, int val)
        {
            if (bonusAttrs.ContainsKey(attrId))
                bonusAttrs[attrId] += val;
            else
                bonusAttrs[attrId] = val;
        }

        public void RemoveBonus(int attrId, int val)
        {
            if (bonusAttrs.ContainsKey(attrId))
                bonusAttrs[attrId] -= val;
        }

        public void ClearBonuses()
        {
            bonusAttrs.Clear();
        }

        // ===== 派生属性 =====

        public int GetMaxHp()
        {
            long baseHp = GetFinal(AttributeIds.HP);
            long hpPctBonus = GetFinal(AttributeIds.HpPercentBonus);
            return (int)(baseHp * (10000 + hpPctBonus) / 10000);
        }

        public int GetAttack()
        {
            long baseAtk = GetFinal(AttributeIds.Attack);
            long atkPctBonus = GetFinal(AttributeIds.AtkPercentBonus);
            return (int)(baseAtk * (10000 + atkPctBonus) / 10000);
        }

        public float GetAttackIntervalSec()
        {
            int ms = GetFinal(AttributeIds.AttackInterval);
            if (ms <= 0) ms = 1000;
            return ms / 1000f;
        }

        public float GetMoveSpeed()
        {
            return GetFinal(AttributeIds.MoveSpeed) / 10000f;
        }

        // ===== 属性遍历(用于召唤物继承) =====

        public Dictionary<int, int> GetAllBaseAttrs()
        {
            return baseAttrs;
        }

        public bool HasBase(int attrId)
        {
            return baseAttrs.ContainsKey(attrId);
        }
    }
}
