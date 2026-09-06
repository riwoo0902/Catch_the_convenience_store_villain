using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;
using UnityEngine.AI;
using Villains.Visuals;

namespace Villains.Movement
{
    public class VillainMovement : MonoBehaviour, IModule
    {
        private const float Gravity = -20f;

        [SerializeField] private float moveSpeed = 7.5f;
        [SerializeField] private float fleeSpeed = 10.5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float arriveDistance = 0.4f;

        private CharacterController _characterController;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private Transform _ownerTransform;
        private float _verticalVelocity;

        public Vector3 Velocity { get; private set; }
        public bool IsArrived { get; private set; }

        public void Init(ModuleOwner owner)
        {
            _ownerTransform = owner.transform;
            _characterController = owner.GetComponent<CharacterController>();
            _navMeshAgent = owner.GetComponent<NavMeshAgent>();
            if (_navMeshAgent == null)
                _navMeshAgent = owner.gameObject.AddComponent<NavMeshAgent>();

            ConfigureNavMeshAgent(moveSpeed);

            _groundVisualAnchor = owner.GetComponent<GroundVisualAnchor>();
            if (_groundVisualAnchor == null)
                _groundVisualAnchor = owner.gameObject.AddComponent<GroundVisualAnchor>();

            _groundVisualAnchor.Configure(owner.transform, null, 0.03f);
        }

        public void Stop()
        {
            Velocity = Vector3.zero;
            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.ResetPath();
                _navMeshAgent.velocity = Vector3.zero;
            }
        }

        public void MoveTo(Vector3 destination)
            => MoveTo(destination, moveSpeed);

        public void FleeTo(Vector3 destination)
            => MoveTo(destination, fleeSpeed);

        public void MoveTo(Vector3 destination, float speed)
        {
            Vector3 direction = destination - _ownerTransform.position;
            direction.y = 0f;

            IsArrived = direction.magnitude <= arriveDistance;
            if (IsArrived)
            {
                Stop();
                MoveWithGravity(Vector3.zero);
                return;
            }

            FaceDirection(direction);
            Velocity = direction.normalized * speed;

            Vector3 moveVelocity = Velocity;
            if (TryMoveWithNavMesh(destination, speed, out Vector3 navVelocity))
                moveVelocity = navVelocity;

            MoveWithGravity(moveVelocity);
        }

        public void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - _ownerTransform.position;
            direction.y = 0f;
            FaceDirection(direction);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            _ownerTransform.rotation = Quaternion.RotateTowards(
                _ownerTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        private void ConfigureNavMeshAgent(float speed)
        {
            if (_navMeshAgent == null)
                return;

            _navMeshAgent.speed = speed;
            _navMeshAgent.angularSpeed = rotationSpeed;
            _navMeshAgent.acceleration = Mathf.Max(8f, speed * 4f);
            _navMeshAgent.stoppingDistance = arriveDistance;
            _navMeshAgent.radius = _characterController != null ? _characterController.radius : 0.35f;
            _navMeshAgent.height = _characterController != null ? _characterController.height : 2f;
            _navMeshAgent.baseOffset = 0f;
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
        }

        private bool TryMoveWithNavMesh(Vector3 destination, float speed, out Vector3 navVelocity)
        {
            navVelocity = Vector3.zero;
            if (_navMeshAgent == null || !_navMeshAgent.enabled)
                return false;

            if (!_navMeshAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(_ownerTransform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    return false;

                _navMeshAgent.Warp(hit.position);
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 2f, NavMesh.AllAreas))
                return false;

            ConfigureNavMeshAgent(speed);
            _navMeshAgent.nextPosition = _ownerTransform.position;
            _navMeshAgent.SetDestination(destinationHit.position);
            navVelocity = _navMeshAgent.desiredVelocity.sqrMagnitude > 0.001f
                ? Vector3.ClampMagnitude(_navMeshAgent.desiredVelocity, speed)
                : Velocity;

            if (navVelocity.sqrMagnitude > 0.001f)
                FaceDirection(navVelocity);

            IsArrived = !_navMeshAgent.pathPending
                        && _navMeshAgent.remainingDistance <= arriveDistance;
            return true;
        }

        private void MoveWithGravity(Vector3 horizontalVelocity)
        {
            if (_characterController == null || !_characterController.enabled)
            {
                _ownerTransform.position += horizontalVelocity * Time.deltaTime;
                return;
            }

            if (_characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                _navMeshAgent.nextPosition = _ownerTransform.position;
        }
    }
}
