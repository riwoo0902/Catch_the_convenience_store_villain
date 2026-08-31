using Branches.CWH.Scripts.Player;
using UnityEngine;

namespace CWH.Player
{
    public sealed class PlayerSpeedVFX : MonoBehaviour
    {
        [SerializeField] private PlayerMovementController _player;
        [SerializeField] private float _speedThreshold = 12f;
        [SerializeField] private Material _speedEffectMat;
 
        private bool _isPlaying;
        
        private void Update()
        {
            if (_player == null || _player.StateSource == null)
            {
                return;
            }

            var speed = _player.StateSource.GetSnapshot().Speed;
            var shouldPlay = speed >= _speedThreshold;

            if (shouldPlay && !_isPlaying)
            {
                
                _isPlaying = true;
            }
            else if (!shouldPlay && _isPlaying)
            {
 
                _isPlaying = false;
            }
        }
    }
}
