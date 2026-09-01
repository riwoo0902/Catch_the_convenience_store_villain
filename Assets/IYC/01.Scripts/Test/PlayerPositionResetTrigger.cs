using UnityEngine;
namespace Branches.CWH.Scripts.Player.Test
{
    public class PlayerPositionResetTrigger : MonoBehaviour
    {
        [SerializeField] private Vector3 _resetPosition;

        private void OnTriggerEnter(Collider col)
        {
            CharacterController controller = col.GetComponent<CharacterController>();
            Debug.Assert(controller != null, $"부딧힌 오브젝트의 CharacterController가 존재하지 않습니다.");
            if (controller != null)
            {
                controller.enabled = false;
                col.transform.position = _resetPosition;
                controller.enabled = true;
            }
            else if (col != null)
            {
                col.transform.position = _resetPosition;
            }
        }

    }
}