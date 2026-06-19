using CoreSystem.DataSystem;
using UnityEngine;

namespace CoreSystem.EffectSystem
{
    public interface IPlayableVfx
    {
        AssetNameSO VfxName { get; }
        void PlayVfx(Vector3 position, Quaternion rotation);
        void PlayVfx();
        void StopVfx();
    }
}