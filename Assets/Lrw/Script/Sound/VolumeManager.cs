using System;
using UnityEngine;
using UnityEngine.Audio;
using AudioType = DevLib.SoundSystem.Runtime.AudioType;

namespace Lrw.Script.Sound
{
    public class VolumeManager : MonoBehaviour
    {
        private const float MinimumDecibels = -80f;

        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private string sfxVolumeParameter = "SFXVolume";
        [SerializeField] private string musicVolumeParameter = "BGMVolume";
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        
        public void SetVolume(AudioType type, float volume)
        {
            AudioMixerGroup targetGroup;
            string volumeParameter;

            switch (type)
            {
                case AudioType.Sfx:
                    targetGroup = sfxGroup;
                    volumeParameter = sfxVolumeParameter;
                    break;
                case AudioType.Music:
                    targetGroup = musicGroup;
                    volumeParameter = musicVolumeParameter;
                    break;
                case AudioType.Master:
                    targetGroup = masterGroup;
                    volumeParameter = masterVolumeParameter;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (targetGroup == null)
            {
                Debug.LogError($"{type} AudioMixerGroup이 할당되지 않았습니다.", this);
                return;
            }

            float normalizedVolume = Mathf.Clamp01(volume);
            float decibels = normalizedVolume <= 0f
                ? MinimumDecibels
                : Mathf.Log10(normalizedVolume) * 20f;

            if (!targetGroup.audioMixer.SetFloat(volumeParameter, decibels))
            {
                Debug.LogWarning(
                    $"AudioMixer에 노출된 파라미터 '{volumeParameter}'가 없습니다.",
                    this);
            }
        }
    }
}
