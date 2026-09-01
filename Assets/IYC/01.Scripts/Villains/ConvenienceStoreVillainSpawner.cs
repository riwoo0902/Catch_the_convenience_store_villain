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
            RuntimeBrickVillain[] villains = FindObjectsByType<RuntimeBrickVillain>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (RuntimeBrickVillain villain in villains)
            {
                villain.BeginFlee();
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
            Vector3 entryDirection = Flatten(_insideDoorPosition - _outsideDoorPosition);
            if (entryDirection.sqrMagnitude < 0.001f)
            {
                entryDirection = Flatten(_player.position - _outsideDoorPosition);
            }

            entryDirection = entryDirection.sqrMagnitude > 0.001f
                ? entryDirection.normalized
                : Vector3.forward;

            GameObject villainObject = Instantiate(
                _settings.VillainVisualPrefab,
                _outsideDoorPosition,
                Quaternion.LookRotation(entryDirection, Vector3.up));
            villainObject.name = "Brick Villain";
            villainObject.transform.localScale *= _settings.VisualScale;
            RuntimeBrickVillain villain = villainObject.AddComponent<RuntimeBrickVillain>();
            villain.Initialize(_settings, _player, _insideDoorPosition, _outsideDoorPosition);
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
