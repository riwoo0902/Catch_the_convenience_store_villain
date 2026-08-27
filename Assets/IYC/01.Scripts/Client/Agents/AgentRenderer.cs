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
            Animator = GetComponent<Animator>();

            if (Animator == null)
                Debug.LogWarning($"{nameof(AgentRenderer)} needs an Animator on the same GameObject.", this);
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            if (Animator == null || Animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"{nameof(AgentRenderer)} cannot play animation because Animator or Controller is missing.", this);
                return;
            }

            Animator.CrossFade(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
    }
}
