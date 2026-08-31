using Agents;

namespace Villains.FSM
{
    public class BrickVillainFleeState : BrickVillainState
    {
        public BrickVillainFleeState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _targetProvider?.ClearTarget();
        }

        public override void Update()
        {
            base.Update();

            _movement.FleeTo(_villain.FleeDestination);
            if (_movement.IsArrived)
                _villain.CompleteFlee();
        }
    }
}
