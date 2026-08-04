#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SexShooter.Dev.Editor
{
    public static class DevContentSetup
    {
        private const string AnimPath = "Assets/_SexShooter/Dev/Animators/Succubus.controller";
        private const string ProjPath = "Assets/_SexShooter/Dev/Prefabs/Enemies/SuccubusProjectile.prefab";
        private const string EnemyPath = "Assets/_SexShooter/Dev/Prefabs/Enemies/Succubus.prefab";
        private const string MatPath = "Assets/_SexShooter/Dev/Prefabs/Enemies/SuccubusProjectileMat.mat";
        private const string GorePath = "Assets/_SexShooter/Dev/Prefabs/Vfx/EnemyGoreBurst.prefab";
        private const string DefinitionPath = "Assets/_SexShooter/Dev/Config/Enemies/Succubus.asset";
        private const string DeathAudioDest = "Assets/_SexShooter/Dev/Audio/Enemies/Succubus_Death.mp3";
        private const string DeathAudioSrc = @"D:\Development\Octogames\Projects\AdultShooter\Assets\_Game\Dev\Audio\Enemies\Succubus_Death.mp3";
        private const string DevPrefabPath = "Assets/_SexShooter/Dev/Dev.prefab";

        [MenuItem("SexShooter/Dev/Setup Succubus + Session")]
        public static void SetupAll()
        {
            EnsureFolders();
            var controller = BuildAnimator();
            var projectile = BuildProjectile();
            var gore = BuildGorePrefab();
            CopyDeathAudio();
            var definition = BuildEnemyDefinition(projectile, gore);
            var enemy = BuildEnemy(controller, projectile);
            WireDefinitionToEnemy(enemy, definition);
            WireIntoDev(enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Succubus + Session setup complete.");
        }

        [MenuItem("SexShooter/Dev/Setup Gore + Enemy Definition")]
        public static void SetupGoreAndDefinition()
        {
            EnsureFolders();
            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjPath);
            if (projectile == null) projectile = BuildProjectile();

            var gore = BuildGorePrefab();
            CopyDeathAudio();
            var definition = BuildEnemyDefinition(projectile, gore);

            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (enemy == null)
            {
                var controller = BuildAnimator();
                enemy = BuildEnemy(controller, projectile);
            }

            WireDefinitionToEnemy(enemy, definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Gore + EnemyDefinition wired.");
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets/_SexShooter/Dev/Animators");
            CreateFolder("Assets/_SexShooter/Dev/Prefabs");
            CreateFolder("Assets/_SexShooter/Dev/Prefabs/Enemies");
            CreateFolder("Assets/_SexShooter/Dev/Prefabs/Vfx");
            CreateFolder("Assets/_SexShooter/Dev/Config");
            CreateFolder("Assets/_SexShooter/Dev/Config/Enemies");
            CreateFolder("Assets/_SexShooter/Dev/Audio");
            CreateFolder("Assets/_SexShooter/Dev/Audio/Enemies");
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) CreateFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static AnimatorController BuildAnimator()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimPath);
            if (existing != null) return existing;

            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/DemonGirlSuccubus/FBX/DemonGirl_Upgrade.fbx");
            AnimationClip idle = null, walk = null, attack = null;
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    if (clip.name == "1_Idle") idle = clip;
                    if (clip.name == "2_Catwalk") walk = clip;
                    if (clip.name == "4_MagicAttack") attack = clip;
                }
            }

            if (idle == null || walk == null || attack == null)
                throw new Exception("DemonGirl clips not found.");

            var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle"); idleState.motion = idle;
            var walkState = sm.AddState("Walk"); walkState.motion = walk;
            var attackState = sm.AddState("Attack"); attackState.motion = attack;
            var hitState = sm.AddState("Hit"); hitState.motion = idle;
            sm.defaultState = idleState;

            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false; idleToWalk.duration = 0.1f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false; walkToIdle.duration = 0.1f;

            var anyAttack = sm.AddAnyStateTransition(attackState);
            anyAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyAttack.hasExitTime = false; anyAttack.duration = 0.05f;
            var attackExit = attackState.AddTransition(idleState);
            attackExit.hasExitTime = true; attackExit.exitTime = 0.9f; attackExit.duration = 0.1f;

            var anyHit = sm.AddAnyStateTransition(hitState);
            anyHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            anyHit.hasExitTime = false; anyHit.duration = 0.05f;
            var hitExit = hitState.AddTransition(idleState);
            hitExit.hasExitTime = true; hitExit.exitTime = 0.6f; hitExit.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject BuildProjectile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjPath);
            if (existing != null) return existing;

            var root = new GameObject("SuccubusProjectile");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.25f;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = new Color(0.7f, 0.2f, 1f, 1f) };
            AssetDatabase.CreateAsset(mat, MatPath);
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.2f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            root.AddComponent<SuccubusProjectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildEnemy(RuntimeAnimatorController controller, GameObject projectilePrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (existing != null) return existing;

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DemonGirlSuccubus/Prefabs/DemonGirl_var1.prefab");
            if (modelPrefab == null) throw new Exception("DemonGirl_var1 missing");

            var root = new GameObject("Succubus");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f; capsule.radius = 0.35f; capsule.center = new Vector3(0f, 0.9f, 0f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            model.name = "Model";
            foreach (var c in model.GetComponentsInChildren<CharacterController>(true))
                UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);

            var anim = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0.2f, 1.2f, 0.4f);

            var health = root.AddComponent<SuccubusEnemy>();
            var brain = root.AddComponent<SuccubusBrain>();

            var healthSO = new SerializedObject(health);
            healthSO.FindProperty("maxHealth").floatValue = 3f;
            healthSO.FindProperty("maxShield").floatValue = 0f;
            healthSO.FindProperty("destroyOnDie").boolValue = true;
            healthSO.FindProperty("_name").stringValue = "Succubus";
            healthSO.FindProperty("showKillFeed").boolValue = true;
            healthSO.FindProperty("showUI").boolValue = false;
            healthSO.ApplyModifiedPropertiesWithoutUndo();

            var brainSO = new SerializedObject(brain);
            brainSO.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            brainSO.FindProperty("animator").objectReferenceValue = anim;
            brainSO.FindProperty("projectilePrefab").objectReferenceValue =
                projectilePrefab.GetComponent<SuccubusProjectile>();
            brainSO.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildGorePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GorePath);
            if (existing != null) return existing;

            var root = new GameObject("EnemyGoreBurst");
            var gore = root.AddComponent<SexShooter.Dev.Vfx.GoreDeathEffect>();
            var so = new SerializedObject(gore);
            so.FindProperty("_lifetime").floatValue = 5f;
            so.FindProperty("_partForceMin").floatValue = 22f;
            so.FindProperty("_partForceMax").floatValue = 42f;
            so.FindProperty("_partDespawnDelay").floatValue = 1f;
            so.FindProperty("_overallScale").floatValue = 1.1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GorePath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CopyDeathAudio()
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(DeathAudioDest) != null) return;
            if (!System.IO.File.Exists(DeathAudioSrc)) return;

            var destAbs = System.IO.Path.GetFullPath(DeathAudioDest);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destAbs));
            System.IO.File.Copy(DeathAudioSrc, destAbs, true);
            AssetDatabase.ImportAsset(DeathAudioDest);
        }

        private static EnemyDefinition BuildEnemyDefinition(GameObject projectilePrefab, GameObject gorePrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
            var def = existing != null ? existing : ScriptableObject.CreateInstance<EnemyDefinition>();

            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "Succubus";
            so.FindProperty("_maxHealth").floatValue = 3f;
            so.FindProperty("_staggerDuration").floatValue = 0.35f;
            so.FindProperty("_moveSpeed").floatValue = 2.5f;
            so.FindProperty("_turnSpeed").floatValue = 8f;
            so.FindProperty("_gravity").floatValue = -20f;
            so.FindProperty("_attackRange").floatValue = 20f;
            so.FindProperty("_attackCooldown").floatValue = 2f;
            so.FindProperty("_projectileDamage").floatValue = 2f;
            so.FindProperty("_projectileSpeed").floatValue = 10f;
            so.FindProperty("_projectileLifetime").floatValue = 5f;
            so.FindProperty("_aimHeight").floatValue = 1.2f;
            so.FindProperty("_deathDespawnDelay").floatValue = 0.05f;
            so.FindProperty("_deathGoreScale").floatValue = 1f;
            so.FindProperty("_deathSoundVolume").floatValue = 6f;
            so.FindProperty("_projectilePrefab").objectReferenceValue =
                projectilePrefab != null ? projectilePrefab.GetComponent<SuccubusProjectile>() : null;
            so.FindProperty("_deathGorePrefab").objectReferenceValue = gorePrefab;

            var deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DeathAudioDest);
            if (deathClip != null)
                so.FindProperty("_deathSound").objectReferenceValue = deathClip;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(def, DefinitionPath);
            else
                EditorUtility.SetDirty(def);

            return def;
        }

        private static void WireDefinitionToEnemy(GameObject enemyPrefab, EnemyDefinition definition)
        {
            if (enemyPrefab == null || definition == null) return;

            var root = PrefabUtility.LoadPrefabContents(EnemyPath);
            try
            {
                var brain = root.GetComponent<SuccubusBrain>();
                if (brain == null) return;

                var so = new SerializedObject(brain);
                so.FindProperty("definition").objectReferenceValue = definition;
                so.FindProperty("deathGorePrefab").objectReferenceValue = definition.DeathGorePrefab;
                so.FindProperty("deathGoreScale").floatValue = definition.DeathGoreScale;
                so.FindProperty("deathSound").objectReferenceValue = definition.DeathSound;
                so.FindProperty("deathSoundVolume").floatValue = definition.DeathSoundVolume;
                if (definition.ProjectilePrefab != null)
                    so.FindProperty("projectilePrefab").objectReferenceValue = definition.ProjectilePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireIntoDev(GameObject enemyPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(DevPrefabPath);
            try
            {
                // Remove old session/spawner if re-running
                Transform oldSession = root.transform.Find("Session");
                if (oldSession != null) UnityEngine.Object.DestroyImmediate(oldSession.gameObject);

                var session = new GameObject("Session");
                session.transform.SetParent(root.transform, false);

                // Spawn markers around Room A
                var markers = new GameObject("EnemySpawnPoints");
                markers.transform.SetParent(session.transform, false);
                Vector3[] pts =
                {
                    new Vector3(75f, -15.5f, -55f),
                    new Vector3(80f, -15.5f, -60f),
                    new Vector3(70f, -15.5f, -50f),
                    new Vector3(85f, -15.5f, -65f),
                    new Vector3(60f, -15.5f, -55f),
                    new Vector3(78f, -15.5f, -70f)
                };
                var spawnTransforms = new Transform[pts.Length];
                for (int i = 0; i < pts.Length; i++)
                {
                    var p = new GameObject("Spawn_" + i);
                    p.transform.SetParent(markers.transform, false);
                    p.transform.position = pts[i];
                    spawnTransforms[i] = p.transform;
                }

                var spawner = session.AddComponent<EnemyWaveSpawner>();
                var spawnerSO = new SerializedObject(spawner);
                spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab.GetComponent<SuccubusEnemy>();
                spawnerSO.FindProperty("initialCount").intValue = 8;
                spawnerSO.FindProperty("maxAlive").intValue = 15;
                spawnerSO.FindProperty("spawnInterval").floatValue = 3f;
                spawnerSO.FindProperty("minDistanceFromPlayer").floatValue = 8f;
                spawnerSO.FindProperty("minSeparation").floatValue = 4f;
                // Ground: Default + Ground + Object
                spawnerSO.FindProperty("groundMask").intValue = (1 << 0) | (1 << 3) | (1 << 8);
                // Blockage: similar + Enemy
                spawnerSO.FindProperty("blockageMask").intValue = (1 << 0) | (1 << 3) | (1 << 7) | (1 << 8);
                var fallback = spawnerSO.FindProperty("fallbackSpawnPoints");
                fallback.arraySize = spawnTransforms.Length;
                for (int i = 0; i < spawnTransforms.Length; i++)
                    fallback.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
                spawnerSO.ApplyModifiedPropertiesWithoutUndo();

                var sessionCtrl = session.AddComponent<GameSessionController>();
                var sessionSO = new SerializedObject(sessionCtrl);
                sessionSO.FindProperty("spawner").objectReferenceValue = spawner;
                var spawnMarker = root.transform.Find("PlayerSpawn");
                if (spawnMarker != null)
                    sessionSO.FindProperty("playerSpawn").objectReferenceValue = spawnMarker;
                // PlayerDependencies assigned at runtime via Find if null
                sessionSO.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, DevPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
