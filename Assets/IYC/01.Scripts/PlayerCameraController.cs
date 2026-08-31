using CWH.Player.Config;
using CWH.Player.Core;
using CWH.Player.Input;
using UnityEngine;

namespace Branches.CWH.Scripts.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerMovementController))]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private PlayerCameraConfig _config;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _wallRunTiltAngle = 10f;

        private IMovementInputReader _inputReader;
        private PlayerMovementController _movement;
        private Camera _camera;
        private float _yaw;
        private float _pitch;
        private float _currentRoll;
        private float _baseCameraLocalY;
        private bool _hasSkippedFirstLookFrame;

        private void Awake()
        {
            _inputReader = GetComponent<IMovementInputReader>();
            _movement = GetComponent<PlayerMovementController>();

            if (_cameraTransform == null)
            {
                _cameraTransform = GetComponentInChildren<Camera>()?.transform;
            }

            _camera = _cameraTransform.GetComponent<Camera>();
            _yaw = transform.eulerAngles.y;
            _baseCameraLocalY = _cameraTransform.localPosition.y;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // 전에 시작 시 마우스 이동 값을 없애 정면을 바라보게 만들었지만 의도대로 작동하지 않아 삭제
            var rawLook = _inputReader.ReadLook();
            var look = rawLook * _config.Sensitivity;

            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, _config.MinPitch, _config.MaxPitch);

            var snapshot = _movement.StateSource.GetSnapshot();
            var targetRoll = snapshot.WallSide switch
            {
                WallSide.Left => -_wallRunTiltAngle,
                WallSide.Right => _wallRunTiltAngle,
                _ => 0f
            };
            _currentRoll = Mathf.MoveTowards(_currentRoll, targetRoll, _config.WallTiltTransitionSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, _currentRoll);
            
            var targetLocalY = _baseCameraLocalY - snapshot.HeightReduction;
            var localPosition = _cameraTransform.localPosition;
            localPosition.y = Mathf.MoveTowards(localPosition.y, targetLocalY, _config.CrouchTransitionSpeed * Time.deltaTime);
            _cameraTransform.localPosition = localPosition;

            var fovT = Mathf.InverseLerp(_config.FovMinSpeed, _config.FovMaxSpeed, snapshot.Speed);
            var targetFov = Mathf.Lerp(_config.BaseFov, _config.MaxFov, fovT);
            _camera.fieldOfView = Mathf.MoveTowards(_camera.fieldOfView, targetFov, _config.FovTransitionSpeed * Time.deltaTime);
        }
    }
}
