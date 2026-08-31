using Agents;
using Agents.FSM;

namespace Villains.FSM
{
    public abstract class AbstractBrickVillainState : AgentState
    {
        protected readonly BrickThrowingVillain _villain;

        protected AbstractBrickVillainState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _villain = agent as BrickThrowingVillain;
        }
    }
}
