using UnityEngine;
using CWH.Player.Config;

namespace CWH.Player.States
{
    /// Pure calculation, not a state: resolves the outgoing velocity when a player
    /// jumps off a wall while wall-running.
    public static class WallJumpResolver
    {
        public static Vector3 Resolve(Vector3 wallNormal, Vector3 currentHorizontalVelocity, WallRunConfig config)
        {
            var outward = wallNormal * config.JumpOutwardForce;
            return new Vector3(
                currentHorizontalVelocity.x + outward.x,
                config.JumpUpwardForce,
                currentHorizontalVelocity.z + outward.z);
        }
    }
}
