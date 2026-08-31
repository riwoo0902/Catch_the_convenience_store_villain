using UnityEngine;

namespace CWH.Player.Config
{
    [CreateAssetMenu(menuName = "CWH/Player/Camera Config", fileName = "PlayerCameraConfig")]
    public sealed class PlayerCameraConfig : AbstractConfigSO
    {
        [Range(0.001f, 10f)] public float Sensitivity = 0.15f;      // 마우스 감도
        public float MinPitch = -90f;                               // 카메라 최소 범위 
        public float MaxPitch = 90f;                                // 카메라 최대 범위
        public float CrouchTransitionSpeed = 8f;                    // 앉기 시 카메라 위치 변환 속도
        public float WallTiltTransitionSpeed = 180f;                // 벽런 카메라 틸트(Roll) 초당 전환 속도(도)

        [Range(30f, 120f)] public float BaseFov = 60f;              // 기본 시야각
        [Range(30f, 120f)] public float MaxFov = 90f;               // 속도에 따른 최대 시야각
        public float FovMinSpeed = 5f;                              // 기본 시야각일 때 속도
        public float FovMaxSpeed = 20f;                             // 최대 시야각일 때 속도
        public float FovTransitionSpeed = 8f;                       // 속도에 따른 시야각 변환 속도
    }
}
