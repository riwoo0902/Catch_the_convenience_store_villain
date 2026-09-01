using CWH.Player.Health;
using UnityEngine;

namespace CWH.Player.Combat
{
    [DisallowMultipleComponent]
    public sealed class PlayerDamageOnCollision : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _damage = 15;
        [SerializeField, Min(0f)] private float _minimumImpactSpeed = 1f;
        [SerializeField, Min(0f)] private float _hitCooldown = 0.75f;

        private float _nextHitAllowedTime;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < _minimumImpactSpeed)
            {
                return;
            }

            TryDamage(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider targetCollider)
        {
            if (Time.time < _nextHitAllowedTime)
            {
                return;
            }

            PlayerHealth playerHealth = targetCollider.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }

            _nextHitAllowedTime = Time.time + _hitCooldown;
            playerHealth.TakeDamage(_damage);
        }
    }
}
