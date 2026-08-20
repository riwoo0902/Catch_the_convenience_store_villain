using Agents;
using Player.FSM;

namespace Client
{
    public class PlayerFallState : AbstractPlayerState
    {
        public PlayerFallState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
        }

        public override void Update()
        {
            base.Update();
            if (_controlMovement.IsGround)
            {
                _player.ChangeState(PlayerState.IDLE, 0.25f);
            }
        }
    }
}
