using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Villains.Visuals;

public static class VillainGroundAlignmentDiagnostics
{
    private const float ExpectedClearance = 0.03f;
    private const float AllowedError = 0.03f;

    private static readonly string[] PrefabPaths =
    {
        "Assets/YKJ/Prefab/Brick Villain.prefab",
        "Assets/YKJ/Prefab/Pickaxe Villain.prefab",
    };

    [MenuItem("Tools/Villains/Diagnostics/Test Ground Alignment")]
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Diagnostic Ground";
        ground.transform.position = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(12f, 0.1f, 12f);
        Physics.SyncTransforms();

        int failures = 0;
        foreach (string prefabPath in PrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[VillainGroundTest] Missing prefab: {prefabPath}");
                failures++;
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name + " Ground Test";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GroundVisualAnchor anchor = instance.GetComponent<GroundVisualAnchor>();
            if (anchor == null)
            {
                anchor = instance.AddComponent<GroundVisualAnchor>();
            }

            anchor.Configure(instance.transform, null, ExpectedClearance);
            Physics.SyncTransforms();
            anchor.AlignNow();
            Physics.SyncTransforms();

            if (!TryGetVisualGroundY(instance.transform, out float visualGroundY))
            {
                Debug.LogError($"[VillainGroundTest] {prefab.name}: no ground reference or renderer bounds");
                failures++;
                UnityEngine.Object.DestroyImmediate(instance);
                continue;
            }

            CharacterController controller = instance.GetComponent<CharacterController>();
            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            float controllerBottom = controller != null
                ? instance.transform.position.y + controller.center.y - controller.height * 0.5f
                : float.NaN;
            float clearance = visualGroundY;
            float error = Mathf.Abs(clearance - ExpectedClearance);

            Debug.Log(
                $"[VillainGroundTest] {prefab.name}: rootY={instance.transform.position.y:F3}, " +
                $"visualGroundY={visualGroundY:F3}, visualClearance={clearance:F3}, " +
                $"controllerBottomY={controllerBottom:F3}, " +
                $"agentUpdatePosition={(agent != null ? agent.updatePosition : false)}");

            if (error > AllowedError)
            {
                Debug.LogError(
                    $"[VillainGroundTest] {prefab.name}: expected visual clearance {ExpectedClearance:F3}, got {clearance:F3}");
                failures++;
            }

            UnityEngine.Object.DestroyImmediate(instance);
        }

        UnityEngine.Object.DestroyImmediate(ground);

        if (failures > 0)
        {
            throw new InvalidOperationException($"Villain ground alignment failed: {failures}");
        }

        Debug.Log("[VillainGroundTest] All villain ground alignment checks passed.");
        }

    private static bool TryGetVisualGroundY(Transform root, out float groundY)
    {
        groundY = float.MaxValue;
        bool found = false;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform transform in transforms)
        {
            if (transform.name == "LeftFoot"
                || transform.name == "RightFoot"
                || transform.name == "LeftToeBase"
                || transform.name == "RightToeBase")
            {
                groundY = Mathf.Min(groundY, transform.position.y);
                found = true;
            }
        }

        if (found)
        {
            return true;
        }

        if (TryGetVisualBounds(root, out Bounds bounds))
        {
            groundY = bounds.min.y;
            return true;
        }

        groundY = 0f;
        return false;
    }

    private static bool TryGetVisualBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || renderer is LineRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
