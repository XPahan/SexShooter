using System.Collections.Generic;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Wave spawner. Prefers authored World/Spawn_Points, then serialized fallbacks.
    /// </summary>
    public class EnemyWaveSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class EnemySpawnEntry
        {
            public SuccubusEnemy prefab;
            [Min(0f)] public float weight = 1f;
        }

        [SerializeField] private SuccubusEnemy enemyPrefab;
        [SerializeField] private EnemySpawnEntry[] enemyEntries;
        [SerializeField] private int initialCount = 28;
        [SerializeField] private int maxAlive = 45;
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private float minSeparation = 2f;
        [SerializeField] private float clearanceRadius = 1f;
        [SerializeField] private float capsuleHeight = 2f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask blockageMask;
        [Tooltip("Optional. Scene World/Spawn_Points children are auto-collected at runtime.")]
        [SerializeField] private Transform[] fallbackSpawnPoints;
        [SerializeField] private string worldSpawnPointsRootName = "Spawn_Points";
        [SerializeField] private bool useRandomGroundFallback = true;

        private Transform player;
        private readonly List<SuccubusEnemy> alive = new List<SuccubusEnemy>();
        private readonly List<Transform> spawnPoints = new List<Transform>();
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
            CollectSpawnPoints();
            running = true;
            nextSpawnTime = Time.time + spawnInterval;

            int toSpawn = Mathf.Min(initialCount, maxAlive);
            int attempts = 0;
            int maxAttempts = toSpawn * 6;
            while (AliveCount < toSpawn && attempts < maxAttempts)
            {
                TrySpawnOne();
                attempts++;
            }
        }

        public void StopSpawning() => running = false;

        private void CollectSpawnPoints()
        {
            spawnPoints.Clear();

            // Level-designer points under World/Spawn_Points (or any object with that name).
            var roots = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < roots.Length; i++)
            {
                var t = roots[i];
                if (t == null || t.name != worldSpawnPointsRootName) continue;
                for (int c = 0; c < t.childCount; c++)
                {
                    var child = t.GetChild(c);
                    if (child != null && child.gameObject.activeInHierarchy)
                        spawnPoints.Add(child);
                }
            }

            if (fallbackSpawnPoints != null)
            {
                for (int i = 0; i < fallbackSpawnPoints.Length; i++)
                {
                    var t = fallbackSpawnPoints[i];
                    if (t == null) continue;
                    if (!spawnPoints.Contains(t))
                        spawnPoints.Add(t);
                }
            }

            Debug.Log("[EnemyWaveSpawner] Spawn points: " + spawnPoints.Count);
        }

        private void Update()
        {
            if (!running || player == null) return;
            alive.RemoveAll(e => e == null || e.IsDead);

            if (AliveCount >= maxAlive) return;
            if (Time.time < nextSpawnTime) return;

            nextSpawnTime = Time.time + spawnInterval;
            TrySpawnOne();
        }

        private SuccubusEnemy PickPrefab()
        {
            float total = 0f;
            if (enemyEntries != null)
            {
                for (int i = 0; i < enemyEntries.Length; i++)
                {
                    var e = enemyEntries[i];
                    if (e != null && e.prefab != null && e.weight > 0f)
                        total += e.weight;
                }
            }

            if (total > 0f)
            {
                float roll = Random.Range(0f, total);
                float acc = 0f;
                for (int i = 0; i < enemyEntries.Length; i++)
                {
                    var e = enemyEntries[i];
                    if (e == null || e.prefab == null || e.weight <= 0f) continue;
                    acc += e.weight;
                    if (roll <= acc) return e.prefab;
                }
            }

            return enemyPrefab;
        }

        private void TrySpawnOne()
        {
            var prefab = PickPrefab();
            if (prefab == null || player == null) return;
            if (!TryFindSpawnPoint(out Vector3 pos)) return;

            var enemy = Instantiate(prefab, pos, Quaternion.identity);
            var brain = enemy.GetComponent<SuccubusBrain>();
            if (brain != null) brain.SetPlayer(player);

            var def = brain != null ? brain.Definition : null;
            if (def != null)
            {
                if (def.SpawnSound != null)
                    EnemySfx.Play3D(def.SpawnSound, pos + Vector3.up, def.SpawnSoundVolume);

                if (def.SpawnVfxPrefab != null)
                    SpawnVfx(def, pos, enemy);
            }

            alive.Add(enemy);
        }

        private static void SpawnVfx(EnemyDefinition def, Vector3 pos, SuccubusEnemy enemy)
        {
            float centerY = 0.9f;
            var cc = enemy.GetComponent<CharacterController>();
            if (cc != null) centerY = cc.center.y;
            else
            {
                var capsule = enemy.GetComponent<CapsuleCollider>();
                if (capsule != null) centerY = capsule.center.y;
            }

            var vfx = Object.Instantiate(def.SpawnVfxPrefab, pos + Vector3.up * centerY, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * def.SpawnVfxScale;

            var systems = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int s = 0; s < systems.Length; s++)
            {
                var ps = systems[s];
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.sharedMaterials != null)
                {
                    var mats = renderer.sharedMaterials;
                    int valid = 0;
                    for (int m = 0; m < mats.Length; m++)
                        if (mats[m] != null) valid++;
                    if (valid > 0 && valid != mats.Length)
                    {
                        var cleaned = new Material[valid];
                        int idx = 0;
                        for (int m = 0; m < mats.Length; m++)
                            if (mats[m] != null) cleaned[idx++] = mats[m];
                        renderer.sharedMaterials = cleaned;
                    }
                }

                ps.Clear(true);
                ps.Play(true);
            }

            Object.Destroy(vfx, 4f);
        }

        private bool TryFindSpawnPoint(out Vector3 result)
        {
            result = Vector3.zero;

            if (spawnPoints.Count > 0 && TryPickFromSpawnPoints(out result))
            {
                // Slight jitter so the same authored point can host multiple enemies.
                Vector2 jitter = Random.insideUnitCircle * 1.25f;
                result += new Vector3(jitter.x, 0f, jitter.y);
                return true;
            }

            if (!useRandomGroundFallback)
                return false;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized *
                               Random.Range(minDistanceFromPlayer, minDistanceFromPlayer + 12f);
                Vector3 probe = player.position + new Vector3(ring.x, 20f, ring.y);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 60f, groundMask))
                    continue;

                if (!IsClear(hit.point)) continue;
                if (Vector3.Distance(hit.point, player.position) < minDistanceFromPlayer) continue;
                if (!HasSeparation(hit.point)) continue;

                result = hit.point;
                return true;
            }

            return false;
        }

        private bool TryPickFromSpawnPoints(out Vector3 result)
        {
            result = Vector3.zero;

            // Shuffle indices so waves don't always fill the same order.
            int count = spawnPoints.Count;
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }

            // Pass 1: far enough from player + clear + separated
            if (TryPickPass(order, requireMinDistance: true, requireClear: true, out result))
                return true;

            // Pass 2: authored points may sit near start — still spawn if clear + separated
            if (TryPickPass(order, requireMinDistance: false, requireClear: true, out result))
                return true;

            // Pass 3: last resort — any separated authored point
            return TryPickPass(order, requireMinDistance: false, requireClear: false, out result);
        }

        private bool TryPickPass(int[] order, bool requireMinDistance, bool requireClear, out Vector3 result)
        {
            result = Vector3.zero;
            for (int i = 0; i < order.Length; i++)
            {
                var t = spawnPoints[order[i]];
                if (t == null) continue;

                Vector3 pos = t.position;
                if (requireMinDistance &&
                    Vector3.Distance(pos, player.position) < minDistanceFromPlayer)
                    continue;
                if (requireClear && !IsClear(pos)) continue;
                if (!HasSeparation(pos)) continue;

                result = pos;
                return true;
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
