using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    /// <summary>
    /// Generic config table with list items + meta singleton.
    /// Usage: Cfg.Hero.Get(id), Cfg.Hero.All, Cfg.Hero.Meta.default_hero_id
    /// </summary>
    public class ConfigTable<TItem, TMeta>
    {
        public List<TItem> All { get; private set; }
        public TMeta Meta { get; private set; }
        private Dictionary<int, TItem> lookup;

        public void Init(List<TItem> items, TMeta meta, Func<TItem, int> keySelector)
        {
            All = items ?? new List<TItem>();
            Meta = meta;
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

    /// <summary>
    /// Meta-only config (no list items, singleton settings only).
    /// Usage: Cfg.Global.Meta.kill_count_for_boss
    /// </summary>
    public class ConfigMeta<TMeta>
    {
        public TMeta Meta { get; private set; }

        public void Init(TMeta meta)
        {
            Meta = meta;
        }
    }
}
