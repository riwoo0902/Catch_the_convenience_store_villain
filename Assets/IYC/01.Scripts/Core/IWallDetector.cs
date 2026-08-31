using UnityEngine;

namespace CWH.Player.Core
{
    public interface IWallDetector
    {
        bool TryDetectWall(Transform origin, out WallHit hit);
    }
}
