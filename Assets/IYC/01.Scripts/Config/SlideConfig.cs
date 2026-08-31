using UnityEngine;

namespace CWH.Player.Config
{
    [CreateAssetMenu(menuName = "CWH/Player/Slide Config", fileName = "SlideConfig")]
    public sealed class SlideConfig : AbstractConfigSO
    {
        public float TriggerMinSpeed = 1f;              // 슬라이드 감지 최소 속도
        public float HoldDuration = 1f;                 // 속도 유지 시간
        public float MinSlideSpeed = 8f;                // 슬라이드 시 적용되는 최소 속도
        public float InitialImpulse = 9f;               // 슬라이드 순간 가속도
        public float Friction = 50f;                    // 마찰력 정도
        public float ColliderHeightMultiplier = 0.5f;   // 슬라이드 시 플레이어의 높이
    }
}
