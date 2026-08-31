using UnityEngine;
using CWH.Player.Core;

namespace CWH.Player.Animation
{
    public readonly struct MovementSnapshot
    {
        public readonly Vector3 Velocity;
        public readonly float Speed;
        public readonly bool IsGrounded;
        public readonly bool IsSprinting;
        public readonly bool IsSliding;
        public readonly bool IsCrouching;
        public readonly float HeightReduction;
        public readonly WallSide WallSide;
        public readonly Vector2 MoveInput;

        public MovementSnapshot(
            Vector3 velocity,
            bool isGrounded,
            bool isSprinting,
            bool isSliding,
            bool isCrouching,
            float heightReduction,
            WallSide wallSide,
            Vector2 moveInput)
        {
            Velocity = velocity;
            Speed = new Vector2(velocity.x, velocity.z).magnitude;
            IsGrounded = isGrounded;
            IsSprinting = isSprinting;
            IsSliding = isSliding;
            IsCrouching = isCrouching;
            HeightReduction = heightReduction;
            WallSide = wallSide;
            MoveInput = moveInput;
        }
    }
}
