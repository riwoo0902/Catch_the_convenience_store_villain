using CWH.Player.Health;
using CWH.Villains;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class AutoDoor : MonoBehaviour
{
    private const string ConvenienceStoreScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
    private const string LeftDoorName = "automaticDoor_L_gp";
    private const string RightDoorName = "automaticDoor_R_gp";
    private const string DoorFrameName = "automaticDoorFrame";

    [Header("Door")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Vector3 leftOpenLocalOffset = new(-0.65f, 0f, 0f);
    [SerializeField] private Vector3 rightOpenLocalOffset = new(0.65f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float moveSpeed = 3.5f;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float checkInterval = 1f;
    [SerializeField] private Vector3 boxCenterOffset = new(0f, 0.9f, 0f);
    [SerializeField] private Vector3 boxHalfExtents = new(1.8f, 1.1f, 1.2f);
    [SerializeField, Min(0f)] private float boxCastDistance = 2.5f;
    [SerializeField] private Vector3 boxCastDirection = Vector3.forward;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField, Min(0f)] private float closeDelay = 1.5f;

    private readonly RaycastHit[] _hits = new RaycastHit[32];
    private Vector3 _leftClosedLocalPosition;
    private Vector3 _rightClosedLocalPosition;
    private float _nextCheckTime;
    private float _lastDetectedTime;
    private bool _isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInConvenienceStore()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsConvenienceStoreScene(activeScene) || FindFirstObjectByType<AutoDoor>() != null)
        {
            return;
        }

        Transform left = FindTransformByName(LeftDoorName);
        Transform right = FindTransformByName(RightDoorName);
        Transform frame = FindTransformByName(DoorFrameName);
        if (left == null || right == null)
        {
            Debug.LogWarning("AutoDoor could not install because the convenience store door objects were not found.");
            return;
        }

        Bounds bounds = BuildDoorBounds(left, right, frame);
        GameObject controllerObject = new("Convenience Store Auto Door");
        controllerObject.transform.SetPositionAndRotation(
            bounds.center,
            frame != null ? frame.rotation : left.rotation);

        AutoDoor autoDoor = controllerObject.AddComponent<AutoDoor>();
        autoDoor.leftDoor = left;
        autoDoor.rightDoor = right;
        autoDoor.boxCenterOffset = controllerObject.transform.InverseTransformPoint(bounds.center + Vector3.up * 0.5f);
        autoDoor.boxHalfExtents = new Vector3(
            Mathf.Max(1.2f, bounds.extents.x + 0.8f),
            Mathf.Max(1f, bounds.extents.y + 0.8f),
            1.2f);
    }

    private void Awake()
    {
        if (leftDoor == null)
        {
            leftDoor = FindTransformByName(LeftDoorName);
        }

        if (rightDoor == null)
        {
            rightDoor = FindTransformByName(RightDoorName);
        }

        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogWarning("AutoDoor needs left and right door transforms.");
            enabled = false;
            return;
        }

        _leftClosedLocalPosition = leftDoor.localPosition;
        _rightClosedLocalPosition = rightDoor.localPosition;
        _nextCheckTime = Time.time;
    }

    private void Update()
    {
        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            if (DetectApproachingActor())
            {
                _lastDetectedTime = Time.time;
                _isOpen = true;
            }
            else if (Time.time >= _lastDetectedTime + closeDelay)
            {
                _isOpen = false;
            }
        }

        Vector3 leftTarget = _leftClosedLocalPosition + (_isOpen ? leftOpenLocalOffset : Vector3.zero);
        Vector3 rightTarget = _rightClosedLocalPosition + (_isOpen ? rightOpenLocalOffset : Vector3.zero);
        leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, leftTarget, moveSpeed * Time.deltaTime);
        rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, rightTarget, moveSpeed * Time.deltaTime);
    }

    private bool DetectApproachingActor()
    {
        Vector3 direction = transform.TransformDirection(
            boxCastDirection.sqrMagnitude > 0.001f ? boxCastDirection.normalized : Vector3.forward);
        Vector3 center = transform.TransformPoint(boxCenterOffset);
        Quaternion orientation = transform.rotation;

        int count = Physics.BoxCastNonAlloc(
            center,
            boxHalfExtents,
            direction,
            _hits,
            orientation,
            boxCastDistance,
            detectionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i].collider;
            if (hit != null && IsDoorActor(hit))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDoorActor(Collider collider)
    {
        Transform root = collider.transform.root;
        if (collider.GetComponentInParent<PlayerHealth>() != null
            || collider.GetComponentInParent<RuntimeBrickVillain>() != null
            || collider.GetComponentInParent<RuntimeProductDisturberVillain>() != null
            || collider.GetComponentInParent<PoliceResponseController>() != null
            || collider.GetComponentInParent<global::Villains.BrickVillain>() != null
            || collider.GetComponentInParent<global::Villains.BrickThrowingVillain>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<CharacterController>() != null
            || collider.GetComponentInParent<NavMeshAgent>() != null)
        {
            return true;
        }

        string rootName = root != null ? root.name : collider.name;
        return rootName.Contains("Player")
               || rootName.Contains("Villain")
               || rootName.Contains("Police");
    }

    private static bool IsConvenienceStoreScene(Scene scene)
    {
        return scene.path == ConvenienceStoreScenePath || scene.name.Contains("ConvenienceStore");
    }

    private static Transform FindTransformByName(string targetName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == targetName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Bounds BuildDoorBounds(Transform left, Transform right, Transform frame)
    {
        Bounds bounds = default;
        bool hasBounds = false;
        AddBounds(left, ref bounds, ref hasBounds);
        AddBounds(right, ref bounds, ref hasBounds);
        AddBounds(frame, ref bounds, ref hasBounds);

        if (!hasBounds)
        {
            bounds = new Bounds((left.position + right.position) * 0.5f, Vector3.one * 2f);
        }

        return bounds;
    }

    private static void AddBounds(Transform root, ref Bounds bounds, ref bool hasBounds)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isOpen ? Color.green : Color.cyan;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(boxCenterOffset),
            transform.rotation,
            Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);

        Vector3 direction = boxCastDirection.sqrMagnitude > 0.001f
            ? boxCastDirection.normalized * boxCastDistance
            : Vector3.forward * boxCastDistance;
        Gizmos.DrawWireCube(direction, boxHalfExtents * 2f);
        Gizmos.matrix = previousMatrix;
    }
}

