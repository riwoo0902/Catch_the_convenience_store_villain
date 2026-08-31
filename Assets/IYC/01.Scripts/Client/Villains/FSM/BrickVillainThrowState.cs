using Agents;
using UnityEngine;

namespace Villains.FSM
{
    public class BrickVillainThrowState : BrickVillainState
    {
        private const float EXIT_AFTER_THROW_DELAY = 1.1f;
        private const float MAX_EVENT_WAIT_TIME = 2f;

        private float _enterTime;
        private float _throwTime;
        private bool _hasThrown;
        private bool _isAnimationEnded;

        public BrickVillainThrowState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _enterTime = Time.time;
            _throwTime = 0f;
            _hasThrown = false;
            _isAnimationEnded = false;
            _movement?.Stop();

            if (_animationEvents != null)
            {
                _animationEvents.OnThrowTrigger += HandleThrowTrigger;
                _animationEvents.OnAnimationEndTrigger += HandleAnimationEndTrigger;
            }
        }

        public override void Update()
        {
            base.Update();

            if (CurrentTarget != null)
                _movement?.LookAt(CurrentTarget.position);

            if (!ShouldExit())
                return;

            ChangeToNextState();
        }

        public override void Exit()
        {
            if (_animationEvents != null)
            {
                _animationEvents.OnThrowTrigger -= HandleThrowTrigger;
                _animationEvents.OnAnimationEndTrigger -= HandleAnimationEndTrigger;
            }

            base.Exit();
        }

        private void HandleThrowTrigger()
        {
            if (_hasThrown)
                return;

            _throwAttack?.ThrowAt(CurrentTarget);
            _hasThrown = true;
            _throwTime = Time.time;
        }

        private void HandleAnimationEndTrigger()
        {
            _isAnimationEnded = true;
        }

        private bool ShouldExit()
        {
            if (_isAnimationEnded)
                return true;

            if (_hasThrown && Time.time >= _throwTime + EXIT_AFTER_THROW_DELAY)
                return true;

            return Time.time >= _enterTime + MAX_EVENT_WAIT_TIME;
        }

        private void ChangeToNextState()
        {
            if (!HasRememberedTarget)
                _villain.ChangeState(VillainState.IDLE);
            else if (IsTargetInAttackRange)
                _villain.ChangeState(VillainState.AIM);
            else
                _villain.ChangeState(VillainState.CHASE);
        }
    }
}
