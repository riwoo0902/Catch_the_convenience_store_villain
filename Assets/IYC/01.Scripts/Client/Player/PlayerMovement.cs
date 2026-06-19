using System.Net.NetworkInformation;
using CoreSystem.DataSystem;
using CoreSystem.EffectSystem;
using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Player
{
    public class PlayerMovement : MonoBehaviour, IModule, IControlMovement, IAfterInitModule
    {
        [SerializeField] private float moveSpeed = 8f, gravity = -9.8f;
        [SerializeField] private AssetNameSO footStepEffect;
        
        private Vector3 _velocity;
        private float _verticalVelocity;
        private Vector3 _movementDirection;
        private CharacterController _characterController;
        private ModuleOwner _owner;
        private VfxModule _vfxModule;
        private Vector3 _autoVelocity;

        public bool CanManualMove { get; set; } = true;
        public Vector3 Velocity => _velocity;
        public bool IsGround => _characterController.isGrounded;

        public void Init(ModuleOwner owner)
        {
            _owner = owner;
            _characterController = owner.GetComponent<CharacterController>();
            _vfxModule = owner.GetModule<VfxModule>();
        }
        
        public void AfterInit()
        {
            _vfxModule?.StopVfx(footStepEffect.AssetHash);
        }

        public void SetMovementVelocity(Vector3 velocity)
        {
            _autoVelocity = velocity;
        }

        public void SetMovementDirection(Vector2 movementInput)
        {
            Vector3 newMovement = new Vector3(movementInput.x, 0, movementInput.y).normalized;
            if (_vfxModule != null)
            {
                if(newMovement.magnitude > 0.01f && _movementDirection.magnitude <= 0.01f)
                    _vfxModule.PlayVfx(footStepEffect.AssetHash);
                else if(newMovement.magnitude <= 0.01f && _movementDirection.magnitude > 0.01f)
                    _vfxModule.StopVfx(footStepEffect.AssetHash);
            }
            _movementDirection = newMovement;
        }

        public void RotateTo(Vector3 direction)
        {
            if (direction.magnitude < Mathf.Epsilon) return;
            direction.y = 0;
            _owner.transform.forward = direction.normalized;
        }

        private void FixedUpdate()
        {
            CalculateMovement();
            ApplyGravity();
            Move();
        }

        private void CalculateMovement()
        {
            if(CanManualMove)
                _velocity = Quaternion.Euler(0, -45f, 0) * _movementDirection;
            else 
                _velocity = _autoVelocity;
            
            _velocity *= moveSpeed * Time.fixedDeltaTime;
            if (_velocity.sqrMagnitude > 0)
            {
                float rotationSpeed = 8f;
                Quaternion targetRotation = Quaternion.LookRotation(_velocity);
                _owner.transform.rotation = Quaternion.Lerp(_owner.transform.rotation,
                                targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void ApplyGravity()
        {
            if (IsGround && _verticalVelocity < 0)
                _verticalVelocity = -0.03f;
            else
                _verticalVelocity += gravity * Time.fixedDeltaTime;
            
            _velocity.y = _verticalVelocity;
        }

        private void Move()
        {
            _characterController.Move(_velocity);
        }
    }
}