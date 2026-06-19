using System;
using UnityEngine;
using Controls;
using UnityEngine.InputSystem;

namespace Player
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "SO/PlayerInput", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.Controls.IPlayerActions
    {
        public event Action<Vector2> OnMovementChange;
        public event Action OnAttackKeyPressed;
        public event Action OnJumpKeyPressed;
        
        [SerializeField] private LayerMask whatIsGround;
        private Controls.Controls _controls;

        private Vector3 _worldMousePosition;
        private Vector2 _screenMousePosition;

        private Camera _mainCam;

        public Camera MainCam
        {
            get
            {
                if(_mainCam == null)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls.Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();
            OnMovementChange?.Invoke(movement);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnAttackKeyPressed?.Invoke();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnJumpKeyPressed?.Invoke();
        }

        public void OnPointer(InputAction.CallbackContext context)
        {
            _screenMousePosition = context.ReadValue<Vector2>();
        }

        public Vector3 GetWorldMousePosition()
        {
            if(MainCam == null)
                return _worldMousePosition;
            
            Ray camRay = MainCam.ScreenPointToRay(_screenMousePosition);
            if (Physics.Raycast(camRay, out RaycastHit hit, MainCam.farClipPlane, whatIsGround))
            {
                _worldMousePosition = hit.point;
            }
            return _worldMousePosition;
        }
    }
}