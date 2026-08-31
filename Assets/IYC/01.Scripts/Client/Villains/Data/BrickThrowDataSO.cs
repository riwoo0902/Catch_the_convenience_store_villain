using UnityEngine;

namespace Villains.Data
{
    [CreateAssetMenu(fileName = "Brick throw data", menuName = "Villain/Brick Throw Data", order = 0)]
    public class BrickThrowDataSO : ScriptableObject
    {
        [Header("Combat")]
        public float attackRange = 7f;
        public float cooldown = 2f;
        public int damage = 1;
        public float projectileLifeTime = 5f;

        [Header("Trajectory")]
        public float minSpeed = 7f;
        public float maxSpeed = 14f;
        public float minPitchDeg = 8f;
        public float maxPitchDeg = 35f;
        public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve pitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
