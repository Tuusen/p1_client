using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private int initialPoolSize = 30;

        private Queue<BulletController> pool = new Queue<BulletController>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateBullet();
            }
        }

        private BulletController CreateBullet()
        {
            GameObject go = Instantiate(bulletPrefab, transform);
            go.SetActive(false);
            var bullet = go.GetComponent<BulletController>();
            pool.Enqueue(bullet);
            return bullet;
        }

        public BulletController GetBullet()
        {
            BulletController bullet;
            if (pool.Count > 0)
            {
                bullet = pool.Dequeue();
            }
            else
            {
                // CreateBullet enqueues it, so dequeue right after
                CreateBullet();
                bullet = pool.Dequeue();
            }
            bullet.gameObject.SetActive(true);
            return bullet;
        }

        public void ReturnBullet(BulletController bullet)
        {
            bullet.gameObject.SetActive(false);
            pool.Enqueue(bullet);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
