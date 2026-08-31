using System;

namespace CWH.Player.Animation
{
    public interface IMovementStateSource
    {
        MovementSnapshot GetSnapshot();
        event Action<MovementEventType> OnMovementEvent;
    }
}
