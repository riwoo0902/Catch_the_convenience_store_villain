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
            if (Keyboard.current.leftAltKey.wasPressedThisFrame)
            {
                if (_characterController != null)
                {
                    _characterController.enabled = false;
                    _characterController.gameObject.transform.position = new Vector3(0f, 1.5f, 0f);
                    _characterController.enabled = true;
                }
            }
        }
    }
}