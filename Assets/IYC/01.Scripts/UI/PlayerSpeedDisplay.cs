using CWH.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Branches.CWH.Scripts.Player.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class PlayerSpeedDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerMovementController _player;
        [SerializeField] private string _format = "Speed: {0:F1} m/s";

        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (_player == null || _player.StateSource == null)
            {
                return;
            }

            var speed = _player.StateSource.GetSnapshot().Speed;
            _text.text = string.Format(_format, speed);
        }
    }
}
