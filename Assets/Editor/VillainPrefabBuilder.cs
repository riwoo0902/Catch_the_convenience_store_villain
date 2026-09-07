using Agents;
using Agents.FSM;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Villains;
using Villains.Animation;
using Villains.Combat;
using Villains.Data;
using Villains.Movement;
using Villains.Projectiles;
using Villains.Targeting;
using Villains.Visuals;

public static class VillainPrefabBuilder
{
    private const float PickaxeVillainScale = 3f;
    private const float VillainMoveSpeed = 7.5f;
    private const float VillainFleeSpeed = 10.5f;
    private const float VillainRotationSpeed = 720f;

    private const string BuilderSourcePath = "Assets/Job-Stickmans-Character-Pack-SS/Prefabs/Stickman-Builder.prefab";
    private const string MinerSourcePath = "Assets/Job-Stickmans-Character-Pack-SS/Prefabs/Stickman-Miner.prefab";
    private const string AnimatorPath = "Assets/YKJ/Animation/PlayerAnimator.controller";
    private const string StateListPath = "Assets/IYC/06.SO/Villains/Brick Villain State List.asset";
    private const string BrickThrowDataPath = "Assets/IYC/06.SO/Villains/Brick Throw Data.asset";
    private const string PickaxeThrowDataPath = "Assets/IYC/06.SO/Villains/Pickaxe Throw Data.asset";
    private const string SpatulaThrowDataPath = "Assets/IYC/06.SO/Villains/Spatula Throw Data.asset";
    private const string BrickProjectilePath = "Assets/YKJ/Prefab/Brick.prefab";
    private const string PickaxeProjectilePath = "Assets/YKJ/Prefab/Pickaxe Projectile.prefab";
    private const string SpatulaModelPath = "Assets/YKJ/Slotted Turner 202112-32.fbx";
    private const string SpatulaProjectilePath = "Assets/YKJ/Prefab/Spatula Projectile.prefab";
    private const string SpawnSettingsPath = "Assets/Resources/VillainSpawnSettings.asset";
    private const string ConvenienceStoreScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
    private const string BrickOutputPath = "Assets/YKJ/Prefab/Brick Villain.prefab";
    private const string PickaxeOutputPath = "Assets/YKJ/Prefab/Pickaxe Villain.prefab";

    [MenuItem("Tools/Villains/Rebuild YKJ Villain Prefabs")]
    public static void RebuildVillainPrefabs()
    {
        BuildBrickVillain();
        BuildPickaxeVillain();
        BuildSpatulaProjectile();
        WireSpawnSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("YKJ villain prefabs rebuilt.");
    }

    [MenuItem("Tools/Villains/Rebuild Spatula Projectile")]
    public static void RebuildSpatulaProjectile()
    {
        BuildSpatulaProjectile();
        WireSpawnSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Spatula projectile prefab rebuilt and wired.");
    }

