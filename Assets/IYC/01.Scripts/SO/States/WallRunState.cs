using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Core;

namespace CWH.Player.States
{
    public sealed class WallRunState : IMovementState
    {
        private readonly MovementStateLibrary _library;
        private float _elapsed;
        private float _currentSpeed; // 벽런 이동 자체에 쓰는 속도 (RunSpeed 하한선 적용됨)
        private float _actualSpeed;  // 하한선 없이 실제로 감쇠하는 속도 (벽점프 가산 기준)
        private WallHit _wallHit;
        private bool _hasWall;

        public WallRunState(MovementStateLibrary library)
        {
            _library = library;
        }

        public MovementStateId Id => MovementStateId.WallRunning;

        public void Enter(MovementContext context)
        {
            _elapsed = 0f;
            _hasWall = context.WallDetector.TryDetectWall(context.Transform, out _wallHit);
            context.CurrentWallSide = _wallHit.Side;

            var currentHorizontalSpeed = new Vector2(context.Velocity.x, context.Velocity.z).magnitude;
            _actualSpeed = currentHorizontalSpeed;
            _currentSpeed = Mathf.Max(currentHorizontalSpeed, context.WallRun.RunSpeed);

            context.RaiseEvent?.Invoke(MovementEventType.WallRunStarted);
        }

        public void Tick(MovementContext context)
        {
            var wallRun = context.WallRun;
            _elapsed += context.DeltaTime;

            _hasWall = context.WallDetector.TryDetectWall(context.Transform, out _wallHit);
            context.CurrentWallSide = _wallHit.Side;

            if (!_hasWall)
            {
                return;
            }

            var wallForward = Vector3.Cross(_wallHit.Normal, Vector3.up);
            if (Vector3.Dot(wallForward, context.Transform.forward) < 0f)
            {
                wallForward = -wallForward;
            }
            
            _currentSpeed = Decay(_currentSpeed, wallRun.SpeedDecayPerSecond, context.DeltaTime);
            _actualSpeed = Decay(_actualSpeed, wallRun.SpeedDecayPerSecond, context.DeltaTime);
            var horizontalVelocity = wallForward * _currentSpeed;

            var verticalVelocity = context.Velocity.y + wallRun.GravityMultiplier * context.Jump.Gravity * context.DeltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, 0f);

            context.Velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

            var stickForce = -_wallHit.Normal * (wallRun.StickForce * context.DeltaTime);
            context.Motor.Move(context.Velocity * context.DeltaTime + stickForce);
        }

        public IMovementState CheckTransitions(MovementContext context)
        {
            var wallRun = context.WallRun;

            if (context.Motor.IsGrounded)
            {
                return _library.Grounded;
            }

            if (!_hasWall)
            {
                return _library.Airborne;
            }

            if (context.JumpBufferTimer > 0f)
            {
                var boostedSpeed = _actualSpeed + wallRun.JumpSpeedBoost;
                var direction = context.Transform.forward;
                var boostedVelocity = direction * boostedSpeed;
                context.Velocity = WallJumpResolver.Resolve(_wallHit.Normal, boostedVelocity, wallRun);
                context.JumpBufferTimer = 0f;
                context.RaiseEvent?.Invoke(MovementEventType.Jumped);
                return _library.Airborne;
            }

            if (_currentSpeed < wallRun.MinSpeedToStart)
            {
                context.SprintLockoutTimer = wallRun.ExhaustionSprintLockoutDuration;
                Debug.Log($"[WallRun] exhausted, currentSpeed={_currentSpeed:F2} < {wallRun.MinSpeedToStart}, setting lockout={wallRun.ExhaustionSprintLockoutDuration}");
                return _library.Airborne;
            }

            if (_elapsed >= wallRun.MaxDuration)
            {
                // 시간 초과로 끝난 경우만 재부착 쿨다운을 건다 (그냥 벽이 끝나서 나가는 건 예외 - 다른 벽으로 체이닝 가능해야 함)
                context.WallRunCooldownTimer = wallRun.ReattachCooldown;
                return _library.Airborne;
            }

            return this;
        }

        public void Exit(MovementContext context)
        {
            context.CurrentWallSide = WallSide.None;
            context.RaiseEvent?.Invoke(MovementEventType.WallRunEnded);
        }

        // 초당 감쇠율만큼 속도를 줄이되 0 밑으로는 안 내려가게 (_currentSpeed, _actualSpeed 공통 사용)
        private static float Decay(float speed, float decayPerSecond, float deltaTime)
        {
            return Mathf.Max(speed - decayPerSecond * deltaTime, 0f);
        }
    }
}
