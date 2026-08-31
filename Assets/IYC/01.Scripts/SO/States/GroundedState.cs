using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Core;

namespace CWH.Player.States
{
    public sealed class GroundedState : IMovementState
    {
        private readonly MovementStateLibrary _library;

        public GroundedState(MovementStateLibrary library)
        {
            _library = library;
        }

        public MovementStateId Id => MovementStateId.Grounded;

        public void Enter(MovementContext context)
        {
            if (context.Motor.CanStandUp())
            {
                context.Motor.ResetHeight();
            }

            context.Velocity.y = -2f;
        }

        public void Tick(MovementContext context)
        {
            var ground = context.Ground;
            var moveInput = context.Input.MoveAxis;

            var isCrouching = context.Input.CrouchHeld || !context.Motor.CanStandUp();
            if (isCrouching)
            {
                context.Motor.SetHeightMultiplier(ground.CrouchHeightMultiplier);
            }
            else
            {
                context.Motor.ResetHeight();
            }

            var sprintAllowed = context.Input.SprintHeld && context.SprintLockoutTimer <= 0f;
            var targetSpeed = sprintAllowed ? ground.SprintSpeed : ground.WalkSpeed;
            if (isCrouching)
            {
                targetSpeed *= ground.CrouchSpeedMultiplier;
            }

            var wishDir = context.Transform.right * moveInput.x + context.Transform.forward * moveInput.y;
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);
            var targetVelocity = wishDir * targetSpeed;

            var horizontalVelocity = new Vector3(context.Velocity.x, 0f, context.Velocity.z);
            var rate = targetVelocity.magnitude > horizontalVelocity.magnitude ? ground.Acceleration : ground.Deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * context.DeltaTime);

            context.Velocity = new Vector3(horizontalVelocity.x, -2f, horizontalVelocity.z);

            context.Motor.Move(context.Velocity * context.DeltaTime);
        }

        public IMovementState CheckTransitions(MovementContext context)
        {
            if (context.JumpBufferTimer > 0f && context.Motor.CanStandUp())
            {
                context.Velocity.y = Mathf.Sqrt(2f * -context.Jump.Gravity * context.Jump.JumpHeight);
                context.JumpBufferTimer = 0f;
                context.RaiseEvent?.Invoke(MovementEventType.Jumped);
                return _library.Airborne;
            }

            if (!context.Motor.IsGrounded)
            {
                return _library.Airborne;
            }

            var horizontalSpeed = new Vector2(context.Velocity.x, context.Velocity.z).magnitude;
            if (context.Input.SlidePressed && horizontalSpeed >= context.Slide.TriggerMinSpeed)
            {
                return _library.Sliding;
            }

            return this;
        }

        public void Exit(MovementContext context)
        {
        }
    }
}
