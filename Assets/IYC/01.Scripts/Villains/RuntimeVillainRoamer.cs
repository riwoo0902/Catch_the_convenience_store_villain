using UnityEngine;
using UnityEngine.AI;
using Villains.Movement;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class RuntimeVillainRoamer : MonoBehaviour
    {
        private global::Villains.BrickVillain _villain;
        private VillainSpawnSettings _settings;
        private PoliceTargetMarker _policeTargetMarker;
        private Transform[] _roamPoints = new Transform[0];
        private Vector3 _currentDestination;
        private Vector3 _entryDestination;
        private float _mischiefDelay;
        private float _mischiefStartTime;
        private float _nextRoamDecisionTime;
        private bool _configured;
        private bool _hasDestination;
        private bool _isEntering;
        private bool _entryAnnounced;
        private bool _angerAnnounced;

        public void Configure(
            global::Villains.BrickVillain villain,
            VillainSpawnSettings settings,
            Transform[] roamPoints,
            float mischiefDelay)
        {
            _villain = villain;
            _settings = settings;
            _roamPoints = roamPoints ?? new Transform[0];
            _mischiefDelay = Mathf.Max(0f, mischiefDelay);
            _mischiefStartTime = Time.time + _mischiefDelay;
            _configured = true;
            _entryAnnounced = false;
            _angerAnnounced = false;
            _policeTargetMarker = EnsurePoliceTargetMarker();
            _policeTargetMarker.SetWanted(false);

            if (_villain != null)
            {
                if (_villain.TargetDetector != null)
                {
                    _villain.TargetDetector.enabled = false;
                }

                _villain.TargetProvider?.ClearTarget();
            }
        }

        public void ConfigureEntry(Vector3 entryDestination)
        {
            _entryDestination = NavMesh.SamplePosition(entryDestination, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position
                : entryDestination;
            _mischiefStartTime = float.PositiveInfinity;
            _isEntering = true;
            _hasDestination = false;
        }

        private void Update()
        {
            if (!_configured || _villain == null || _settings == null)
            {
                return;
            }

            _villain.TargetProvider?.ClearTarget();
            if (_isEntering)
            {
                UpdateEntering();
                return;
            }

            if (Time.time >= _mischiefStartTime)
            {
                AnnounceAnger();

                if (_villain.TargetDetector != null)
                {
                    _villain.TargetDetector.enabled = true;
                }

                Destroy(this);
                return;
            }

            VillainMovement movement = _villain.Movement;
            if (movement == null)
            {
                return;
            }

            if (!_hasDestination
                || Time.time >= _nextRoamDecisionTime
                || FlatSqrDistance(transform.position, _currentDestination) <= _settings.RoamPointReachDistance * _settings.RoamPointReachDistance)
            {
                PickNextDestination();
            }

            movement.MoveTo(_currentDestination, _settings.RoamSpeed);
        }

        private void UpdateEntering()
        {
            VillainMovement movement = _villain.Movement;
            if (movement == null)
            {
                return;
            }

            if (FlatSqrDistance(transform.position, _entryDestination) <= _settings.RoamPointReachDistance * _settings.RoamPointReachDistance)
            {
                _isEntering = false;
                _mischiefStartTime = Time.time + _mischiefDelay;
                _hasDestination = false;
                movement.Stop();
                AnnounceEntry();
                return;
            }

            movement.MoveTo(_entryDestination, _settings.ChaseSpeed);
        }

        private void PickNextDestination()
        {
            if (_roamPoints.Length > 0)
            {
                Transform point = _roamPoints[Random.Range(0, _roamPoints.Length)];
                if (point != null)
                {
                    SetDestination(point.position);
                    return;
                }
            }

            Vector2 randomCircle = Random.insideUnitCircle * _settings.FallbackRoamRadius;
            SetDestination(transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y));
        }

        private void SetDestination(Vector3 destination)
        {
            _currentDestination = NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position
                : destination;
            _hasDestination = true;
            _nextRoamDecisionTime = Time.time + Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
        }

        private static float FlatSqrDistance(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void AnnounceEntry()
        {
            if (_entryAnnounced)
            {
                return;
            }

            _entryAnnounced = true;
            ConvenienceStoreVillainSpawner.NotifyVillainEnteredStore(name);
        }

        private void AnnounceAnger()
        {
            if (_angerAnnounced)
            {
                return;
            }

            _angerAnnounced = true;
            _policeTargetMarker ??= EnsurePoliceTargetMarker();
            _policeTargetMarker.SetWanted(true);
            ConvenienceStoreVillainSpawner.NotifyVillainBecameAngry();
        }

        private PoliceTargetMarker EnsurePoliceTargetMarker()
        {
            PoliceTargetMarker marker = GetComponent<PoliceTargetMarker>();
            return marker != null ? marker : gameObject.AddComponent<PoliceTargetMarker>();
        }
    }
}
