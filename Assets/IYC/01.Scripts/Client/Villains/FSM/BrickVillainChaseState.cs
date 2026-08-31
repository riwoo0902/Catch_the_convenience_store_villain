using Agents;

namespace Villains.FSM
{
    public class BrickVillainChaseState : BrickVillainState
    {
        public BrickVillainChaseState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Update()
        {
            base.Update();

            if (!HasRememberedTarget)
            {
                _villain.ChangeState(VillainState.IDLE);
                return;
            }

            if (IsTargetInAttackRange)
            {
                _villain.ChangeState(VillainState.AIM);
                return;
            }

            _movement.MoveTo(_targetProvider.LastTargetPosition);
        }
    }
}
