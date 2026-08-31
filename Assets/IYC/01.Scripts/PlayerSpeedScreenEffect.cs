using Branches.CWH.Scripts.Player;
using UnityEngine;

namespace CWH.Player
{
    public sealed class PlayerSpeedScreenEffect : MonoBehaviour
    {
        [SerializeField] private PlayerMovementController _player;
        [SerializeField] private Material _effectMaterial;
        [SerializeField] private string _colorProperty = "_Color";
        [SerializeField] private string _transitionValueProperty = "_TransitionValue";
        [SerializeField] private Color _baseColor = Color.white;
        [SerializeField] private float _minSpeed = 5f;
        [SerializeField] private float _maxSpeed = 20f;
        [SerializeField] private float _minIntensity = 0f;
        [SerializeField] private float _maxIntensity = 3f;
        [SerializeField] private float _transitionSpeed = 5f;

        private int _colorId;
        private int _transitionValueId;
        private float _currentIntensity;

        private void Awake()
        {
            _colorId = Shader.PropertyToID(_colorProperty);
            _transitionValueId = Shader.PropertyToID(_transitionValueProperty);
        }

        private void Update()
        {
            if (_player == null || _player.StateSource == null || _effectMaterial == null)
            {
                return;
            }

            var speed = _player.StateSource.GetSnapshot().Speed;
            var speedT = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);
            var targetIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, speedT);
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, targetIntensity, _transitionSpeed * Time.deltaTime);

            var scale = Mathf.Pow(2f, _currentIntensity);
            var color = new Color(
                _baseColor.r * scale,
                _baseColor.g * scale,
                _baseColor.b * scale,
                _baseColor.a);
            _effectMaterial.SetColor(_colorId, color);

            var transitionValue = speed < _minSpeed ? -2f : 0.4f;
            _effectMaterial.SetFloat(_transitionValueId, transitionValue);
        }
    }
}
