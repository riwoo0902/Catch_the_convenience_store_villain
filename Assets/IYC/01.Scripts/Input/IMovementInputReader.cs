using UnityEngine;

namespace CWH.Player.Input
{
    public interface IMovementInputReader
    {
        MovementInputSample Sample();

        /// Non-destructive per-frame read of the Look axis, safe to poll from
        /// Update independently of Sample() (which is consumed once per fixed
        /// tick by the movement state machine and clears edge-triggered flags).
        Vector2 ReadLook();
    }
}
