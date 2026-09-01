using Agents;
using Agents.FSM;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
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
    private const string BuilderSourcePath = "Assets/Job-Stickmans-Character-Pack-SS/Prefabs/Stickman-Builder.prefab";
    private const string MinerSourcePath = "Assets/Job-Stickmans-Character-Pack-SS/Prefabs/Stickman-Miner.prefab";
    private const string AnimatorPath = "Assets/YKJ/Animation/PlayerAnimator.controller";
    private const string StateListPath = "Assets/IYC/06.SO/Villains/Brick Villain State List.asset";
    private const string BrickThrowDataPath = "Assets/IYC/06.SO/Villains/Brick Throw Data.asset";
    private const string PickaxeThrowDataPath = "Assets/IYC/06.SO/Villains/Pickaxe Throw Data.asset";
    private const string BrickProjectilePath = "Assets/YKJ/Prefab/Brick.prefab";
    private const string PickaxeProjectilePath = "Assets/YKJ/Prefab/Pickaxe Projectile.prefab";
    private const string BrickOutputPath = "Assets/YKJ/Prefab/Brick Villain.prefab";
    private const string PickaxeOutputPath = "Assets/YKJ/Prefab/Pickaxe Villain.prefab";

    [MenuItem("Tools/Villains/Rebuild YKJ Villain Prefabs")]
    public static void RebuildVillainPrefabs()
    {
        BuildBrickVillain();
        BuildPickaxeVillain();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("YKJ villain prefabs rebuilt.");
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
                Vector3.zero
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
                new Vector3(90f, 0f, 0f)
            );

            SavePrefab(root, PickaxeOutputPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
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
        EnsureComponent<VillainMovement>(root);
        EnsureComponent<AgentRenderer>(root);
        EnsureComponent<VillainAnimationEventRelay>(root);
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
        Vector3 projectileRotationOffset)
    {
        SerializedObject serializedObject = new SerializedObject(attack);
        serializedObject.FindProperty("throwData").objectReferenceValue = throwData;
        serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedObject.FindProperty("throwPoint").objectReferenceValue = throwPoint;
        serializedObject.FindProperty("spawnOffset").vector3Value = spawnOffset;
        serializedObject.FindProperty("minimumSpawnHeightFromOwner").floatValue = minimumSpawnHeight;
        serializedObject.FindProperty("projectileRotationOffset").vector3Value = projectileRotationOffset;
        serializedObject.FindProperty("aimHeightOffset").floatValue = 1.2f;
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
}
