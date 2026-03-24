using System;
using System.Collections.Generic;

namespace GeometryTD
{
    [Serializable]
    public class HeroConfig
    {
        public string name;
        public string description;
        public int attack_skill_id;
        public float attack_range;
        public float hp;
        public float shield;
        public float attack_interval;
        public float base_attack;
    }

    [Serializable]
    public class MonsterConfig
    {
        public int id;
        public string name;
        public float hp;
        public int level;
        public float damage;
        public bool is_boss;
        public float move_speed;
        public int attack_skill_id;
        public float attack_range;
        public float attack_interval;
    }

    [Serializable]
    public class MonsterConfigList
    {
        public List<MonsterConfig> monsters;
    }

    [Serializable]
    public class SkillEvent
    {
        public int type;
        public float[] param;
    }

    [Serializable]
    public class SkillConfig
    {
        public int id;
        public int level;
        public string name;
        public string[] desList;
        public string icon;
        public int dmg;
        public int dmgType;
        public int mp;
        public int mpType;
        public float bulletSpeed;
        public int atkCnt;
        public float cd;
        public SkillEvent[] events;
    }

    [Serializable]
    public class SkillConfigList
    {
        public List<SkillConfig> skills;
    }

    [Serializable]
    public class GameConfig
    {
        public int kill_count_for_boss;
        public float monster_spawn_interval;
        public int boss_monster_id;
        public int[] skill_slot_ids;
    }
}
