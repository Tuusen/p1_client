using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public enum BuffType
    {
        AttrModify,
        DamageOverTime,
        Freeze,
        Knockback,
        HealOverTime,
        Charge,
    }

    public class BuffEntry
    {
        public int buffId;
        public BuffType type;
        public float duration;
        public int value;
        public int secondaryValue;
        public int attrId;
        public float tickInterval;
        public float tickTimer;
        public Vector3 knockbackDir;
    }

    public interface IBuffTarget
    {
        AttrComponent Attrs { get; }
        void OnBuffDamage(float dmg);
        void OnBuffHeal(float heal);
        bool IsDead { get; }
        Vector3 Position { get; }
    }

    public class BuffSystem
    {
        private List<BuffEntry> buffs = new List<BuffEntry>();
        private int nextBuffId = 1;

        public void AddBuff(BuffEntry buff)
        {
            buff.buffId = nextBuffId++;

            switch (buff.type)
            {
                case BuffType.DamageOverTime:
                    // Refresh duration, keep highest damage
                    for (int i = 0; i < buffs.Count; i++)
                    {
                        if (buffs[i].type == BuffType.DamageOverTime)
                        {
                            if (buff.duration > buffs[i].duration)
                                buffs[i].duration = buff.duration;
                            if (buff.value > buffs[i].value)
                                buffs[i].value = buff.value;
                            buffs[i].tickTimer = 0f;
                            return;
                        }
                    }
                    break;

                case BuffType.Freeze:
                    for (int i = 0; i < buffs.Count; i++)
                    {
                        if (buffs[i].type == BuffType.Freeze)
                        {
                            if (buff.duration > buffs[i].duration)
                                buffs[i].duration = buff.duration;
                            return;
                        }
                    }
                    break;

                case BuffType.Knockback:
                    for (int i = 0; i < buffs.Count; i++)
                    {
                        if (buffs[i].type == BuffType.Knockback)
                        {
                            buffs[i].duration = buff.duration;
                            buffs[i].knockbackDir = buff.knockbackDir;
                            return;
                        }
                    }
                    break;

                case BuffType.Charge:
                    // Charge buffs are managed by controller (isCharging flag)
                    // Allow multiple charge buffs (one per attribute)
                    break;

                case BuffType.AttrModify:
                    // Each AttrModify buff is independent, apply bonus on add
                    break;

                case BuffType.HealOverTime:
                    // Refresh
                    for (int i = 0; i < buffs.Count; i++)
                    {
                        if (buffs[i].type == BuffType.HealOverTime)
                        {
                            buffs[i].duration = buff.duration;
                            buffs[i].value = buff.value;
                            buffs[i].tickTimer = 0f;
                            return;
                        }
                    }
                    break;
            }

            // Apply attr bonus on add for AttrModify and Charge
            if (buff.type == BuffType.AttrModify || buff.type == BuffType.Charge)
            {
                // AttrModify stores attrId and value; applied via target.Attrs
                // Actual apply happens in ApplyAttrBuffs
            }

            buffs.Add(buff);
        }

        public void RemoveBuff(int buffId)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].buffId == buffId)
                {
                    buffs.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveBuffsByType(BuffType type)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].type == type)
                    buffs.RemoveAt(i);
            }
        }

        public bool HasBuff(BuffType type)
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].type == type)
                    return true;
            }
            return false;
        }

        public void Tick(float deltaTime, IBuffTarget target)
        {
            if (target == null || target.IsDead) return;

            // Recalculate attr bonuses from buffs each tick
            ReapplyAttrBonuses(target);

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];

                switch (buff.type)
                {
                    case BuffType.DamageOverTime:
                        buff.tickTimer += deltaTime;
                        if (buff.tickTimer >= buff.tickInterval)
                        {
                            buff.tickTimer -= buff.tickInterval;
                            target.OnBuffDamage(buff.value);
                            if (target.IsDead) return;
                        }
                        break;

                    case BuffType.HealOverTime:
                        buff.tickTimer += deltaTime;
                        if (buff.tickTimer >= buff.tickInterval)
                        {
                            buff.tickTimer -= buff.tickInterval;
                            target.OnBuffHeal(buff.value);
                        }
                        break;

                    case BuffType.Knockback:
                        if (buff.duration > 0)
                        {
                            float step = 20f * deltaTime;
                            if (step > buff.duration) step = buff.duration;
                            var t = target as MonoBehaviour;
                            if (t != null)
                                t.transform.position += buff.knockbackDir * step;
                            buff.duration -= step;
                        }
                        break;
                }

                // Duration countdown (Knockback uses duration as remaining distance)
                if (buff.type != BuffType.Knockback && buff.duration >= 0)
                {
                    buff.duration -= deltaTime;
                    if (buff.duration <= 0)
                    {
                        buffs.RemoveAt(i);
                    }
                }
                else if (buff.type == BuffType.Knockback && buff.duration <= 0)
                {
                    buffs.RemoveAt(i);
                }
            }
        }

        // Re-apply all AttrModify/Charge bonuses to target's AttrComponent
        // Called at the start of each Tick to keep bonuses in sync
        private void ReapplyAttrBonuses(IBuffTarget target)
        {
            if (target.Attrs == null) return;

            // Clear all buff bonuses first
            target.Attrs.ClearBonuses();

            // Re-add from active buffs
            for (int i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                if ((buff.type == BuffType.AttrModify || buff.type == BuffType.Charge) && buff.attrId > 0)
                {
                    target.Attrs.AddBonus(buff.attrId, buff.value);
                }
            }
        }

        public bool IsKnockingBack()
        {
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].type == BuffType.Knockback && buffs[i].duration > 0)
                    return true;
            }
            return false;
        }

        public void Clear()
        {
            buffs.Clear();
        }
    }
}
