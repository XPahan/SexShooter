using cowsins;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Simple enemy bolt that damages the Cowsins player on trigger hit.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SuccubusProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float damage = 2f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        private Vector3 direction;
        private float dieAt;
        private bool launched;

        public void Launch(Vector3 dir, float projectileSpeed, float projectileDamage, float projectileLifetime)
        {
            direction = dir.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            lifetime = projectileLifetime;
            dieAt = Time.time + lifetime;
            launched = true;
            transform.forward = direction;
        }

        private void Update()
        {
            if (!launched) return;
            transform.position += direction * speed * Time.deltaTime;
            if (Time.time >= dieAt) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!launched) return;

            // Ignore other enemies / projectiles
            if (other.CompareTag("Enemy") || other.GetComponentInParent<SuccubusEnemy>() != null)
                return;

            // Walk up for PlayerStats (collider may be on a child)
            Transform t = other.transform;
            while (t != null)
            {
                if (t.TryGetComponent(out PlayerStats stats))
                {
                    if (!stats.IsDead) stats.Damage(damage, false);
                    Destroy(gameObject);
                    return;
                }

                if (t.CompareTag("Player") && t.TryGetComponent(out IDamageable dmg))
                {
                    dmg.Damage(damage, false);
                    Destroy(gameObject);
                    return;
                }

                t = t.parent;
            }

            // Solid world hit
            if (!other.isTrigger)
                Destroy(gameObject);
        }
    }
}
