using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Villains.Environment;
using Villains.Visuals;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class RuntimeProductDisturberVillain : MonoBehaviour
    {
        private const float Gravity = -20f;
        private const string ShelfAName = "shelfA_gp";
        private const string ShelfBName = "shelfB_gp";
        private const string ProductGroupPrefix = "produce";
        private const string GroupSuffix = "_gp";

        private readonly List<Transform> _products = new();

        private VillainSpawnSettings _settings;
        private CharacterController _controller;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private Animator _animator;
        private Transform[] _roamPoints = new Transform[0];
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Vector3 _currentDestination;
        private float _verticalVelocity;
        private float _mischiefStartTime;
        private float _nextRoamDecisionTime;
        private float _nextDisturbTime;
        private float _fleeStartedTime;
        private string _currentAnimation;
        private bool _isEntering;
        private bool _isFleeing;
        private bool _isMischiefActive;
        private bool _hasDestination;
        private bool _reachedInsideExitWaypoint;

        public bool IsFleeing => _isFleeing;

        public void Initialize(
            VillainSpawnSettings settings,
            Vector3 insideDoorPosition,
            Vector3 outsideDoorPosition,
            bool enterFromOutside,
            float mischiefDelay,
            Transform[] roamPoints)
        {
            _settings = settings;
            _insideDoorPosition = insideDoorPosition;
            _outsideDoorPosition = outsideDoorPosition;
            _isEntering = enterFromOutside;
            _mischiefStartTime = Time.time + Mathf.Max(0f, mischiefDelay);
            _nextDisturbTime = _mischiefStartTime;
            _roamPoints = roamPoints ?? new Transform[0];

            EnsureMovementComponents();
            CacheProducts();

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && settings.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = settings.AnimatorController;
                _animator.applyRootMotion = false;
            }

            PlayAnimation("Run", 0f);
        }

        public void BeginFlee()
        {
            if (_isFleeing)
            {
                return;
            }

            _isFleeing = true;
            _isEntering = false;
            _isMischiefActive = false;
            _reachedInsideExitWaypoint = FlatSqrDistance(transform.position, _outsideDoorPosition)
                                         < FlatSqrDistance(transform.position, _insideDoorPosition);
            _fleeStartedTime = Time.time;
            PlayAnimation("Fast Run", 0.1f);
        }

        private void Update()
        {
            if (_settings == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_isFleeing)
            {
                UpdateFlee();
                return;
            }

            if (_isEntering)
            {
                UpdateEntering();
                return;
            }

            UpdateRoamAndDisturb();
        }

        private void EnsureMovementComponents()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = 1.8f;
                _controller.radius = 0.32f;
                _controller.center = new Vector3(0f, 0.9f, 0f);
                _controller.stepOffset = 0.25f;
            }

            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (_navMeshAgent == null)
            {
                _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
            }

            ConfigureNavMeshAgent(_settings.RoamSpeed, _settings.RoamPointReachDistance);

            _groundVisualAnchor = GetComponent<GroundVisualAnchor>();
            if (_groundVisualAnchor == null)
            {
                _groundVisualAnchor = gameObject.AddComponent<GroundVisualAnchor>();
            }

            _groundVisualAnchor.Configure(transform, null, 0.03f);
        }

        private void UpdateEntering()
        {
            Vector3 toInsideDoor = Flatten(_insideDoorPosition - transform.position);
            if (toInsideDoor.sqrMagnitude < 0.5f)
            {
                _isEntering = false;
                return;
            }

            FaceDirection(toInsideDoor);
            _currentDestination = _insideDoorPosition;
            Move(toInsideDoor.normalized * _settings.RoamSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void UpdateRoamAndDisturb()
        {
            if (!_isMischiefActive && Time.time >= _mischiefStartTime)
            {
                _isMischiefActive = true;
                _nextDisturbTime = Time.time;
            }

            if (_isMischiefActive && Time.time >= _nextDisturbTime)
            {
                if (!TryDisturbNearbyProduct())
                {
                    PickProductDestination();
                }

                _nextDisturbTime = Time.time + Mathf.Max(0.1f, _settings.ProductDisturbInterval);
            }

            if (!_hasDestination
                || Time.time >= _nextRoamDecisionTime
                || FlatSqrDistance(transform.position, _currentDestination) <= _settings.RoamPointReachDistance * _settings.RoamPointReachDistance)
            {
                PickNextDestination();
            }

            Vector3 toDestination = Flatten(_currentDestination - transform.position);
            if (toDestination.sqrMagnitude <= 0.001f)
            {
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.1f);
                return;
            }

            FaceDirection(toDestination);
            Move(toDestination.normalized * _settings.RoamSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void UpdateFlee()
        {
            Vector3 destination = _reachedInsideExitWaypoint
                ? _outsideDoorPosition
                : _insideDoorPosition;
            Vector3 toExit = Flatten(destination - transform.position);
            if (toExit.sqrMagnitude < 0.5f)
            {
                if (!_reachedInsideExitWaypoint)
                {
                    _reachedInsideExitWaypoint = true;
                    return;
                }

                Destroy(gameObject);
                return;
            }

            if (Time.time >= _fleeStartedTime + 15f)
            {
                Destroy(gameObject);
                return;
            }

            FaceDirection(toExit);
            _currentDestination = destination;
            Move(toExit.normalized * _settings.FleeSpeed);
            PlayAnimation("Fast Run", 0.1f);
        }

        private bool TryDisturbNearbyProduct()
        {
            RemoveMissingProducts();
            List<Transform> nearbyProducts = new();
            float radiusSqr = _settings.ProductDisturbRadius * _settings.ProductDisturbRadius;
            foreach (Transform product in _products)
            {
                if (FlatSqrDistance(transform.position, product.position) <= radiusSqr)
                {
                    nearbyProducts.Add(product);
                }
            }

            if (nearbyProducts.Count == 0)
            {
                return false;
            }

            Transform selectedProduct = nearbyProducts[UnityEngine.Random.Range(0, nearbyProducts.Count)];
            return ShelfProductDisturbance.TryDisturb(
                selectedProduct,
                _settings.ProductDisturbMaxPositionOffset,
                _settings.ProductDisturbMaxRotationOffset);
        }

        private void PickNextDestination()
        {
            if (_isMischiefActive && _products.Count > 0 && UnityEngine.Random.value < 0.65f)
            {
                PickProductDestination();
                return;
            }

            if (_roamPoints.Length > 0)
            {
                Transform point = _roamPoints[UnityEngine.Random.Range(0, _roamPoints.Length)];
                if (point != null)
                {
                    SetDestination(point.position);
                    return;
                }
            }

            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _settings.FallbackRoamRadius;
            SetDestination(transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y));
        }

        private void PickProductDestination()
        {
            RemoveMissingProducts();
            if (_products.Count == 0)
            {
                PickNextDestination();
                return;
            }

            Transform product = _products[UnityEngine.Random.Range(0, _products.Count)];
            SetDestination(product.position);
        }

        private void SetDestination(Vector3 destination)
        {
            _currentDestination = destination;
            _hasDestination = true;
            _nextRoamDecisionTime = Time.time + UnityEngine.Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
        }

        private void CacheProducts()
        {
            _products.Clear();
            HashSet<Transform> seenProducts = new();
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (Transform candidate in sceneTransforms)
            {
                if (!IsProduct(candidate) || !seenProducts.Add(candidate))
                {
                    continue;
                }

                _products.Add(candidate);
            }
        }

        private static bool IsProduct(Transform candidate)
        {
            if (candidate == null || candidate.parent == null || candidate.parent.parent == null)
            {
                return false;
            }

            Transform productGroup = candidate.parent;
            Transform shelf = productGroup.parent;
            return productGroup.name.StartsWith(ProductGroupPrefix, StringComparison.OrdinalIgnoreCase)
                   && productGroup.name.EndsWith(GroupSuffix, StringComparison.OrdinalIgnoreCase)
                   && (string.Equals(shelf.name, ShelfAName, StringComparison.Ordinal)
                       || string.Equals(shelf.name, ShelfBName, StringComparison.Ordinal));
        }

        private void RemoveMissingProducts()
        {
            for (int i = _products.Count - 1; i >= 0; i--)
            {
                if (_products[i] == null)
                {
                    _products.RemoveAt(i);
                }
            }
        }

        private void Move(Vector3 horizontalVelocity)
        {
            if (TryMoveWithNavMesh(horizontalVelocity))
            {
                return;
            }

            if (_controller == null || !_controller.enabled)
            {
                transform.position += horizontalVelocity * Time.deltaTime;
                return;
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private bool TryMoveWithNavMesh(Vector3 horizontalVelocity)
        {
            if (_navMeshAgent == null || !_navMeshAgent.enabled)
            {
                return false;
            }

            if (!_navMeshAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return false;
                }

                _navMeshAgent.Warp(hit.position);
            }

            if (horizontalVelocity.sqrMagnitude <= 0.001f)
            {
                _navMeshAgent.ResetPath();
                _navMeshAgent.velocity = Vector3.zero;
                return true;
            }

            if (!NavMesh.SamplePosition(_currentDestination, out NavMeshHit destinationHit, 2f, NavMesh.AllAreas))
            {
                return false;
            }

            ConfigureNavMeshAgent(horizontalVelocity.magnitude, _settings.RoamPointReachDistance);
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            if (desiredVelocity.sqrMagnitude > 0.001f)
            {
                FaceDirection(desiredVelocity);
            }

            return true;
        }

        private void ConfigureNavMeshAgent(float speed, float stoppingDistance)
        {
            if (_navMeshAgent == null)
            {
                return;
            }

            _navMeshAgent.speed = speed;
            _navMeshAgent.angularSpeed = 720f;
            _navMeshAgent.acceleration = Mathf.Max(8f, speed * 4f);
            _navMeshAgent.stoppingDistance = stoppingDistance;
            _navMeshAgent.radius = _controller != null ? _controller.radius : 0.32f;
            _navMeshAgent.height = _controller != null ? _controller.height : 1.8f;
            _navMeshAgent.baseOffset = 0f;
            _navMeshAgent.updateRotation = false;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

        private void PlayAnimation(string stateName, float transitionDuration)
        {
            if (_animator == null || _currentAnimation == stateName)
            {
                return;
            }

            _currentAnimation = stateName;
            if (transitionDuration <= 0f)
            {
                _animator.Play(stateName);
            }
            else
            {
                _animator.CrossFadeInFixedTime(stateName, transitionDuration);
            }
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        private static float FlatSqrDistance(Vector3 first, Vector3 second)
        {
            return Flatten(first - second).sqrMagnitude;
        }
    }
}
