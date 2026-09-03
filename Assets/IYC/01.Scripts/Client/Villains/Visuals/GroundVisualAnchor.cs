using UnityEngine;

namespace Villains.Visuals
{
    [DisallowMultipleComponent]
    public sealed class GroundVisualAnchor : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform ignoredRoot;
        [SerializeField, Min(0f)] private float groundClearance = 0.03f;
        [SerializeField, Min(0.1f)] private float raycastHeight = 2f;
        [SerializeField, Min(0.1f)] private float raycastDistance = 5f;

        private static readonly RaycastHit[] GroundHits = new RaycastHit[16];

        private Vector3 _baseLocalPosition;
        private bool _usesObjectRoot;

        public void Configure(Transform root, Transform ignored, float clearance)
        {
            visualRoot = root != null ? root : transform;
            ignoredRoot = ignored;
            groundClearance = Mathf.Max(0f, clearance);
            _usesObjectRoot = visualRoot == transform;
            _baseLocalPosition = visualRoot.localPosition;
            AlignNow();
        }

        public void SetIgnoredRoot(Transform ignored)
        {
            ignoredRoot = ignored;
        }

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _usesObjectRoot = visualRoot == transform;
            _baseLocalPosition = visualRoot.localPosition;
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!_usesObjectRoot)
            {
                visualRoot.localPosition = _baseLocalPosition;
            }

            AlignNow();
        }

        public void AlignNow()
        {
            if (visualRoot == null || !TryGetVisualBounds(out Bounds bounds))
            {
                return;
            }

            float targetMinY = FindGroundY() + groundClearance;
            float yOffset = targetMinY - bounds.min.y;
            if (Mathf.Abs(yOffset) <= 0.001f)
            {
                return;
            }

            visualRoot.position += Vector3.up * yOffset;
            if (!_usesObjectRoot)
            {
                _baseLocalPosition = visualRoot.localPosition;
            }
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null
                    || !renderer.enabled
                    || renderer is LineRenderer
                    || (ignoredRoot != null && renderer.transform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private float FindGroundY()
        {
            Vector3 origin = transform.position + Vector3.up * raycastHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                raycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            float groundY = transform.position.y;
            bool foundGround = false;
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = GroundHits[i].transform;
                if (hitTransform == null || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (GroundHits[i].distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = GroundHits[i].distance;
                groundY = GroundHits[i].point.y;
                foundGround = true;
            }

            return foundGround ? groundY : transform.position.y;
        }
    }
}
