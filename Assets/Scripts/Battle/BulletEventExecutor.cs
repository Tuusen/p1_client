using System.Collections.Generic;
using UnityEngine;

namespace GeometryTD
{
    public static class BulletEventExecutor
    {
        public static BulletEventData BuildBulletData(int[] bulletEventIds)
        {
            var data = new BulletEventData();
            if (bulletEventIds == null || bulletEventIds.Length == 0)
                return data;

            List<int> targetEvts = null;
            List<int> casterEvts = null;

            for (int i = 0; i < bulletEventIds.Length; i++)
            {
                var config = Cfg.BulletEvent.Get(bulletEventIds[i]);
                if (config == null) continue;

                var args = config.args;
                switch (config.type)
                {
                    case BulletEventType.Pierce:
                        if (args != null && args.Length >= 1)
                            data.pierceCount = args[0];
                        break;

                    case BulletEventType.Explosion:
                        if (args != null && args.Length >= 2)
                        {
                            data.explosionDmgRate = args[0];
                            data.explosionRadius = args[1];
                        }
                        break;

                    case BulletEventType.Tracking:
                        data.homing = true;
                        break;

                    case BulletEventType.Scatter:
                        if (args != null && args.Length >= 2)
                        {
                            data.scatterCount = args[0];
                            data.scatterAngle = args[1];
                        }
                        break;

                    case BulletEventType.Bounce:
                        if (args != null && args.Length >= 4)
                        {
                            data.bounceCount = args[0];
                            data.bounceRadius = args[1];
                            data.bounceMinDist = args[2];
                            data.bounceDmgMod = args[3];
                        }
                        break;

                    case BulletEventType.Burst:
                        if (args != null && args.Length >= 1)
                            data.burstCount = args[0];
                        break;

                    case BulletEventType.Volley:
                        if (args != null && args.Length >= 1)
                            data.volleyCount = args[0];
                        break;

                    case BulletEventType.AttachToTarget:
                        if (args != null && args.Length >= 1)
                        {
                            if (targetEvts == null) targetEvts = new List<int>();
                            targetEvts.Add(args[0]);
                        }
                        break;

                    case BulletEventType.AttachToCaster:
                        if (args != null && args.Length >= 1)
                        {
                            if (casterEvts == null) casterEvts = new List<int>();
                            casterEvts.Add(args[0]);
                        }
                        break;

                    default:
                        Debug.LogWarning($"[BulletEventExecutor] 未知子弹事件类型: {config.type}, id={config.id}");
                        break;
                }
            }

            data.attachToTargetEventIds = targetEvts;
            data.attachToCasterEventIds = casterEvts;
            return data;
        }
    }
}
