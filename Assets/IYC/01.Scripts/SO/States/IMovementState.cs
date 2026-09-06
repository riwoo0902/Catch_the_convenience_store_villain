using CWH.Player.Core;

namespace CWH.Player.States
{
    public interface IMovementState
    {
        MovementStateId Id { get; }

        void Enter(MovementContext context);
        void Tick(MovementContext context);
        IMovementState CheckTransitions(MovementContext context);
        void Exit(MovementContext context);
    }
}
