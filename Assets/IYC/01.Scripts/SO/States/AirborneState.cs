using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Core;

namespace CWH.Player.States
{
    public sealed class AirborneState : IMovementState
    {
        private readonly MovementStateLibrary _library;

        public AirborneState(MovementStateLibrary library)
        {
            _library = library;
        }

        public MovementStateId Id => MovementStateId.Airborne;

        public void Enter(MovementContext context)
        {
        }

        public void Tick(MovementContext context)
        {
            var jump = context.Jump;
            var ground = context.Ground;

            context.Velocity.y = Mathf.Max(context.Velocity.y + jump.Gravity * context.DeltaTime, jump.MaxFallSpeed);

            var wishDir = context.Transform.right * context.Input.MoveAxis.x + context.Transform.forward * context.Input.MoveAxis.y;
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);
            var sprintAllowed = context.Input.SprintHeld && context.SprintLockoutTimer <= 0f;
            var targetSpeed = sprintAllowed ? ground.SprintSpeed : ground.WalkSpeed;
            var targetVelocity = wishDir * targetSpeed;

            context.Velocity = new Vector3(targetVelocity.x, context.Velocity.y, targetVelocity.z);

            context.Motor.Move(context.Velocity * context.DeltaTime);
        }

        public IMovementState CheckTransitions(MovementContext context)
        {
            if (context.Motor.IsGrounded)
            {
                context.RaiseEvent?.Invoke(MovementEventType.Landed);
                return _library.Grounded;
            }

            if (context.JumpBufferTimer > 0f && context.AirborneTime <= context.Jump.CoyoteTime)
            {
                context.Velocity.y = Mathf.Sqrt(2f * -context.Jump.Gravity * context.Jump.JumpHeight);
                context.JumpBufferTimer = 0f;
                context.RaiseEvent?.Invoke(MovementEventType.Jumped);
                return this;
            }

            var wallRun = context.WallRun;
            var horizontalSpeed = new Vector2(context.Velocity.x, context.Velocity.z).magnitude;
            if (context.Velocity.y <= 0f
                && horizontalSpeed >= wallRun.MinSpeedToStart
                && context.WallRunCooldownTimer <= 0f
                && context.Motor.HasClearanceAboveGround(wallRun.MinHeightAboveGround)
                && context.WallDetector.TryDetectWall(context.Transform, out _))
            {
                return _library.WallRunning;
            }

            return this;
        }

        public void Exit(MovementContext context)
        {
        }
    }
}
