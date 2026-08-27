using System.Collections.Generic;
using DevLib.EventChannelSystem;
using DevLib.ObjectPool.Runtime;
using UnityEngine;

namespace DevLib.SoundSystem.Runtime
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO soundItem;
        [SerializeField] private EventChannelSO soundChannel;

        private readonly Dictionary<int, SoundPlayer> _soundPlayers = new();

        private void Awake()
        {
            SoundManager[] managers = FindObjectsByType<SoundManager>(FindObjectsSortMode.None);
            if (managers.Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            
            soundChannel.AddListener<PlaySoundEvents>(HandlePlaySound);
            soundChannel.AddListener<StopSoundEvent>(HandleStopSound);
        }

        
        private void OnDestroy()
        {
            soundChannel.RemoveListener<PlaySoundEvents>(HandlePlaySound);
            soundChannel.RemoveListener<StopSoundEvent>(HandleStopSound);
        }

        private void HandlePlaySound(PlaySoundEvents evt)
        {
            SoundPlayer player = poolManager.Pop<SoundPlayer>(soundItem);
            player.transform.position = evt.Position;
            player.PlaySound(evt.ClipData);
            player.OnPlayFinished += HandlePlayFinish;

            if (evt.ChannelNumber > 0 && evt.ClipData.loop)
            {
                if (_soundPlayers.TryGetValue(evt.ChannelNumber, out SoundPlayer existingPlayer))
                {
                    existingPlayer.ForceStopSound();
                    poolManager.Push(existingPlayer);
                    _soundPlayers.Remove(evt.ChannelNumber);
                }
                _soundPlayers.Add(evt.ChannelNumber,player);
            }
            else if (evt.ChannelNumber <= 0 && evt.ClipData.loop)
            {
                Debug.LogWarning($"루프 사운드는 기본 채널리 1 이상의 정수로 지정해야 합니다. {evt.ClipData.name}");
            }
        }

        private void HandlePlayFinish(SoundPlayer player)
        {
            player.OnPlayFinished -= HandlePlayFinish;
            poolManager.Push(player);
        }

        private void HandleStopSound(StopSoundEvent evt)
        {
            if (_soundPlayers.TryGetValue(evt.ChannelNumber, out SoundPlayer player))
            {
                player.ForceStopSound();
                player.OnPlayFinished -= HandlePlayFinish;
                poolManager.Push(player);
                _soundPlayers.Remove(evt.ChannelNumber);
            }
        }
    }
}