using UnityEngine;

namespace DevLib.SoundSystem.Runtime
{
    public enum AudioType
    {
        Sfx, Music,Master
    }
    
    [CreateAssetMenu(fileName = "Sound clip", menuName = "Lib/Sound/Clip", order = 0)]
    public class SoundClipSO : ScriptableObject
    {
        public AudioType audioType;
        public AudioClip clip;
        public bool loop = false;
        public bool randomizePitch = false;

        [Range(0, 1f)] public float randomPitchModifier = 0.1f;
        [Range(0.1f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;

        public float startTime = 0;
        public float endTime = 0;
    }
}