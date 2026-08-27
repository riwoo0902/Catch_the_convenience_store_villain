using UnityEngine;
using AudioType = DevLib.SoundSystem.Runtime.AudioType;

namespace Lrw.Script.Sound
{
    public class SoundBar : MonoBehaviour
    {
        [SerializeField] private AudioType type;
        [SerializeField] private VolumeManager volumeManager;
        
        public void SetVolume(float volume)
        {
            volumeManager.SetVolume(type, volume);
        }
        
    }
}