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
        private const string MeleeEnemyPath = "Assets/_SexShooter/Dev/Prefabs/Enemies/SuccubusMelee.prefab";
        private const string MatPath = "Assets/_SexShooter/Dev/Prefabs/Enemies/SuccubusProjectileMat.mat";
        private const string GorePath = "Assets/_SexShooter/Dev/Prefabs/Vfx/EnemyGoreBurst.prefab";
        private const string DefinitionPath = "Assets/_SexShooter/Dev/Config/Enemies/Succubus.asset";
        private const string MeleeDefinitionPath = "Assets/_SexShooter/Dev/Config/Enemies/SuccubusMelee.asset";
        private const string MeleeModelPath = "Assets/DemonGirlSuccubus/Prefabs/DemonGirl_var6.prefab";
        private const string DeathAudioDest = "Assets/_SexShooter/Dev/Audio/Enemies/Succubus_Death.mp3";
        private const string AttackAudioDest = "Assets/_SexShooter/Dev/Audio/Enemies/Succubus_Attack.wav";
        private const string SpawnAudioDest = "Assets/_SexShooter/Dev/Audio/Enemies/Succubus_Spawn.mp3";
        private const string DeathAudioSrc = @"D:\Development\Octogames\Projects\AdultShooter\Assets\_Game\Dev\Audio\Enemies\Succubus_Death.mp3";
        private const string AttackAudioSrc = @"D:\Development\Octogames\Projects\AdultShooter\Assets\_Game\Dev\Audio\Enemies\Succubus_Attack.wav";
        private const string SpawnAudioSrc = @"D:\Development\Octogames\Projects\AdultShooter\Assets\_Game\Dev\Audio\Enemies\Succubus_Spawn.mp3";
        private const string EffectProjectileSrc = "Assets/EffectCore/packs/StylizedProjectilePack1/prefabs/Plasma/Plasma_PurpleHaze/Plasma_Medium_PurpleHaze/Plasma_PurpleHaze_Medium_Projectile.prefab";
        private const string EffectMuzzleSrc = "Assets/EffectCore/packs/StylizedProjectilePack1/prefabs/Plasma/Plasma_PurpleHaze/Plasma_Medium_PurpleHaze/Plasma_PurpleHaze_Medium_MuzzleFlare.prefab";
        private const string EffectImpactSrc = "Assets/EffectCore/packs/StylizedProjectilePack1/prefabs/Plasma/Plasma_PurpleHaze/Plasma_Medium_PurpleHaze/Plasma_PurpleHaze_Medium_Impact.prefab";
        private const string BgmPath = "Assets/Aggressive FPS Game Music/intensity 1.wav";
        private const string DevPrefabPath = "Assets/_SexShooter/Dev/Dev.prefab";
        private const string SpawnVfxPath = "Assets/_SexShooter/Dev/Prefabs/Vfx/TeleportFinish.prefab";
        private const string PistolPath = "Assets/Cowsins/ScriptableObjects/Weapons/Pistol.asset";
        private const string RiflePath = "Assets/Cowsins/ScriptableObjects/Weapons/Rifle.asset";
        private const string ShotgunPath = "Assets/Cowsins/ScriptableObjects/Weapons/Shotgun.asset";
        private const string BulletPickupPath = "Assets/Cowsins/Prefabs/DragAndDropExtras/Bullet Pickeable.prefab";
        private const string HealthPickupPath = "Assets/Cowsins/Prefabs/DragAndDropExtras/PowerUps/Healthpack.prefab";

        [MenuItem("SexShooter/Dev/Setup Succubus + Session")]
        public static void SetupAll()
        {
            EnsureFolders();
            var controller = BuildAnimator();
            var projectile = BuildEffectCoreProjectile(forceRebuild: false);
            var gore = BuildGorePrefab();
            CopyEnemyAudio();
            var definition = BuildEnemyDefinition(projectile, gore);
            var enemy = BuildEnemy(controller, projectile);
            WireDefinitionToEnemy(enemy, definition);
            WireIntoDev(enemy);
            WireSessionMusic();
            AssetDatabase.SaveAssets();
            Debug.Log("[SexShooter.Dev] Setup complete.");
        }

        [MenuItem("SexShooter/Dev/Wire World Spawn Points To Session")]
        public static void WireWorldSpawnPointsToSession()
        {
            var root = GameObject.Find("World");
            if (root == null)
            {
                Debug.LogError("[SexShooter.Dev] World not found in loaded scenes.");
                return;
            }

            var pointsRoot = root.transform.Find("Spawn_Points");
            if (pointsRoot == null)
            {
                Debug.LogError("[SexShooter.Dev] World/Spawn_Points not found.");
                return;
            }

            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[SexShooter.Dev] EnemyWaveSpawner not found in loaded scenes.");
                return;
            }

            var children = new Transform[pointsRoot.childCount];
            for (int i = 0; i < pointsRoot.childCount; i++)
                children[i] = pointsRoot.GetChild(i);

            var so = new SerializedObject(spawner);
            var prop = so.FindProperty("spawnPoints");
            prop.arraySize = children.Length;
            for (int i = 0; i < children.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
            EditorUtility.SetDirty(spawner);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

            // Remove old Dev fallback folder if still present under Session.
            var markers = spawner.transform.Find("EnemySpawnPoints");
            if (markers != null)
                UnityEngine.Object.DestroyImmediate(markers.gameObject);

            Debug.Log("[SexShooter.Dev] Wired " + children.Length + " World/Spawn_Points into " + spawner.name + ".");
        }

        [MenuItem("SexShooter/Dev/Setup Gore + Enemy Definition")]
        public static void SetupGoreAndDefinition()
        {
            EnsureFolders();
            var projectile = BuildEffectCoreProjectile(forceRebuild: false);
            var gore = BuildGorePrefab();
            CopyEnemyAudio();
            var definition = BuildEnemyDefinition(projectile, gore);

            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (enemy == null)
            {
                var controller = BuildAnimator();
                enemy = BuildEnemy(controller, projectile);
            }

            WireDefinitionToEnemy(enemy, definition);
            WireSessionMusic();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Gore + EnemyDefinition wired.");
        }

        [MenuItem("SexShooter/Dev/Setup Audio VFX Music")]
        public static void SetupAudioVfxMusic()
        {
            EnsureFolders();
            CopyEnemyAudio();
            var projectile = BuildEffectCoreProjectile(forceRebuild: true);
            var gore = AssetDatabase.LoadAssetAtPath<GameObject>(GorePath);
            if (gore == null) gore = BuildGorePrefab();
            var definition = BuildEnemyDefinition(projectile, gore);
            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (enemy != null) WireDefinitionToEnemy(enemy, definition);
            WireSessionMusic();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Audio + EffectCore VFX + Music wired.");
        }

        [MenuItem("SexShooter/Dev/Setup Weapons Pickups SpawnVFX")]
        public static void SetupWeaponsPickupsSpawnVfx()
        {
            EnsureFolders();
            WireInitialWeapons();
            WirePickupSpawner();
            WireSpawnVfx();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Weapons + Pickups + SpawnVFX wired.");
        }

        [MenuItem("SexShooter/Dev/Setup Melee Succubus (var6)")]
        public static void SetupMeleeSuccubus()
        {
            EnsureFolders();
            var controller = BuildAnimator();
            var gore = AssetDatabase.LoadAssetAtPath<GameObject>(GorePath);
            if (gore == null) gore = BuildGorePrefab();
            CopyEnemyAudio();
            var definition = BuildMeleeEnemyDefinition(gore);
            var enemy = BuildMeleeEnemy(controller);
            WireDefinitionToEnemyPrefab(MeleeEnemyPath, definition);
            WireMeleeIntoSpawner(enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SexShooter.Dev] Melee Succubus (var6) ready.");
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
            return BuildEffectCoreProjectile(forceRebuild: false);
        }

        private static GameObject BuildEffectCoreProjectile(bool forceRebuild)
        {
            if (!forceRebuild)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjPath);
                if (existing != null && existing.GetComponent<SuccubusProjectile>() != null
                    && existing.GetComponentInChildren<ParticleSystem>() != null)
                    return existing;
            }

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(EffectProjectileSrc);
            if (src == null)
            {
                Debug.LogWarning("[SexShooter.Dev] EffectCore plasma projectile missing, falling back to simple sphere.");
                return BuildSimpleProjectileFallback();
            }

            var impact = AssetDatabase.LoadAssetAtPath<GameObject>(EffectImpactSrc);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(src);
            root.name = "SuccubusProjectile";

            // Strip EffectCore gameplay scripts; keep particle visuals.
            foreach (var mb in root.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var typeName = mb.GetType().Name;
                if (typeName.StartsWith("EC") || typeName.Contains("particleColorChanger"))
                    UnityEngine.Object.DestroyImmediate(mb);
            }

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var col = root.GetComponent<SphereCollider>();
            if (col == null) col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.25f;

            var proj = root.GetComponent<SuccubusProjectile>();
            if (proj == null) proj = root.AddComponent<SuccubusProjectile>();
            var projSO = new SerializedObject(proj);
            var impactProp = projSO.FindProperty("impactPrefab");
            if (impactProp != null) impactProp.objectReferenceValue = impact;
            projSO.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ProjPath) != null)
                AssetDatabase.DeleteAsset(ProjPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildSimpleProjectileFallback()
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
            healthSO.FindProperty("showKillFeed").boolValue = false;
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

        private static void CopyDeathAudio() => CopyEnemyAudio();

        private static void CopyEnemyAudio()
        {
            CopyAudioIfNeeded(DeathAudioSrc, DeathAudioDest);
            CopyAudioIfNeeded(AttackAudioSrc, AttackAudioDest);
            CopyAudioIfNeeded(SpawnAudioSrc, SpawnAudioDest);
        }

        private static void CopyAudioIfNeeded(string src, string dst)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(dst) != null) return;
            if (!System.IO.File.Exists(src)) return;
            var destAbs = System.IO.Path.GetFullPath(dst);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destAbs));
            System.IO.File.Copy(src, destAbs, true);
            AssetDatabase.ImportAsset(dst);
        }

        private static EnemyDefinition BuildEnemyDefinition(GameObject projectilePrefab, GameObject gorePrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
            var def = existing != null ? existing : ScriptableObject.CreateInstance<EnemyDefinition>();

            var muzzle = AssetDatabase.LoadAssetAtPath<GameObject>(EffectMuzzleSrc);
            var impact = AssetDatabase.LoadAssetAtPath<GameObject>(EffectImpactSrc);

            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "Succubus";
            so.FindProperty("_combatStyle").enumValueIndex = (int)EnemyCombatStyle.Ranged;
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
            so.FindProperty("_muzzleFlashScale").floatValue = 0.35f;
            so.FindProperty("_attackSoundVolume").floatValue = 1f;
            so.FindProperty("_spawnSoundVolume").floatValue = 1f;
            so.FindProperty("_projectilePrefab").objectReferenceValue =
                projectilePrefab != null ? projectilePrefab.GetComponent<SuccubusProjectile>() : null;
            so.FindProperty("_deathGorePrefab").objectReferenceValue = gorePrefab;
            so.FindProperty("_muzzleFlashPrefab").objectReferenceValue = muzzle;
            so.FindProperty("_impactPrefab").objectReferenceValue = impact;

            var deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DeathAudioDest);
            if (deathClip != null) so.FindProperty("_deathSound").objectReferenceValue = deathClip;
            var attackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AttackAudioDest);
            if (attackClip != null) so.FindProperty("_attackSound").objectReferenceValue = attackClip;
            var spawnClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SpawnAudioDest);
            if (spawnClip != null) so.FindProperty("_spawnSound").objectReferenceValue = spawnClip;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(def, DefinitionPath);
            else
                EditorUtility.SetDirty(def);

            return def;
        }

        private static void WireSessionMusic()
        {
            var bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            if (bgm == null)
            {
                Debug.LogWarning("[SexShooter.Dev] BGM missing: " + BgmPath);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(DevPrefabPath);
            try
            {
                var session = root.transform.Find("Session");
                if (session == null) return;
                var ctrl = session.GetComponent<GameSessionController>();
                if (ctrl == null) return;
                var so = new SerializedObject(ctrl);
                so.FindProperty("backgroundMusic").objectReferenceValue = bgm;
                so.FindProperty("backgroundMusicVolume").floatValue = 0.35f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, DevPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireInitialWeapons()
        {
            var pistol = AssetDatabase.LoadAssetAtPath<cowsins.Weapon_SO>(PistolPath);
            var rifle = AssetDatabase.LoadAssetAtPath<cowsins.Weapon_SO>(RiflePath);
            var shotgun = AssetDatabase.LoadAssetAtPath<cowsins.Weapon_SO>(ShotgunPath);
            if (pistol == null || rifle == null || shotgun == null)
            {
                Debug.LogError("[SexShooter.Dev] Missing Pistol/Rifle/Shotgun Weapon_SO.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(DevPrefabPath);
            try
            {
                var wc = root.GetComponentInChildren<cowsins.WeaponController>(true);
                if (wc == null)
                {
                    Debug.LogError("[SexShooter.Dev] WeaponController not found in Dev.prefab.");
                    return;
                }

                var so = new SerializedObject(wc);
                var settings = so.FindProperty("settings");
                settings.FindPropertyRelative("inventorySize").intValue = 3;
                var iw = settings.FindPropertyRelative("initialWeapons");
                iw.arraySize = 3;
                iw.GetArrayElementAtIndex(0).objectReferenceValue = pistol;
                iw.GetArrayElementAtIndex(1).objectReferenceValue = rifle;
                iw.GetArrayElementAtIndex(2).objectReferenceValue = shotgun;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, DevPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WirePickupSpawner()
        {
            var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPickupPath);
            var health = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPickupPath);

            var root = PrefabUtility.LoadPrefabContents(DevPrefabPath);
            try
            {
                var session = root.transform.Find("Session");
                if (session == null)
                {
                    Debug.LogError("[SexShooter.Dev] Session missing in Dev.prefab.");
                    return;
                }

                var pickup = session.GetComponent<PickupSpawner>();
                if (pickup == null) pickup = session.gameObject.AddComponent<PickupSpawner>();

                var so = new SerializedObject(pickup);
                so.FindProperty("minDistanceFromPlayer").floatValue = 5f;
                so.FindProperty("minSeparation").floatValue = 3f;
                so.FindProperty("clearanceRadius").floatValue = 0.5f;
                so.FindProperty("groundMask").intValue = (1 << 0) | (1 << 3) | (1 << 8);
                so.FindProperty("blockageMask").intValue = (1 << 0) | (1 << 3) | (1 << 7) | (1 << 8);

                var entries = so.FindProperty("entries");
                entries.arraySize = 0;
                void AddEntry(GameObject prefab, int count)
                {
                    if (prefab == null) return;
                    int i = entries.arraySize;
                    entries.arraySize = i + 1;
                    var e = entries.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    e.FindPropertyRelative("count").intValue = count;
                }
                AddEntry(bullet, 10);
                AddEntry(health, 4);

                // Pickup fallbacks stay empty — assign World/Spawn_Points in Inspector if needed.
                var fallback = so.FindProperty("fallbackSpawnPoints");
                if (fallback != null)
                    fallback.arraySize = 0;

                so.ApplyModifiedPropertiesWithoutUndo();

                var ctrl = session.GetComponent<GameSessionController>();
                if (ctrl != null)
                {
                    var cso = new SerializedObject(ctrl);
                    cso.FindProperty("pickupSpawner").objectReferenceValue = pickup;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, DevPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireSpawnVfx()
        {
            var spawnVfx = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnVfxPath);
            if (spawnVfx == null)
            {
                Debug.LogWarning("[SexShooter.Dev] Spawn VFX missing: " + SpawnVfxPath);
                return;
            }

            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
            if (def == null)
            {
                Debug.LogWarning("[SexShooter.Dev] EnemyDefinition missing: " + DefinitionPath);
                return;
            }

            var so = new SerializedObject(def);
            so.FindProperty("_spawnVfxPrefab").objectReferenceValue = spawnVfx;
            var scale = so.FindProperty("_spawnVfxScale");
            if (scale != null) scale.floatValue = 9f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        private static void WireDefinitionToEnemy(GameObject enemyPrefab, EnemyDefinition definition)
        {
            WireDefinitionToEnemyPrefab(EnemyPath, definition);
        }

        private static void WireDefinitionToEnemyPrefab(string prefabPath, EnemyDefinition definition)
        {
            if (definition == null) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
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

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static EnemyDefinition BuildMeleeEnemyDefinition(GameObject gorePrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(MeleeDefinitionPath);
            var def = existing != null ? existing : ScriptableObject.CreateInstance<EnemyDefinition>();
            var ranged = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);

            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "Succubus Melee";
            so.FindProperty("_combatStyle").enumValueIndex = (int)EnemyCombatStyle.Melee;
            so.FindProperty("_maxHealth").floatValue = 4f;
            so.FindProperty("_staggerDuration").floatValue = 0.4f;
            so.FindProperty("_moveSpeed").floatValue = 4.2f;
            so.FindProperty("_turnSpeed").floatValue = 10f;
            so.FindProperty("_gravity").floatValue = -20f;
            so.FindProperty("_attackRange").floatValue = 2.2f;
            so.FindProperty("_attackCooldown").floatValue = 1.25f;
            so.FindProperty("_aimHeight").floatValue = 1.2f;
            so.FindProperty("_meleeDamage").floatValue = 2f;
            so.FindProperty("_meleeHitDelay").floatValue = 0.45f;
            so.FindProperty("_meleeHitRadius").floatValue = 1.9f;
            so.FindProperty("_deathDespawnDelay").floatValue = 0.05f;
            so.FindProperty("_deathGoreScale").floatValue = 1f;
            so.FindProperty("_deathSoundVolume").floatValue = 1f;
            so.FindProperty("_attackSoundVolume").floatValue = 1f;
            so.FindProperty("_spawnSoundVolume").floatValue = 1f;
            so.FindProperty("_spawnVfxScale").floatValue = 9f;
            so.FindProperty("_deathGorePrefab").objectReferenceValue = gorePrefab;

            if (ranged != null)
            {
                so.FindProperty("_deathSound").objectReferenceValue = ranged.DeathSound;
                so.FindProperty("_attackSound").objectReferenceValue = ranged.AttackSound;
                so.FindProperty("_spawnSound").objectReferenceValue = ranged.SpawnSound;
                so.FindProperty("_spawnVfxPrefab").objectReferenceValue = ranged.SpawnVfxPrefab;
                if (gorePrefab == null)
                    so.FindProperty("_deathGorePrefab").objectReferenceValue = ranged.DeathGorePrefab;
            }
            else
            {
                var deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DeathAudioDest);
                if (deathClip != null) so.FindProperty("_deathSound").objectReferenceValue = deathClip;
                var attackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AttackAudioDest);
                if (attackClip != null) so.FindProperty("_attackSound").objectReferenceValue = attackClip;
                var spawnClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SpawnAudioDest);
                if (spawnClip != null) so.FindProperty("_spawnSound").objectReferenceValue = spawnClip;
                so.FindProperty("_spawnVfxPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(SpawnVfxPath);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
                AssetDatabase.CreateAsset(def, MeleeDefinitionPath);
            else
                EditorUtility.SetDirty(def);

            return def;
        }

        private static GameObject BuildMeleeEnemy(RuntimeAnimatorController controller)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MeleeEnemyPath);
            if (existing != null)
            {
                // Refresh model/controller wiring if prefab already exists.
                var rootExisting = PrefabUtility.LoadPrefabContents(MeleeEnemyPath);
                try
                {
                    var existingAnim = rootExisting.GetComponentInChildren<Animator>();
                    if (existingAnim != null) existingAnim.runtimeAnimatorController = controller;

                    // Keep size in sync with ranged Succubus.
                    rootExisting.transform.localScale = Vector3.one * 1.5f;
                    var modelT = rootExisting.transform.Find("Model");
                    if (modelT != null) modelT.localScale = Vector3.one * 0.5f;
                    var ccExisting = rootExisting.GetComponent<CharacterController>();
                    if (ccExisting != null)
                    {
                        ccExisting.height = 2.3f;
                        ccExisting.radius = 0.35f;
                        ccExisting.center = new Vector3(0f, 1.15f, 0f);
                        ccExisting.skinWidth = 0.08f;
                    }
                    var capExisting = rootExisting.GetComponent<CapsuleCollider>();
                    if (capExisting != null)
                    {
                        capExisting.height = 2.3f;
                        capExisting.radius = 0.35f;
                        capExisting.center = new Vector3(0f, 1.15f, 0f);
                    }

                    PrefabUtility.SaveAsPrefabAsset(rootExisting, MeleeEnemyPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(rootExisting);
                }
                return AssetDatabase.LoadAssetAtPath<GameObject>(MeleeEnemyPath);
            }

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeleeModelPath);
            if (modelPrefab == null) throw new Exception("DemonGirl_var6 missing");

            var root = new GameObject("SuccubusMelee");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Match Succubus (ranged) sizing: root 1.5, model 0.5, capsule 2.3 @ 1.15
            root.transform.localScale = Vector3.one * 1.5f;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 2.3f; cc.radius = 0.35f; cc.center = new Vector3(0f, 1.15f, 0f);
            cc.skinWidth = 0.08f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2.3f; capsule.radius = 0.35f; capsule.center = new Vector3(0f, 1.15f, 0f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            model.name = "Model";
            model.transform.localScale = Vector3.one * 0.5f;
            foreach (var c in model.GetComponentsInChildren<CharacterController>(true))
                UnityEngine.Object.DestroyImmediate(c);
            foreach (var c in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);

            var anim = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            var health = root.AddComponent<SuccubusEnemy>();
            root.AddComponent<SuccubusBrain>();

            var healthSO = new SerializedObject(health);
            healthSO.FindProperty("maxHealth").floatValue = 4f;
            healthSO.FindProperty("maxShield").floatValue = 0f;
            healthSO.FindProperty("destroyOnDie").boolValue = true;
            healthSO.FindProperty("_name").stringValue = "Succubus Melee";
            healthSO.FindProperty("showKillFeed").boolValue = false;
            healthSO.FindProperty("showUI").boolValue = false;
            healthSO.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MeleeEnemyPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void WireMeleeIntoSpawner(GameObject meleeEnemyPrefab)
        {
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            var root = PrefabUtility.LoadPrefabContents(DevPrefabPath);
            try
            {
                var session = root.transform.Find("Session");
                if (session == null) return;
                var spawner = session.GetComponent<EnemyWaveSpawner>();
                if (spawner == null) return;

                var so = new SerializedObject(spawner);
                if (ranged != null)
                    so.FindProperty("enemyPrefab").objectReferenceValue = ranged.GetComponent<SuccubusEnemy>();

                var entries = so.FindProperty("enemyEntries");
                entries.arraySize = 2;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue =
                    ranged != null ? ranged.GetComponent<SuccubusEnemy>() : null;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("weight").floatValue = 1f;
                entries.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue =
                    meleeEnemyPrefab.GetComponent<SuccubusEnemy>();
                entries.GetArrayElementAtIndex(1).FindPropertyRelative("weight").floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, DevPrefabPath);
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

                var spawner = session.AddComponent<EnemyWaveSpawner>();
                var spawnerSO = new SerializedObject(spawner);
                spawnerSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab.GetComponent<SuccubusEnemy>();
                spawnerSO.FindProperty("initialCount").intValue = 28;
                spawnerSO.FindProperty("maxAlive").intValue = 45;
                spawnerSO.FindProperty("spawnInterval").floatValue = 1.2f;
                spawnerSO.FindProperty("minDistanceFromPlayer").floatValue = 8f;
                spawnerSO.FindProperty("minSeparation").floatValue = 2f;
                // Blockage: Default + Ground + Enemy + Object
                spawnerSO.FindProperty("blockageMask").intValue = (1 << 0) | (1 << 3) | (1 << 7) | (1 << 8);
                // Spawn points must be assigned in the scene Inspector (World/Spawn_Points children).
                spawnerSO.FindProperty("spawnPoints").arraySize = 0;
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
