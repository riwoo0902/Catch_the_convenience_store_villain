using UnityEngine;

namespace CWH.Player.Core
{
    // 에디터 디버그 시각화 전용. 런타임 물리/이동 로직(CharacterControllerMotor)과
    // 분리해서, 디버그용 코드가 실제 동작 코드와 섞이지 않게 한다.
    [RequireComponent(typeof(CharacterControllerMotor))]
    public sealed class CharacterControllerMotorGizmos : MonoBehaviour
    {
        private CharacterControllerMotor _motor;

        private void OnDrawGizmosSelected()
        {
            if (_motor == null)
            {
                _motor = GetComponent<CharacterControllerMotor>();
            }

            Gizmos.color = _motor.IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * _motor.GroundCheckDistance);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * _motor.GroundCheckDistance, 0.05f);
        }
    }
}
