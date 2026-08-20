using Agents;
using UnityEngine;

namespace Villains.FSM
{
    public class BrickVillainThrowState : AbstractBrickVillainState
    {
        private const float THROW_DELAY = 0.35f;
        private const float EXIT_DELAY = 0.65f;

        private float _enterTime;
        private bool _hasThrown;

        public BrickVillainThrowState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _enterTime = Time.time;
            _hasThrown = false;
        }

        public override void Update()
        {
            base.Update();

            _villain.FaceTarget();

            if (!_hasThrown && Time.time >= _enterTime + THROW_DELAY)
            {
                _villain.ThrowBrick();
                _hasThrown = true;
            }

            if (Time.time < _enterTime + EXIT_DELAY)
                return;

            _villain.ChangeState(
                _villain.IsTargetInDetectionRange ? VillainState.CHASE : VillainState.IDLE,
                0.1f
            );
        }
    }
}
