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
        [SerializeField] private GameObject _policePrefab;
        [SerializeField] private GameObject _policeWeaponPrefab;

        [Header("Appearance")]
        [SerializeField, Min(0.1f)] private float _visualScale = 3f;
        [SerializeField, Min(0.1f)] private float _policeVisualScale = 2.2f;
        [SerializeField] private float _policeVisualYOffset = 0.05f;
        [SerializeField, Min(0f)] private float _policeGroundClearance = 0.03f;

        [Header("Arrival")]
        [SerializeField, Min(0f)] private float _minimumSpawnDelay = 10f;
        [SerializeField, Min(0f)] private float _maximumSpawnDelay = 20f;
        [SerializeField, Min(1f)] private float _spawnDistanceBehindPlayer = 10f;
        [SerializeField, Min(0.5f)] private float _doorInsideDistance = 2f;
        [SerializeField, Min(0.5f)] private float _doorOutsideDistance = 4f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _chaseSpeed = 5f;
        [SerializeField, Min(0.1f)] private float _fleeSpeed = 7f;
        [SerializeField, Min(0.5f)] private float _preferredAttackDistance = 10.5f;

        [Header("Police Combat")]
        [SerializeField, Min(0.5f)] private float _policeAttackRange = 2f;
        [SerializeField, Min(0.1f)] private float _policeAttackInterval = 1.15f;
        [SerializeField, Min(0f)] private float _policeAttackHitDelay = 0.35f;
        [SerializeField, Min(0.1f)] private float _policeAttackLockDuration = 0.9f;
        [SerializeField] private Vector3 _policeWeaponLocalPosition = new Vector3(0.02f, 0.04f, 0.12f);
        [SerializeField] private Vector3 _policeWeaponLocalRotation = new Vector3(15f, 90f, 80f);
        [SerializeField] private Vector3 _policeWeaponLocalScale = Vector3.one * 0.7f;

        public GameObject VillainVisualPrefab => _villainVisualPrefab;
        public BrickProjectile BrickPrefab => _brickPrefab;
        public BrickThrowDataSO ThrowData => _throwData;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public GameObject PolicePrefab => _policePrefab;
        public GameObject PoliceWeaponPrefab => _policeWeaponPrefab != null ? _policeWeaponPrefab : _brickPrefab != null ? _brickPrefab.gameObject : null;
        public float VisualScale => _visualScale;
        public float PoliceVisualScale => _policeVisualScale;
        public float PoliceVisualYOffset => _policeVisualYOffset;
        public float PoliceGroundClearance => _policeGroundClearance;
        public float MinimumSpawnDelay => _minimumSpawnDelay;
        public float MaximumSpawnDelay => Mathf.Max(_minimumSpawnDelay, _maximumSpawnDelay);
        public float SpawnDistanceBehindPlayer => _spawnDistanceBehindPlayer;
        public float DoorInsideDistance => _doorInsideDistance;
        public float DoorOutsideDistance => _doorOutsideDistance;
        public float ChaseSpeed => _chaseSpeed;
        public float FleeSpeed => _fleeSpeed;
        public float PreferredAttackDistance => _preferredAttackDistance;
        public float PoliceAttackRange => _policeAttackRange;
        public float PoliceAttackInterval => _policeAttackInterval;
        public float PoliceAttackHitDelay => _policeAttackHitDelay;
        public float PoliceAttackLockDuration => _policeAttackLockDuration;
        public Vector3 PoliceWeaponLocalPosition => _policeWeaponLocalPosition;
        public Vector3 PoliceWeaponLocalRotation => _policeWeaponLocalRotation;
        public Vector3 PoliceWeaponLocalScale => _policeWeaponLocalScale;
    }
}
