using UnityEngine;

namespace SexShooter.Dev.Vfx
{
    public class GorePartDespawnOnCollision : MonoBehaviour
    {
        private float _delay = 1f;
        private bool _collisionHandled;

        public void Initialize(float delay, float maxLifetime)
        {
            _delay = Mathf.Max(0f, delay);
            Invoke(nameof(Despawn), maxLifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_collisionHandled) return;
            _collisionHandled = true;
            CancelInvoke(nameof(Despawn));
            Destroy(gameObject, _delay);
        }

        private void Despawn() => Destroy(gameObject);
    }
}
