using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;
using Villains.Data;
using Villains.Projectiles;

namespace Villains.Combat
{
    public class BrickThrowAttack : MonoBehaviour, IModule
    {
        [SerializeField] private BrickThrowDataSO throwData;
        [SerializeField] private BrickProjectile brickPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float aimHeightOffset = 1.2f;
        [SerializeField] private LayerMask excludeLayer;

        private ModuleOwner _owner;
        private float _lastAttackTime = -999f;

        public float AttackRange => throwData != null ? throwData.attackRange : 0f;
        public bool CanAttack => throwData != null && brickPrefab != null && Time.time >= _lastAttackTime + throwData.cooldown;

        public void Init(ModuleOwner owner)
        {
            _owner = owner;
        }

        public void ThrowAt(Transform target)
        {
            if (!CanAttack || target == null)
                return;

            Transform spawnTransform = throwPoint != null ? throwPoint : transform;
            Vector3 spawnPosition = spawnTransform.position;
            Vector3 targetPosition = target.position + Vector3.up * aimHeightOffset;
            Vector3 velocity = BuildInitialVelocity(spawnPosition, targetPosition);

            BrickProjectile projectile = Instantiate(brickPrefab, spawnPosition, Quaternion.LookRotation(velocity.normalized));
            projectile.InitProjectile(_owner, throwData.damage, throwData.projectileLifeTime, velocity, excludeLayer);
            _lastAttackTime = Time.time;
        }

        private Vector3 BuildInitialVelocity(Vector3 origin, Vector3 targetPosition)
        {
            Vector3 offset = targetPosition - origin;
            Vector3 flatOffset = new Vector3(offset.x, 0f, offset.z);
            float distance = flatOffset.magnitude;
            Vector3 flatDirection = distance > 0.001f ? flatOffset / distance : transform.forward;

            float ratio = Mathf.InverseLerp(0f, throwData.attackRange, distance);
            float speedT = throwData.speedCurve != null ? throwData.speedCurve.Evaluate(ratio) : ratio;
            float pitchT = throwData.pitchCurve != null ? throwData.pitchCurve.Evaluate(ratio) : ratio;

            float speed = Mathf.Lerp(throwData.minSpeed, throwData.maxSpeed, speedT);
            float pitch = Mathf.Lerp(throwData.minPitchDeg, throwData.maxPitchDeg, pitchT) * Mathf.Deg2Rad;

            Vector3 horizontalVelocity = flatDirection * (speed * Mathf.Cos(pitch));
            return horizontalVelocity + Vector3.up * (speed * Mathf.Sin(pitch));
        }
    }
}
