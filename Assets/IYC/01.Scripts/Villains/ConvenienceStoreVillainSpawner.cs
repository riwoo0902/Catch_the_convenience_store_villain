using System.Collections;
using CWH.Player.Health;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class ConvenienceStoreVillainSpawner : MonoBehaviour
    {
        private const string SettingsResourceName = "VillainSpawnSettings";
        private static readonly string[] EntranceDoorNames =
        {
            "automaticDoor_L_gp",
            "automaticDoor_R_gp",
            "automaticDoorFrame"
        };

        private VillainSpawnSettings _settings;
        private Transform _player;
        private PlayerHealth _playerHealth;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Transform[] _spawnPoints = new Transform[0];
        private Transform[] _roamPoints = new Transform[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInConvenienceStore()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.name.Contains("ConvenienceStore")
                || FindFirstObjectByType<ConvenienceStoreVillainSpawner>() != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            GameObject spawnerObject = new("Convenience Store Villain Spawner");
            spawnerObject.AddComponent<ConvenienceStoreVillainSpawner>();
        }

        public static void RequestAllVillainsFlee()
        {
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

        private void Awake()
        {
            _settings = Resources.Load<VillainSpawnSettings>(SettingsResourceName);
            GameObject playerObject = GameObject.Find("Player");
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
            while (enabled && _playerHealth != null && !_playerHealth.IsDead)
            {
                float delay = Random.Range(_settings.MinimumSpawnDelay, _settings.MaximumSpawnDelay);
                yield return new WaitForSeconds(delay);

                if (_player != null && _playerHealth != null && !_playerHealth.IsDead)
                {
                    SpawnVillain();
                }
            }
        }

        private void SpawnVillain()
        {
            bool useCustomSpawnPoint = TryGetRandomPoint(_spawnPoints, out Vector3 spawnPosition);
            Vector3 entryDirection = useCustomSpawnPoint
                ? ResolveInitialFacing(spawnPosition)
                : Flatten(_insideDoorPosition - _outsideDoorPosition);
            if (entryDirection.sqrMagnitude < 0.001f)
            {
                entryDirection = Flatten(_player.position - _outsideDoorPosition);
            }

            entryDirection = entryDirection.sqrMagnitude > 0.001f
                ? entryDirection.normalized
                : Vector3.forward;

            float mischiefDelay = Random.Range(_settings.MinimumMischiefDelay, _settings.MaximumMischiefDelay);
            if (ShouldSpawnChefVillain())
            {
                SpawnChefVillain(
                    useCustomSpawnPoint,
                    useCustomSpawnPoint ? spawnPosition : _outsideDoorPosition,
                    entryDirection,
                    mischiefDelay);
                return;
            }

            if (ShouldSpawnProductDisturber())
            {
                SpawnProductDisturber(
                    useCustomSpawnPoint,
                    useCustomSpawnPoint ? spawnPosition : _outsideDoorPosition,
                    entryDirection,
                    mischiefDelay);
                return;
            }

            GameObject villainObject = Instantiate(
                _settings.VillainVisualPrefab,
                useCustomSpawnPoint ? spawnPosition : _outsideDoorPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            villainObject.name = "Brick Villain";

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
                return;
            }

            villainObject.transform.localScale = Vector3.one * _settings.VisualScale;
            RuntimeBrickVillain villain = villainObject.AddComponent<RuntimeBrickVillain>();
            villain.Initialize(
                _settings,
                _player,
                _insideDoorPosition,
                _outsideDoorPosition,
                !useCustomSpawnPoint,
                mischiefDelay,
                _roamPoints);
        }

        private bool ShouldSpawnChefVillain()
        {
            return _settings.ChefVillainVisualPrefab != null
                   && _settings.SpatulaProjectileVisualPrefab != null
                   && Random.value <= _settings.ChefVillainSpawnChance;
        }

        private void SpawnChefVillain(
            bool spawnedInside,
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
                !spawnedInside,
                mischiefDelay,
                _roamPoints);
        }

        private bool ShouldSpawnProductDisturber()
        {
            return _settings.ProductDisturberVisualPrefab != null
                   && Random.value <= _settings.ProductDisturberSpawnChance;
        }

        private void SpawnProductDisturber(
            bool spawnedInside,
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
                !spawnedInside,
                mischiefDelay,
                _roamPoints);
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
            _roamPoints = ExtractTransforms(roamPoints);
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
                float fallbackHeight = FindPlayerFootHeight();
                _insideDoorPosition.y = fallbackHeight;
                _outsideDoorPosition.y = fallbackHeight;
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
            float groundHeight = FindPlayerFootHeight();
            _insideDoorPosition = doorCenter + insideDirection * _settings.DoorInsideDistance;
            _outsideDoorPosition = doorCenter - insideDirection * _settings.DoorOutsideDistance;
            _insideDoorPosition.y = groundHeight;
            _outsideDoorPosition.y = groundHeight;
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

        private float FindPlayerFootHeight()
        {
            CharacterController playerController = _player.GetComponent<CharacterController>();
            if (playerController == null)
            {
                return _player.position.y;
            }

            return _player.position.y + playerController.center.y - playerController.height * 0.5f;
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
