using UnityEngine;

namespace GeometryTD
{
    public class BulletController : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trailRenderer;
        
        private Vector2 direction;
        private float speed;
        private int damage;
        private bool isPlayerBullet;
        private float lifetime = 5f;
        private float timer;

        public void Init(Vector2 dir, float spd, int dmg, bool playerBullet)
        {
            direction = dir;
            speed = spd;
            damage = dmg;
            isPlayerBullet = playerBullet;
            timer = 0f;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                // Color based on bullet type
                if (playerBullet)
                {
                    trailRenderer.startColor = new Color(0.2f, 0.8f, 1f, 1f);
                    trailRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0f);
                }
                else
                {
                    trailRenderer.startColor = new Color(1f, 0.3f, 0.3f, 1f);
                    trailRenderer.endColor = new Color(1f, 0.3f, 0.3f, 0f);
                }
            }

            // Set sprite color
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = playerBullet
                    ? new Color(0.2f, 0.8f, 1f, 1f)
                    : new Color(1f, 0.3f, 0.3f, 1f);
            }
        }

        private void Update()
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);

            timer += Time.deltaTime;
            if (timer >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPlayerBullet)
            {
                EnemyController enemy = other.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }

                BossController boss = other.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }
            }
            else
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }
            }
        }

        private void ReturnToPool()
        {
            if (BulletPool.Instance != null)
                BulletPool.Instance.ReturnBullet(this);
            else
                Destroy(gameObject);
        }
    }
}
