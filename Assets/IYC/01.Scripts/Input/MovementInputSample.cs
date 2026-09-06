using UnityEngine;

namespace CWH.Player.Input
{
    public readonly struct MovementInputSample
    {
        public readonly Vector2 MoveAxis;
        public readonly Vector2 LookDelta;
        public readonly bool JumpPressed;
        public readonly bool SprintHeld;
        public readonly bool CrouchPressed;
        public readonly bool CrouchHeld;
        public readonly bool SlidePressed;

        public MovementInputSample(
            Vector2 moveAxis,
            Vector2 lookDelta,
            bool jumpPressed,
            bool sprintHeld,
            bool crouchPressed,
            bool crouchHeld,
            bool slidePressed)
        {
            MoveAxis = moveAxis;
            LookDelta = lookDelta;
            JumpPressed = jumpPressed;
            SprintHeld = sprintHeld;
            CrouchPressed = crouchPressed;
            CrouchHeld = crouchHeld;
            SlidePressed = slidePressed;
        }
    }
}
