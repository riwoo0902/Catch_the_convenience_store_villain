using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Branches.CWH.Scripts.Player.Test
{
    public class PlayerPositionResetKeyTrigger : MonoBehaviour
    {
        private CharacterController _characterController;

        private void Awake()
        {
            _characterController = GetComponentInParent<CharacterController>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.leftAltKey.wasPressedThisFrame)
            {
                if (_characterController != null)
                {
                    if (_characterController.TryGetComponent<global::CWH.Player.PlayerFallRecovery>(out var recovery))
                    {
                        recovery.RaiseToHeight(1.5f);
                        return;
                    }
                    Vector3 position = _characterController.transform.position;
                    position.y = Mathf.Max(position.y, 1.5f);
                    _characterController.enabled = false;
                    _characterController.transform.position = position;
                    _characterController.enabled = true;
                }
            }
        }
    }
}
