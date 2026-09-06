using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;
using UnityEngine.AI;
using Villains.Visuals;

namespace Villains.Movement
{
    public class VillainMovement : MonoBehaviour, IModule
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float fleeSpeed = 6.5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float arriveDistance = 0.4f;

        private CharacterController _characterController;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private Transform _ownerTransform;

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
                return;
            }

            FaceDirection(direction);
            Velocity = direction.normalized * speed;

            if (TryMoveWithNavMesh(destination, speed))
                return;

            if (_characterController != null)
                _characterController.SimpleMove(Velocity);
            else
                _ownerTransform.position += Velocity * Time.deltaTime;
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
            _navMeshAgent.updateRotation = false;
        }

        private bool TryMoveWithNavMesh(Vector3 destination, float speed)
        {
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
            _navMeshAgent.SetDestination(destinationHit.position);
            Velocity = _navMeshAgent.desiredVelocity.sqrMagnitude > 0.001f
                ? _navMeshAgent.desiredVelocity
                : Velocity;

            if (Velocity.sqrMagnitude > 0.001f)
                FaceDirection(Velocity);

            IsArrived = !_navMeshAgent.pathPending
                        && _navMeshAgent.remainingDistance <= arriveDistance;
            return true;
        }
    }
}