[DisallowMultipleComponent]
public sealed class ConvenienceStorePlayerBoundary : MonoBehaviour
{
    private const string ConvenienceStoreScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
    private const string LeftDoorName = "automaticDoor_L_gp";
    private const string RightDoorName = "automaticDoor_R_gp";
    private const string DoorFrameName = "automaticDoorFrame";
    private const string PlayerLayerName = "Player";
    private const string BoundaryLayerName = "PlayerBoundary";
    private const float InsidePadding = 0.45f;

    private BoxCollider _blockerCollider;
    private Vector3 _boundaryPoint;
    private Vector3 _insideNormal;
    private bool _configured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInConvenienceStore()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsConvenienceStoreScene(activeScene) || FindFirstObjectByType<ConvenienceStorePlayerBoundary>() != null)
        {
            return;
        }

        GameObject playerObject = FindPlayerObject();
        Transform left = FindTransformByName(LeftDoorName);
        Transform right = FindTransformByName(RightDoorName);
        Transform frame = FindTransformByName(DoorFrameName);
        if (playerObject == null || left == null || right == null)
        {
            return;
        }

        Bounds doorBounds = BuildDoorBounds(left, right, frame);
        Vector3 insideNormal = playerObject.transform.position - doorBounds.center;
        insideNormal.y = 0f;
        if (insideNormal.sqrMagnitude <= 0.001f)
        {
            insideNormal = frame != null ? frame.forward : Vector3.forward;
        }

        GameObject boundaryObject = new("Convenience Store Player Boundary");
        ConvenienceStorePlayerBoundary boundary = boundaryObject.AddComponent<ConvenienceStorePlayerBoundary>();
        boundary.Configure(doorBounds, insideNormal.normalized);
    }

    private void Configure(Bounds doorBounds, Vector3 insideNormal)
    {
        _insideNormal = insideNormal.sqrMagnitude > 0.001f ? insideNormal.normalized : Vector3.forward;
        _boundaryPoint = doorBounds.center + _insideNormal * InsidePadding;

        int boundaryLayer = LayerMask.NameToLayer(BoundaryLayerName);
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        if (boundaryLayer < 0 || playerLayer < 0)
        {
            Debug.LogWarning($"ConvenienceStorePlayerBoundary needs '{BoundaryLayerName}' and '{PlayerLayerName}' layers.");
            enabled = false;
            return;
        }

        gameObject.layer = boundaryLayer;
        ConfigurePhysicsLayerCollisions(boundaryLayer, playerLayer);

        transform.SetPositionAndRotation(
            _boundaryPoint,
            Quaternion.LookRotation(_insideNormal, Vector3.up));

        _blockerCollider = gameObject.AddComponent<BoxCollider>();
        _blockerCollider.isTrigger = false;
        _blockerCollider.size = new Vector3(
            Mathf.Max(2.5f, Mathf.Max(doorBounds.size.x, doorBounds.size.z) + 1.4f),
            Mathf.Max(3f, doorBounds.size.y + 1f),
            0.35f);
        _blockerCollider.center = Vector3.zero;
        _configured = true;
    }

    private static void ConfigurePhysicsLayerCollisions(int boundaryLayer, int playerLayer)
    {
        for (int layer = 0; layer < 32; layer++)
        {
            Physics.IgnoreLayerCollision(boundaryLayer, layer, layer != playerLayer);
        }

        Physics.IgnoreLayerCollision(boundaryLayer, playerLayer, false);
    }

    private static bool IsConvenienceStoreScene(Scene scene)
    {
        return scene.path == ConvenienceStoreScenePath || scene.name.Contains("ConvenienceStore");
    }

    private static GameObject FindPlayerObject()
    {
        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            return namedPlayer;
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.gameObject;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform.root.gameObject : null;
    }

    private static Transform FindTransformByName(string targetName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == targetName)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Bounds BuildDoorBounds(Transform left, Transform right, Transform frame)
    {
        Bounds bounds = default;
        bool hasBounds = false;
        AddBounds(left, ref bounds, ref hasBounds);
        AddBounds(right, ref bounds, ref hasBounds);
        AddBounds(frame, ref bounds, ref hasBounds);

        if (!hasBounds)
        {
            bounds = new Bounds((left.position + right.position) * 0.5f, Vector3.one * 2f);
        }

        return bounds;
    }

    private static void AddBounds(Transform root, ref Bounds bounds, ref bool hasBounds)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
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
    }

    private void OnDrawGizmosSelected()
    {
        if (!_configured)
        {
            return;
        }

        Gizmos.color = Color.red;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Vector3 size = _blockerCollider != null ? _blockerCollider.size : new Vector3(4f, 3f, 0.35f);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = previousMatrix;
    }
}
