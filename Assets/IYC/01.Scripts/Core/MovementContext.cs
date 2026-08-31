using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Config;
using CWH.Player.Input;

namespace CWH.Player.Core
{
    public sealed class MovementContext
    {
        public readonly GroundMovementConfig Ground;
        public readonly JumpConfig Jump;
        public readonly WallRunConfig WallRun;
        public readonly SlideConfig Slide;
        public readonly ICharacterMotor Motor;
        public readonly IWallDetector WallDetector;
        public readonly Transform Transform;

        public MovementInputSample Input;
        public Vector3 Velocity;
        public float DeltaTime;
        public float AirborneTime;
        public float JumpBufferTimer;
        public float SprintLockoutTimer;
        public float WallRunCooldownTimer;
        public WallSide CurrentWallSide;
        public System.Action<MovementEventType> RaiseEvent;

        public MovementContext(
            GroundMovementConfig ground,
            JumpConfig jump,
            WallRunConfig wallRun,
            SlideConfig slide,
            ICharacterMotor motor,
            IWallDetector wallDetector,
            Transform transform)
        {
            Ground = ground;
            Jump = jump;
            WallRun = wallRun;
            Slide = slide;
            Motor = motor;
            WallDetector = wallDetector;
            Transform = transform;
        }
    }
}
