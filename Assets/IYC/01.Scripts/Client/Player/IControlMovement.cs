using System;
using UnityEngine;

namespace Player
{
    public interface IControlMovement
    {

        Action<Vector3> OnVelocityChange { get; set; }
        bool CanManualMove { get; set; }
        bool IsGround { get; }
        void SetMovementVelocity(Vector3 velocity);
        void SetMovementDirection(Vector2 movementInput);
        void RotateTo(Vector3 direction);

        void AddForceToAgent(Vector3 force);
    }
}