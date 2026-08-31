using System;
using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Villains.Animation
{
    public class VillainAnimationEventRelay : MonoBehaviour, IModule
    {
        public event Action OnThrowTrigger;
        public event Action OnAnimationEndTrigger;

        public void Init(ModuleOwner owner)
        {
        }

        public void ThrowBrick() => OnThrowTrigger?.Invoke();
        public void Throw() => OnThrowTrigger?.Invoke();
        public void OnThrow() => OnThrowTrigger?.Invoke();
        public void FireProjectile() => OnThrowTrigger?.Invoke();
        public void DamageCastTrigger() => OnThrowTrigger?.Invoke();

        public void AnimationEnd() => OnAnimationEndTrigger?.Invoke();
        public void OnAnimationEnd() => OnAnimationEndTrigger?.Invoke();
        public void AnimationEndTrigger() => OnAnimationEndTrigger?.Invoke();
    }
}
