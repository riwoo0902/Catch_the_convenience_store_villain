using Agents;
using Agents.FSM;
using Player;
using UnityEngine;

namespace Villains
{
    public class BrickThrowingVillain : Agent
    {
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
        [SerializeField] private float rotationSpeed = 540f;

        [Header("Throw")]
        [SerializeField] private BrickProjectile brickPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float throwSpeed = 12f;
        [SerializeField] private float throwCooldown = 2f;
        [SerializeField] private float brickLifeTime = 5f;
        [SerializeField] private int brickDamage = 1;

        private StateMachine _stateMachine;
        private CharacterController _characterController;
        private float _lastThrowTime = -999f;

        public bool HasTarget => target != null;
        public bool IsTargetInDetectionRange => HasTarget && DistanceToTarget <= detectionRange;
        public bool IsTargetInThrowRange => HasTarget && DistanceToTarget <= throwRange;
        public bool IsThrowReady => Time.time >= _lastThrowTime + throwCooldown;
        public float DistanceToTarget => HasTarget ? Vector3.Distance(transform.position, target.position) : float.MaxValue;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _characterController = GetComponent<CharacterController>();

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
            _stateMachine?.UpdateMachine();
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
            if (_characterController != null)
                _characterController.SimpleMove(velocity);
            else
                transform.position += velocity * Time.deltaTime;
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
    }
}
