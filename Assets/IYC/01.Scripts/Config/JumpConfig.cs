using UnityEngine;

namespace CWH.Player.Config
{
    [CreateAssetMenu(menuName = "CWH/Player/Jump Config", fileName = "JumpConfig")]
    public sealed class JumpConfig : AbstractConfigSO
    {
        public float JumpHeight = 3f;               // 점프 높이
        public float Gravity = -25f;                // 중력의 정도
        public float MaxFallSpeed = -40f;           // 최대 낙하 속도
        public float AirControlMultiplier = 0.5f;   // 공중 방향전환 가속도 배율
        public float AirDeceleration = 5f;          // 공중 감속도
        public float CoyoteTime = 0.12f;            // 점프 유예 시간 (이유 : 조작감의 불편함을 줄이기 위해)
        public float JumpBufferTime = 0.12f;        // 착지 직전 입력을 착지 순간까지 저장해두는 시간
    }
}
