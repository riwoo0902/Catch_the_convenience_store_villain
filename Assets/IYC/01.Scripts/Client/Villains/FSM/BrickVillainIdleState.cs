using Agents;

namespace Villains.FSM
{
    public class BrickVillainIdleState : BrickVillainState
    {
        public BrickVillainIdleState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _movement?.Stop();
        }

        public override void Update()
        {
            base.Update();

            if (HasRememberedTarget)
                _villain.ChangeState(VillainState.CHASE);
        }
    }
}
