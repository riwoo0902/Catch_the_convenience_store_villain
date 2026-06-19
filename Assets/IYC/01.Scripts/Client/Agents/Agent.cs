using IYC._01.Scripts.CoreSystem.Module;

namespace Agents
{
    public abstract class Agent : ModuleOwner
    {
        protected override void InitializeModules()
        {
            base.InitializeModules();
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
        }
    }
}