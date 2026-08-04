using UnityEngine;

namespace SexShooter.Dev
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "SexShooter/Dev/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Succubus";

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 3f;
        [SerializeField] private float _staggerDuration = 0.35f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _turnSpeed = 8f;
        [SerializeField] private float _gravity = -20f;

        [Header("Combat")]
        [SerializeField] private float _attackRange = 20f;
        [SerializeField] private float _attackCooldown = 2f;
        [SerializeField] private float _projectileDamage = 2f;
        [SerializeField] private float _projectileSpeed = 10f;
        [SerializeField] private float _projectileLifetime = 5f;
        [SerializeField] private float _aimHeight = 1.2f;
        [SerializeField] private SuccubusProjectile _projectilePrefab;
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private float _muzzleFlashScale = 0.35f;
        [SerializeField] private GameObject _impactPrefab;

        [Header("Death")]
        [SerializeField] private float _deathDespawnDelay = 0.05f;
        [SerializeField] private GameObject _deathGorePrefab;
        [SerializeField] private float _deathGoreScale = 1f;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private float _deathSoundVolume = 6f;

        [Header("Attack Audio")]
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private float _attackSoundVolume = 1f;

        [Header("Spawn")]
        [SerializeField] private AudioClip _spawnSound;
        [SerializeField] private float _spawnSoundVolume = 1f;
        [SerializeField] private GameObject _spawnVfxPrefab;

        public string DisplayName => _displayName;
        public float MaxHealth => _maxHealth;
        public float StaggerDuration => _staggerDuration;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float Gravity => _gravity;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float ProjectileDamage => _projectileDamage;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float AimHeight => _aimHeight;
        public SuccubusProjectile ProjectilePrefab => _projectilePrefab;
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public float MuzzleFlashScale => Mathf.Max(0.01f, _muzzleFlashScale);
        public GameObject ImpactPrefab => _impactPrefab;
        public float DeathDespawnDelay => Mathf.Max(0.01f, _deathDespawnDelay);
        public GameObject DeathGorePrefab => _deathGorePrefab;
        public float DeathGoreScale => Mathf.Max(0.1f, _deathGoreScale);
        public AudioClip DeathSound => _deathSound;
        public float DeathSoundVolume => Mathf.Clamp(_deathSoundVolume, 0f, 10f);
        public AudioClip AttackSound => _attackSound;
        public float AttackSoundVolume => Mathf.Clamp01(_attackSoundVolume);
        public AudioClip SpawnSound => _spawnSound;
        public float SpawnSoundVolume => Mathf.Clamp01(_spawnSoundVolume);
        public GameObject SpawnVfxPrefab => _spawnVfxPrefab;
    }
}
