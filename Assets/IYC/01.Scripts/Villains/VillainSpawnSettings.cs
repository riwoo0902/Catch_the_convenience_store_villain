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
        [SerializeField] private GameObject _productDisturberVisualPrefab;
        [SerializeField] private GameObject _chefVillainVisualPrefab;
        [SerializeField] private Projectile _spatulaProjectilePrefab;
        [SerializeField] private GameObject _spatulaProjectileVisualPrefab;
        [SerializeField] private ProjectileThrowDataSO _spatulaThrowData;

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
        [SerializeField, Min(0.1f)] private float _roamSpeed = 2.2f;
        [SerializeField, Min(0.25f)] private float _roamPointReachDistance = 0.55f;
        [SerializeField, Min(0f)] private float _minimumRoamWait = 0.5f;
        [SerializeField, Min(0f)] private float _maximumRoamWait = 2f;
        [SerializeField, Min(0f)] private float _minimumMischiefDelay = 4f;
        [SerializeField, Min(0f)] private float _maximumMischiefDelay = 10f;
        [SerializeField, Min(0.5f)] private float _fallbackRoamRadius = 6f;

        [Header("Product Disturber")]
        [SerializeField, Range(0f, 1f)] private float _productDisturberSpawnChance = 0.4f;
        [SerializeField, Min(0.1f)] private float _productDisturbInterval = 2.5f;
        [SerializeField, Min(0.1f)] private float _productDisturbRadius = 1.6f;
        [SerializeField] private Vector3 _productDisturbMaxPositionOffset = new Vector3(0.22f, 0.08f, 0.2f);
        [SerializeField] private Vector3 _productDisturbMaxRotationOffset = new Vector3(35f, 55f, 35f);

        [Header("Chef Villain")]
        [SerializeField, Range(0f, 1f)] private float _chefVillainSpawnChance = 0.25f;
        [SerializeField] private Vector3 _spatulaProjectileVisualLocalScale = Vector3.one * 0.12f;
        [SerializeField] private Vector3 _spatulaProjectileVisualLocalRotation = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 _spatulaProjectileColliderSize = new Vector3(0.65f, 0.12f, 0.3f);

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
        public GameObject PoliceWeaponPrefab => _policeWeaponPrefab;
        public GameObject ProductDisturberVisualPrefab => _productDisturberVisualPrefab != null ? _productDisturberVisualPrefab : _villainVisualPrefab;
        public GameObject ChefVillainVisualPrefab => _chefVillainVisualPrefab != null ? _chefVillainVisualPrefab : _villainVisualPrefab;
        public Projectile SpatulaProjectilePrefab => _spatulaProjectilePrefab;
        public GameObject SpatulaProjectileVisualPrefab => _spatulaProjectileVisualPrefab;
        public ProjectileThrowDataSO SpatulaThrowData => _spatulaThrowData != null ? _spatulaThrowData : _throwData;
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
        public float RoamSpeed => _roamSpeed;
        public float RoamPointReachDistance => _roamPointReachDistance;
        public float MinimumRoamWait => _minimumRoamWait;
        public float MaximumRoamWait => Mathf.Max(_minimumRoamWait, _maximumRoamWait);
        public float MinimumMischiefDelay => _minimumMischiefDelay;
        public float MaximumMischiefDelay => Mathf.Max(_minimumMischiefDelay, _maximumMischiefDelay);
        public float FallbackRoamRadius => _fallbackRoamRadius;
        public float ProductDisturberSpawnChance => _productDisturberSpawnChance;
        public float ProductDisturbInterval => _productDisturbInterval;
        public float ProductDisturbRadius => _productDisturbRadius;
        public Vector3 ProductDisturbMaxPositionOffset => _productDisturbMaxPositionOffset;
        public Vector3 ProductDisturbMaxRotationOffset => _productDisturbMaxRotationOffset;
        public float ChefVillainSpawnChance => _chefVillainSpawnChance;
        public Vector3 SpatulaProjectileVisualLocalScale => _spatulaProjectileVisualLocalScale;
        public Vector3 SpatulaProjectileVisualLocalRotation => _spatulaProjectileVisualLocalRotation;
        public Vector3 SpatulaProjectileColliderSize => _spatulaProjectileColliderSize;
        public float PoliceAttackRange => _policeAttackRange;
        public float PoliceAttackInterval => _policeAttackInterval;
        public float PoliceAttackHitDelay => _policeAttackHitDelay;
        public float PoliceAttackLockDuration => _policeAttackLockDuration;
        public Vector3 PoliceWeaponLocalPosition => _policeWeaponLocalPosition;
        public Vector3 PoliceWeaponLocalRotation => _policeWeaponLocalRotation;
        public Vector3 PoliceWeaponLocalScale => _policeWeaponLocalScale;
    }
}
