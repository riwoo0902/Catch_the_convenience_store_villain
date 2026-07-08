using IYC._01.Scripts.CoreSystem.Module;
using Player;
using UnityEngine;

namespace YKJ.Player
{
    public class PlayerMovement : MonoBehaviour, IModule, IControlMovement
    {
        [Header("Move")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float rotationSmoothTime = 0.12f;

        [Header("Camera")]
        [SerializeField] private Transform cameraTarget;

        private CharacterController _controller;
        private PlayerController _owner;

        private float _rotationVelocity;
        private float _targetRotation;
        private float _verticalVelocity;


        private Vector3 _velocity;
        private Vector3 _moveInput;

        public bool CanManualMove { get; set; } = true;
        public bool IsGround => _controller.isGrounded;

        public void Init(ModuleOwner owner)
        {
            _owner = owner as PlayerController;
            _controller = owner.GetComponent<CharacterController>();


        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void SetMovementDirection(Vector2 movementInput)
        {
            _moveInput = new Vector3(movementInput.x, 0.0f, movementInput.y).normalized;
        }


        private void Update()
        {
            Move();
            ApplyGravity();
        }
        private void Move()
        {

            Vector3 moveDir = Vector3.zero;

            if (_moveInput != Vector3.zero)
            {
                _targetRotation = Mathf.Atan2(_moveInput.x, _moveInput.z) * Mathf.Rad2Deg + cameraTarget.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    _owner.transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    rotationSmoothTime
                );

                _owner.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                moveDir = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            }


            _velocity = moveDir.normalized * moveSpeed + Vector3.up * _verticalVelocity;

            _controller.Move(_velocity * Time.deltaTime);
        }
        private void ApplyGravity()
        {
            if (IsGround && _verticalVelocity < 0)
                _verticalVelocity = -0.03f;
            else
                _verticalVelocity += gravity * Time.fixedDeltaTime;

            _velocity.y = _verticalVelocity;
        }

        public void SetMovementVelocity(Vector3 velocity)
        {

        }

        public void RotateTo(Vector3 direction)
        {

        }
    }
}