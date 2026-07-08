using Agents;
using Agents.FSM;
using UnityEngine;

namespace Player.FSM
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected readonly PlayerController _player;
        protected readonly IControlMovement _controlMovement;
        protected const float INPUT_DEADZONE = 0.1f;
        
        protected AbstractPlayerState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _player = agent as PlayerController;
            Debug.Log(agent.GetComponentInChildren<IControlMovement>());
            _controlMovement = agent.GetComponentInChildren<IControlMovement>();
        }
    }
}