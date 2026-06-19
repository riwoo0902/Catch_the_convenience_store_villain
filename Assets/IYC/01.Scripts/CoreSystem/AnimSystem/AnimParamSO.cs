using System;
using UnityEngine;

namespace CoreSystem.AnimationSystem
{
    [CreateAssetMenu(fileName = "Anim param so", menuName = "System/Animator param", order = 20)]
    public class AnimParamSO : ScriptableObject
    {
        [field: SerializeField] public string ParamName { get; private set; }
        [field: SerializeField] public int ParamHash {get; private set; }

        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(ParamName))
                ParamHash = Animator.StringToHash(ParamName);
        }
    }
}