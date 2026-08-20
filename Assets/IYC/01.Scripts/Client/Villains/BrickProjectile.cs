using UnityEngine;

namespace Villains
{
    [RequireComponent(typeof(Rigidbody))]
    public class BrickProjectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private int damage = 1;
        [SerializeField] private string targetTag = "Player";

        private Rigidbody _rigidbody;
        private float _spawnTime;
        private bool _isInitialized;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time >= _spawnTime + lifeTime)
                Destroy(gameObject);
        }

        public void Launch(Vector3 velocity, int projectileDamage, float projectileLifeTime)
        {
            damage = projectileDamage;
            lifeTime = projectileLifeTime;
            _spawnTime = Time.time;
            _isInitialized = true;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.linearVelocity = velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isInitialized)
                return;

            if (collision.collider.CompareTag(targetTag))
                Debug.Log($"Brick hit {collision.collider.name}. Damage: {damage}", this);

            Destroy(gameObject);
        }
    }
}
