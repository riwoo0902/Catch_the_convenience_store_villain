using Agents;
using UnityEngine;

namespace Villains.FSM
{
    public class BrickVillainThrowState : BrickVillainState
    {
        private const float EXIT_AFTER_THROW_DELAY = 1.1f;

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

            TryFallbackThrowByAnimationTime();

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
            ThrowNow();
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

            float maxWaitTime = _throwAttack != null ? _throwAttack.MaxAnimationWaitTime : 2f;
            return Time.time >= _enterTime + Mathf.Max(0.1f, maxWaitTime);
        }

        private void TryFallbackThrowByAnimationTime()
        {
            if (_hasThrown || _throwAttack == null)
                return;

            float releaseTime = Mathf.Clamp01(_throwAttack.ReleaseNormalizedTime);
            if (TryGetThrowStateNormalizedTime(out float normalizedTime))
            {
                if (normalizedTime >= releaseTime)
                    ThrowNow();

                return;
            }

            float maxWaitTime = Mathf.Max(0.1f, _throwAttack.MaxAnimationWaitTime);
            if (Time.time >= _enterTime + maxWaitTime * releaseTime)
                ThrowNow();
        }

        private bool TryGetThrowStateNormalizedTime(out float normalizedTime)
        {
            normalizedTime = 0f;

            Animator animator = _renderer?.Animator;
            if (animator == null || _stateClipHash == 0)
                return false;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == _stateClipHash || stateInfo.fullPathHash == _stateClipHash)
            {
                normalizedTime = stateInfo.normalizedTime % 1f;
                return true;
            }

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(0);
                if (nextStateInfo.shortNameHash == _stateClipHash || nextStateInfo.fullPathHash == _stateClipHash)
                {
                    normalizedTime = nextStateInfo.normalizedTime % 1f;
                    return true;
                }
            }

            return false;
        }

        private void ThrowNow()
        {
            if (_hasThrown)
                return;

            _throwAttack?.ThrowAt(CurrentTarget);
            _hasThrown = true;
            _throwTime = Time.time;
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
