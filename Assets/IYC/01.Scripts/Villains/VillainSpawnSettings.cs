using UnityEngine;
using Villains.Data;
using Villains.Projectiles;

namespace CWH.Villains
{
    [CreateAssetMenu(fileName = "VillainSpawnSettings", menuName = "CWH/Villain Spawn Settings")]
    public sealed class VillainSpawnSettings : ScriptableObject
    {
        [Header("Assets")]
        [SerializeField] private GameObject _villainVisualPrefab;
        [SerializeField] private BrickProjectile _brickPrefab;
        [SerializeField] private BrickThrowDataSO _throwData;
        [SerializeField] private RuntimeAnimatorController _animatorController;

        [Header("Appearance")]
        [SerializeField, Min(0.1f)] private float _visualScale = 3f;

        [Header("Arrival")]
        [SerializeField, Min(0f)] private float _minimumSpawnDelay = 10f;
        [SerializeField, Min(0f)] private float _maximumSpawnDelay = 20f;
        [SerializeField, Min(1f)] private float _spawnDistanceBehindPlayer = 10f;
        [SerializeField, Min(0.5f)] private float _doorInsideDistance = 2f;
        [SerializeField, Min(0.5f)] private float _doorOutsideDistance = 4f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _chaseSpeed = 5f;
        [SerializeField, Min(0.1f)] private float _fleeSpeed = 7f;
        [SerializeField, Min(0.5f)] private float _preferredAttackDistance = 5.5f;

        public GameObject VillainVisualPrefab => _villainVisualPrefab;
        public BrickProjectile BrickPrefab => _brickPrefab;
        public BrickThrowDataSO ThrowData => _throwData;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public float VisualScale => _visualScale;
        public float MinimumSpawnDelay => _minimumSpawnDelay;
        public float MaximumSpawnDelay => Mathf.Max(_minimumSpawnDelay, _maximumSpawnDelay);
        public float SpawnDistanceBehindPlayer => _spawnDistanceBehindPlayer;
        public float DoorInsideDistance => _doorInsideDistance;
        public float DoorOutsideDistance => _doorOutsideDistance;
        public float ChaseSpeed => _chaseSpeed;
        public float FleeSpeed => _fleeSpeed;
        public float PreferredAttackDistance => _preferredAttackDistance;
    }
}
