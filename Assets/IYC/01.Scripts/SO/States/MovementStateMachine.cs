using System;
using UnityEngine;
using CWH.Player.Animation;
using CWH.Player.Core;
using CWH.Player.Input;

namespace CWH.Player.States
{
    public sealed class MovementStateMachine : IMovementStateSource
    {
        private readonly MovementContext _context;
        private IMovementState _current;

        public event Action<MovementEventType> OnMovementEvent;

        public MovementStateId CurrentStateId => _current.Id;

        public MovementStateMachine(MovementContext context, IMovementState initialState)
        {
            _context = context;
            _context.RaiseEvent = RaiseEvent;
            _current = initialState;
            _current.Enter(_context);
        }

        public void Tick(float deltaTime, MovementInputSample input)
        {
            _context.DeltaTime = deltaTime;
            _context.Input = input;

            _context.JumpBufferTimer = input.JumpPressed
                ? _context.Jump.JumpBufferTime
                : Mathf.Max(0f, _context.JumpBufferTimer - deltaTime);

            _context.AirborneTime = _context.Motor.IsGrounded ? 0f : _context.AirborneTime + deltaTime;
            _context.SprintLockoutTimer = Mathf.Max(0f, _context.SprintLockoutTimer - deltaTime);
            _context.WallRunCooldownTimer = Mathf.Max(0f, _context.WallRunCooldownTimer - deltaTime);

            _current.Tick(_context);

            // 여러 가속 효과가 겹쳐서 속도가 무한정 쌓이는 걸 막는 안전장치
            var horizontalVelocity = new Vector3(_context.Velocity.x, 0f, _context.Velocity.z);
            if (horizontalVelocity.magnitude > _context.Ground.MaxSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * _context.Ground.MaxSpeed;
                _context.Velocity = new Vector3(horizontalVelocity.x, _context.Velocity.y, horizontalVelocity.z);
            }

            var next = _current.CheckTransitions(_context);
            if (next != null && !ReferenceEquals(next, _current))
            {
                _current.Exit(_context);
                _current = next;
                _current.Enter(_context);
            }
        }

        public MovementSnapshot GetSnapshot()
        {
            return new MovementSnapshot(
                velocity: _context.Velocity,
                isGrounded: _context.Motor.IsGrounded,
                isSprinting: _context.Input.SprintHeld,
                isSliding: _current.Id == MovementStateId.Sliding,
                isCrouching: _context.Input.CrouchHeld || _current.Id == MovementStateId.Sliding,
                heightReduction: _context.Motor.HeightReduction,
                wallSide: _context.CurrentWallSide,
                moveInput: _context.Input.MoveAxis);
        }

        private void RaiseEvent(MovementEventType eventType) => OnMovementEvent?.Invoke(eventType);
    }
}
