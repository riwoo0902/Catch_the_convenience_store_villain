using UnityEngine;
using Villains.Movement;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class RuntimeVillainRoamer : MonoBehaviour
    {
        private global::Villains.BrickVillain _villain;
        private VillainSpawnSettings _settings;
        private Transform[] _roamPoints = new Transform[0];
        private Vector3 _currentDestination;
        private float _mischiefStartTime;
        private float _nextRoamDecisionTime;
        private bool _configured;
        private bool _hasDestination;

        public void Configure(
            global::Villains.BrickVillain villain,
            VillainSpawnSettings settings,
            Transform[] roamPoints,
            float mischiefDelay)
        {
            _villain = villain;
            _settings = settings;
            _roamPoints = roamPoints ?? new Transform[0];
            _mischiefStartTime = Time.time + Mathf.Max(0f, mischiefDelay);
            _configured = true;

            if (_villain != null)
            {
                if (_villain.TargetDetector != null)
                {
                    _villain.TargetDetector.enabled = false;
                }

                _villain.TargetProvider?.ClearTarget();
            }
        }

        private void Update()
        {
            if (!_configured || _villain == null || _settings == null)
            {
                return;
            }

            _villain.TargetProvider?.ClearTarget();
            if (Time.time >= _mischiefStartTime)
            {
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

        private void PickNextDestination()
        {
            if (_roamPoints.Length > 0)
            {
                Transform point = _roamPoints[Random.Range(0, _roamPoints.Length)];
                if (point != null)
                {
                    _currentDestination = point.position;
                    _hasDestination = true;
                    _nextRoamDecisionTime = Time.time + Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
                    return;
                }
            }

            Vector2 randomCircle = Random.insideUnitCircle * _settings.FallbackRoamRadius;
            _currentDestination = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            _hasDestination = true;
            _nextRoamDecisionTime = Time.time + Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
        }

        private static float FlatSqrDistance(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }
    }
}
