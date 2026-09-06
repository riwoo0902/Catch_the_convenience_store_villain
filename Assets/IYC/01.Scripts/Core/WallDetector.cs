using UnityEngine;

namespace CWH.Player.Core
{
    public sealed class WallDetector : IWallDetector
    {
        private readonly LayerMask _wallMask;
        private readonly float _detectionDistance;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public WallDetector(LayerMask wallMask, float detectionDistance)
        {
            _wallMask = wallMask;
            _detectionDistance = detectionDistance;
        }

        public bool TryDetectWall(Transform origin, out WallHit hit)
        {
            if (TryDetectSide(origin, origin.right, out var rightHit))
            {
                hit = new WallHit(WallSide.Right, rightHit.normal, rightHit.point);
                return true;
            }

            if (TryDetectSide(origin, -origin.right, out var leftHit))
            {
                hit = new WallHit(WallSide.Left, leftHit.normal, leftHit.point);
                return true;
            }

            hit = WallHit.None;
            return false;
        }

        private bool TryDetectSide(Transform origin, Vector3 direction, out RaycastHit wallHit)
        {
            int count = Physics.RaycastNonAlloc(origin.position, direction, _hits,
                _detectionDistance, _wallMask, QueryTriggerInteraction.Ignore);
            RaycastHit[] hits = _hits;
            if (count == _hits.Length)
            {
                hits = Physics.RaycastAll(origin.position, direction, _detectionDistance,
                    _wallMask, QueryTriggerInteraction.Ignore);
                count = hits.Length;
            }

            wallHit = default;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = hits[i];
                Collider collider = candidate.collider;
                // Wall-running uses solid scenery, never projectiles, characters or held objects.
                if (collider.attachedRigidbody != null
                    || collider.transform.IsChildOf(origin)
                    || collider.GetComponentInParent<CharacterController>() != null
                    || Mathf.Abs(candidate.normal.y) > 0.2f
                    || candidate.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = candidate.distance;
                wallHit = candidate;
            }

            return nearestDistance < float.PositiveInfinity;
        }
    }
}
