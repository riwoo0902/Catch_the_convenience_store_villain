using DevLib.EventChannelSystem;
using UnityEngine;

namespace DevLib.SoundSystem.Runtime
{
    public static class SoundEvents
    {
        public static readonly PlaySoundEvents PlaySound = new();
        public static readonly StopSoundEvent StopSound = new();
    }

    public class PlaySoundEvents : GameEvent
    {
        public Vector3 Position;
        public SoundClipSO ClipData;
        public int ChannelNumber;

        public PlaySoundEvents Init(Vector3 position, SoundClipSO clipData, int channelNumber = 0)
        {
            Position = position;
            ClipData = clipData;
            ChannelNumber = channelNumber;
            return this;
        }
    }

    public class StopSoundEvent : GameEvent
    {
        public int ChannelNumber;

        public StopSoundEvent Init(int channelNumber = 0)
        {
            ChannelNumber = channelNumber;
            return this;
        }
    }
}