using UnityEngine;

namespace CWH.Player.Core
{
    public interface ICharacterMotor
    {
        bool IsGrounded { get; }
        Vector3 GroundNormal { get; }
        float HeightReduction { get; }

        CollisionFlags Move(Vector3 motion);
        void SetHeightMultiplier(float multiplier);
        void ResetHeight();
        bool HasClearanceAboveGround(float minHeight);
        bool CanStandUp();
    }
}
