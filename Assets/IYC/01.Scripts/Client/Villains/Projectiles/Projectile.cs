using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;
using Villains.Environment;

namespace Villains.Projectiles
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private Vector3 spinAxis = Vector3.forward;
        [SerializeField] private float spinSpeed = 720f;
        [SerializeField] private bool disturbShelfProductsOnHit = true;
        [SerializeField] private Vector3 shelfProductMaxPositionOffset = new Vector3(0.22f, 0.08f, 0.2f);
        [SerializeField] private Vector3 shelfProductMaxRotationOffset = new Vector3(35f, 55f, 35f);

        private Rigidbody _rigidbody;
        private ModuleOwner _owner;
        private LayerMask _excludeLayer;
        private int _damage;
        private bool _isInitialized;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void InitProjectile(ModuleOwner owner, int damage, float lifeTime, Vector3 velocity, LayerMask excludeLayer)
        {
            _owner = owner;
            _damage = damage;
            _excludeLayer = excludeLayer;
            _isInitialized = true;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            ApplySpin();

            Destroy(gameObject, lifeTime);
        }

        private void ApplySpin()
        {
            if (spinSpeed <= 0f || spinAxis.sqrMagnitude <= 0.001f)
                return;

            Vector3 worldSpinAxis = transform.TransformDirection(spinAxis.normalized);
            _rigidbody.angularVelocity = worldSpinAxis * (spinSpeed * Mathf.Deg2Rad);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        private void HandleHit(Collider hitCollider)
        {
            if (!_isInitialized || hitCollider == null || IsExcluded(hitCollider.gameObject))
                return;

            if (_owner != null && hitCollider.transform.root == _owner.transform)
                return;

            TryDisturbShelfProduct(hitCollider.transform);
            hitCollider.SendMessageUpwards("TakeDamage", _damage, SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject);
        }

        private void TryDisturbShelfProduct(Transform hitTransform)
        {
            if (!disturbShelfProductsOnHit)
                return;

            ShelfProductDisturbance.TryDisturb(
                hitTransform,
                shelfProductMaxPositionOffset,
                shelfProductMaxRotationOffset);
        }

        private bool IsExcluded(GameObject target)
        {
            return (_excludeLayer.value & (1 << target.layer)) != 0;
        }
    }
}
