using System;
using UnityEngine;

namespace CoreSystem.DataSystem
{
    [CreateAssetMenu(fileName = "AssetName", menuName = "SO/AssetName", order = 10)]
    public class AssetNameSO : ScriptableObject
    {
        [field: SerializeField] public string AssetName { get; private set; }
        [field: SerializeField] public int AssetHash { get; private set; }

        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(AssetName))
                AssetHash = Animator.StringToHash(AssetName);
        }
    }
}