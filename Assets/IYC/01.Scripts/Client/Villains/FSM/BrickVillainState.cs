using Agents;
using Agents.FSM;
using UnityEngine;
using Villains.Animation;
using Villains.Combat;
using Villains.Movement;
using Villains.Targeting;

namespace Villains.FSM
{
    public abstract class BrickVillainState : AgentState
    {
        protected readonly BrickVillain _villain;
        protected readonly VillainTargetProvider _targetProvider;
        protected readonly VillainMovement _movement;
        protected readonly BrickThrowAttack _throwAttack;
        protected readonly VillainAnimationEventRelay _animationEvents;

        protected Transform Target => _targetProvider != null ? _targetProvider.Target : null;
        protected Transform CurrentTarget => _targetProvider != null ? _targetProvider.CurrentTarget : null;
        protected bool HasRememberedTarget => CurrentTarget != null;
        protected bool IsTargetInAttackRange =>
            CurrentTarget != null
            && _throwAttack != null
            && _targetProvider.GetTargetDistance(_villain.transform.position) <= _throwAttack.AttackRange;
        protected bool CanAttack => _throwAttack != null && _throwAttack.CanAttack;

        protected BrickVillainState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _villain = agent as BrickVillain;
            _targetProvider = _villain.TargetProvider;
            _movement = _villain.Movement;
            _throwAttack = _villain.ThrowAttack;
            _animationEvents = _villain.AnimationEvents;
        }
    }
}
