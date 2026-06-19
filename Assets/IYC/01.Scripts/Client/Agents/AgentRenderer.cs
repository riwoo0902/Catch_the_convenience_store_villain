using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Agents
{
    public class AgentRenderer : MonoBehaviour, IModule, IRenderer
    {
        public Animator Animator { get; private set; }
        protected ModuleOwner owner;
        
        public void Init(ModuleOwner owner)
        {
            this.owner = owner;
            Animator =  GetComponent<Animator>();
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            Animator.CrossFade(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
    }
}