    [MenuItem("Tools/Villains/Setup And Bake Convenience Store NavMesh")]
    public static void SetupAndBakeConvenienceStoreNavMesh()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ConvenienceStoreScenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                activeScene = EditorSceneManager.OpenScene(ConvenienceStoreScenePath);
            }
            else
            {
                Debug.LogWarning("Convenience Store NavMesh setup canceled.");
                return;
            }
        }

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject surfaceObject = new GameObject("Convenience Store NavMesh Surface");
            surface = surfaceObject.AddComponent<NavMeshSurface>();
        }

        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.08f;
        surface.minRegionArea = 0.35f;

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Convenience Store NavMesh surface baked.");
    }

    [MenuItem("Tools/Villains/Bake Convenience Store NavMesh No Prompt")]
    public static void BakeConvenienceStoreNavMeshNoPrompt()
    {
        Scene scene = EditorSceneManager.OpenScene(ConvenienceStoreScenePath, OpenSceneMode.Single);
        BakeActiveConvenienceStoreNavMesh(scene);
    }

    public static void BakeConvenienceStoreNavMeshBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ConvenienceStoreScenePath, OpenSceneMode.Single);
        BakeActiveConvenienceStoreNavMesh(scene);
    }

    private static void BakeActiveConvenienceStoreNavMesh(Scene scene)
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject surfaceObject = new GameObject("Convenience Store NavMesh Surface");
            surface = surfaceObject.AddComponent<NavMeshSurface>();
        }

        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.defaultArea = 0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.08f;
        surface.minRegionArea = 0.35f;

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Convenience Store NavMesh surface baked without prompt.");
    }

    private static void BuildBrickVillain()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BuilderSourcePath);
        try
        {
            root.name = "Brick Villain";
            ConfigureSharedVillainRoot(root);

            BrickVillain villain = EnsureComponent<BrickVillain>(root);
            ConfigureVillain(villain, new Vector3(0f, 0f, -12f));

            BrickThrowAttack attack = EnsureComponent<BrickThrowAttack>(root);
            ConfigureThrowAttack(
                attack,
                AssetDatabase.LoadAssetAtPath<BrickThrowDataSO>(BrickThrowDataPath),
                LoadProjectile(BrickProjectilePath),
                FindChild(root.transform, "RightHand"),
                Vector3.zero,
                0f,
                Vector3.zero,
                0.8f
            );

            SavePrefab(root, BrickOutputPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildPickaxeVillain()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MinerSourcePath);
        try
        {
            root.name = "Pickaxe Villain";
            ConfigureSharedVillainRoot(root);
            root.transform.localScale = Vector3.one * PickaxeVillainScale;

            PickaxeVillain villain = EnsureComponent<PickaxeVillain>(root);
            ConfigureVillain(villain, new Vector3(0f, 0f, -12f));

            PickaxeThrowAttack attack = EnsureComponent<PickaxeThrowAttack>(root);
            ConfigureThrowAttack(
                attack,
                AssetDatabase.LoadAssetAtPath<PickaxeThrowDataSO>(PickaxeThrowDataPath),
                LoadProjectile(PickaxeProjectilePath),
                FindChild(root.transform, "RightHand"),
                new Vector3(0f, 0.25f, 0.45f),
                1.2f,
                new Vector3(90f, 0f, 0f),
                1.2f
            );

            SavePrefab(root, PickaxeOutputPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildSpatulaProjectile()
    {
        var importer = AssetImporter.GetAtPath(SpatulaModelPath) as ModelImporter;
        if (importer != null && (importer.importCameras || importer.importLights))
        {
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        GameObject spatulaModel = AssetDatabase.LoadAssetAtPath<GameObject>(SpatulaModelPath);
        if (spatulaModel == null)
        {
            throw new System.InvalidOperationException($"Spatula model was not found: {SpatulaModelPath}");
        }

        if (spatulaModel.GetComponentInChildren<Renderer>(true) == null)
        {
            throw new System.InvalidOperationException($"Spatula model has no renderer: {SpatulaModelPath}");
        }

        GameObject root = new GameObject("Spatula Projectile");
        try
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.65f, 0.12f, 0.3f);

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 0.6f;
            rigidbody.angularDamping = 0.02f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Projectile projectile = root.AddComponent<Projectile>();
            SerializedObject serializedProjectile = new SerializedObject(projectile);
            serializedProjectile.FindProperty("spinAxis").vector3Value = Vector3.forward;
            serializedProjectile.FindProperty("spinSpeed").floatValue = 1080f;
            serializedProjectile.FindProperty("disturbShelfProductsOnHit").boolValue = true;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(spatulaModel, root.transform);
            visual.name = "Spatula Visual";
            StripNonProjectileVisualComponents(visual);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            visual.transform.localScale = Vector3.one * 0.12f;

            SavePrefab(root, SpatulaProjectilePath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void WireSpawnSettings()
    {
        CWH.Villains.VillainSpawnSettings settings =
            AssetDatabase.LoadAssetAtPath<CWH.Villains.VillainSpawnSettings>(SpawnSettingsPath);
        if (settings == null)
        {
            Debug.LogWarning($"VillainSpawnSettings was not found: {SpawnSettingsPath}");
            return;
        }

        Projectile spatulaProjectile = LoadProjectile(SpatulaProjectilePath);
        GameObject spatulaModel = AssetDatabase.LoadAssetAtPath<GameObject>(SpatulaModelPath);
        GameObject pickaxeVillain = AssetDatabase.LoadAssetAtPath<GameObject>(PickaxeOutputPath);
        ProjectileThrowDataSO spatulaThrowData = AssetDatabase.LoadAssetAtPath<ProjectileThrowDataSO>(SpatulaThrowDataPath);

        SerializedObject serializedObject = new SerializedObject(settings);
        serializedObject.FindProperty("_pickaxeVillainPrefab").objectReferenceValue = pickaxeVillain;
        serializedObject.FindProperty("_spatulaProjectilePrefab").objectReferenceValue = spatulaProjectile;
        serializedObject.FindProperty("_spatulaProjectileVisualPrefab").objectReferenceValue = spatulaModel;
        serializedObject.FindProperty("_spatulaThrowData").objectReferenceValue = spatulaThrowData;
        serializedObject.FindProperty("_chefVillainSpawnChance").floatValue = 0.35f;
        serializedObject.FindProperty("_pickaxeVillainSpawnChance").floatValue = 0.35f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }

    private static void ConfigureSharedVillainRoot(GameObject root)
    {
        Animator animator = root.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
            animator.applyRootMotion = false;
        }

        CharacterController controller = EnsureComponent<CharacterController>(root);
        controller.center = new Vector3(0f, 1f, 0f);
        controller.radius = 0.35f;
        controller.height = 2f;
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;

        EnsureComponent<VillainTargetProvider>(root);
        ConfigureTargetDetector(EnsureComponent<VillainTargetDetector>(root));
        EnsureComponent<VillainDetectionVisualizer>(root);
        ConfigureMovement(EnsureComponent<VillainMovement>(root));
        EnsureComponent<AgentRenderer>(root);
        EnsureComponent<VillainAnimationEventRelay>(root);
    }

    private static void ConfigureMovement(VillainMovement movement)
    {
        SerializedObject serializedObject = new SerializedObject(movement);
        serializedObject.FindProperty("moveSpeed").floatValue = VillainMoveSpeed;
        serializedObject.FindProperty("fleeSpeed").floatValue = VillainFleeSpeed;
        serializedObject.FindProperty("rotationSpeed").floatValue = VillainRotationSpeed;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureVillain(BrickVillain villain, Vector3 fallbackFleeDestination)
    {
        SerializedObject serializedObject = new SerializedObject(villain);
        serializedObject.FindProperty("stateList").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StateListSO>(StateListPath);
        serializedObject.FindProperty("initialState").enumValueIndex = 0;
        serializedObject.FindProperty("fleeDestination").objectReferenceValue = null;
        serializedObject.FindProperty("fallbackFleeDestination").vector3Value = fallbackFleeDestination;
        serializedObject.FindProperty("disableOnFleeCompleted").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTargetDetector(VillainTargetDetector detector)
    {
        SerializedObject serializedObject = new SerializedObject(detector);
        serializedObject.FindProperty("detectionRange").floatValue = 20f;
        serializedObject.FindProperty("closeDetectionRange").floatValue = 4f;
        serializedObject.FindProperty("viewAngle").floatValue = 220f;
        serializedObject.FindProperty("targetLayer").FindPropertyRelative("m_Bits").intValue = 128;
        serializedObject.FindProperty("obstacleLayer").FindPropertyRelative("m_Bits").intValue = 0;
        serializedObject.FindProperty("eyePoint").objectReferenceValue = null;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureThrowAttack(
        ProjectileThrowAttack attack,
        ProjectileThrowDataSO throwData,
        Projectile projectilePrefab,
        Transform throwPoint,
        Vector3 spawnOffset,
        float minimumSpawnHeight,
        Vector3 projectileRotationOffset,
        float aimHeightOffset)
    {
        SerializedObject serializedObject = new SerializedObject(attack);
        serializedObject.FindProperty("throwData").objectReferenceValue = throwData;
        serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedObject.FindProperty("throwPoint").objectReferenceValue = throwPoint;
        serializedObject.FindProperty("spawnOffset").vector3Value = spawnOffset;
        serializedObject.FindProperty("minimumSpawnHeightFromOwner").floatValue = minimumSpawnHeight;
        serializedObject.FindProperty("projectileRotationOffset").vector3Value = projectileRotationOffset;
        serializedObject.FindProperty("aimHeightOffset").floatValue = aimHeightOffset;
        serializedObject.FindProperty("excludeLayer").FindPropertyRelative("m_Bits").intValue = 0;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        Debug.LogWarning($"{childName} was not found under {root.name}. Throw point will use root transform.");
        return root;
    }

    private static Projectile LoadProjectile(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<Projectile>() : null;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success)
            throw new System.InvalidOperationException($"Failed to save prefab: {path}");
    }

    private static void StripNonProjectileVisualComponents(GameObject visualRoot)
    {
        if (visualRoot == null)
        {
            return;
        }

        foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
        {
            camera.enabled = false;
            Object.DestroyImmediate(camera);
        }

        foreach (Light light in visualRoot.GetComponentsInChildren<Light>(true))
        {
            light.enabled = false;
            Object.DestroyImmediate(light);
        }

        foreach (AudioListener listener in visualRoot.GetComponentsInChildren<AudioListener>(true))
        {
            listener.enabled = false;
            Object.DestroyImmediate(listener);
        }
    }
}
