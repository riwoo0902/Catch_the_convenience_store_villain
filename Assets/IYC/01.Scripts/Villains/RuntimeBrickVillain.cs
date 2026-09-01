using UnityEngine;
using Villains.Data;
using Villains.Projectiles;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBrickVillain : MonoBehaviour
    {
        private const float Gravity = -20f;

        private VillainSpawnSettings _settings;
        private Transform _target;
        private CharacterController _controller;
        private Animator _animator;
        private Vector3 _insideDoorPosition;
        private Vector3 _outsideDoorPosition;
        private float _verticalVelocity;
        private float _nextThrowTime;
        private float _fleeStartedTime;
        private float _animationLockedUntil;
        private string _currentAnimation;
        private bool _isEntering;
        private bool _isFleeing;
        private bool _reachedInsideExitWaypoint;

        public bool IsFleeing => _isFleeing;

        public void Initialize(
            VillainSpawnSettings settings,
            Transform target,
            Vector3 insideDoorPosition,
            Vector3 outsideDoorPosition)
        {
            _settings = settings;
            _target = target;
            _insideDoorPosition = insideDoorPosition;
            _outsideDoorPosition = outsideDoorPosition;
            _isEntering = true;
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

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && settings.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = settings.AnimatorController;
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

            if (_target == null)
            {
                BeginFlee();
                return;
            }

            Vector3 toTarget = Flatten(_target.position - transform.position);
            float distance = toTarget.magnitude;
            if (distance > _settings.PreferredAttackDistance)
            {
                FaceDirection(toTarget);
                Move(toTarget.normalized * _settings.ChaseSpeed);
                PlayAnimation("Run", 0.12f);
            }
            else
            {
                FaceDirection(toTarget);
                Move(Vector3.zero);
                TryThrowBrick();
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
                return;
            }

            FaceDirection(toInsideDoor);
            Move(toInsideDoor.normalized * _settings.ChaseSpeed);
            PlayAnimation("Run", 0.1f);
        }

        private void TryThrowBrick()
        {
            BrickThrowDataSO throwData = _settings.ThrowData;
            BrickProjectile brickPrefab = _settings.BrickPrefab;
            if (throwData == null || brickPrefab == null || Time.time < _nextThrowTime)
            {
                return;
            }

            float visualScale = _settings.VisualScale;
            Vector3 spawnPosition = transform.position
                                    + Vector3.up * (1.25f * visualScale)
                                    + transform.forward * (0.75f * visualScale);
            Vector3 targetPosition = _target.position + Vector3.up * 1.2f;
            Vector3 velocity = BuildInitialVelocity(spawnPosition, targetPosition, throwData);
            BrickProjectile projectile = Instantiate(
                brickPrefab,
                spawnPosition,
                Quaternion.LookRotation(velocity.normalized));
            projectile.InitProjectile(null, throwData.damage, throwData.projectileLifeTime, velocity, 0);

            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (projectileCollider != null)
            {
                foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(projectileCollider, ownCollider, true);
                }
            }

            _nextThrowTime = Time.time + throwData.cooldown;
            _animationLockedUntil = Time.time + 0.55f;
            PlayAnimation("Throw", 0.05f, true);
        }

        private static Vector3 BuildInitialVelocity(
            Vector3 origin,
            Vector3 targetPosition,
            BrickThrowDataSO throwData)
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
    }
}
