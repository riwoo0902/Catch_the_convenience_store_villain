using UnityEngine;
using UnityEngine.AI;
using Villains.Animation;
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
        private PoliceTargetMarker _policeTargetMarker;
        private VillainAnimationEventRelay _animationEvents;
        private Animator _animator;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private Transform[] _roamPoints = new Transform[0];
        private Vector3 _currentRoamDestination;
        private float _verticalVelocity;
        private float _mischiefDelay;
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
        private bool _entryAnnounced;
        private bool _angerAnnounced;

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
            _mischiefDelay = Mathf.Max(0f, mischiefDelay);
            _entryAnnounced = false;
            _angerAnnounced = false;
            _isRoaming = !enterFromOutside || _mischiefDelay > 0f;
            _mischiefStartTime = enterFromOutside
                ? float.PositiveInfinity
                : Time.time + _mischiefDelay;
            _roamPoints = roamPoints ?? new Transform[0];
            _nextThrowTime = Time.time + 0.8f;
            _policeTargetMarker = EnsurePoliceTargetMarker();
            _policeTargetMarker.SetWanted(false);

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
            var navMeshFilter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            if (_navMeshAgent == null
                && NavMesh.SamplePosition(transform.position, out NavMeshHit spawnHit, 2f, navMeshFilter))
            {
                transform.position = spawnHit.position;
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

            InstallAnimationEvents();
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
            _hasPendingThrow = false;
            _isEntering = false;
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
                _mischiefStartTime = Time.time + _mischiefDelay;
                _isRoaming = _mischiefDelay > 0f;
                AnnounceEntry();
                if (!_isRoaming)
                {
                    AnnounceAnger();
                }
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
                AnnounceAnger();
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
                    SetRoamDestination(point.position);
                    return;
                }
            }

            Vector2 randomCircle = Random.insideUnitCircle * _settings.FallbackRoamRadius;
            SetRoamDestination(transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y));
        }

        private void SetRoamDestination(Vector3 destination)
        {
            _currentRoamDestination = NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas)
                ? hit.position
                : destination;
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
            float throwAnimationDuration = GetAnimationDurationOrFallback("Throw", maxWaitTime);
            float releaseDelay = throwAnimationDuration * Mathf.Clamp01(throwData.releaseNormalizedTime);
            _pendingThrowTime = Time.time + Mathf.Clamp(releaseDelay, 0f, throwAnimationDuration * 0.95f);
            _nextThrowTime = Time.time + throwData.cooldown;
            _animationLockedUntil = Time.time + throwAnimationDuration;
            _hasPendingThrow = true;
            PlayAnimation("Throw", 0.05f, true);
        }

        private void UpdatePendingThrow()
        {
            if (!_hasPendingThrow || Time.time < _pendingThrowTime)
            {
                return;
            }

            ReleasePendingProjectile();
        }

        private void ReleasePendingProjectile()
        {
            // A queued throw must never fire while the villain is running in, roaming or fleeing.
            if (!_hasPendingThrow || _isFleeing || _isEntering || _isRoaming)
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

            if (_useRuntimeProjectileOverride)
            {
                return CreateRuntimeProjectile(spawnPosition, velocity);
            }

            if (_requiresProjectileOverride)
            {
                return CreateRuntimeProjectile(spawnPosition, velocity);
            }

            return Instantiate(_settings.BrickPrefab, spawnPosition, rotation);
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

            GameObject visual = CreateProjectileVisual(projectileObject.transform);
            if (visual != null)
            {
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

        private GameObject CreateProjectileVisual(Transform parent)
        {
            if (_projectileVisualPrefabOverride == null)
            {
                return null;
            }

            GameObject source = Instantiate(_projectileVisualPrefabOverride);
            source.name = "Spatula Visual Source";
            source.SetActive(false);
            StripNonProjectileVisualComponents(source);

            GameObject visualRoot = new("Spatula Visual");
            visualRoot.transform.SetParent(parent, false);
            CopyMeshRenderers(source.transform, visualRoot.transform);
            Destroy(source);

            if (visualRoot.GetComponentsInChildren<Renderer>(true).Length <= 0)
            {
                Destroy(visualRoot);
                return null;
            }

            return visualRoot;
        }

        private static void CopyMeshRenderers(Transform sourceRoot, Transform targetRoot)
        {
            foreach (MeshFilter sourceFilter in sourceRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
                if (sourceRenderer == null || sourceFilter.sharedMesh == null)
                {
                    continue;
                }

                GameObject copy = new(sourceFilter.gameObject.name);
                copy.transform.SetParent(targetRoot, false);
                CopyRelativeTransform(sourceRoot, sourceFilter.transform, copy.transform);

                MeshFilter targetFilter = copy.AddComponent<MeshFilter>();
                targetFilter.sharedMesh = sourceFilter.sharedMesh;

                MeshRenderer targetRenderer = copy.AddComponent<MeshRenderer>();
                targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                CopyRendererSettings(sourceRenderer, targetRenderer);
            }
        }

        private static void CopyRelativeTransform(Transform sourceRoot, Transform source, Transform destination)
        {
            destination.localPosition = sourceRoot.InverseTransformPoint(source.position);
            destination.localRotation = Quaternion.Inverse(sourceRoot.rotation) * source.rotation;

            Vector3 rootScale = sourceRoot.lossyScale;
            Vector3 sourceScale = source.lossyScale;
            destination.localScale = new Vector3(
                Mathf.Abs(rootScale.x) > 0.0001f ? sourceScale.x / rootScale.x : source.localScale.x,
                Mathf.Abs(rootScale.y) > 0.0001f ? sourceScale.y / rootScale.y : source.localScale.y,
                Mathf.Abs(rootScale.z) > 0.0001f ? sourceScale.z / rootScale.z : source.localScale.z);
        }

        private static void CopyRendererSettings(Renderer source, Renderer destination)
        {
            destination.shadowCastingMode = source.shadowCastingMode;
            destination.receiveShadows = source.receiveShadows;
            destination.lightProbeUsage = source.lightProbeUsage;
            destination.reflectionProbeUsage = source.reflectionProbeUsage;
            destination.probeAnchor = source.probeAnchor;
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
            PlayAnimation("Run", 0.1f);
        }

        private void Move(Vector3 horizontalVelocity)
        {
            Vector3 moveVelocity = horizontalVelocity;
            if (TryMoveWithNavMesh(horizontalVelocity, out Vector3 navVelocity))
            {
                moveVelocity = navVelocity;
            }

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

        private bool TryMoveWithNavMesh(Vector3 horizontalVelocity, out Vector3 navVelocity)
        {
            navVelocity = Vector3.zero;
            if (_navMeshAgent == null || !_navMeshAgent.enabled || _settings == null)
            {
                return false;
            }

            if (!_navMeshAgent.isOnNavMesh)
            {
                var filter = new NavMeshQueryFilter
                {
                    agentTypeID = _navMeshAgent.agentTypeID,
                    areaMask = _navMeshAgent.areaMask
                };
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, filter)
                    || !_navMeshAgent.Warp(hit.position)
                    || !_navMeshAgent.isOnNavMesh)
                {
                    return false;
                }
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
            _navMeshAgent.nextPosition = transform.position;
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            navVelocity = desiredVelocity.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(desiredVelocity, speed)
                : BuildSteeringVelocity(destinationHit.position, speed);

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

            if (_isEntering || _isFleeing || _isRoaming)
            {
                return Vector3.zero;
            }

            Vector3 toDestination = Flatten(destination - transform.position);
            return toDestination.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(toDestination.normalized * speed, speed)
                : Vector3.zero;
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

        private void InstallAnimationEvents()
        {
            UninstallAnimationEvents();

            if (_animator == null)
            {
                return;
            }

            _animationEvents = _animator.GetComponent<VillainAnimationEventRelay>();
            if (_animationEvents == null)
            {
                _animationEvents = _animator.gameObject.AddComponent<VillainAnimationEventRelay>();
            }

            _animationEvents.OnThrowTrigger += HandleThrowAnimationEvent;
            _animationEvents.OnAnimationEndTrigger += HandleThrowAnimationEndEvent;
        }

        private void UninstallAnimationEvents()
        {
            if (_animationEvents == null)
            {
                return;
            }

            _animationEvents.OnThrowTrigger -= HandleThrowAnimationEvent;
            _animationEvents.OnAnimationEndTrigger -= HandleThrowAnimationEndEvent;
            _animationEvents = null;
        }

        private void HandleThrowAnimationEvent()
        {
            ReleasePendingProjectile();
        }

        private void HandleThrowAnimationEndEvent()
        {
            _animationLockedUntil = Time.time;
            if (_hasPendingThrow)
            {
                ReleasePendingProjectile();
            }
        }

        private float GetAnimationDurationOrFallback(string stateName, float fallbackDuration)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return Mathf.Max(0.1f, fallbackDuration);
            }

            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == stateName)
                {
                    return Mathf.Max(0.1f, clip.length / Mathf.Max(0.01f, _animator.speed));
                }
            }

            return Mathf.Max(0.1f, fallbackDuration);
        }

        private void OnDestroy()
        {
            UninstallAnimationEvents();
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

        private static void StripNonProjectileVisualComponents(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            foreach (Camera camera in visualRoot.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
                Destroy(camera);
            }

            foreach (Light light in visualRoot.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
                Destroy(light);
            }

            foreach (AudioListener listener in visualRoot.GetComponentsInChildren<AudioListener>(true))
            {
                listener.enabled = false;
                Destroy(listener);
            }
        }
    }
}
