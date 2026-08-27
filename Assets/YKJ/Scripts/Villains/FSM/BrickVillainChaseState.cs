using Agents;

namespace Villains.FSM
{
    public class BrickVillainChaseState : AbstractBrickVillainState
    {
        public BrickVillainChaseState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Update()
        {
            base.Update();

            if (!_villain.IsTargetInDetectionRange)
            {
                _villain.ChangeState(VillainState.IDLE, 0.1f);
                return;
            }

            if (_villain.IsTargetInThrowRange && _villain.IsThrowReady)
            {
                _villain.ChangeState(VillainState.THROW, 0.1f);
                return;
            }

            _villain.MoveToTarget();
        }
    }
}
