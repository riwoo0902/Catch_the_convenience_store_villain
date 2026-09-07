using Branches.CWH.Scripts.Player;
using CWH.GameFlow;
using UnityEngine;

namespace CWH.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFallRecovery : MonoBehaviour
    {
        [SerializeField] private float _fallThresholdY = -10f;
        [SerializeField] private float _recoveryHeight = 2f;
        private CharacterController _controller;
        private PlayerMovementController _movement;
        private float _startingHeight;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _movement = GetComponent<PlayerMovementController>();
            _startingHeight = transform.position.y;
        }

        private void LateUpdate()
        {
            if (GameLoopController.AllowsGameplay && transform.position.y < _fallThresholdY)
                RaiseToHeight(Mathf.Max(_recoveryHeight, _startingHeight));
        }

        public void RaiseToHeight(float height)
        {
            Vector3 position = transform.position;
            position.y = Mathf.Max(position.y, height, _recoveryHeight, _startingHeight);
            bool wasEnabled = _controller.enabled;
            _controller.enabled = false;
            transform.position = position;
            _movement?.ResetVerticalMotion();
            _controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }
    }
}
