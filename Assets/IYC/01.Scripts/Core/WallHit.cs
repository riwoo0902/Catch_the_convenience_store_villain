using UnityEngine;

namespace CWH.Player.Core
{
    public readonly struct WallHit
    {
        public readonly WallSide Side;
        public readonly Vector3 Normal;
        public readonly Vector3 Point;

        public WallHit(WallSide side, Vector3 normal, Vector3 point)
        {
            Side = side;
            Normal = normal;
            Point = point;
        }

        public static WallHit None => new WallHit(WallSide.None, Vector3.zero, Vector3.zero);
    }
}
