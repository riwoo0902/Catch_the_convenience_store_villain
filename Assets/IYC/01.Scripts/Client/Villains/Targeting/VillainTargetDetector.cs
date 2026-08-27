using IYC._01.Scripts.CoreSystem.Module;
using Player;
using UnityEngine;

namespace Villains.Targeting
{
    public class VillainTargetDetector : MonoBehaviour, IModule
    {
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private float closeDetectionRange = 2f;
        [SerializeField, Range(0f, 360f)] private float viewAngle = 130f;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Transform eyePoint;

        private readonly Collider[] _detectedColliders = new Collider[16];
        private VillainTargetProvider _targetProvider;

        private Vector3 EyePosition => eyePoint != null ? eyePoint.position : transform.position + Vector3.up;

        public void Init(ModuleOwner owner)
        {
            _targetProvider = owner.GetModule<VillainTargetProvider>();
        }

        private void Update()
        {
            if (_targetProvider == null)
                return;

            if (_targetProvider.Target != null)
            {
                if (!IsTargetVisible(_targetProvider.Target))
                    _targetProvider.LoseVisibleTarget();
                return;
            }

            Transform target = FindClosestVisibleTarget();
            if (target != null)
                _targetProvider.SetTarget(target);
        }

        private Transform FindClosestVisibleTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                EyePosition,
                detectionRange,
                _detectedColliders,
                targetLayer
            );

            float closestDistance = float.MaxValue;
            Transform closestTarget = null;

            for (int i = 0; i < count; i++)
            {
                Collider detectedCollider = _detectedColliders[i];
                if (detectedCollider == null)
                    continue;

                Transform target = ResolveTargetTransform(detectedCollider);
                if (target == null || !IsTargetVisible(target))
                    continue;

                float distance = Vector3.Distance(EyePosition, target.position);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = target;
            }

            return closestTarget;
        }

        private Transform ResolveTargetTransform(Collider detectedCollider)
        {
            PlayerController player = detectedCollider.GetComponentInParent<PlayerController>();
            if (player != null)
                return player.transform;

            return detectedCollider.attachedRigidbody != null
                ? detectedCollider.attachedRigidbody.transform
                : detectedCollider.transform;
        }

        public bool IsTargetVisible(Transform target)
        {
            if (target == null)
                return false;

            Vector3 targetPosition = target.position;
            targetPosition.y = EyePosition.y;

            Vector3 direction = targetPosition - EyePosition;
            float distance = direction.magnitude;

            if (distance > detectionRange)
                return false;

            if (distance > closeDetectionRange)
            {
                Vector3 flatDirection = direction;
                flatDirection.y = 0f;
                if (Vector3.Angle(transform.forward, flatDirection.normalized) > viewAngle * 0.5f)
                    return false;
            }

            return !Physics.Raycast(EyePosition, direction.normalized, distance, obstacleLayer);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, detectionRange);
        }
    }
}
