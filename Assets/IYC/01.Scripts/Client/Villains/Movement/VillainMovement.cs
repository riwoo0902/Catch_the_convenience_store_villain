using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Villains.Movement
{
    public class VillainMovement : MonoBehaviour, IModule
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float fleeSpeed = 5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float arriveDistance = 0.4f;

        private CharacterController _characterController;
        private Transform _ownerTransform;

        public Vector3 Velocity { get; private set; }
        public bool IsArrived { get; private set; }

        public void Init(ModuleOwner owner)
        {
            _ownerTransform = owner.transform;
            _characterController = owner.GetComponent<CharacterController>();
        }

        public void Stop()
        {
            Velocity = Vector3.zero;
        }

        public void MoveTo(Vector3 destination)
            => MoveTo(destination, moveSpeed);

        public void FleeTo(Vector3 destination)
            => MoveTo(destination, fleeSpeed);

        private void MoveTo(Vector3 destination, float speed)
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
    }
}
