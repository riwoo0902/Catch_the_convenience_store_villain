using System.Collections;
using CWH.GameFlow;
using System.Collections.Generic;
using CWH.Player.Health;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class ConvenienceStoreVillainSpawner : MonoBehaviour
    {
        private const string SettingsResourceName = "VillainSpawnSettings";
        private const string ConvenienceStoreScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
        private const float GroundProbeHeight = 8f;
        private const float GroundProbeDistance = 20f;
        private static readonly RaycastHit[] GroundHits = new RaycastHit[16];
        private static readonly string[] EntranceDoorNames =
        {
            "automaticDoor_L_gp",
            "automaticDoor_R_gp",
            "automaticDoorFrame"
        };

        [Header("Shift Arrival Timing")]
        [SerializeField, Min(0.1f)] private float _minimumArrivalDelay = 20f;
        [SerializeField, Min(0.1f)] private float _maximumArrivalDelay = 40f;

        private VillainSpawnSettings _settings;
        private Transform _player;
        private PlayerHealth _playerHealth;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Transform[] _spawnPoints = new Transform[0];
        private Transform[] _roamPoints = new Transform[0];

        public static event System.Action<string> VillainEnteredStore;
        public static event System.Action VillainBecameAngry;

        private enum SpawnKind
        {
            Brick,
            Chef,
            Pickaxe,
            ProductDisturber
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInConvenienceStore()
        {
            TryInstallInCurrentScene();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstallInCurrentScene();
        }

        private static void TryInstallInCurrentScene()
        {
            if (FindFirstObjectByType<ConvenienceStoreVillainSpawner>() != null)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!IsConvenienceStoreScene(activeScene))
            {
                return;
            }

            ConvenienceStoreNavMeshBootstrapper.EnsureBuiltForActiveScene();

            VillainSpawnSettings settings = Resources.Load<VillainSpawnSettings>(SettingsResourceName);
            GameObject player = FindPlayerObject();
            if (settings == null || player == null)
            {
                Debug.LogWarning($"Villain spawner install skipped. settings: {settings != null}, player: {player != null}");
                return;
            }

            GameObject spawnerObject = new("Convenience Store Villain Spawner");
            spawnerObject.AddComponent<ConvenienceStoreVillainSpawner>();
            Debug.Log("Convenience Store Villain Spawner installed.");
        }

        private static bool IsConvenienceStoreScene(Scene scene)
        {
            return scene.path == ConvenienceStoreScenePath
                   || scene.name.Contains("ConvenienceStore")
                   || scene.name.Contains("Convenience Store")
                   || FindFirstObjectByType<VillainSpawnPoint>() != null
                   || GameObject.Find(EntranceDoorNames[0]) != null
                   || GameObject.Find(EntranceDoorNames[1]) != null;
        }

        public static void RequestAllVillainsFlee()
        {
            ConvenienceStoreNavMeshBootstrapper.EnsureBuiltForActiveScene();

            global::Villains.BrickVillain[] fsmVillains = FindObjectsByType<global::Villains.BrickVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (global::Villains.BrickVillain villain in fsmVillains)
            {
                villain.FleeFromStore();
            }

            RuntimeBrickVillain[] villains = FindObjectsByType<RuntimeBrickVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (RuntimeBrickVillain villain in villains)
            {
                villain.BeginFlee();
            }

            RuntimeProductDisturberVillain[] productDisturbers = FindObjectsByType<RuntimeProductDisturberVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (RuntimeProductDisturberVillain villain in productDisturbers)
            {
                villain.BeginFlee();
            }

            global::Villains.BrickThrowingVillain[] legacyVillains = FindObjectsByType<global::Villains.BrickThrowingVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (global::Villains.BrickThrowingVillain villain in legacyVillains)
            {
                villain.FleeFromStore();
            }
        }

        public static void NotifyVillainEnteredStore(string villainName)
        {
            VillainEnteredStore?.Invoke(string.IsNullOrWhiteSpace(villainName) ? "진상" : villainName);
        }

        public static void NotifyVillainBecameAngry()
        {
            VillainBecameAngry?.Invoke();
        }

        private void Awake()
        {
            _settings = Resources.Load<VillainSpawnSettings>(SettingsResourceName);
            GameObject playerObject = FindPlayerObject();
            _player = playerObject != null ? playerObject.transform : null;
            _playerHealth = PlayerHealth.GetOrCreate();

            if (_settings == null
                || _settings.VillainVisualPrefab == null
                || _player == null
                || _playerHealth == null)
            {
                Debug.LogWarning("Villain spawner could not start because its settings, visual prefab, Player, or health is missing.");
                enabled = false;
                return;
            }

            ResolveDoorWaypoints();
            ResolveScenePoints();
            _playerHealth.Died += HandlePlayerDied;
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            // The first arrival uses the same delay as later arrivals. Only working
            // time advances this timer; the introductory story cannot consume it.
            yield return null;
            while (enabled && _playerHealth != null && !_playerHealth.IsDead)
            {
                float remaining = Random.Range(_minimumArrivalDelay, Mathf.Max(_minimumArrivalDelay, _maximumArrivalDelay));
                while (remaining > 0f && enabled && _playerHealth != null && !_playerHealth.IsDead)
                {
                    if (GameLoopController.AllowsRandomSpawns)
                    {
                        remaining -= Time.deltaTime;
                    }

                    yield return null;
                }

                if (enabled && GameLoopController.AllowsRandomSpawns && _player != null && _playerHealth != null && !_playerHealth.IsDead)
                {
                    SpawnVillain(false);
                }
            }
        }

        public GameObject TutorialVillain { get; private set; }

        public bool TrySpawnTutorialVillain()
        {
            if (TutorialVillain != null) return true;
            if (!enabled || _settings == null || _settings.VillainVisualPrefab == null || _player == null)
                return false;
            SpawnVillain(true);
            return TutorialVillain != null;
        }

        private void SpawnVillain(bool tutorial)
        {
            if (!GameLoopController.AllowsGameplay || _playerHealth == null || _playerHealth.IsDead)
            {
                return;
            }

            if (!tutorial && HasReachedActiveVillainLimit())
            {
                return;
            }

            bool useCustomSpawnPoint = TryGetRandomPoint(_spawnPoints, out Vector3 spawnPosition);
            if (tutorial)
            {
                useCustomSpawnPoint = true;
                spawnPosition = ResolveSpawnHeight(_insideDoorPosition);
            }
            if (useCustomSpawnPoint)
            {
                spawnPosition = ResolveSpawnHeight(spawnPosition);
            }
            else
            {
                spawnPosition = _outsideDoorPosition;
            }

            Vector3 entryDirection = Flatten(_insideDoorPosition - spawnPosition);
            if (entryDirection.sqrMagnitude < 0.001f)
            {
                entryDirection = ResolveInitialFacing(spawnPosition);
            }

            entryDirection = entryDirection.sqrMagnitude > 0.001f
                ? entryDirection.normalized
                : Vector3.forward;

            float mischiefDelay = Random.Range(_settings.MinimumMischiefDelay, _settings.MaximumMischiefDelay);
            SpawnKind spawnKind = tutorial ? SpawnKind.Brick : PickSpawnKind();
            if (spawnKind == SpawnKind.Chef)
            {
                Debug.Log($"Spawning Chef Spatula Villain at {spawnPosition}");
                SpawnChefVillain(
                    spawnPosition,
                    entryDirection,
                    mischiefDelay);
                return;
            }

            if (spawnKind == SpawnKind.ProductDisturber)
            {
                Debug.Log($"Spawning Product Disturber Villain at {spawnPosition}");
                SpawnProductDisturber(
                    spawnPosition,
                    entryDirection,
                    mischiefDelay);
                return;
            }

            if (spawnKind == SpawnKind.Pickaxe)
            {
                Debug.Log($"Spawning Pickaxe Villain at {spawnPosition}");
                SpawnFsmVillain(
                    _settings.PickaxeVillainPrefab,
                    "Pickaxe Villain",
                    spawnPosition,
                    entryDirection,
                    mischiefDelay);
                return;
            }

            GameObject villainObject = Instantiate(
                _settings.VillainVisualPrefab,
                spawnPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            villainObject.name = "Brick Villain";
            if (tutorial) TutorialVillain = villainObject;
            Debug.Log($"Spawning Brick Villain at {villainObject.transform.position}");

            global::Villains.BrickVillain fsmVillain = villainObject.GetComponent<global::Villains.BrickVillain>();
            if (fsmVillain != null)
            {
                fsmVillain.SetFallbackFleeDestination(_outsideDoorPosition);
                RuntimeVillainRoamer roamer = villainObject.GetComponent<RuntimeVillainRoamer>();
                if (roamer == null)
                {
                    roamer = villainObject.AddComponent<RuntimeVillainRoamer>();
                }

                roamer.Configure(fsmVillain, _settings, _roamPoints, mischiefDelay);
                roamer.ConfigureEntry(_insideDoorPosition);
                return;
            }

            villainObject.transform.localScale = Vector3.one * _settings.VisualScale;
            RuntimeBrickVillain villain = villainObject.AddComponent<RuntimeBrickVillain>();
            villain.Initialize(
                _settings,
                _player,
                _insideDoorPosition,
                _outsideDoorPosition,
                true,
                mischiefDelay,
                _roamPoints);
        }

        private SpawnKind PickSpawnKind()
        {
            float brickWeight = _settings.VillainVisualPrefab != null ? 1f : 0f;
            float chefWeight = CanSpawnChefVillain() ? Mathf.Max(0f, _settings.ChefVillainSpawnChance) : 0f;
            float pickaxeWeight = CanSpawnPickaxeVillain() ? Mathf.Max(0f, _settings.PickaxeVillainSpawnChance) : 0f;
            float productDisturberWeight = CanSpawnProductDisturber()
                ? Mathf.Max(0f, _settings.ProductDisturberSpawnChance)
                : 0f;

            float totalWeight = brickWeight + chefWeight + pickaxeWeight + productDisturberWeight;
            if (totalWeight <= 0f)
            {
                return SpawnKind.Brick;
            }

            float roll = Random.value * totalWeight;
            if (roll < chefWeight)
            {
                return SpawnKind.Chef;
            }

            roll -= chefWeight;
            if (roll < pickaxeWeight)
            {
                return SpawnKind.Pickaxe;
            }

            roll -= pickaxeWeight;
            if (roll < productDisturberWeight)
            {
                return SpawnKind.ProductDisturber;
            }

            return SpawnKind.Brick;
        }

        private bool CanSpawnChefVillain()
        {
            return _settings.ChefVillainVisualPrefab != null
                   && _settings.SpatulaProjectileVisualPrefab != null
                   && _settings.SpatulaThrowData != null;
        }

        private bool CanSpawnPickaxeVillain()
        {
            return _settings.PickaxeVillainPrefab != null;
        }

        private void SpawnChefVillain(
            Vector3 spawnPosition,
            Vector3 entryDirection,
            float mischiefDelay)
        {
            GameObject villainObject = Instantiate(
                _settings.ChefVillainVisualPrefab,
                spawnPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            villainObject.name = "Chef Spatula Villain";

            villainObject.transform.localScale = Vector3.one * _settings.VisualScale;
            RuntimeBrickVillain villain = villainObject.GetComponent<RuntimeBrickVillain>();
            if (villain == null)
            {
                villain = villainObject.AddComponent<RuntimeBrickVillain>();
            }

            villain.UseProjectileVisual(_settings.SpatulaProjectileVisualPrefab, _settings.SpatulaThrowData);
            villain.Initialize(
                _settings,
                _player,
                _insideDoorPosition,
                _outsideDoorPosition,
                true,
                mischiefDelay,
                _roamPoints);
        }

        private bool CanSpawnProductDisturber()
        {
            return _settings.ProductDisturberVisualPrefab != null;
        }

        private void SpawnProductDisturber(
            Vector3 spawnPosition,
            Vector3 entryDirection,
            float mischiefDelay)
        {
            GameObject disturberObject = Instantiate(
                _settings.ProductDisturberVisualPrefab,
                spawnPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            disturberObject.name = "Product Disturber Villain";

            if (disturberObject.GetComponent<global::Villains.BrickVillain>() != null)
            {
                Destroy(disturberObject.GetComponent<global::Villains.BrickVillain>());
            }

            disturberObject.transform.localScale = Vector3.one * _settings.VisualScale;
            RuntimeProductDisturberVillain disturber = disturberObject.GetComponent<RuntimeProductDisturberVillain>();
            if (disturber == null)
            {
                disturber = disturberObject.AddComponent<RuntimeProductDisturberVillain>();
            }

            disturber.Initialize(
                _settings,
                _insideDoorPosition,
                _outsideDoorPosition,
                true,
                mischiefDelay,
                _roamPoints);
        }

        private void SpawnFsmVillain(
            GameObject prefab,
            string instanceName,
            Vector3 spawnPosition,
            Vector3 entryDirection,
            float mischiefDelay)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject villainObject = Instantiate(
                prefab,
                spawnPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            villainObject.name = instanceName;

            if (instanceName.Contains("Pickaxe"))
            {
                villainObject.transform.localScale = Vector3.one * _settings.VisualScale;
            }

            global::Villains.BrickVillain fsmVillain = villainObject.GetComponent<global::Villains.BrickVillain>();
            if (fsmVillain == null)
            {
                RuntimeBrickVillain runtimeVillain = villainObject.GetComponent<RuntimeBrickVillain>();
                if (runtimeVillain == null)
                {
                    runtimeVillain = villainObject.AddComponent<RuntimeBrickVillain>();
                }

                runtimeVillain.Initialize(
                    _settings,
                    _player,
                    _insideDoorPosition,
                    _outsideDoorPosition,
                    true,
                    mischiefDelay,
                    _roamPoints);
                return;
            }

            fsmVillain.SetFallbackFleeDestination(_outsideDoorPosition);
            RuntimeVillainRoamer roamer = villainObject.GetComponent<RuntimeVillainRoamer>();
            if (roamer == null)
            {
                roamer = villainObject.AddComponent<RuntimeVillainRoamer>();
            }

            roamer.Configure(fsmVillain, _settings, _roamPoints, mischiefDelay);
            roamer.ConfigureEntry(_insideDoorPosition);
        }

        private bool HasReachedActiveVillainLimit()
        {
            return _settings != null && CountActiveVillains() >= _settings.MaximumActiveVillains;
        }

        private static int CountActiveVillains()
        {
            HashSet<GameObject> activeVillains = new();
            AddActiveVillains(FindObjectsByType<RuntimeBrickVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None), activeVillains);
            AddActiveVillains(FindObjectsByType<RuntimeProductDisturberVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None), activeVillains);
            AddActiveVillains(FindObjectsByType<global::Villains.BrickVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None), activeVillains);
            AddActiveVillains(FindObjectsByType<global::Villains.BrickThrowingVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None), activeVillains);
            return activeVillains.Count;
        }

        private static void AddActiveVillains<T>(T[] villains, HashSet<GameObject> activeVillains)
            where T : Component
        {
            if (villains == null)
            {
                return;
            }

            foreach (T villain in villains)
            {
                if (villain == null || !villain.gameObject.activeInHierarchy)
                {
                    continue;
                }

                activeVillains.Add(villain.gameObject);
            }
        }

        private void ResolveScenePoints()
        {
            VillainSpawnPoint[] spawnPoints = FindObjectsByType<VillainSpawnPoint>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            _spawnPoints = ExtractTransforms(spawnPoints);

            VillainRoamPoint[] roamPoints = FindObjectsByType<VillainRoamPoint>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            VillainCheckpoint[] checkpoints = FindObjectsByType<VillainCheckpoint>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            _roamPoints = ExtractUniqueTransforms(roamPoints, checkpoints);
            Debug.Log($"Villain spawner found {_spawnPoints.Length} spawn points and {_roamPoints.Length} checkpoints.");
        }

        private static Transform[] ExtractTransforms<T>(T[] points)
            where T : Component
        {
            if (points == null || points.Length == 0)
            {
                return new Transform[0];
            }

            Transform[] transforms = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                transforms[i] = points[i].transform;
            }

            return transforms;
        }

        private static Transform[] ExtractUniqueTransforms(VillainRoamPoint[] roamPoints, VillainCheckpoint[] checkpoints)
        {
            List<Transform> transforms = new();
            HashSet<Transform> seen = new();
            AddTransforms(roamPoints, transforms, seen);
            AddTransforms(checkpoints, transforms, seen);
            return transforms.ToArray();
        }

        private static void AddTransforms<T>(T[] points, List<Transform> transforms, HashSet<Transform> seen)
            where T : Component
        {
            if (points == null)
            {
                return;
            }

            foreach (T point in points)
            {
                if (point == null || !seen.Add(point.transform))
                {
                    continue;
                }

                transforms.Add(point.transform);
            }
        }

        private static bool TryGetRandomPoint(Transform[] points, out Vector3 position)
        {
            position = default;
            if (points == null || points.Length == 0)
            {
                return false;
            }

            Transform point = points[Random.Range(0, points.Length)];
            if (point == null)
            {
                return false;
            }

            position = point.position;
            return true;
        }

        private Vector3 ResolveInitialFacing(Vector3 spawnPosition)
        {
            if (TryGetRandomPoint(_roamPoints, out Vector3 roamPosition))
            {
                return Flatten(roamPosition - spawnPosition);
            }

            return _player != null
                ? Flatten(_player.position - spawnPosition)
                : Vector3.forward;
        }

        private Vector3 ResolveSpawnHeight(Vector3 spawnPosition)
        {
            return TryProjectToGround(spawnPosition, out Vector3 groundedPosition)
                ? groundedPosition
                : spawnPosition;
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
            if (mainCamera != null)
            {
                Transform current = mainCamera.transform;
                while (current != null)
                {
                    if (current.CompareTag("Player") || current.name.Contains("Player"))
                    {
                        return current.gameObject;
                    }

                    current = current.parent;
                }
            }

            return null;
        }

        private void ResolveDoorWaypoints()
        {
            if (!TryGetEntranceDoorBounds(out Bounds doorBounds))
            {
                Vector3 behindPlayer = -Flatten(_player.forward);
                if (behindPlayer.sqrMagnitude < 0.001f)
                {
                    behindPlayer = Vector3.forward;
                }

                behindPlayer.Normalize();
                _insideDoorPosition = _player.position + behindPlayer * 2f;
                _outsideDoorPosition = _player.position + behindPlayer * _settings.SpawnDistanceBehindPlayer;
                _insideDoorPosition = ResolveSpawnHeight(_insideDoorPosition);
                _outsideDoorPosition = ResolveSpawnHeight(_outsideDoorPosition);
                Debug.LogWarning("Automatic entrance door was not found. Villains will use the fallback route behind the Player.");
                return;
            }

            Vector3 doorCenter = doorBounds.center;
            Vector3 insideDirection = Flatten(_player.position - doorCenter);
            if (insideDirection.sqrMagnitude < 0.001f)
            {
                insideDirection = Vector3.forward;
            }

            insideDirection.Normalize();
            _insideDoorPosition = doorCenter + insideDirection * _settings.DoorInsideDistance;
            _outsideDoorPosition = doorCenter - insideDirection * _settings.DoorOutsideDistance;
            _insideDoorPosition = ResolveSpawnHeight(_insideDoorPosition);
            _outsideDoorPosition = ResolveSpawnHeight(_outsideDoorPosition);
        }

        private static bool TryGetEntranceDoorBounds(out Bounds doorBounds)
        {
            doorBounds = default;
            bool foundRenderer = false;

            foreach (string doorName in EntranceDoorNames)
            {
                GameObject doorObject = GameObject.Find(doorName);
                if (doorObject == null)
                {
                    continue;
                }

                Renderer[] renderers = doorObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (!foundRenderer)
                    {
                        doorBounds = renderer.bounds;
                        foundRenderer = true;
                    }
                    else
                    {
                        doorBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return foundRenderer;
        }

        private void HandlePlayerDied()
        {
            StopAllCoroutines();
            RequestAllVillainsFlee();
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= HandlePlayerDied;
            }
        }

        private static bool TryProjectToGround(Vector3 position, out Vector3 groundedPosition)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit navMeshHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                groundedPosition = navMeshHit.position;
                return true;
            }

            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                GroundProbeDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            groundedPosition = position;
            bool foundGround = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = GroundHits[i];
                if (hit.transform == null || hit.normal.y < 0.45f || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                groundedPosition = new Vector3(position.x, hit.point.y, position.z);
                foundGround = true;
            }

            return foundGround;
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
