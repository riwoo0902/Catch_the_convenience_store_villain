using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

namespace Branches.CWH.Scripts.Player.Test
{
    public class PlayerPositionResetTrigger : MonoBehaviour
    {
        [SerializeField] private Vector3 _resetPosition;

        private void OnTriggerEnter(Collider col)
        {
            CharacterController controller = col.GetComponentInParent<CharacterController>();
            if (controller != null)
            {
                if (controller.TryGetComponent<global::CWH.Player.PlayerFallRecovery>(out var recovery))
                {
                    recovery.RaiseToHeight(_resetPosition.y);
                    return;
                }
                Vector3 position = controller.transform.position;
                position.y = Mathf.Max(position.y, _resetPosition.y);
                bool wasEnabled = controller.enabled;
                controller.enabled = false;
                controller.transform.position = position;
                controller.enabled = wasEnabled;
            }
            else if (col != null)
            {
                Transform target = col.attachedRigidbody != null ? col.attachedRigidbody.transform : col.transform;
                Vector3 position = target.position;
                position.y = Mathf.Max(position.y, _resetPosition.y);
                target.position = position;
                if (col.attachedRigidbody != null)
                {
                    Vector3 velocity = col.attachedRigidbody.linearVelocity;
                    velocity.y = 0f;
                    col.attachedRigidbody.linearVelocity = velocity;
                }
            }
        }

    }
}
