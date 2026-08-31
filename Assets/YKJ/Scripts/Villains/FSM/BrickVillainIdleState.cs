using Agents;

namespace Villains.FSM
{
    public class BrickVillainIdleState : AbstractBrickVillainState
    {
        public BrickVillainIdleState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Update()
        {
            base.Update();

            if (_villain.IsTargetInDetectionRange)
                _villain.ChangeState(VillainState.CHASE, 0.1f);
        }
    }
}
