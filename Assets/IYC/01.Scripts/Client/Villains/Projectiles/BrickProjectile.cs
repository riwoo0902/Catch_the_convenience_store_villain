using System;
using IYC._01.Scripts.CoreSystem.Module;
using UnityEngine;

namespace Villains.Projectiles
{
    [RequireComponent(typeof(Rigidbody))]
    public class BrickProjectile : MonoBehaviour
    {
        public static event Action<Transform> HitTarget;

        private Rigidbody _rigidbody;
        private ModuleOwner _owner;
        private LayerMask _excludeLayer;
        private float _lifeTime;
        private int _damage;
        private bool _isInitialized;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void InitProjectile(ModuleOwner owner, int damage, float lifeTime, Vector3 velocity, LayerMask excludeLayer)
        {
            _owner = owner;
            _damage = damage;
            _lifeTime = lifeTime;
            _excludeLayer = excludeLayer;
            _isInitialized = true;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.AddTorque(velocity, ForceMode.Impulse);

            Destroy(gameObject, _lifeTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        private void HandleHit(Collider hitCollider)
        {
            if (!_isInitialized || hitCollider == null || IsExcluded(hitCollider.gameObject))
                return;

            if (_owner != null && hitCollider.transform.root == _owner.transform)
                return;

            HitTarget?.Invoke(hitCollider.transform);
            hitCollider.SendMessageUpwards("TakeDamage", _damage, SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject);
        }

        private bool IsExcluded(GameObject target)
        {
            return (_excludeLayer.value & (1 << target.layer)) != 0;
        }
    }
}
