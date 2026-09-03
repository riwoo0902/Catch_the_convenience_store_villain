using UnityEngine;
using UnityEngine.AI;
using Villains.Data;
using Villains.Projectiles;
using Villains.Visuals;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBrickVillain : MonoBehaviour
    {
        private const float Gravity = -20f;

        private VillainSpawnSettings _settings;
        private Transform _target;
        private ProjectileThrowDataSO _throwDataOverride;
        private Projectile _projectilePrefabOverride;
        private GameObject _projectileVisualPrefabOverride;
        private bool _useRuntimeProjectileOverride;
        private bool _requiresProjectileOverride;
        private CharacterController _controller;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private Animator _animator;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Transform[] _roamPoints = new Transform[0];
        private Vector3 _currentRoamDestination;
        private float _verticalVelocity;
        private float _nextThrowTime;
        private float _fleeStartedTime;
        private float _animationLockedUntil;
        private float _pendingThrowTime;
        private float _mischiefStartTime;
        private float _nextRoamDecisionTime;
        private string _currentAnimation;
        private bool _isEntering;
        private bool _isFleeing;
        private bool _isRoaming;
        private bool _hasRoamDestination;
        private bool _reachedInsideExitWaypoint;
        private bool _hasPendingThrow;

        public bool IsFleeing => _isFleeing;

        public void Initialize(
            VillainSpawnSettings settings,
            Transform target,
            Vector3 insideDoorPosition,
            Vector3 outsideDoorPosition,
            bool enterFromOutside,
            float mischiefDelay,
            Transform[] roamPoints)
        {
            _settings = settings;
            _target = target;
            _insideDoorPosition = insideDoorPosition;
            _outsideDoorPosition = outsideDoorPosition;
            _isEntering = enterFromOutside;
            _isRoaming = !enterFromOutside || mischiefDelay > 0f;
            _mischiefStartTime = Time.time + Mathf.Max(0f, mischiefDelay);
            _roamPoints = roamPoints ?? new Transform[0];
            _nextThrowTime = Time.time + 0.8f;

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

            ConfigureNavMeshAgent(settings.ChaseSpeed);

            _groundVisualAnchor = GetComponent<GroundVisualAnchor>();
            if (_groundVisualAnchor == null)
            {
                _groundVisualAnchor = gameObject.AddComponent<GroundVisualAnchor>();
            }

            _groundVisualAnchor.Configure(transform, null, 0.03f);

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && settings.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = settings.AnimatorController;
            }

            PlayAnimation("Run", 0f);
        }

        public void UseProjectileVisual(GameObject projectileVisualPrefab, ProjectileThrowDataSO throwData)
        {
            _projectileVisualPrefabOverride = IsInvalidProjectileVisual(projectileVisualPrefab)
                ? null
                : projectileVisualPrefab;
            _throwDataOverride = throwData;
            _useRuntimeProjectileOverride = _projectileVisualPrefabOverride != null;
            _requiresProjectileOverride = true;

            if (projectileVisualPrefab != null && _projectileVisualPrefabOverride == null)
            {
                Debug.LogError($"{name} refused projectile visual '{projectileVisualPrefab.name}' because it looks like a Player/character prefab.");
            }
        }

        public void UseProjectilePrefab(Projectile projectilePrefab, ProjectileThrowDataSO throwData)
        {
            _projectilePrefabOverride = IsInvalidProjectilePrefab(projectilePrefab)
                ? null
                : projectilePrefab;
            _throwDataOverride = throwData;
            _useRuntimeProjectileOverride = false;
            _requiresProjectileOverride = true;

            if (projectilePrefab != null && _projectilePrefabOverride == null)
            {
                Debug.LogError($"{name} refused projectile prefab '{projectilePrefab.name}' because it looks like a Player/character prefab.");
            }
        }

        public void BeginFlee()
        {
            if (_isFleeing)
            {
                return;
            }

            _isFleeing = true;
            _isEntering = false;
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

            if (_isRoaming)
            {
                UpdateRoaming();
                return;
            }

            if (_target == null)
            {
                BeginFlee();
                return;
            }

            Vector3 toTarget = Flatten(_target.position - transform.position);
            float distance = toTarget.magnitude;
            float attackDistance = GetAttackDistance();
            if (distance > attackDistance)
            {
                FaceDirection(toTarget);
                Move(toTarget.normalized * _settings.ChaseSpeed);
                PlayAnimation("Run", 0.12f);
            }
            else
            {
                FaceDirection(toTarget);
                Move(Vector3.zero);
                UpdatePendingThrow();
                TryThrowProjectile();
                if (Time.time >= _animationLockedUntil)
                {
                    PlayAnimation("Idle", 0.12f);
                }
            }
        }

        private void UpdateEntering()
        {
            Vector3 toInsideDoor = Flatten(_insideDoorPosition - transform.position);
            if (toInsideDoor.sqrMagnitude < 0.5f)
            {
                _isEntering = false;
                _isRoaming = Time.time < _mischiefStartTime;
                return;
            }

            FaceDirection(toInsideDoor);
            Move(toInsideDoor.normalized * _settings.ChaseSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void UpdateRoaming()
        {
            if (Time.time >= _mischiefStartTime)
            {
                _isRoaming = false;
                _hasRoamDestination = false;
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.1f);
                return;
            }

            if (!_hasRoamDestination
                || Time.time >= _nextRoamDecisionTime
                || FlatSqrDistance(transform.position, _currentRoamDestination) <= _settings.RoamPointReachDistance * _settings.RoamPointReachDistance)
            {
                PickNextRoamDestination();
            }

            Vector3 toRoamDestination = Flatten(_currentRoamDestination - transform.position);
            if (toRoamDestination.sqrMagnitude <= 0.001f)
            {
                Move(Vector3.zero);
                PlayAnimation("Idle", 0.1f);
                return;
            }

            FaceDirection(toRoamDestination);
            Move(toRoamDestination.normalized * _settings.RoamSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void PickNextRoamDestination()
        {
            if (_roamPoints.Length > 0)
            {
                Transform point = _roamPoints[Random.Range(0, _roamPoints.Length)];
                if (point != null)
                {
                    _currentRoamDestination = point.position;
                    _hasRoamDestination = true;
                    _nextRoamDecisionTime = Time.time + Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
                    return;
                }
            }

            Vector2 randomCircle = Random.insideUnitCircle * _settings.FallbackRoamRadius;
            _currentRoamDestination = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            _hasRoamDestination = true;
            _nextRoamDecisionTime = Time.time + Random.Range(_settings.MinimumRoamWait, _settings.MaximumRoamWait);
        }

        private void TryThrowProjectile()
        {
            ProjectileThrowDataSO throwData = GetThrowData();
            if (throwData == null || !HasProjectileAsset() || _hasPendingThrow || Time.time < _nextThrowTime)
            {
                return;
            }

            float maxWaitTime = Mathf.Max(0.1f, throwData.maxAnimationWaitTime);
            _pendingThrowTime = Time.time + maxWaitTime * Mathf.Clamp01(throwData.releaseNormalizedTime);
            _nextThrowTime = Time.time + throwData.cooldown;
            _animationLockedUntil = Time.time + maxWaitTime;
            _hasPendingThrow = true;
            PlayAnimation("Throw", 0.05f, true);
        }

        private void UpdatePendingThrow()
        {
            if (!_hasPendingThrow || Time.time < _pendingThrowTime)
            {
                return;
            }

            _hasPendingThrow = false;
            ReleaseProjectile();
        }

        private void ReleaseProjectile()
        {
            ProjectileThrowDataSO throwData = GetThrowData();
            if (throwData == null || !HasProjectileAsset() || _target == null)
            {
                return;
            }

            float visualScale = _settings.VisualScale;
            Vector3 spawnPosition = transform.position
                                    + Vector3.up * (1.25f * visualScale)
                                    + transform.forward * (0.75f * visualScale);
            Vector3 targetPosition = _target.position + Vector3.up * 0.8f;
            Vector3 velocity = BuildInitialVelocity(spawnPosition, targetPosition, throwData);

            Component projectile = CreateProjectile(spawnPosition, velocity);
            if (projectile == null || IsProjectileTarget(projectile.transform))
            {
                if (projectile != null)
                {
                    Destroy(projectile.gameObject);
                }

                Debug.LogError($"{name} tried to throw an invalid projectile. Check VillainSpawnSettings projectile prefab.");
                return;
            }

            if (projectile is BrickProjectile brickProjectile)
            {
                brickProjectile.InitProjectile(null, throwData.damage, throwData.projectileLifeTime, velocity, 0);
            }
            else if (projectile is Projectile genericProjectile)
            {
                genericProjectile.InitProjectile(null, throwData.damage, throwData.projectileLifeTime, velocity, 0);
            }

            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
            {
                foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(projectileCollider, ownCollider, true);
                }
            }
        }

        private ProjectileThrowDataSO GetThrowData()
        {
            return _throwDataOverride != null ? _throwDataOverride : _settings.ThrowData;
        }

        private bool HasProjectileAsset()
        {
            if (_requiresProjectileOverride)
            {
                return _projectilePrefabOverride != null || _useRuntimeProjectileOverride;
            }

            return _projectilePrefabOverride != null || _useRuntimeProjectileOverride || _settings.BrickPrefab != null;
        }

        private float GetAttackDistance()
        {
            ProjectileThrowDataSO throwData = GetThrowData();
            float dataRange = throwData != null ? throwData.attackRange : 0f;
            return Mathf.Max(0.5f, dataRange > 0f ? dataRange : _settings.PreferredAttackDistance);
        }

        private Component CreateProjectile(Vector3 spawnPosition, Vector3 velocity)
        {
            Quaternion rotation = velocity.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
                : transform.rotation;

            if (_projectilePrefabOverride != null)
            {
                return Instantiate(_projectilePrefabOverride, spawnPosition, rotation);
            }

            if (_requiresProjectileOverride)
            {
                return null;
            }

            return _useRuntimeProjectileOverride
                ? CreateRuntimeProjectile(spawnPosition, velocity)
                : Instantiate(_settings.BrickPrefab, spawnPosition, rotation);
        }

        private Projectile CreateRuntimeProjectile(Vector3 spawnPosition, Vector3 velocity)
        {
            Quaternion rotation = velocity.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
                : transform.rotation;

            GameObject projectileObject = new("Spatula Projectile");
            projectileObject.transform.SetPositionAndRotation(spawnPosition, rotation);

            BoxCollider collider = projectileObject.AddComponent<BoxCollider>();
            collider.size = _settings.SpatulaProjectileColliderSize;

            Rigidbody rigidbody = projectileObject.AddComponent<Rigidbody>();
            rigidbody.mass = 0.6f;
            rigidbody.angularDamping = 0.02f;

            Projectile projectile = projectileObject.AddComponent<Projectile>();

            if (_projectileVisualPrefabOverride != null)
            {
                GameObject visual = Instantiate(_projectileVisualPrefabOverride, projectileObject.transform);
                visual.name = "Spatula Visual";
                visual.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(_settings.SpatulaProjectileVisualLocalRotation));
                visual.transform.localScale = _settings.SpatulaProjectileVisualLocalScale;
            }
            else
            {
                GameObject fallbackVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallbackVisual.name = "Spatula Visual Fallback";
                Destroy(fallbackVisual.GetComponent<Collider>());
                fallbackVisual.transform.SetParent(projectileObject.transform, false);
                fallbackVisual.transform.localScale = new Vector3(0.08f, 0.02f, 0.75f);
            }

            return projectile;
        }

        private static Vector3 BuildInitialVelocity(
            Vector3 origin,
            Vector3 targetPosition,
            ProjectileThrowDataSO throwData)
        {
            Vector3 offset = targetPosition - origin;
            Vector3 flatOffset = Flatten(offset);
            float distance = flatOffset.magnitude;
            Vector3 flatDirection = distance > 0.001f ? flatOffset / distance : Vector3.forward;

            float ratio = Mathf.InverseLerp(0f, throwData.attackRange, distance);
            float speedT = throwData.speedCurve != null ? throwData.speedCurve.Evaluate(ratio) : ratio;
            float pitchT = throwData.pitchCurve != null ? throwData.pitchCurve.Evaluate(ratio) : ratio;
            float speed = Mathf.Lerp(throwData.minSpeed, throwData.maxSpeed, speedT);
            float pitch = Mathf.Lerp(throwData.minPitchDeg, throwData.maxPitchDeg, pitchT) * Mathf.Deg2Rad;

            return flatDirection * (speed * Mathf.Cos(pitch)) + Vector3.up * (speed * Mathf.Sin(pitch));
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
            Move(toExit.normalized * _settings.FleeSpeed);
            PlayAnimation("Fast Run", 0.1f);
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
            if (_navMeshAgent == null || !_navMeshAgent.enabled || _settings == null)
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

            float speed = horizontalVelocity.magnitude;
            Vector3 destination = transform.position + horizontalVelocity.normalized * 1.5f;
            if (_isEntering)
            {
                destination = _insideDoorPosition;
            }
            else if (_isFleeing)
            {
                destination = _reachedInsideExitWaypoint ? _outsideDoorPosition : _insideDoorPosition;
            }
            else if (_isRoaming)
            {
                destination = _currentRoamDestination;
            }
            else if (_target != null)
            {
                destination = _target.position;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 2f, NavMesh.AllAreas))
            {
                return false;
            }

            ConfigureNavMeshAgent(speed);
            _navMeshAgent.stoppingDistance = _isEntering || _isFleeing
                ? 0.4f
                : _isRoaming
                    ? _settings.RoamPointReachDistance
                : Mathf.Max(0.4f, _settings.PreferredAttackDistance * 0.85f);
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            if (desiredVelocity.sqrMagnitude > 0.001f)
            {
                FaceDirection(desiredVelocity);
            }

            return true;
        }

        private void ConfigureNavMeshAgent(float speed)
        {
            if (_navMeshAgent == null)
            {
                return;
            }

            _navMeshAgent.speed = speed;
            _navMeshAgent.angularSpeed = 720f;
            _navMeshAgent.acceleration = Mathf.Max(8f, speed * 4f);
            _navMeshAgent.stoppingDistance = 0.4f;
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

        private void PlayAnimation(string stateName, float transitionDuration, bool force = false)
        {
            if (_animator == null || (!force && _currentAnimation == stateName))
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

        private bool IsProjectileTarget(Transform projectileTransform)
        {
            return projectileTransform != null
                   && _target != null
                   && projectileTransform.root == _target.root;
        }

        private static bool IsInvalidProjectilePrefab(Projectile projectilePrefab)
        {
            if (projectilePrefab == null)
            {
                return false;
            }

            Transform prefabTransform = projectilePrefab.transform;
            string rootName = prefabTransform.root.name;
            if (rootName.Contains("Player") || projectilePrefab.name.Contains("Player"))
            {
                return true;
            }

            return projectilePrefab.GetComponent<CharacterController>() != null;
        }

        private static bool IsInvalidProjectileVisual(GameObject projectileVisualPrefab)
        {
            if (projectileVisualPrefab == null)
            {
                return false;
            }

            return projectileVisualPrefab.name.Contains("Player")
                   || projectileVisualPrefab.GetComponent<CharacterController>() != null;
        }
    }
}
