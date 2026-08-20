using Agents;
using Player.FSM;
using System;
using UnityEngine;

namespace Client
{
    public class PlayerJumpState : AbstractPlayerState
    {
        public PlayerJumpState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
            // _controlMovement.SetMovementDirection(Vector2.zero);
            _controlMovement.OnVelocityChange += HandleMovementChange;
            _controlMovement.AddForceToAgent(Vector3.up * 10);
            Debug.Log("점프 상태로 전환");
        }

        public override void Exit()
        {
            base.Exit();
            _controlMovement.OnVelocityChange -= HandleMovementChange;
        }

        private void HandleMovementChange(Vector3 vector)
        {
            if (vector.y < 0)
            {
                _player.ChangeState(PlayerState.FALL, 0.25f);
            }
        }
    }
}
