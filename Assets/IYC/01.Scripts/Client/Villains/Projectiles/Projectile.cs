using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Villains.Projectiles
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private Vector3 spinAxis = Vector3.forward;
        [SerializeField] private float spinSpeed = 720f;

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
            if (!_isInitialized || IsExcluded(collision.collider.gameObject))
                return;

            if (_owner != null && collision.collider.transform.root == _owner.transform)
                return;

            collision.collider.SendMessageUpwards("TakeDamage", _damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }

        private bool IsExcluded(GameObject target)
        {
            return (_excludeLayer.value & (1 << target.layer)) != 0;
        }
    }
}
