using UnityEngine;

namespace CWH.Player.Core
{
    public sealed class WallDetector : IWallDetector
    {
        private readonly LayerMask _wallMask;
        private readonly float _detectionDistance;

        public WallDetector(LayerMask wallMask, float detectionDistance)
        {
            _wallMask = wallMask;
            _detectionDistance = detectionDistance;
        }

        public bool TryDetectWall(Transform origin, out WallHit hit)
        {
            if (Physics.Raycast(origin.position, origin.right, out var rightHit, _detectionDistance, _wallMask, QueryTriggerInteraction.Ignore))
            {
                hit = new WallHit(WallSide.Right, rightHit.normal, rightHit.point);
                return true;
            }

            if (Physics.Raycast(origin.position, -origin.right, out var leftHit, _detectionDistance, _wallMask, QueryTriggerInteraction.Ignore))
            {
                hit = new WallHit(WallSide.Left, leftHit.normal, leftHit.point);
                return true;
            }

            hit = WallHit.None;
            return false;
        }
    }
}
