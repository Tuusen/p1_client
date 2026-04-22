using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Generic config table with list items only.
    /// Usage: Cfg.Hero.Get(id), Cfg.Hero.All
    /// Meta values are now in MetaConsts
    /// </summary>
    public class ConfigTable<TItem, TMeta>
    {
        public List<TItem> All { get; private set; }
        private Dictionary<int, TItem> lookup;

        public void Init(List<TItem> items, Func<TItem, int> keySelector)
        {
            All = items ?? new List<TItem>();
            lookup = new Dictionary<int, TItem>();
            foreach (var item in All)
                lookup[keySelector(item)] = item;
        }

        public TItem Get(int id)
        {
            if (lookup != null && lookup.TryGetValue(id, out var item))
                return item;
            return default(TItem);
        }
    }

    /// <summary>
    /// Generic config table with list items only (no meta).
    /// Usage: Cfg.Buff.Get(id), Cfg.Buff.All
    /// </summary>
    public class ConfigTable<TItem>
    {
        public List<TItem> All { get; private set; }
        private Dictionary<int, TItem> lookup;

        public void Init(List<TItem> items, Func<TItem, int> keySelector)
        {
            All = items ?? new List<TItem>();
            lookup = new Dictionary<int, TItem>();
            foreach (var item in All)
                lookup[keySelector(item)] = item;
        }

        public TItem Get(int id)
        {
            if (lookup != null && lookup.TryGetValue(id, out var item))
                return item;
            return default(TItem);
        }
    }
}
