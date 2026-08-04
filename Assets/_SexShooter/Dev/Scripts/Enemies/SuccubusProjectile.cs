using cowsins;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Enemy bolt that damages the Cowsins player and spawns impact VFX.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SuccubusProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float damage = 2f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private GameObject impactPrefab;

        private Vector3 direction;
        private float dieAt;
        private bool launched;
        private bool destroyed;

        public void Launch(Vector3 dir, float projectileSpeed, float projectileDamage, float projectileLifetime, GameObject impact = null)
        {
            direction = dir.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            lifetime = projectileLifetime;
            if (impact != null) impactPrefab = impact;
            dieAt = Time.time + lifetime;
            launched = true;
            transform.forward = direction;
        }

        private void Update()
        {
            if (!launched || destroyed) return;
            transform.position += direction * speed * Time.deltaTime;
            if (Time.time >= dieAt) DestroyProjectile(spawnImpact: false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!launched || destroyed) return;

            if (other.CompareTag("Enemy") || other.GetComponentInParent<SuccubusEnemy>() != null)
                return;

            Transform t = other.transform;
            while (t != null)
            {
                if (t.TryGetComponent(out PlayerStats stats))
                {
                    if (!stats.IsDead) stats.Damage(damage, false);
                    DestroyProjectile(spawnImpact: true);
                    return;
                }

                if (t.CompareTag("Player") && t.TryGetComponent(out IDamageable dmg))
                {
                    dmg.Damage(damage, false);
                    DestroyProjectile(spawnImpact: true);
                    return;
                }

                t = t.parent;
            }

            if (!other.isTrigger)
                DestroyProjectile(spawnImpact: true);
        }

        private void DestroyProjectile(bool spawnImpact)
        {
            if (destroyed) return;
            destroyed = true;

            if (spawnImpact && impactPrefab != null)
                Instantiate(impactPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
