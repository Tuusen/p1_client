using UnityEngine;

namespace GeometryTD
{
    public class SummonController : MonoBehaviour
    {
        private float damage;
        private float attackInterval;
        private float duration;
        private bool homing;
        private float attackTimer;
        private BattleManager battleManager;
        private Animator animator;
        private CharacterFacing facing;

        public void Init(float dmg, float atkInterval, float dur, bool isHoming, BattleManager bm)
        {
            damage = dmg;
            attackInterval = atkInterval;
            duration = dur;
            homing = isHoming;
            battleManager = bm;
            attackTimer = 0f;

            animator = GetComponent<Animator>();
            facing = GetComponent<CharacterFacing>();
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            if (duration <= 0)
            {
                Destroy(gameObject);
                return;
            }

            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                Attack();
            }
        }

        private void Attack()
        {
            if (battleManager == null) return;

            Transform target = battleManager.GetNearestEnemy(transform.position, 50f);
            if (target == null) return;

            facing?.FaceToward(target.position);

            if (homing)
            {
                var mods = new BulletModifiers { homing = true };
                battleManager.SpawnSkillBullet(transform.position, target, damage, 8f, mods);
            }
            else
            {
                battleManager.SpawnHeroBullet(transform.position, target, damage, 8f);
            }

            animator?.SetTrigger("Attack");
        }
    }
}
