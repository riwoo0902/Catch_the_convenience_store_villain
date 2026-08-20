using IYC._01.Scripts.CoreSystem.Module;
using Player;
using System;
using UnityEngine;

namespace YKJ.Player
{
    public class PlayerMovement : MonoBehaviour, IModule, IControlMovement
    {
        [Header("Move")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float rotationSmoothTime = 0.12f;

        [Header("External Force")]
        [SerializeField] private float forceDamping = 12f;
        [SerializeField] private float forceStopThreshold = 0.05f;

        [Header("Camera")]
        [SerializeField] private Transform cameraTarget;

        private CharacterController _controller;
        private PlayerController _owner;

        private float _rotationVelocity;
        private float _targetRotation;
        private float _verticalVelocity;

        private Vector3 _velocity;
        private Vector3 _moveInput;
        private Vector3 _externalVelocity;

        public bool CanManualMove { get; set; } = true;

        public bool IsGround =>
            _controller != null && _controller.isGrounded;

        public Action<Vector3> OnVelocityChange { get; set; }

        public void Init(ModuleOwner owner)
        {
            _owner = owner as PlayerController;
            _controller = owner.GetComponent<CharacterController>();

            if (_owner == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerMovement)} owner가 PlayerController가 아닙니다.",
                    this
                );
            }

            if (_controller == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterController)}를 찾을 수 없습니다.",
                    this
                );
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_controller == null || _owner == null)
                return;

            ApplyGravity();
            UpdateExternalVelocity();
            Move();
        }

        public void SetMovementDirection(Vector2 movementInput)
        {
            _moveInput = new Vector3(
                movementInput.x,
                0f,
                movementInput.y
            ).normalized;
        }

        private void Move()
        {
            Vector3 manualVelocity = CalculateManualVelocity();

            _velocity =
                manualVelocity
                + _externalVelocity
                + Vector3.up * _verticalVelocity;

            _controller.Move(_velocity * Time.deltaTime);
            OnVelocityChange?.Invoke(_velocity);
        }

        private Vector3 CalculateManualVelocity()
        {
            if (!CanManualMove)
                return Vector3.zero;

            if (_moveInput.sqrMagnitude <= 0.001f)
                return Vector3.zero;

            float cameraRotationY = cameraTarget != null
                ? cameraTarget.eulerAngles.y
                : 0f;

            _targetRotation =
                Mathf.Atan2(_moveInput.x, _moveInput.z)
                * Mathf.Rad2Deg
                + cameraRotationY;

            float rotation = Mathf.SmoothDampAngle(
                _owner.transform.eulerAngles.y,
                _targetRotation,
                ref _rotationVelocity,
                rotationSmoothTime
            );

            _owner.transform.rotation =
                Quaternion.Euler(0f, rotation, 0f);

            Vector3 moveDirection =
                Quaternion.Euler(0f, _targetRotation, 0f)
                * Vector3.forward;

            return moveDirection.normalized * moveSpeed;
        }

        private void ApplyGravity()
        {
            if (IsGround)
            {
                /*
                 * 0으로 만들면 CharacterController가 바닥에서
                 * 살짝 떨어진 것으로 판단할 수 있으므로
                 * 작은 아래 방향 속도를 유지합니다.
                 */
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                /*
                 * 착지 후에도 아래 방향 외부 속도가 남아 있으면
                 * 바닥 충돌과 반복적으로 충돌할 수 있으므로 제거합니다.
                 */
                if (_externalVelocity.y < 0f)
                    _externalVelocity.y = 0f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void UpdateExternalVelocity()
        {
            /*
             * 수평 외부 속도만 감속시킵니다.
             * 수직 속도는 중력 로직이 담당합니다.
             */
            Vector3 horizontalVelocity = new Vector3(
                _externalVelocity.x,
                0f,
                _externalVelocity.z
            );

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                forceDamping * Time.deltaTime
            );

            _externalVelocity.x = horizontalVelocity.x;
            _externalVelocity.z = horizontalVelocity.z;

            if (horizontalVelocity.sqrMagnitude
                <= forceStopThreshold * forceStopThreshold)
            {
                _externalVelocity.x = 0f;
                _externalVelocity.z = 0f;
            }
        }

        public void SetMovementVelocity(Vector3 velocity)
        {
            _externalVelocity.x = velocity.x;
            _externalVelocity.z = velocity.z;

            if (velocity.y > 0f)
                _verticalVelocity = velocity.y;
        }

        public void RotateTo(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            _owner.transform.rotation =
                Quaternion.LookRotation(direction.normalized);
        }

        public void AddForceToAgent(Vector3 force)
        {
            /*
             * CharacterController는 Rigidbody가 아니므로
             * 실제 Force가 아니라 속도를 더하는 방식입니다.
             */

            _externalVelocity.x += force.x;
            _externalVelocity.z += force.z;

            /*
             * 위쪽 힘은 점프 또는 띄우기 효과로 처리합니다.
             * 아래 방향 힘은 중력과 충돌할 수 있으므로 직접 누적하지 않습니다.
             */
            if (force.y > 0f)
                _verticalVelocity = force.y;
        }
    }
}