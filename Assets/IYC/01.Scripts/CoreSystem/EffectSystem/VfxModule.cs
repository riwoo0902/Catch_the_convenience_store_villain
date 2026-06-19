using System.Collections.Generic;
using System.Linq;
using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace CoreSystem.EffectSystem
{
    public class VfxModule : MonoBehaviour, IModule
    {
        private ModuleOwner _owner;
        private Dictionary<int, IPlayableVfx> _vfxDict;
        
        public void Init(ModuleOwner owner)
        {
            _owner = owner;
            _vfxDict = GetComponentsInChildren<IPlayableVfx>().ToDictionary(vfx => vfx.VfxName.AssetHash);
        }

        public void PlayVfx(int vfxHash, Vector3 position, Quaternion rotation)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVfx vfx))
            {
                vfx.PlayVfx(position, rotation);
            }
            else
            {
                Debug.LogError($"Vfx {vfxHash} not found");
            }
        }

        public void PlayVfx(int vfxHash)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVfx vfx))
            {
                vfx.PlayVfx();
            }
            else
            {
                Debug.LogError($"Vfx {vfxHash} not found");
            }
        }

        public void StopVfx(int vfxHash)
        {
            if (_vfxDict.TryGetValue(vfxHash, out IPlayableVfx vfx))
            {
                vfx.StopVfx();
            }
        }
    }
}