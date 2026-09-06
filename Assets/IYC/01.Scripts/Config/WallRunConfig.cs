using UnityEngine;

namespace CWH.Player.Config
{
    [CreateAssetMenu(menuName = "CWH/Player/Wall Run Config", fileName = "WallRunConfig")]
    public sealed class WallRunConfig : AbstractConfigSO
    {
        public float DetectionDistance = 0.7f;              //  좌우 감지 범위
        public float MinHeightAboveGround = 1.0f;           //  월 런 인식 최소 높이
        public float MinSpeedToStart = 10f;                 //  월 런 인식 최소 속도
        public float RunSpeed = 30f;                        //  월 런 시 시작하는 속도
        public float GravityMultiplier = 0.2f;              //  중력
        public float MaxDuration = 5f;                      //  최대 유지 시간
        public float StickForce = 8f;                       //  벽에 붙게 도와주는 녀석
        public float ExitUpwardBoost = 2f;                  //  속도 부족으로 종료 시 달리기 잠구는 시간
        public float JumpOutwardForce = 3f;                 //  월 런 종료 시 옆으로 튀어 오르는 정도
        public float JumpUpwardForce = 7.5f;                //  월 런 종료 시 위로 튀어 오르는 정도
        public float SpeedDecayPerSecond = 3f;              //  월 런 시 초당 감속도
        public float JumpSpeedBoost = 10f;                  //  월 런 종료 시 얻는 가속도
        public float ExhaustionSprintLockoutDuration = 5f;  //  월 런 종료 시 튀어 오르는 정도 (구현 안됨)
        public float ReattachCooldown = 1f;                 //  최대 지속시간으로 종료됐을 때, 같은 벽에 다시 붙기까지 걸리는 시간
    }
}