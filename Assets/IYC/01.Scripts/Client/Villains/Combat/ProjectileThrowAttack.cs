using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;
using UnityEngine.Serialization;
using Villains.Data;
using Villains.Projectiles;

namespace Villains.Combat
{
    public class ProjectileThrowAttack : MonoBehaviour, IModule
    {
        [SerializeField] private ProjectileThrowDataSO throwData;
        [FormerlySerializedAs("brickPrefab")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private float minimumSpawnHeightFromOwner = 0f;
        [SerializeField] private Vector3 projectileRotationOffset = Vector3.zero;
        [SerializeField] private float aimHeightOffset = 1.2f;
        [SerializeField] private LayerMask excludeLayer;

        private ModuleOwner _owner;
        private float _lastAttackTime = -999f;

        public float AttackRange => throwData != null ? throwData.attackRange : 0f;
        public float ReleaseNormalizedTime => throwData != null ? throwData.releaseNormalizedTime : 0.55f;
        public float MaxAnimationWaitTime => throwData != null ? throwData.maxAnimationWaitTime : 2f;
        public bool CanAttack => throwData != null && projectilePrefab != null && Time.time >= _lastAttackTime + throwData.cooldown;

        public void Init(ModuleOwner owner)
        {
            _owner = owner;
        }

        public void ThrowAt(Transform target)
        {
            if (!CanAttack || target == null)
                return;

            Transform spawnTransform = throwPoint != null ? throwPoint : transform;
            Vector3 spawnPosition = BuildSpawnPosition(spawnTransform);
            Vector3 targetPosition = target.position + Vector3.up * aimHeightOffset;
            Vector3 velocity = BuildInitialVelocity(spawnPosition, targetPosition);

            Quaternion projectileRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up)
                                            * Quaternion.Euler(projectileRotationOffset);
            Projectile projectile = Instantiate(projectilePrefab, spawnPosition, projectileRotation);
            projectile.InitProjectile(_owner, throwData.damage, throwData.projectileLifeTime, velocity, excludeLayer);
            _lastAttackTime = Time.time;
        }

        private Vector3 BuildSpawnPosition(Transform spawnTransform)
        {
            Transform offsetSpace = _owner != null ? _owner.transform : spawnTransform;
            Vector3 spawnPosition = spawnTransform.position + offsetSpace.TransformVector(spawnOffset);

            if (_owner != null && minimumSpawnHeightFromOwner > 0f)
            {
                float minimumHeight = _owner.transform.position.y + minimumSpawnHeightFromOwner;
                spawnPosition.y = Mathf.Max(spawnPosition.y, minimumHeight);
            }

            return spawnPosition;
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
