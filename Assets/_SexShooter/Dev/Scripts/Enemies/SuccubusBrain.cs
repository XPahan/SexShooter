using System.Collections;
using cowsins;
using UnityEngine;

namespace SexShooter.Dev
{
    /// <summary>
    /// Chase player; ranged bolts or melee swipe depending on EnemyDefinition.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SuccubusEnemy))]
    public class SuccubusBrain : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private EnemyDefinition definition;

        [Header("Fallback Movement (if no SO)")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float gravity = -20f;

        [Header("Fallback Combat (if no SO)")]
        [SerializeField] private float attackRange = 20f;
        [SerializeField] private float visionRange = 40f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float aimHeight = 1.2f;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileDamage = 2f;
        [SerializeField] private float projectileLifetime = 5f;
        [SerializeField] private float meleeDamage = 2f;
        [SerializeField] private float meleeHitDelay = 0.4f;
        [SerializeField] private float meleeHitRadius = 1.8f;
        [SerializeField] private LayerMask losMask = ~0;
        [SerializeField] private Transform muzzle;
        [SerializeField] private SuccubusProjectile projectilePrefab;

        [Header("Fallback Death (if no SO)")]
        [SerializeField] private GameObject deathGorePrefab;
        [SerializeField] private float deathGoreScale = 1f;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private float deathSoundVolume = 6f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string hitTrigger = "Hit";

        private CharacterController controller;
        private SuccubusEnemy enemy;
        private Transform player;
        private float verticalVelocity;
        private float nextAttackTime;
        private bool dead;
        private bool attackInProgress;
        private bool aggroed;
        private int hitLayerIndex = -1;
        private Coroutine meleeRoutine;
        private readonly RaycastHit[] losHits = new RaycastHit[8];

        public EnemyDefinition Definition => definition;

        public void SetPlayer(Transform playerTransform) => player = playerTransform;

        public void SetDefinition(EnemyDefinition def) => definition = def;

        private bool IsMelee => definition != null ? definition.IsMelee : projectilePrefab == null;
        private float MoveSpeed => definition != null ? definition.MoveSpeed : moveSpeed;
        private float TurnSpeed => definition != null ? definition.TurnSpeed : turnSpeed;
        private float Gravity => definition != null ? definition.Gravity : gravity;
        private float AttackRange => definition != null ? definition.AttackRange : attackRange;
        private float VisionRange => definition != null ? definition.VisionRange : visionRange;
        private float AttackCooldown => definition != null ? definition.AttackCooldown : attackCooldown;
        private float AimHeight => definition != null ? definition.AimHeight : aimHeight;
        private float ProjectileSpeed => definition != null ? definition.ProjectileSpeed : projectileSpeed;
        private float ProjectileDamage => definition != null ? definition.ProjectileDamage : projectileDamage;
        private float ProjectileLifetime => definition != null ? definition.ProjectileLifetime : projectileLifetime;
        private float MeleeDamage => definition != null ? definition.MeleeDamage : meleeDamage;
        private float MeleeHitDelay => definition != null ? definition.MeleeHitDelay : meleeHitDelay;
        private float MeleeHitRadius => definition != null ? definition.MeleeHitRadius : meleeHitRadius;
        private SuccubusProjectile ProjectilePrefab =>
            definition != null && definition.ProjectilePrefab != null ? definition.ProjectilePrefab : projectilePrefab;
        private GameObject DeathGorePrefab =>
            definition != null && definition.DeathGorePrefab != null ? definition.DeathGorePrefab : deathGorePrefab;
        private float DeathGoreScale => definition != null ? definition.DeathGoreScale : deathGoreScale;
        private AudioClip DeathSound => definition != null ? definition.DeathSound : deathSound;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            enemy = GetComponent<SuccubusEnemy>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (muzzle == null)
            {
                var m = transform.Find("Muzzle");
                if (m != null) muzzle = m;
            }

            if (animator != null)
            {
                for (int i = 0; i < animator.layerCount; i++)
                {
                    if (animator.GetLayerName(i) == "Hit")
                    {
                        hitLayerIndex = i;
                        animator.SetLayerWeight(i, 1f);
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (meleeRoutine != null)
            {
                StopCoroutine(meleeRoutine);
                meleeRoutine = null;
            }
            attackInProgress = false;
        }

        private void Update()
        {
            if (dead || enemy.IsDead || player == null) return;
            if (enemy.IsStaggered)
            {
                ApplyGravityOnly();
                SetAnimSpeed(0f);
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            bool canSee = CanSeePlayer(distance);

            if (!aggroed && canSee)
                aggroed = true;

            if (!aggroed)
            {
                ApplyGravityOnly();
                SetAnimSpeed(0f);
                return;
            }

            if (distance > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
            }

            if (attackInProgress)
            {
                ApplyGravityOnly();
                SetAnimSpeed(0f);
                return;
            }

            if (distance > AttackRange)
            {
                Vector3 move = toPlayer.normalized * MoveSpeed;
                Move(move);
                SetAnimSpeed(MoveSpeed);
            }
            else
            {
                ApplyGravityOnly();
                SetAnimSpeed(0f);
                if (canSee)
                    TryAttack();
            }
        }

        private bool CanSeePlayer(float flatDistance)
        {
            if (player == null) return false;
            float maxRange = Mathf.Max(VisionRange, AttackRange);
            if (flatDistance > maxRange) return false;

            Vector3 origin = transform.position + Vector3.up * AimHeight;
            Vector3 target = player.position + Vector3.up * AimHeight;
            Vector3 delta = target - origin;
            float dist = delta.magnitude;
            if (dist < 0.05f) return true;

            Vector3 dir = delta / dist;
            int count = Physics.RaycastNonAlloc(
                origin, dir, losHits, dist, losMask, QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            Collider nearestCol = null;
            for (int i = 0; i < count; i++)
            {
                var col = losHits[i].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                float d = losHits[i].distance;
                if (d < nearest)
                {
                    nearest = d;
                    nearestCol = col;
                }
            }

            if (nearestCol == null) return true;
            return IsPlayerCollider(nearestCol);
        }

        private static bool IsPlayerCollider(Collider col)
        {
            if (col.GetComponentInParent<PlayerStats>() != null) return true;
            return col.CompareTag("Player");
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime) return;

            if (IsMelee)
            {
                nextAttackTime = Time.time + AttackCooldown;
                meleeRoutine = StartCoroutine(MeleeAttackRoutine());
                return;
            }

            var prefab = ProjectilePrefab;
            if (prefab == null) return;
            nextAttackTime = Time.time + AttackCooldown;

            if (animator != null && !string.IsNullOrEmpty(attackTrigger))
                animator.SetTrigger(attackTrigger);

            Vector3 origin = muzzle != null
                ? muzzle.position
                : transform.position + Vector3.up * AimHeight;

            Vector3 target = player.position + Vector3.up * AimHeight;
            Vector3 dir = (target - origin).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

            if (definition != null && definition.AttackSound != null)
                EnemySfx.Play3D(definition.AttackSound, origin, definition.AttackSoundVolume);

            if (definition != null && definition.MuzzleFlashPrefab != null)
            {
                var flash = Instantiate(definition.MuzzleFlashPrefab, origin, Quaternion.LookRotation(dir));
                flash.transform.localScale = Vector3.one * definition.MuzzleFlashScale;
                Destroy(flash, 2f);
            }

            GameObject impact = definition != null ? definition.ImpactPrefab : null;
            var bolt = Instantiate(prefab, origin, Quaternion.LookRotation(dir));
            bolt.Launch(dir, ProjectileSpeed, ProjectileDamage, ProjectileLifetime, impact);
        }

        private IEnumerator MeleeAttackRoutine()
        {
            attackInProgress = true;

            if (animator != null && !string.IsNullOrEmpty(attackTrigger))
                animator.SetTrigger(attackTrigger);

            Vector3 origin = transform.position + Vector3.up * AimHeight;
            if (definition != null && definition.AttackSound != null)
                EnemySfx.Play3D(definition.AttackSound, origin, definition.AttackSoundVolume);

            float delay = MeleeHitDelay;
            float elapsed = 0f;
            while (elapsed < delay)
            {
                if (dead || enemy.IsDead || enemy.IsStaggered)
                {
                    attackInProgress = false;
                    meleeRoutine = null;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyMeleeHit();
            yield return new WaitForSeconds(0.35f);
            attackInProgress = false;
            meleeRoutine = null;
        }

        private void ApplyMeleeHit()
        {
            if (player == null || dead || enemy.IsDead) return;

            Vector3 center = transform.position + Vector3.up * AimHeight + transform.forward * 0.6f;
            float radius = MeleeHitRadius;
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;

                var stats = col.GetComponentInParent<PlayerStats>();
                if (stats != null)
                {
                    if (!stats.IsDead) stats.Damage(MeleeDamage, false);
                    return;
                }

                if (col.CompareTag("Player"))
                {
                    var dmg = col.GetComponentInParent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.Damage(MeleeDamage, false);
                        return;
                    }
                }
            }
        }

        private void Move(Vector3 horizontal)
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Gravity * Time.deltaTime;
            Vector3 velocity = horizontal + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyGravityOnly() => Move(Vector3.zero);

        private void SetAnimSpeed(float speed)
        {
            if (animator != null && !string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, speed);
        }

        public void NotifyHit()
        {
            aggroed = true;
            if (dead || animator == null || string.IsNullOrEmpty(hitTrigger)) return;
            if (hitLayerIndex >= 0)
                animator.SetLayerWeight(hitLayerIndex, 1f);
            animator.ResetTrigger(hitTrigger);
            animator.SetTrigger(hitTrigger);
        }

        public void NotifyDeath()
        {
            if (dead) return;
            dead = true;
            attackInProgress = false;
            if (meleeRoutine != null)
            {
                StopCoroutine(meleeRoutine);
                meleeRoutine = null;
            }

            if (controller != null) controller.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            SetAnimSpeed(0f);

            HideModel();
            PlayDeathSound();
            SpawnDeathGore();
        }

        private void HideModel()
        {
            var model = transform.Find("Model");
            var root = model != null ? model : transform;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        private void PlayDeathSound()
        {
            var clip = DeathSound;
            if (clip == null) return;
            EnemySfx.Play3D(clip, transform.position + Vector3.up * AimHeight, 1f);
        }

        private void SpawnDeathGore()
        {
            var gorePrefab = DeathGorePrefab;
            if (gorePrefab == null) return;

            Vector3 spawnPosition = transform.position + Vector3.up * AimHeight;
            var gore = Instantiate(gorePrefab, spawnPosition, Quaternion.identity);
            gore.transform.localScale = Vector3.one * DeathGoreScale;
        }
    }
}
