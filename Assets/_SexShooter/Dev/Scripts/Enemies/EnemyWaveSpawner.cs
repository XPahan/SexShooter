using System.Collections.Generic;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Wave spawner ported from AdultShooter EnemySpawner.
    /// </summary>
    public class EnemyWaveSpawner : MonoBehaviour
    {
        [SerializeField] private SuccubusEnemy enemyPrefab;
        [SerializeField] private int initialCount = 8;
        [SerializeField] private int maxAlive = 15;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private float minSeparation = 4f;
        [SerializeField] private float clearanceRadius = 1f;
        [SerializeField] private float capsuleHeight = 2f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask blockageMask;
        [SerializeField] private Transform[] fallbackSpawnPoints;

        private Transform player;
        private readonly List<SuccubusEnemy> alive = new List<SuccubusEnemy>();
        private float nextSpawnTime;
        private bool running;

        public int AliveCount
        {
            get
            {
                alive.RemoveAll(e => e == null || e.IsDead);
                return alive.Count;
            }
        }

        public void Begin(Transform playerTransform)
        {
            player = playerTransform;
            running = true;
            nextSpawnTime = Time.time + spawnInterval;

            int toSpawn = Mathf.Min(initialCount, maxAlive);
            for (int i = 0; i < toSpawn; i++)
                TrySpawnOne();
        }

        public void StopSpawning() => running = false;

        private void Update()
        {
            if (!running || player == null) return;
            alive.RemoveAll(e => e == null || e.IsDead);

            if (AliveCount >= maxAlive) return;
            if (Time.time < nextSpawnTime) return;

            nextSpawnTime = Time.time + spawnInterval;
            TrySpawnOne();
        }

        private void TrySpawnOne()
        {
            if (enemyPrefab == null || player == null) return;
            if (!TryFindSpawnPoint(out Vector3 pos)) return;

            var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            var brain = enemy.GetComponent<SuccubusBrain>();
            if (brain != null) brain.SetPlayer(player);

            var def = brain != null ? brain.Definition : null;
            if (def != null)
            {
                if (def.SpawnSound != null)
                    AudioSource.PlayClipAtPoint(def.SpawnSound, pos, def.SpawnSoundVolume);

                if (def.SpawnVfxPrefab != null)
                {
                    var vfx = Instantiate(def.SpawnVfxPrefab, pos, Quaternion.identity);
                    Destroy(vfx, 3f);
                }
            }

            alive.Add(enemy);
        }

        private bool TryFindSpawnPoint(out Vector3 result)
        {
            result = Vector3.zero;

            // Prefer random points around player on walkable ground
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(minDistanceFromPlayer, minDistanceFromPlayer + 12f);
                Vector3 probe = player.position + new Vector3(ring.x, 20f, ring.y);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 60f, groundMask))
                    continue;

                if (!IsClear(hit.point)) continue;
                if (Vector3.Distance(hit.point, player.position) < minDistanceFromPlayer) continue;
                if (!HasSeparation(hit.point)) continue;

                result = hit.point;
                return true;
            }

            // Fallback markers
            if (fallbackSpawnPoints != null)
            {
                for (int i = 0; i < fallbackSpawnPoints.Length; i++)
                {
                    var t = fallbackSpawnPoints[i];
                    if (t == null) continue;
                    if (Vector3.Distance(t.position, player.position) < minDistanceFromPlayer) continue;
                    if (!IsClear(t.position) || !HasSeparation(t.position)) continue;
                    result = t.position;
                    return true;
                }
            }

            return false;
        }

        private bool IsClear(Vector3 point)
        {
            Vector3 bottom = point + Vector3.up * (clearanceRadius + 0.05f);
            Vector3 top = point + Vector3.up * (capsuleHeight - clearanceRadius);
            return !Physics.CheckCapsule(bottom, top, clearanceRadius, blockageMask, QueryTriggerInteraction.Ignore);
        }

        private bool HasSeparation(Vector3 point)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                if (alive[i] == null) continue;
                if (Vector3.Distance(alive[i].transform.position, point) < minSeparation)
                    return false;
            }
            return true;
        }
    }
}
