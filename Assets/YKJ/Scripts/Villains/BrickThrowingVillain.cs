using Agents;
using Agents.FSM;
using Player;
using UnityEngine;
using UnityEngine.AI;
using Villains.Visuals;

namespace Villains
{
    public class BrickThrowingVillain : Agent
    {
        private const float Gravity = -20f;

        [Header("FSM")]
        [SerializeField] private StateListSO stateList;

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool findPlayerOnStart = true;
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private float throwRange = 7f;
        [SerializeField] private float aimHeightOffset = 1.2f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float fleeSpeed = 6.5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private Vector3 fallbackFleeDestination = new Vector3(0f, 0f, -12f);

        [Header("Throw")]
        [SerializeField] private BrickProjectile brickPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float throwSpeed = 12f;
        [SerializeField] private float throwCooldown = 2f;
        [SerializeField] private float brickLifeTime = 5f;
        [SerializeField] private int brickDamage = 1;

        private StateMachine _stateMachine;
        private CharacterController _characterController;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private float _lastThrowTime = -999f;
        private float _verticalVelocity;
        private int _lastGroundMoveFrame = -1;
        private bool _isFleeing;

        public bool HasTarget => target != null;
        public bool IsTargetInDetectionRange => HasTarget && DistanceToTarget <= detectionRange;
        public bool IsTargetInThrowRange => HasTarget && DistanceToTarget <= throwRange;
        public bool IsThrowReady => Time.time >= _lastThrowTime + throwCooldown;
        public float DistanceToTarget => HasTarget ? Vector3.Distance(transform.position, target.position) : float.MaxValue;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _characterController = GetComponent<CharacterController>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (_navMeshAgent == null)
                _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();

            ConfigureNavMeshAgent(moveSpeed, 0.4f);

            _groundVisualAnchor = GetComponent<GroundVisualAnchor>();
            if (_groundVisualAnchor == null)
                _groundVisualAnchor = gameObject.AddComponent<GroundVisualAnchor>();

            _groundVisualAnchor.Configure(transform, null, 0.03f);

            if (findPlayerOnStart && target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                    target = player.transform;
            }

            if (stateList != null)
                _stateMachine = new StateMachine(this, stateList.states);
        }

        private void Start()
        {
            ChangeState(VillainState.IDLE, 0f);
        }

        private void Update()
        {
            if (_isFleeing)
            {
                UpdateFlee();
                GroundIfIdleThisFrame();
                return;
            }

            _stateMachine?.UpdateMachine();
            GroundIfIdleThisFrame();
        }

        public void ChangeState(VillainState newState, float transitionDuration)
        {
            if (_stateMachine == null)
                return;

            _stateMachine.ChangeState((int)newState, transitionDuration);
        }

        public void MoveToTarget()
        {
            if (!HasTarget)
                return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.01f)
                return;

            RotateTo(direction);

            Vector3 velocity = direction.normalized * moveSpeed;
            if (TryMoveWithNavMesh(target.position, moveSpeed, throwRange, out Vector3 navVelocity))
                velocity = navVelocity;

            MoveWithGravity(velocity);
        }

        public void FaceTarget()
        {
            if (!HasTarget)
                return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            RotateTo(direction);
        }

        public void ThrowBrick()
        {
            if (!HasTarget || brickPrefab == null)
                return;

            Transform spawnTransform = throwPoint != null ? throwPoint : transform;
            Vector3 spawnPosition = spawnTransform.position;
            Vector3 targetPosition = target.position + Vector3.up * aimHeightOffset;
            Vector3 throwDirection = (targetPosition - spawnPosition).normalized;

            BrickProjectile brick = Instantiate(brickPrefab, spawnPosition, Quaternion.LookRotation(throwDirection));
            brick.Launch(throwDirection * throwSpeed, brickDamage, brickLifeTime);
            _lastThrowTime = Time.time;
        }

        public void FleeFromStore()
        {
            target = null;
            _isFleeing = true;
        }

        private void UpdateFlee()
        {
            Vector3 direction = fallbackFleeDestination - transform.position;
            direction.y = 0f;
            if (direction.magnitude <= 0.5f)
            {
                gameObject.SetActive(false);
                return;
            }

            RotateTo(direction);
            Vector3 velocity = direction.normalized * fleeSpeed;
            if (TryMoveWithNavMesh(fallbackFleeDestination, fleeSpeed, 0.5f, out Vector3 navVelocity))
                velocity = navVelocity;

            MoveWithGravity(velocity);
        }

        private void RotateTo(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private bool TryMoveWithNavMesh(Vector3 destination, float speed, float stoppingDistance, out Vector3 navVelocity)
        {
            navVelocity = Vector3.zero;
            if (_navMeshAgent == null || !_navMeshAgent.enabled)
                return false;

            if (!_navMeshAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    return false;

                _navMeshAgent.Warp(hit.position);
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 2f, NavMesh.AllAreas))
                return false;

            ConfigureNavMeshAgent(speed, stoppingDistance);
            _navMeshAgent.nextPosition = transform.position;
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            navVelocity = desiredVelocity.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(desiredVelocity, speed)
                : BuildSteeringVelocity(destinationHit.position, speed);

            if (navVelocity.sqrMagnitude > 0.001f)
                RotateTo(navVelocity);

            return true;
        }

        private Vector3 BuildSteeringVelocity(Vector3 destination, float speed)
        {
            if (_navMeshAgent != null && !_navMeshAgent.pathPending)
            {
                Vector3 toSteeringTarget = _navMeshAgent.steeringTarget - transform.position;
                toSteeringTarget.y = 0f;
                if (toSteeringTarget.sqrMagnitude > 0.001f)
                {
                    return Vector3.ClampMagnitude(toSteeringTarget.normalized * speed, speed);
                }
            }

            if (_isFleeing)
            {
                return Vector3.zero;
            }

            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            return toDestination.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(toDestination.normalized * speed, speed)
                : Vector3.zero;
        }

        private void MoveWithGravity(Vector3 horizontalVelocity)
        {
            if (_characterController == null || !_characterController.enabled)
            {
                transform.position += horizontalVelocity * Time.deltaTime;
                _lastGroundMoveFrame = Time.frameCount;
                return;
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                _navMeshAgent.nextPosition = transform.position;

            _lastGroundMoveFrame = Time.frameCount;
        }

        private void GroundIfIdleThisFrame()
        {
            if (_lastGroundMoveFrame == Time.frameCount)
                return;

            MoveWithGravity(Vector3.zero);
        }

        private void ConfigureNavMeshAgent(float speed, float stoppingDistance)
        {
            if (_navMeshAgent == null)
                return;

            _navMeshAgent.speed = speed;
            _navMeshAgent.angularSpeed = rotationSpeed;
            _navMeshAgent.acceleration = Mathf.Max(8f, speed * 4f);
            _navMeshAgent.stoppingDistance = stoppingDistance;
            _navMeshAgent.radius = _characterController != null ? _characterController.radius : 0.35f;
            _navMeshAgent.height = _characterController != null ? _characterController.height : 2f;
            _navMeshAgent.baseOffset = 0f;
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
        }
    }
}
