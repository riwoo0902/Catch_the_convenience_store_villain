namespace CWH.Player.States
{
    /// Resolves mutual references between sibling states (e.g. Grounded needs
    /// to hand off to Airborne and vice versa) without a central switch statement.
    /// Populated once, right after all states are constructed.
    public sealed class MovementStateLibrary
    {
        public IMovementState Grounded;
        public IMovementState Airborne;
        public IMovementState Sliding;
    }
}
