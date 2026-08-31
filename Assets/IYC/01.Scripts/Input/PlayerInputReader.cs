using UnityEngine;
using UnityEngine.InputSystem;

namespace CWH.Player.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour, IMovementInputReader
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _actionMapName = "Player";

        private InputActionMap _map;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _slideAction;

        private bool _jumpPressedThisFrame;
        private bool _crouchPressedThisFrame;
        private bool _slidePressedThisFrame;

        private void Awake()
        {
            _map = _actions.FindActionMap(_actionMapName, throwIfNotFound: true);
            _moveAction = _map.FindAction("Move", throwIfNotFound: true);
            _lookAction = _map.FindAction("Look", throwIfNotFound: true);
            _jumpAction = _map.FindAction("Jump", throwIfNotFound: true);
            _sprintAction = _map.FindAction("Sprint", throwIfNotFound: true);
            _crouchAction = _map.FindAction("Crouch", throwIfNotFound: true);
            _slideAction = _map.FindAction("Sliding", throwIfNotFound: true);

            _jumpAction.performed += OnJumpPerformed;
            _crouchAction.performed += OnCrouchPerformed;
            _slideAction.performed += OnSlidePerformed;
        }

        private void OnDestroy()
        {
            _jumpAction.performed -= OnJumpPerformed;
            _crouchAction.performed -= OnCrouchPerformed;
            _slideAction.performed -= OnSlidePerformed;
        }

        private void OnEnable()
        {
            _map?.Enable();
        }

        private void OnDisable()
        {
            _map?.Disable();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context) => _jumpPressedThisFrame = true;

        private void OnCrouchPerformed(InputAction.CallbackContext context) => _crouchPressedThisFrame = true;

        private void OnSlidePerformed(InputAction.CallbackContext context) => _slidePressedThisFrame = true;

        public MovementInputSample Sample()
        {
            var sample = new MovementInputSample(
                moveAxis: _moveAction.ReadValue<Vector2>(),
                lookDelta: _lookAction.ReadValue<Vector2>(),
                jumpPressed: _jumpPressedThisFrame,
                sprintHeld: _sprintAction.IsPressed(),
                crouchPressed: _crouchPressedThisFrame,
                crouchHeld: _crouchAction.IsPressed(),
                slidePressed: _slidePressedThisFrame);

            _jumpPressedThisFrame = false;
            _crouchPressedThisFrame = false;
            _slidePressedThisFrame = false;

            return sample;
        }

        public Vector2 ReadLook() => _lookAction.ReadValue<Vector2>();
    }
}
