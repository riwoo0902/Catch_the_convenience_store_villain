using Agents;
using UnityEngine;

namespace Villains.FSM
{
    public class BrickVillainStunState : BrickVillainState
    {
        private float _endTime;

        public BrickVillainStunState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public void SetDuration(float duration)
        {
            _endTime = Time.time + duration;
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _movement?.Stop();

            if (_endTime <= Time.time)
                _endTime = Time.time + 1f;
        }

        public override void Update()
        {
            base.Update();

            if (Time.time < _endTime)
                return;

            _villain.ChangeState(HasRememberedTarget ? VillainState.CHASE : VillainState.IDLE);
        }
    }
}
