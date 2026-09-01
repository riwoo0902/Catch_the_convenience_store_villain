using System;
using CWH.Player.Interaction;
using UnityEngine;

namespace CWH.Player.Health
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        [SerializeField, Min(0f)] private float _hitInvulnerabilityTime = 0.35f;

        [Header("Continuous Effects")]
        [SerializeField, Min(0f)] private float _unorganizedDamagePerSecond = 2f;
        [SerializeField, Min(0f)] private float _youtubeHealingPerSecond = 5f;

        private float _currentHealth;
        private float _nextHitAllowedTime;
        private bool _isWatchingYoutube;

        public static PlayerHealth Instance { get; private set; }
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetInstance()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnPlayer()
        {
            GetOrCreate();
        }

        public static PlayerHealth GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            PlayerHealth existing = FindFirstObjectByType<PlayerHealth>();
            if (existing != null)
            {
                return existing;
            }

            Transform playerRoot = FindPlayerRoot();
            return playerRoot != null ? playerRoot.gameObject.AddComponent<PlayerHealth>() : null;
        }

        private static Transform FindPlayerRoot()
        {
            GameObject namedPlayer = GameObject.Find("Player");
            if (namedPlayer != null)
            {
                return namedPlayer.transform;
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                for (Transform current = camera.transform; current != null; current = current.parent)
                {
                    if (string.Equals(current.name, "Player", StringComparison.Ordinal))
                    {
                        return current;
                    }
                }
            }

            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _currentHealth = _maxHealth;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (ShelfProductInteraction.UnorganizedProductCount > 0)
            {
                ChangeHealth(-_unorganizedDamagePerSecond * deltaTime);
            }

            if (_isWatchingYoutube)
            {
                ChangeHealth(_youtubeHealingPerSecond * deltaTime);
            }
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || IsDead || Time.time < _nextHitAllowedTime)
            {
                return;
            }

            _nextHitAllowedTime = Time.time + _hitInvulnerabilityTime;
            ChangeHealth(-damage);
        }

        public void Heal(float amount)
        {
            if (amount > 0f && !IsDead)
            {
                ChangeHealth(amount);
            }
        }

        public void SetYoutubeHealing(bool isWatching)
        {
            _isWatchingYoutube = isWatching;
        }

        private void ChangeHealth(float delta)
        {
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + delta, 0f, _maxHealth);
            if (Mathf.Approximately(previousHealth, _currentHealth))
            {
                return;
            }

            HealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (previousHealth > 0f && IsDead)
            {
                _isWatchingYoutube = false;
                Died?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
