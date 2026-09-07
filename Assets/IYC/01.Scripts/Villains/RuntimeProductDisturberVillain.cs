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
        private const float DisturbPoseDuration = 0.9f;
        private static readonly RaycastHit[] ShelfAvoidanceHits = new RaycastHit[8];

        private readonly List<Transform> _products = new();

        private VillainSpawnSettings _settings;
        private CharacterController _controller;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private PoliceTargetMarker _policeTargetMarker;
        private Animator _animator;
        private Transform[] _roamPoints = new Transform[0];
        private Transform _currentTargetProduct;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Vector3 _currentDestination;
        private Vector3 _lastStuckCheckPosition;
        private float _mischiefDelay;
        private float _verticalVelocity;
        private float _mischiefStartTime;
        private float _nextRoamDecisionTime;
        private float _nextDisturbTime;
        private float _fleeStartedTime;
        private float _stuckTimer;
        private float _poseLockedUntil;
        private string _currentAnimation;
        private bool _isEntering;
        private bool _isFleeing;
        private bool _isMischiefActive;
        private bool _hasDestination;
        private bool _reachedInsideExitWaypoint;
        private bool _entryAnnounced;
        private bool _angerAnnounced;

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
            _mischiefDelay = Mathf.Max(0f, mischiefDelay);
            _entryAnnounced = false;
            _angerAnnounced = false;
            _mischiefStartTime = enterFromOutside
                ? float.PositiveInfinity
                : Time.time + _mischiefDelay;
            _nextDisturbTime = _mischiefStartTime;
            _roamPoints = roamPoints ?? new Transform[0];

            EnsureMovementComponents();
            _policeTargetMarker = EnsurePoliceTargetMarker();
            _policeTargetMarker.SetWanted(false);
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
            PlayAnimation("Run", 0.1f);
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
            }

            float scale = Mathf.Max(0.01f, GetLargestWorldScale(transform));
            _controller.height = 1.8f / scale;
            _controller.radius = 0.32f / scale;
            _controller.center = new Vector3(0f, 0.9f / scale, 0f);
            _controller.stepOffset = 0.25f / scale;

            _navMeshAgent = GetComponent<NavMeshAgent>();
            var navMeshFilter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            if (_navMeshAgent == null
                && NavMesh.SamplePosition(transform.position, out NavMeshHit spawnHit, 2f, navMeshFilter))
            {
                transform.position = spawnHit.position;
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
                _mischiefStartTime = Time.time + _mischiefDelay;
                _nextDisturbTime = _mischiefStartTime;
                AnnounceEntry();
                return;
            }

            FaceDirection(toInsideDoor);
            _currentDestination = _insideDoorPosition;
            Move(toInsideDoor.normalized * _settings.ChaseSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void UpdateRoamAndDisturb()
        {
            if (!_isMischiefActive && Time.time >= _mischiefStartTime)
            {
                _isMischiefActive = true;
                _nextDisturbTime = Time.time;
                AnnounceAnger();
            }

            if (Time.time < _poseLockedUntil)
            {
                FaceTargetProduct();
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.08f);
                return;
            }

            if (_isMischiefActive && Time.time >= _nextDisturbTime)
            {
                if (!TryDisturbTargetProduct() && !TryDisturbNearbyProduct())
                {
                    PickProductDestination();
                }

                _nextDisturbTime = Time.time + Mathf.Max(0.1f, _settings.ProductDisturbInterval);
            }

            if (_hasDestination
                && FlatSqrDistance(transform.position, _currentDestination) <= _settings.RoamPointReachDistance * _settings.RoamPointReachDistance)
            {
                if (_isMischiefActive && TryDisturbTargetProduct())
                {
                    _nextDisturbTime = Time.time + Mathf.Max(0.1f, _settings.ProductDisturbInterval);
                }

                _hasDestination = false;
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.08f);
                return;
            }

            if (ShouldStopBeforeShelf())
            {
                if (_isMischiefActive && TryDisturbTargetProduct())
                {
                    _nextDisturbTime = Time.time + Mathf.Max(0.1f, _settings.ProductDisturbInterval);
                }

                _hasDestination = false;
                FaceTargetProduct();
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.08f);
                return;
            }

            if (!_hasDestination
                || Time.time >= _nextRoamDecisionTime)
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
            RecoverIfStuck();
            PlayAnimation(Time.time < _poseLockedUntil || ShouldStopBeforeShelf() ? "Idle" : "Run", 0.1f);
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
            PlayAnimation("Run", 0.1f);
        }

        private bool TryDisturbNearbyProduct()
        {
            RemoveMissingProducts();
            List<Transform> nearbyProducts = new();
            float disturbRadius = GetEffectiveDisturbRadius();
            float radiusSqr = disturbRadius * disturbRadius;
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
            _currentTargetProduct = selectedProduct;
            bool disturbed = ShelfProductDisturbance.TryDisturb(
                selectedProduct,
                _settings.ProductDisturbMaxPositionOffset,
                _settings.ProductDisturbMaxRotationOffset);
            if (disturbed)
            {
                LockDisturbPose();
            }

            return disturbed;
        }

        private bool TryDisturbTargetProduct()
        {
            if (_currentTargetProduct == null)
            {
                return false;
            }

            float reachDistance = Mathf.Max(_settings.RoamPointReachDistance * 2f, 1.2f);
            if (FlatSqrDistance(transform.position, _currentDestination) > reachDistance * reachDistance)
            {
                return false;
            }

            bool disturbed = ShelfProductDisturbance.TryDisturb(
                _currentTargetProduct,
                _settings.ProductDisturbMaxPositionOffset,
                _settings.ProductDisturbMaxRotationOffset);
            if (disturbed)
            {
                LockDisturbPose();
            }

            return disturbed;
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
            _currentTargetProduct = product;
            SetDestination(ResolveProductApproachDestination(product));
        }

        private void SetDestination(Vector3 destination)
        {
            _currentDestination = NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position
                : destination;
            _hasDestination = true;
            _lastStuckCheckPosition = transform.position;
            _stuckTimer = 0f;
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
            Vector3 moveVelocity = horizontalVelocity;
            if (TryMoveWithNavMesh(horizontalVelocity, out Vector3 navVelocity))
            {
                moveVelocity = navVelocity;
            }

            AvoidShelfHeadbutt(ref moveVelocity);

            if (_controller == null || !_controller.enabled)
            {
                transform.position += moveVelocity * Time.deltaTime;
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

            Vector3 velocity = moveVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.nextPosition = transform.position;
            }
        }

        private Vector3 ResolveProductApproachDestination(Transform product)
        {
            if (product == null)
            {
                return transform.position;
            }

            Transform shelf = ResolveShelf(product);
            if (shelf != null && TryGetRendererBounds(shelf, out Bounds shelfBounds))
            {
                float clearance = GetShelfClearance();
                Vector3[] directions = BuildShelfApproachDirections(product, shelf, shelfBounds);
                foreach (Vector3 direction in directions)
                {
                    if (direction.sqrMagnitude <= 0.001f)
                    {
                        continue;
                    }

                    Vector3 candidate = PointOutsideBounds(shelfBounds, direction.normalized, clearance);
                    candidate.y = transform.position.y;
                    if (TrySampleReachablePosition(candidate, 2.5f, out Vector3 sampled)
                        && IsPositionClearOfShelf(sampled, shelfBounds, clearance * 0.65f))
                    {
                        return sampled;
                    }
                }
            }

            Vector3 fallbackDirection = Flatten(transform.position - product.position);
            if (fallbackDirection.sqrMagnitude <= 0.001f)
            {
                fallbackDirection = Vector3.forward;
            }

            Vector3 fallback = product.position + fallbackDirection.normalized * GetShelfClearance();
            fallback.y = transform.position.y;
            if (TrySampleReachablePosition(fallback, 2.5f, out Vector3 sampledFallback))
            {
                return sampledFallback;
            }

            return transform.position;
        }

        private bool TrySampleReachablePosition(Vector3 candidate, float maxDistance, out Vector3 sampledPosition)
        {
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                sampledPosition = candidate;
                return false;
            }

            sampledPosition = hit.position;
            if (!IsPositionClearOfAnyShelf(sampledPosition, GetWorldControllerRadius() + 0.15f))
            {
                return false;
            }

            if (_navMeshAgent == null || !_navMeshAgent.enabled || !_navMeshAgent.isOnNavMesh)
            {
                return true;
            }

            NavMeshPath path = new();
            return _navMeshAgent.CalculatePath(sampledPosition, path)
                   && path.status != NavMeshPathStatus.PathInvalid;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
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

        private void AvoidShelfHeadbutt(ref Vector3 moveVelocity)
        {
            if (moveVelocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Vector3 direction = moveVelocity.normalized;
            Vector3 upper = transform.position + Vector3.up * 1.45f;
            float radius = Mathf.Max(0.22f, GetWorldControllerRadius() * 0.9f);
            int hitCount = Physics.SphereCastNonAlloc(
                upper,
                radius,
                direction,
                ShelfAvoidanceHits,
                1.1f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = ShelfAvoidanceHits[i];
                if (hit.transform == null || hit.transform.IsChildOf(transform) || !IsShelfOrProductPart(hit.transform))
                {
                    continue;
                }

                Vector3 normal = Flatten(hit.normal);
                if (normal.sqrMagnitude <= 0.001f)
                {
                    moveVelocity = Vector3.zero;
                    _hasDestination = false;
                    _nextRoamDecisionTime = Time.time;
                    LockDisturbPose();
                    return;
                }

                Vector3 slideVelocity = Vector3.ProjectOnPlane(moveVelocity, normal.normalized);
                if (slideVelocity.sqrMagnitude <= 0.001f)
                {
                    moveVelocity = Vector3.zero;
                    _hasDestination = false;
                    _nextRoamDecisionTime = Time.time;
                    LockDisturbPose();
                    return;
                }

                moveVelocity = Vector3.ClampMagnitude(slideVelocity, moveVelocity.magnitude);
                return;
            }

            Vector3 lower = transform.position + Vector3.up * 0.45f;
            hitCount = Physics.SphereCastNonAlloc(
                lower,
                radius,
                direction,
                ShelfAvoidanceHits,
                0.9f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = ShelfAvoidanceHits[i];
                if (hit.transform == null || hit.transform.IsChildOf(transform) || !IsShelfOrProductPart(hit.transform))
                {
                    continue;
                }

                moveVelocity = Vector3.zero;
                _hasDestination = false;
                _nextRoamDecisionTime = Time.time;
                LockDisturbPose();
                return;
            }
        }

        private static bool IsShelfOrProductPart(Transform candidate)
        {
            for (Transform current = candidate; current != null; current = current.parent)
            {
                if (string.Equals(current.name, ShelfAName, StringComparison.Ordinal)
                    || string.Equals(current.name, ShelfBName, StringComparison.Ordinal)
                    || (current.name.StartsWith(ProductGroupPrefix, StringComparison.OrdinalIgnoreCase)
                        && current.name.EndsWith(GroupSuffix, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private Transform ResolveShelf(Transform product)
        {
            return product != null && product.parent != null ? product.parent.parent : null;
        }

        private Vector3[] BuildShelfApproachDirections(Transform product, Transform shelf, Bounds shelfBounds)
        {
            Vector3 fromShelfToVillain = Flatten(transform.position - shelfBounds.center);
            Vector3 fromShelfToProduct = Flatten(product.position - shelfBounds.center);
            Vector3 shelfForward = Flatten(shelf.forward);
            Vector3 shelfRight = Flatten(shelf.right);

            return new[]
            {
                fromShelfToVillain,
                fromShelfToProduct,
                shelfForward,
                -shelfForward,
                shelfRight,
                -shelfRight,
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left
            };
        }

        private static Vector3 PointOutsideBounds(Bounds bounds, Vector3 direction, float clearance)
        {
            Vector3 flatDirection = Flatten(direction);
            if (flatDirection.sqrMagnitude <= 0.001f)
            {
                flatDirection = Vector3.forward;
            }

            flatDirection.Normalize();
            float projectedExtent = Mathf.Abs(flatDirection.x) * bounds.extents.x
                                    + Mathf.Abs(flatDirection.z) * bounds.extents.z;
            return bounds.center + flatDirection * (projectedExtent + clearance);
        }

        private bool IsPositionClearOfAnyShelf(Vector3 position, float clearance)
        {
            foreach (Transform product in _products)
            {
                Transform shelf = ResolveShelf(product);
                if (shelf != null
                    && TryGetRendererBounds(shelf, out Bounds shelfBounds)
                    && !IsPositionClearOfShelf(position, shelfBounds, clearance))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPositionClearOfShelf(Vector3 position, Bounds shelfBounds, float clearance)
        {
            Bounds expandedBounds = shelfBounds;
            expandedBounds.Expand(new Vector3(clearance * 2f, 0f, clearance * 2f));
            return !expandedBounds.Contains(new Vector3(position.x, shelfBounds.center.y, position.z));
        }

        private float GetEffectiveDisturbRadius()
        {
            return Mathf.Max(_settings.ProductDisturbRadius, GetShelfClearance() + 0.35f);
        }

        private float GetShelfClearance()
        {
            return Mathf.Max(
                _settings.ProductApproachDistance,
                GetWorldControllerRadius() + 0.9f,
                _settings.VisualScale * 0.85f);
        }

        private float GetWorldControllerRadius()
        {
            return _controller != null
                ? _controller.radius * GetLargestWorldScale(transform)
                : 0.32f;
        }

        private static float GetLargestWorldScale(Transform target)
        {
            Vector3 scale = target.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private void RecoverIfStuck()
        {
            if (!_hasDestination)
            {
                return;
            }

            float movedSqr = FlatSqrDistance(transform.position, _lastStuckCheckPosition);
            if (movedSqr > 0.04f)
            {
                _lastStuckCheckPosition = transform.position;
                _stuckTimer = 0f;
                return;
            }

            _stuckTimer += Time.deltaTime;
            if (_stuckTimer < 0.75f)
            {
                return;
            }

            _hasDestination = false;
            _currentTargetProduct = null;
            _nextRoamDecisionTime = Time.time;
            _stuckTimer = 0f;
            LockDisturbPose();
        }

        private void LockDisturbPose()
        {
            _poseLockedUntil = Mathf.Max(_poseLockedUntil, Time.time + DisturbPoseDuration);
        }

        private bool ShouldStopBeforeShelf()
        {
            if (!_isMischiefActive || _currentTargetProduct == null)
            {
                return false;
            }

            float stopDistance = Mathf.Max(_settings.RoamPointReachDistance, 0.85f);
            if (FlatSqrDistance(transform.position, _currentDestination) <= stopDistance * stopDistance)
            {
                return true;
            }

            Transform shelf = ResolveShelf(_currentTargetProduct);
            if (shelf == null || !TryGetRendererBounds(shelf, out Bounds shelfBounds))
            {
                return false;
            }

            Bounds expandedBounds = shelfBounds;
            float clearance = Mathf.Max(GetWorldControllerRadius() + 0.35f, 0.7f);
            expandedBounds.Expand(new Vector3(clearance * 2f, 0f, clearance * 2f));
            return expandedBounds.Contains(new Vector3(transform.position.x, shelfBounds.center.y, transform.position.z));
        }

        private void FaceTargetProduct()
        {
            if (_currentTargetProduct == null)
            {
                return;
            }

            Vector3 toProduct = Flatten(_currentTargetProduct.position - transform.position);
            FaceDirection(toProduct);
        }

        private bool TryMoveWithNavMesh(Vector3 horizontalVelocity, out Vector3 navVelocity)
        {
            navVelocity = Vector3.zero;
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
            _navMeshAgent.nextPosition = transform.position;
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            navVelocity = desiredVelocity.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(desiredVelocity, horizontalVelocity.magnitude)
                : BuildSteeringVelocity(destinationHit.position, horizontalVelocity.magnitude);

            if (navVelocity.sqrMagnitude > 0.001f)
            {
                FaceDirection(navVelocity);
            }

            return true;
        }

        private Vector3 BuildSteeringVelocity(Vector3 destination, float speed)
        {
            if (_navMeshAgent != null && !_navMeshAgent.pathPending)
            {
                Vector3 toSteeringTarget = Flatten(_navMeshAgent.steeringTarget - transform.position);
                if (toSteeringTarget.sqrMagnitude > 0.001f)
                {
                    return Vector3.ClampMagnitude(toSteeringTarget.normalized * speed, speed);
                }
            }

            if (_isEntering || _isFleeing || _hasDestination)
            {
                return Vector3.zero;
            }

            Vector3 toDestination = Flatten(destination - transform.position);
            return toDestination.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(toDestination.normalized * speed, speed)
                : Vector3.zero;
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
            _navMeshAgent.updatePosition = false;
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
