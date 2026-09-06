using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Core;

namespace CWH.Player.States
{
    public sealed class SlideState : IMovementState
    {
        private readonly MovementStateLibrary _library;
        private float _elapsed;
        private Vector3 _slideDirection;

        public SlideState(MovementStateLibrary library)
        {
            _library = library;
        }

        public MovementStateId Id => MovementStateId.Sliding;

        public void Enter(MovementContext context)
        {
            _elapsed = 0f;

            var horizontalVelocity = new Vector3(context.Velocity.x, 0f, context.Velocity.z);

            var inputDir = context.Transform.right * context.Input.MoveAxis.x + context.Transform.forward * context.Input.MoveAxis.y;
            if (inputDir.sqrMagnitude > 0.0001f)
            {
                _slideDirection = inputDir.normalized;
            }
            else if (horizontalVelocity.sqrMagnitude > 0.0001f)
            {
                _slideDirection = horizontalVelocity.normalized;
            }
            else
            {
                _slideDirection = context.Transform.forward;
            }

            context.Motor.SetHeightMultiplier(context.Slide.ColliderHeightMultiplier);

            var boostedSpeed = Mathf.Max(
                horizontalVelocity.magnitude + context.Slide.InitialImpulse,
                context.Slide.MinSlideSpeed);
            context.Velocity = new Vector3(_slideDirection.x * boostedSpeed, -2f, _slideDirection.z * boostedSpeed);

            context.RaiseEvent?.Invoke(MovementEventType.SlideStarted);
        }

        public void Tick(MovementContext context)
        {
            var slide = context.Slide;
            _elapsed += context.DeltaTime;

            var horizontalSpeed = new Vector2(context.Velocity.x, context.Velocity.z).magnitude;
            if (_elapsed > slide.HoldDuration)
            {
                horizontalSpeed = Mathf.Max(0f, horizontalSpeed - slide.Friction * context.DeltaTime);
            }

            context.Velocity = new Vector3(_slideDirection.x * horizontalSpeed, -2f, _slideDirection.z * horizontalSpeed);
            context.Motor.Move(context.Velocity * context.DeltaTime);
        }

        public IMovementState CheckTransitions(MovementContext context)
        {
            if (!context.Motor.IsGrounded)
            {
                return _library.Airborne;
            }

            if (context.JumpBufferTimer > 0f && context.Motor.CanStandUp())
            {
                context.Velocity.y = Mathf.Sqrt(2f * -context.Jump.Gravity * context.Jump.JumpHeight);
                context.JumpBufferTimer = 0f;
                context.RaiseEvent?.Invoke(MovementEventType.Jumped);
                return _library.Airborne;
            }

            var horizontalSpeed = new Vector2(context.Velocity.x, context.Velocity.z).magnitude;
            if (horizontalSpeed <= 0f)
            {
                return _library.Grounded;
            }

            return this;
        }

        public void Exit(MovementContext context)
        {
            if (context.Motor.CanStandUp())
            {
                context.Motor.ResetHeight();
            }

            context.RaiseEvent?.Invoke(MovementEventType.SlideEnded);
        }
    }
}
