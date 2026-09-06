using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        private Transform[] _rootVisualChildren = new Transform[0];
        private Vector3[] _rootVisualBaseLocalPositions = new Vector3[0];
        private Transform[] _groundReferencePoints = new Transform[0];

        public void Configure(Transform root, Transform ignored, float clearance)
        {
            visualRoot = root != null ? root : transform;
            ignoredRoot = ignored;
            groundClearance = Mathf.Max(0f, clearance);
            _usesObjectRoot = visualRoot == transform;
            _baseLocalPosition = visualRoot.localPosition;
            RebuildRootVisualCache();
            RebuildGroundReferences();
            AlignNow();
        }

        public void SetIgnoredRoot(Transform ignored)
        {
            ignoredRoot = ignored;
            RebuildRootVisualCache();
        }

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            _usesObjectRoot = visualRoot == transform;
            _baseLocalPosition = visualRoot.localPosition;
            RebuildRootVisualCache();
            RebuildGroundReferences();
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (_usesObjectRoot)
            {
                ResetRootVisualChildren();
            }
            else
            {
                visualRoot.localPosition = _baseLocalPosition;
            }

            AlignNow();
        }

        public void AlignNow()
        {
            if (visualRoot == null || !TryGetVisualGroundY(out float visualGroundY))
            {
                return;
            }

            float targetMinY = FindGroundY() + groundClearance;
            float yOffset = targetMinY - visualGroundY;
            if (Mathf.Abs(yOffset) <= 0.001f)
            {
                return;
            }

            Vector3 offset = Vector3.up * yOffset;
            if (_usesObjectRoot)
            {
                OffsetRootVisualChildren(offset);
                return;
            }

            visualRoot.position += offset;
            if (!_usesObjectRoot)
            {
                _baseLocalPosition = visualRoot.localPosition;
            }
        }

        private void RebuildRootVisualCache()
        {
            if (!_usesObjectRoot)
            {
                _rootVisualChildren = new Transform[0];
                _rootVisualBaseLocalPositions = new Vector3[0];
                return;
            }

            List<Transform> children = new List<Transform>(transform.childCount);
            List<Vector3> localPositions = new List<Vector3>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (ignoredRoot != null && (child == ignoredRoot || child.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                children.Add(child);
                localPositions.Add(child.localPosition);
            }

            _rootVisualChildren = children.ToArray();
            _rootVisualBaseLocalPositions = localPositions.ToArray();
        }

        private void RebuildGroundReferences()
        {
            if (visualRoot == null)
            {
                _groundReferencePoints = new Transform[0];
                return;
            }

            List<Transform> references = new List<Transform>();
            Transform[] children = visualRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == null || (ignoredRoot != null && child.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                if (IsGroundReferenceName(child.name))
                {
                    references.Add(child);
                }
            }

            _groundReferencePoints = references.ToArray();
        }

        private void ResetRootVisualChildren()
        {
            int count = Mathf.Min(_rootVisualChildren.Length, _rootVisualBaseLocalPositions.Length);
            for (int i = 0; i < count; i++)
            {
                Transform child = _rootVisualChildren[i];
                if (child == null)
                {
                    continue;
                }

                child.localPosition = _rootVisualBaseLocalPositions[i];
            }
        }

        private void OffsetRootVisualChildren(Vector3 offset)
        {
            for (int i = 0; i < _rootVisualChildren.Length; i++)
            {
                Transform child = _rootVisualChildren[i];
                if (child == null)
                {
                    continue;
                }

                child.position += offset;
            }
        }

        private bool TryGetVisualGroundY(out float groundY)
        {
            if (TryGetLowestGroundReferenceY(out groundY))
            {
                return true;
            }

            if (TryGetVisualBounds(out Bounds bounds))
            {
                groundY = bounds.min.y;
                return true;
            }

            groundY = 0f;
            return false;
        }

        private bool TryGetLowestGroundReferenceY(out float groundY)
        {
            groundY = float.MaxValue;
            bool found = false;
            for (int i = 0; i < _groundReferencePoints.Length; i++)
            {
                Transform point = _groundReferencePoints[i];
                if (point == null)
                {
                    continue;
                }

                groundY = Mathf.Min(groundY, point.position.y);
                found = true;
            }

            if (!found)
            {
                groundY = 0f;
            }

            return found;
        }

        private static bool IsGroundReferenceName(string transformName)
        {
            return transformName == "LeftFoot"
                   || transformName == "RightFoot"
                   || transformName == "LeftToeBase"
                   || transformName == "RightToeBase";
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
            float groundY = transform.position.y;
            bool foundGround = false;

            if (NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit navMeshHit,
                    raycastHeight + raycastDistance,
                    NavMesh.AllAreas))
            {
                groundY = navMeshHit.position.y;
                foundGround = true;
            }

            Vector3 origin = transform.position + Vector3.up * raycastHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                raycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = GroundHits[i].transform;
                if (hitTransform == null || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (GroundHits[i].normal.y < 0.45f)
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
