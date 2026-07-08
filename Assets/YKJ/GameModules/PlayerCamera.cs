using IYC._01.Scripts.CoreSystem.Module;
using Player;
using UnityEngine;

public class PlayerCamera : MonoBehaviour, IModule
{
    [SerializeField] private Transform cameraTarget;

    private float _yaw;
    private float _pitch;

    [SerializeField] private float topClamp = 70f;
    [SerializeField] private float bottomClamp = -30f;
    [SerializeField] private float sensitivity = 2.0f;
    private PlayerController _player;

    private float _cinemachineYaw;
    private float _cinemachinePitch;
    public void Init(ModuleOwner owner)
    {
        _player = owner as PlayerController;
        _cinemachineYaw = cameraTarget.eulerAngles.y;
    }

    private void Start()
    {
        _yaw = cameraTarget.eulerAngles.y;
    }

    private void Update()
    {

        RotateCamera();
        Debug.DrawRay(transform.position, Camera.main.transform.forward * 5, Color.blue);
    }

    private void RotateCamera()
    {


        if (_player.PlayerInput.LookDir.sqrMagnitude > 0.01f)
        {
            _cinemachineYaw += _player.PlayerInput.LookDir.x * sensitivity * Time.deltaTime;
            _cinemachinePitch -= _player.PlayerInput.LookDir.y * sensitivity * Time.deltaTime;
        }

        _cinemachinePitch = Mathf.Clamp(_cinemachinePitch, bottomClamp, topClamp);

        cameraTarget.rotation = Quaternion.Euler(_cinemachinePitch, _cinemachineYaw, 0f);
    }
}
