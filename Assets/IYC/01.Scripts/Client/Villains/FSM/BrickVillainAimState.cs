using Agents;
using UnityEngine;

namespace Villains.FSM
{
    public class BrickVillainAimState : BrickVillainState
    {
        private const float AIM_DURATION = 0.35f;
        private float _enterTime;

        public BrickVillainAimState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _enterTime = Time.time;
            _movement?.Stop();
        }

        public override void Update()
        {
            base.Update();

            if (!HasRememberedTarget)
            {
                _villain.ChangeState(VillainState.IDLE);
                return;
            }

            if (!IsTargetInAttackRange)
            {
                _villain.ChangeState(VillainState.CHASE);
                return;
            }

            if (CurrentTarget != null)
                _movement?.LookAt(CurrentTarget.position);

            if (Time.time >= _enterTime + AIM_DURATION && CanAttack)
                _villain.ChangeState(VillainState.THROW);
        }
    }
}
