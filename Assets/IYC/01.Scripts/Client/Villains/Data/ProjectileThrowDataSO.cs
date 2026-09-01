using UnityEngine;

namespace Villains.Data
{
    [CreateAssetMenu(fileName = "Projectile throw data", menuName = "Villain/Projectile Throw Data", order = 0)]
    public class ProjectileThrowDataSO : ScriptableObject
    {
        [Header("Combat")]
        public float attackRange = 7f;
        public float cooldown = 2f;
        public int damage = 1;
        public float projectileLifeTime = 5f;
        [Range(0f, 1f)] public float releaseNormalizedTime = 0.55f;
        public float maxAnimationWaitTime = 2f;

        [Header("Trajectory")]
        public float minSpeed = 7f;
        public float maxSpeed = 14f;
        public float minPitchDeg = 8f;
        public float maxPitchDeg = 35f;
        public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve pitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
