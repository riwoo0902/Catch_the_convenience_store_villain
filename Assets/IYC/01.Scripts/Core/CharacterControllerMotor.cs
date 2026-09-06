using UnityEngine;

namespace CWH.Player.Core
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerMotor : MonoBehaviour, ICharacterMotor
    {
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundCheckDistance = 0.15f;

        private CharacterController _controller;
        private float _defaultHeight = 2f;
        private Vector3 _defaultCenter = Vector3.zero;

        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float HeightReduction => _defaultHeight - _controller.height;
        public float GroundCheckDistance => _groundCheckDistance; // 디버그 기즈모용

        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            Debug.Assert(_controller != null, "캐릭터 컨트롤러가 없습니다. _controller == null");
            _defaultHeight = _controller.height;
            _defaultCenter = _controller.center;
        }

        public CollisionFlags Move(Vector3 motion)
        {
            var flags = _controller.Move(motion);
            UpdateGroundState();
            return flags;
        }

        public void SetHeightMultiplier(float multiplier)
        {
            var newHeight = _defaultHeight * multiplier;
            var centerDrop = (_defaultHeight - newHeight) * 0.5f;
            _controller.height = newHeight;
            _controller.center = new Vector3(_defaultCenter.x, _defaultCenter.y - centerDrop, _defaultCenter.z);
        }

        public void ResetHeight()
        {
            _controller.height = _defaultHeight;
            _controller.center = _defaultCenter;
        }

        public bool HasClearanceAboveGround(float minHeight)
        {
            var bottomSphereCenter = transform.TransformPoint(_controller.center)
                - Vector3.up * (_controller.height * 0.5f - _controller.radius);

            var groundWithinMinHeight = Physics.SphereCast(
                bottomSphereCenter + Vector3.up * 0.05f,
                _controller.radius * 0.95f,
                Vector3.down,
                out _,
                minHeight + 0.05f,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            return !groundWithinMinHeight;
        }
        
        public bool CanStandUp()
        {
            var clearanceNeeded = _defaultHeight - _controller.height;
            if (clearanceNeeded <= 0f)
            {
                return true;
            }

            var topSphereCenter = transform.TransformPoint(_controller.center)
                + Vector3.up * (_controller.height * 0.5f - _controller.radius);

            var blocked = Physics.SphereCast(
                topSphereCenter,
                _controller.radius * 0.95f,
                Vector3.up,
                out _,
                clearanceNeeded,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            return !blocked;
        }

        private void UpdateGroundState()
        {
            var bottomSphereCenter = transform.TransformPoint(_controller.center)
                - Vector3.up * (_controller.height * 0.5f - _controller.radius);

            var grounded = Physics.SphereCast(
                bottomSphereCenter + Vector3.up * 0.05f,
                _controller.radius * 0.95f,
                Vector3.down,
                out var hit,
                _groundCheckDistance + 0.05f,
                _groundMask,
                QueryTriggerInteraction.Ignore);

            IsGrounded = grounded;
            GroundNormal = grounded ? hit.normal : Vector3.up;
        }
    }
}
