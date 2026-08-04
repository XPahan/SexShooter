using cowsins;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Cowsins EnemyHealth with AdultShooter-style stagger on hit.
    /// Reads HP/stagger from EnemyDefinition when present on SuccubusBrain.
    /// </summary>
    public class SuccubusEnemy : EnemyHealth
    {
        [SerializeField] private float staggerDuration = 0.35f;

        private float staggerUntil;
        private SuccubusBrain brain;

        public bool IsStaggered => Time.time < staggerUntil;

        public override void Start()
        {
            brain = GetComponent<SuccubusBrain>();
            ApplyDefinition();
            base.Start();
        }

        private void ApplyDefinition()
        {
            if (brain == null || brain.Definition == null) return;

            var def = brain.Definition;
            maxHealth = def.MaxHealth;
            maxShield = 0f;
            staggerDuration = def.StaggerDuration;
            _name = def.DisplayName;
            showKillFeed = true;
        }

        public override void Damage(float damage, bool isHeadshot)
        {
            if (isDead) return;

            float healthBefore = health;
            base.Damage(damage, isHeadshot);

            if (!isDead && health < healthBefore)
            {
                staggerUntil = Time.time + staggerDuration;
                brain?.NotifyHit();
            }
        }

        public override void Die()
        {
            if (isDead) return;
            brain?.NotifyDeath();

            // Hide body immediately; gore is the death visual.
            // Still call base for killfeed / events, then destroy quickly.
            isDead = true;
            events.OnDeath.Invoke();
            if (showKillFeed) UIEvents.onEnemyKilled.Invoke(_name);

            float delay = brain != null && brain.Definition != null
                ? brain.Definition.DeathDespawnDelay
                : 0.05f;

            Destroy(gameObject, delay);
        }
    }
}
