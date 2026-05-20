using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        private List<EnemyController> activeEnemies = new List<EnemyController>();

        public IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            Instance = this;
        }

        public void Register(EnemyController enemy)
        {
            if (!activeEnemies.Contains(enemy))
                activeEnemies.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            activeEnemies.Remove(enemy);
        }

        public void ClearAll()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                    Destroy(activeEnemies[i].gameObject);
            }
            activeEnemies.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
