using UnityEngine;

namespace CWH.Player.Config
{
    [CreateAssetMenu(menuName = "CWH/Player/Ground Movement Config", fileName = "GroundMovementConfig")]
    public sealed class GroundMovementConfig : AbstractConfigSO
    {
        public float WalkSpeed = 5f;                    // 걸을 때 속도 
        public float SprintSpeed = 15f;                 // 뛸 때 속도
        public float Acceleration = 40f;                // 초당 가속도
        public float Deceleration = 50f;                // 초당 감속도
        public float CrouchSpeedMultiplier = 0.5f;      // 앉았을 때 속도
        public float CrouchHeightMultiplier = 0.6f;     // 앉았을 때 높이
        public float MaxSpeed = 50f;                    // 전체 이동 속도 상한선 (여러 가속 효과가 겹쳐도 이 값을 못 넘음)
    }
}
