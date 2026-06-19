using Agents;
using Agents.FSM;
using UnityEngine;

namespace Player.FSM
{
    public class PlayerIdleState : AbstractPlayerState
    {
        public PlayerIdleState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            _player.PlayerInput.OnMovementChange += HandleMovementChange;
            _controlMovement.SetMovementDirection(Vector2.zero);
        }

        public override void Exit()
        {
            _player.PlayerInput.OnMovementChange -= HandleMovementChange;
            base.Exit();
        }

        private void HandleMovementChange(Vector2 movementKey)
        {
            if (movementKey.magnitude > INPUT_DEADZONE)
            {
                _player.ChangeState(PlayerState.RUN, transitionDuration: 0.1f); // RUN상태로 전환
            }
        }
    }
}