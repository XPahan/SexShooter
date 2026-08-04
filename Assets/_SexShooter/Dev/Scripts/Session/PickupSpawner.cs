using System.Collections.Generic;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Spawns ammo/health pickups once around the player at session start.
    /// Mirrors AdultShooter AmmoSpawner behaviour.
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class PickupEntry
        {
            public GameObject prefab;
            public int count = 6;
        }

        [SerializeField] private PickupEntry[] entries;
        [SerializeField] private float minDistanceFromPlayer = 5f;
        [SerializeField] private float minSeparation = 3f;
        [SerializeField] private float clearanceRadius = 0.5f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask blockageMask;
        [SerializeField] private Transform[] fallbackSpawnPoints;

        private readonly List<Vector3> occupied = new List<Vector3>();

        public void Begin(Transform player)
        {
            if (player == null || entries == null) return;
            occupied.Clear();

            for (int e = 0; e < entries.Length; e++)
            {
                var entry = entries[e];
                if (entry == null || entry.prefab == null || entry.count <= 0) continue;

                for (int i = 0; i < entry.count; i++)
                {
                    if (!TryFindSpawnPoint(player.position, out Vector3 pos)) continue;
                    var go = Instantiate(entry.prefab, pos + Vector3.up * 0.2f, Quaternion.identity);
                    go.name = entry.prefab.name + "_" + i;
                    occupied.Add(pos);
                }
            }
        }

        private bool TryFindSpawnPoint(Vector3 playerPos, out Vector3 result)
        {
            result = Vector3.zero;

            for (int attempt = 0; attempt < 28; attempt++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized *
                               Random.Range(minDistanceFromPlayer, minDistanceFromPlayer + 14f);
                Vector3 probe = playerPos + new Vector3(ring.x, 25f, ring.y);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 70f, groundMask))
                    continue;

                if (Vector3.Distance(hit.point, playerPos) < minDistanceFromPlayer) continue;
                if (!IsClear(hit.point) || !HasSeparation(hit.point)) continue;

                result = hit.point;
                return true;
            }

            if (fallbackSpawnPoints != null)
            {
                for (int i = 0; i < fallbackSpawnPoints.Length; i++)
                {
                    var t = fallbackSpawnPoints[i];
                    if (t == null) continue;
                    if (Vector3.Distance(t.position, playerPos) < minDistanceFromPlayer) continue;
                    if (!IsClear(t.position) || !HasSeparation(t.position)) continue;
                    result = t.position;
                    return true;
                }
            }

            return false;
        }

        private bool IsClear(Vector3 point)
        {
            Vector3 p = point + Vector3.up * (clearanceRadius + 0.05f);
            return !Physics.CheckSphere(p, clearanceRadius, blockageMask, QueryTriggerInteraction.Ignore);
        }

        private bool HasSeparation(Vector3 point)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                if (Vector3.Distance(occupied[i], point) < minSeparation)
                    return false;
            }
            return true;
        }
    }
}
