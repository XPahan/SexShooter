using UnityEngine;

namespace SexShooter.Dev.Vfx
{
    /// <summary>
    /// Procedural gore burst (blood particles + physics gibs). Ported from AdultShooter.
    /// </summary>
    public class GoreDeathEffect : MonoBehaviour
    {
        private static Material _bloodParticleMaterial;
        private static Material _gibMaterial;

        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private float _partForceMin = 22f;
        [SerializeField] private float _partForceMax = 42f;
        [SerializeField] private float _partDespawnDelay = 1f;
        [SerializeField] private float _overallScale = 1f;

        private void Awake()
        {
            EnsureMaterials();
            var scale = Mathf.Max(0.1f, _overallScale);
            CreateBloodBurst(scale);
            CreateBloodSplatter(scale);
            SpawnBodyParts(scale);
        }

        private void Start()
        {
            Destroy(gameObject, _lifetime);
        }

        private static void EnsureMaterials()
        {
            if (_bloodParticleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit");
                _bloodParticleMaterial = new Material(shader)
                {
                    color = new Color(0.55f, 0.02f, 0.02f, 1f)
                };
            }

            if (_gibMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                _gibMaterial = new Material(shader)
                {
                    color = new Color(0.42f, 0.04f, 0.04f, 1f)
                };
            }
        }

        private void CreateBloodBurst(float scale)
        {
            var go = new GameObject("BloodBurst");
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.175f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f * scale, 7f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f * scale, 0.1f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.03f, 0.03f, 1f),
                new Color(0.35f, 0.01f, 0.01f, 1f));
            main.gravityModifier = 0.525f;
            main.maxParticles = 700;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)400) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f * scale;
            go.transform.localRotation = Quaternion.LookRotation(Vector3.up);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _bloodParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }

        private void CreateBloodSplatter(float scale)
        {
            var go = new GameObject("BloodSplatter");
            go.transform.SetParent(transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.275f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f * scale, 10f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f * scale, 0.06f * scale);
            main.startColor = new Color(0.75f, 0.04f, 0.03f, 1f);
            main.gravityModifier = 0.825f;
            main.maxParticles = 640;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)360) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f * scale;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _bloodParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.1f;
            renderer.velocityScale = 0.06f;

            ps.Play();
        }

        private void SpawnBodyParts(float scale)
        {
            var headDirection = (Vector3.up * 0.75f + Random.insideUnitSphere * 0.3f).normalized;
            var torsoDirection = (Vector3.back * 0.45f + Vector3.up * 0.5f + Random.insideUnitSphere * 0.25f).normalized;

            SpawnPhysicsPart(
                PrimitiveType.Sphere,
                "GoreHead",
                new Vector3(0f, 0.45f, 0f) * scale,
                Vector3.one * (0.28f * scale),
                headDirection,
                scale,
                1f);

            SpawnPhysicsPart(
                PrimitiveType.Cube,
                "GoreTorso",
                new Vector3(0f, 0.05f, 0f) * scale,
                new Vector3(0.36f, 0.52f, 0.21f) * scale,
                torsoDirection,
                scale,
                1.35f);

            SpawnPhysicsPart(
                PrimitiveType.Cube,
                "GoreLimb_1",
                new Vector3(-0.34f, 0.12f, 0f) * scale,
                new Vector3(0.1f, 0.36f, 0.1f) * scale,
                (new Vector3(-1f, 0.35f, 0.15f) + Random.insideUnitSphere * 0.2f).normalized,
                scale,
                0.85f);

            SpawnPhysicsPart(
                PrimitiveType.Cube,
                "GoreLimb_2",
                new Vector3(0.34f, 0.12f, 0f) * scale,
                new Vector3(0.1f, 0.36f, 0.1f) * scale,
                (new Vector3(1f, 0.35f, 0.15f) + Random.insideUnitSphere * 0.2f).normalized,
                scale,
                0.85f);

            SpawnPhysicsPart(
                PrimitiveType.Cube,
                "GoreLimb_3",
                new Vector3(-0.18f, -0.36f, 0f) * scale,
                new Vector3(0.11f, 0.4f, 0.11f) * scale,
                (new Vector3(-0.55f, 0.15f, 0.35f) + Random.insideUnitSphere * 0.2f).normalized,
                scale,
                0.85f);

            SpawnPhysicsPart(
                PrimitiveType.Cube,
                "GoreLimb_4",
                new Vector3(0.18f, -0.36f, 0f) * scale,
                new Vector3(0.11f, 0.4f, 0.11f) * scale,
                (new Vector3(0.55f, 0.15f, 0.35f) + Random.insideUnitSphere * 0.2f).normalized,
                scale,
                0.85f);
        }

        private void SpawnPhysicsPart(
            PrimitiveType primitiveType,
            string partName,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 launchDirection,
            float scale,
            float massMultiplier)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Random.rotation;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = _gibMaterial;

            var rb = part.AddComponent<Rigidbody>();
            var mass = Mathf.Max(localScale.x, localScale.y, localScale.z) * massMultiplier * 10f;
            rb.mass = mass;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var force = Random.Range(_partForceMin, _partForceMax) * scale;
            rb.AddForce(launchDirection.normalized * force, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * (force * 0.75f), ForceMode.Impulse);

            var despawn = part.AddComponent<GorePartDespawnOnCollision>();
            despawn.Initialize(_partDespawnDelay, _lifetime);
        }
    }
}
