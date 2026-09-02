using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Villains.Visuals;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    public sealed class PoliceResponseController : MonoBehaviour
    {
        private static readonly string[] EntranceDoorNames =
        {
            "automaticDoor_L_gp",
            "automaticDoor_R_gp",
            "automaticDoorFrame"
        };

        [SerializeField, Min(1f)] private float spawnDistanceFromPlayer = 16f;
        [SerializeField, Min(0.5f)] private float arriveDistance = 1.4f;
        [SerializeField, Min(0.1f)] private float policeRunSpeed = 8.5f;
        [SerializeField, Min(0f)] private float cooldown = 1f;

        private static PoliceResponseController s_instance;
        private float _nextCallAllowedTime;
        private RuntimePoliceOfficer _activePolice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<PoliceResponseController>() != null)
            {
                return;
            }

            if (GameObject.Find("Player") == null)
            {
                return;
            }

            GameObject controllerObject = new("Police Response Controller");
            controllerObject.AddComponent<PoliceResponseController>();
        }

        public static void RequestPoliceResponse()
        {
            PoliceResponseController controller = s_instance != null
                ? s_instance
                : FindFirstObjectByType<PoliceResponseController>();

            if (controller == null)
            {
                GameObject controllerObject = new("Police Response Controller");
                controller = controllerObject.AddComponent<PoliceResponseController>();
            }

            controller.CallPolice();
        }

        public void CallPolice()
        {
            if (Time.time < _nextCallAllowedTime)
            {
                return;
            }

            _nextCallAllowedTime = Time.time + cooldown;
            ConvenienceStoreVillainSpawner.RequestAllVillainsFlee();
            SpawnOrRetargetPolice();
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
        }

        private void SpawnOrRetargetPolice()
        {
            Transform player = FindPlayer();
            if (player == null)
            {
                return;
            }

            ResolvePoliceRoute(player, out Vector3 spawnPosition, out Vector3 destination);
            if (_activePolice != null)
            {
                _activePolice.SetDestination(destination);
                return;
            }

            VillainSpawnSettings settings = Resources.Load<VillainSpawnSettings>("VillainSpawnSettings");
            GameObject policeObject = RuntimePoliceOfficer.Create(
                spawnPosition,
                settings != null ? settings.PolicePrefab : null,
                settings != null ? settings.AnimatorController : null,
                settings != null ? settings.PoliceWeaponPrefab : null,
                settings != null ? settings.PoliceWeaponLocalPosition : new Vector3(0.02f, 0.04f, 0.12f),
                settings != null ? settings.PoliceWeaponLocalRotation : new Vector3(15f, 90f, 80f),
                settings != null ? settings.PoliceWeaponLocalScale : Vector3.one * 0.7f,
                settings != null ? settings.PoliceVisualScale : 2.2f,
                settings != null ? settings.PoliceVisualYOffset : 0.05f,
                settings != null ? settings.PoliceGroundClearance : 0.03f);
            _activePolice = policeObject.GetComponent<RuntimePoliceOfficer>();
            _activePolice.Initialize(
                destination,
                policeRunSpeed,
                arriveDistance,
                settings != null ? settings.PoliceAttackRange : 2f,
                settings != null ? settings.PoliceAttackInterval : 1.15f,
                settings != null ? settings.PoliceAttackHitDelay : 0.35f,
                settings != null ? settings.PoliceAttackLockDuration : 0.9f);
        }

        private void ResolvePoliceRoute(Transform player, out Vector3 spawnPosition, out Vector3 destination)
        {
            if (TryGetEntranceDoorBounds(out Bounds doorBounds))
            {
                Vector3 doorCenter = doorBounds.center;
                Vector3 insideDirection = player.position - doorCenter;
                insideDirection.y = 0f;
                if (insideDirection.sqrMagnitude < 0.001f)
                {
                    insideDirection = Vector3.forward;
                }

                insideDirection.Normalize();
                float groundHeight = FindGroundHeight(player);
                spawnPosition = doorCenter - insideDirection * 5f;
                destination = doorCenter + insideDirection * 2f;
                spawnPosition.y = groundHeight;
                destination.y = groundHeight;
                return;
            }

            Vector3 fromBehind = -player.forward;
            fromBehind.y = 0f;
            if (fromBehind.sqrMagnitude < 0.001f)
            {
                fromBehind = Vector3.back;
            }

            fromBehind.Normalize();
            float fallbackHeight = FindGroundHeight(player);
            spawnPosition = player.position + fromBehind * spawnDistanceFromPlayer;
            destination = player.position + fromBehind * 2f;
            spawnPosition.y = fallbackHeight;
            destination.y = fallbackHeight;
        }

        private static bool TryGetEntranceDoorBounds(out Bounds doorBounds)
        {
            doorBounds = default;
            bool foundRenderer = false;

            foreach (string doorName in EntranceDoorNames)
            {
                GameObject doorObject = GameObject.Find(doorName);
                if (doorObject == null)
                {
                    continue;
                }

                Renderer[] renderers = doorObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (!foundRenderer)
                    {
                        doorBounds = renderer.bounds;
                        foundRenderer = true;
                    }
                    else
                    {
                        doorBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return foundRenderer;
        }

        private static Transform FindPlayer()
        {
            GameObject player = GameObject.Find("Player");
            return player != null ? player.transform : null;
        }

        private static float FindGroundHeight(Transform player)
        {
            CharacterController playerController = player.GetComponent<CharacterController>();
            if (playerController == null)
            {
                return player.position.y;
            }

            return player.position.y + playerController.center.y - playerController.height * 0.5f;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _activePolice = null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimePoliceOfficer : MonoBehaviour
    {
        private static Material s_uniformMaterial;
        private static Material s_skinMaterial;
        private static Material s_badgeMaterial;
        private static Material s_batMaterial;
        private static Material s_batHandleMaterial;

        private CharacterController _controller;
        private NavMeshAgent _navMeshAgent;
        private GroundVisualAnchor _groundVisualAnchor;
        private Transform _visualRoot;
        private Transform _weaponRoot;
        private Animator _animator;
        private Transform _target;
        private Vector3 _visualBaseLocalPosition;
        private int _currentAnimationHash;
        private Vector3 _destination;
        private float _runSpeed;
        private float _arriveDistance;
        private float _attackRange;
        private float _attackInterval;
        private float _attackHitDelay;
        private float _attackLockDuration;
        private float _groundClearance;
        private float _arrivedTime;
        private float _nextAttackTime;
        private float _attackStartedTime;
        private float _pendingHitTime;
        private bool _arrived;
        private bool _isAttacking;
        private bool _hasPendingHit;

        public static GameObject Create(
            Vector3 position,
            GameObject policePrefab,
            RuntimeAnimatorController animatorController,
            GameObject weaponPrefab,
            Vector3 weaponLocalPosition,
            Vector3 weaponLocalRotation,
            Vector3 weaponLocalScale,
            float visualScale,
            float visualYOffset,
            float groundClearance)
        {
            GameObject root = new("Police Officer");
            root.name = "Police Officer";
            root.transform.position = position;
            root.transform.localScale = Vector3.one;

            GameObject visual = null;
            if (policePrefab != null)
            {
                visual = Instantiate(policePrefab, root.transform);
                visual.name = "Police Visual";
                visual.transform.localPosition = Vector3.up * visualYOffset;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * visualScale;
            }

            CharacterController controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = root.AddComponent<CharacterController>();
            }

            float controllerHeight = Mathf.Max(1.8f, 2f * visualScale);
            controller.height = controllerHeight;
            controller.radius = Mathf.Max(0.25f, 0.35f * visualScale);
            controller.center = new Vector3(0f, controllerHeight * 0.5f, 0f);
            controller.stepOffset = Mathf.Min(0.45f, controllerHeight * 0.2f);

            NavMeshAgent navMeshAgent = root.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                navMeshAgent = root.AddComponent<NavMeshAgent>();
            }

            navMeshAgent.speed = 8.5f;
            navMeshAgent.angularSpeed = 720f;
            navMeshAgent.acceleration = 24f;
            navMeshAgent.stoppingDistance = 1.4f;
            navMeshAgent.radius = controller.radius;
            navMeshAgent.height = controller.height;
            navMeshAgent.baseOffset = 0f;
            navMeshAgent.updateRotation = false;

            Animator animator = visual != null ? visual.GetComponent<Animator>() : null;
            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;
            }

            RuntimePoliceOfficer officer = root.GetComponent<RuntimePoliceOfficer>();
            if (officer == null)
            {
                officer = root.AddComponent<RuntimePoliceOfficer>();
            }

            if (policePrefab == null)
            {
                officer.BuildFallbackVisuals();
            }
            else
            {
                officer._visualRoot = visual.transform;
                officer._visualBaseLocalPosition = visual.transform.localPosition;
            }

            officer._groundClearance = groundClearance;
            officer._weaponRoot = officer.FindExistingWeaponRoot();
            if (officer._weaponRoot == null)
            {
                officer.AttachTemporaryWeapon(weaponPrefab, weaponLocalPosition, weaponLocalRotation, weaponLocalScale);
            }

            officer.InstallGroundAnchor();
            return root;
        }

        public void Initialize(
            Vector3 destination,
            float runSpeed,
            float arriveDistance,
            float attackRange,
            float attackInterval,
            float attackHitDelay,
            float attackLockDuration)
        {
            _controller = GetComponent<CharacterController>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponentInChildren<Animator>();
            InstallGroundAnchor();
            _destination = destination;
            _runSpeed = runSpeed;
            _arriveDistance = arriveDistance;
            _attackRange = attackRange;
            _attackInterval = attackInterval;
            _attackHitDelay = attackHitDelay;
            _attackLockDuration = attackLockDuration;
        }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
            _arrived = false;
        }

        private void Update()
        {
            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }
            if (_navMeshAgent == null)
            {
                _navMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (_isAttacking)
            {
                UpdateAttack();
                return;
            }

            if (!IsValidTarget(_target))
            {
                _target = FindClosestVillain(transform.position);
            }

            if (_target != null)
            {
                UpdateCombat();
                return;
            }

            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_visualRoot != null && _animator != null && _groundVisualAnchor == null)
            {
                _visualRoot.localPosition = _visualBaseLocalPosition;
                _visualRoot.localRotation = Quaternion.identity;
                AlignVisualFeetToGround();
            }
        }

        private void UpdateCombat()
        {
            Vector3 toTarget = Flatten(_target.position - transform.position);
            if (toTarget.magnitude > _attackRange)
            {
                MoveToward(toTarget, _runSpeed);
                AnimateRun();
                return;
            }

            FaceDirection(toTarget);
            if (Time.time >= _nextAttackTime)
            {
                BeginAttack();
            }
            else
            {
                AnimateIdle();
            }
        }

        private void BeginAttack()
        {
            _isAttacking = true;
            _hasPendingHit = true;
            _attackStartedTime = Time.time;
            _pendingHitTime = Time.time + _attackHitDelay;
            _nextAttackTime = Time.time + _attackInterval;
            PlayAnimation("Standing Melee Attack Downward", true);
        }

        private void UpdateAttack()
        {
            if (_target != null)
            {
                FaceDirection(Flatten(_target.position - transform.position));
            }

            if (_hasPendingHit && Time.time >= _pendingHitTime)
            {
                _hasPendingHit = false;
                SuppressTarget(_target);
                _target = null;
            }

            if (Time.time >= _attackStartedTime + _attackLockDuration)
            {
                _isAttacking = false;
            }
        }

        private void UpdateArrival()
        {
            Vector3 toDestination = Flatten(_destination - transform.position);
            if (toDestination.magnitude <= _arriveDistance)
            {
                if (!_arrived)
                {
                    _arrived = true;
                    _arrivedTime = Time.time;
                }

                AnimateIdle();
                if (Time.time >= _arrivedTime + 8f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            MoveToward(toDestination, _runSpeed);
            AnimateRun();
        }

        private void MoveToward(Vector3 direction, float speed)
        {
            FaceDirection(direction);
            Vector3 velocity = direction.sqrMagnitude > 0.001f ? direction.normalized * speed : Vector3.zero;
            if (TryMoveWithNavMesh(direction, speed))
            {
                return;
            }

            if (_controller != null)
            {
                _controller.SimpleMove(velocity);
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                720f * Time.deltaTime);
        }

        private bool TryMoveWithNavMesh(Vector3 direction, float speed)
        {
            if (_navMeshAgent == null || !_navMeshAgent.enabled)
            {
                return false;
            }

            if (!_navMeshAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return false;
                }

                _navMeshAgent.Warp(hit.position);
            }

            Vector3 destination = transform.position;
            if (_target != null)
            {
                destination = _target.position;
            }
            else if (direction.sqrMagnitude > 0.001f)
            {
                destination = _destination;
            }

            if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, 2.5f, NavMesh.AllAreas))
            {
                return false;
            }

            _navMeshAgent.speed = speed;
            _navMeshAgent.angularSpeed = 720f;
            _navMeshAgent.acceleration = Mathf.Max(12f, speed * 4f);
            _navMeshAgent.stoppingDistance = _target != null ? _attackRange : _arriveDistance;
            _navMeshAgent.baseOffset = 0f;
            _navMeshAgent.SetDestination(destinationHit.position);

            Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
            if (desiredVelocity.sqrMagnitude > 0.001f)
            {
                FaceDirection(desiredVelocity);
            }

            return true;
        }

        private void AttachTemporaryWeapon(
            GameObject weaponPrefab,
            Vector3 localPosition,
            Vector3 localRotation,
            Vector3 localScale)
        {
            Transform hand = ResolveWeaponHand();
            if (hand == null)
            {
                return;
            }

            if (weaponPrefab == null)
            {
                BuildFallbackBat(hand, localPosition, localRotation, localScale);
                return;
            }

            GameObject weapon = Instantiate(weaponPrefab, hand);
            weapon.name = "Temporary Police Bat";
            _weaponRoot = weapon.transform;
            weapon.transform.localPosition = localPosition;
            weapon.transform.localRotation = Quaternion.Euler(localRotation);
            weapon.transform.localScale = localScale;

            foreach (Collider weaponCollider in weapon.GetComponentsInChildren<Collider>())
            {
                weaponCollider.enabled = false;
            }

            foreach (Rigidbody weaponRigidbody in weapon.GetComponentsInChildren<Rigidbody>())
            {
                weaponRigidbody.isKinematic = true;
                weaponRigidbody.detectCollisions = false;
            }
        }

        private Transform ResolveWeaponHand()
        {
            Transform hand = FindChildRecursive(transform, "RightHand");
            if (hand != null)
            {
                return hand;
            }

            Transform parent = _visualRoot != null ? _visualRoot : transform;
            GameObject handObject = new("RightHand");
            handObject.transform.SetParent(parent, false);
            handObject.transform.localPosition = new Vector3(0.45f, 1.35f, 0.35f);
            handObject.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            return handObject.transform;
        }

        private Transform FindExistingWeaponRoot()
        {
            Transform searchRoot = _visualRoot != null ? _visualRoot : transform;
            Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                string childName = child.name;
                if (childName.Contains("Bat") || childName.Contains("Baseball Bat") || childName.Contains("Low Poly Baseball Bat"))
                {
                    return child;
                }
            }

            return null;
        }

        private void BuildFallbackBat(
            Transform hand,
            Vector3 localPosition,
            Vector3 localRotation,
            Vector3 localScale)
        {
            GameObject batRoot = new("Temporary Police Bat");
            batRoot.transform.SetParent(hand, false);
            batRoot.transform.localPosition = localPosition;
            batRoot.transform.localRotation = Quaternion.Euler(localRotation);
            batRoot.transform.localScale = localScale;
            _weaponRoot = batRoot.transform;

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Bat Barrel";
            barrel.transform.SetParent(batRoot.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            barrel.transform.localScale = new Vector3(0.11f, 0.42f, 0.11f);
            Destroy(barrel.GetComponent<Collider>());
            barrel.GetComponent<Renderer>().sharedMaterial = BatMaterial;

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Bat Handle";
            handle.transform.SetParent(batRoot.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            handle.transform.localScale = new Vector3(0.045f, 0.18f, 0.045f);
            Destroy(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().sharedMaterial = BatHandleMaterial;
        }

        private void InstallGroundAnchor()
        {
            if (_visualRoot == null)
            {
                return;
            }

            _groundVisualAnchor = GetComponent<GroundVisualAnchor>();
            if (_groundVisualAnchor == null)
            {
                _groundVisualAnchor = gameObject.AddComponent<GroundVisualAnchor>();
            }

            _groundVisualAnchor.Configure(_visualRoot, _weaponRoot, _groundClearance);
        }

        private void BuildFallbackVisuals()
        {
            _visualRoot = new GameObject("Visual").transform;
            _visualRoot.SetParent(transform, false);
            _visualBaseLocalPosition = _visualRoot.localPosition;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Police Body";
            body.transform.SetParent(_visualRoot, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = UniformMaterial;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Police Head";
            head.transform.SetParent(_visualRoot, false);
            head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = SkinMaterial;

            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hat.name = "Police Hat";
            hat.transform.SetParent(_visualRoot, false);
            hat.transform.localPosition = new Vector3(0f, 2.22f, 0f);
            hat.transform.localScale = new Vector3(0.55f, 0.16f, 0.5f);
            Destroy(hat.GetComponent<Collider>());
            hat.GetComponent<Renderer>().sharedMaterial = UniformMaterial;

            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Police Badge";
            badge.transform.SetParent(_visualRoot, false);
            badge.transform.localPosition = new Vector3(0.18f, 1.32f, 0.43f);
            badge.transform.localScale = new Vector3(0.12f, 0.12f, 0.03f);
            Destroy(badge.GetComponent<Collider>());
            badge.GetComponent<Renderer>().sharedMaterial = BadgeMaterial;
        }

        private void AlignVisualFeetToGround()
        {
            if (_visualRoot == null || !TryGetVisualBounds(out Bounds bounds))
            {
                return;
            }

            float targetMinY = transform.position.y + _groundClearance;
            float yOffset = targetMinY - bounds.min.y;
            if (Mathf.Abs(yOffset) <= 0.001f)
            {
                return;
            }

            _visualRoot.position += Vector3.up * yOffset;
            _visualBaseLocalPosition = _visualRoot.localPosition;
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || (_weaponRoot != null && renderer.transform.IsChildOf(_weaponRoot)))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private void AnimateRun()
        {
            if (_animator != null)
            {
                PlayAnimation("Fast Run");
                return;
            }

            if (_visualRoot == null)
            {
                return;
            }

            float bob = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.12f;
            float sway = Mathf.Sin(Time.time * 10f) * 5f;
            _visualRoot.localPosition = Vector3.up * bob;
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, sway);
        }

        private void AnimateIdle()
        {
            if (_animator != null)
            {
                PlayAnimation("Idle");
                return;
            }

            if (_visualRoot == null)
            {
                return;
            }

            _visualRoot.localPosition = Vector3.zero;
            _visualRoot.localRotation = Quaternion.identity;
        }

        private void PlayAnimation(string stateName, bool force = false)
        {
            int stateHash = Animator.StringToHash(stateName);
            if (!force && _currentAnimationHash == stateHash)
            {
                return;
            }

            _currentAnimationHash = stateHash;
            _animator.CrossFadeInFixedTime(stateHash, 0.08f);
        }

        private static Transform FindClosestVillain(Vector3 origin)
        {
            Transform closest = null;
            float closestSqrDistance = float.MaxValue;

            CheckTargets(Object.FindObjectsByType<RuntimeBrickVillain>(FindObjectsSortMode.None), origin, ref closest, ref closestSqrDistance);
            CheckTargets(Object.FindObjectsByType<RuntimeProductDisturberVillain>(FindObjectsSortMode.None), origin, ref closest, ref closestSqrDistance);
            CheckTargets(Object.FindObjectsByType<global::Villains.BrickVillain>(FindObjectsSortMode.None), origin, ref closest, ref closestSqrDistance);
            CheckTargets(Object.FindObjectsByType<global::Villains.BrickThrowingVillain>(FindObjectsSortMode.None), origin, ref closest, ref closestSqrDistance);

            return closest;
        }

        private static void CheckTargets<T>(T[] targets, Vector3 origin, ref Transform closest, ref float closestSqrDistance)
            where T : Component
        {
            foreach (T target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float sqrDistance = (target.transform.position - origin).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closest = target.transform;
                closestSqrDistance = sqrDistance;
            }
        }

        private static bool IsValidTarget(Transform target)
        {
            return target != null && target.gameObject.activeInHierarchy;
        }

        private static void SuppressTarget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.SendMessageUpwards("TakeDamage", 999, SendMessageOptions.DontRequireReceiver);

            RuntimeBrickVillain runtimeVillain = target.GetComponentInParent<RuntimeBrickVillain>();
            if (runtimeVillain != null)
            {
                Destroy(runtimeVillain.gameObject);
                return;
            }

            RuntimeProductDisturberVillain productDisturber = target.GetComponentInParent<RuntimeProductDisturberVillain>();
            if (productDisturber != null)
            {
                Destroy(productDisturber.gameObject);
                return;
            }

            global::Villains.BrickVillain brickVillain = target.GetComponentInParent<global::Villains.BrickVillain>();
            if (brickVillain != null)
            {
                brickVillain.CompleteFlee();
                return;
            }

            global::Villains.BrickThrowingVillain legacyVillain = target.GetComponentInParent<global::Villains.BrickThrowingVillain>();
            if (legacyVillain != null)
            {
                legacyVillain.gameObject.SetActive(false);
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        private static Material UniformMaterial
        {
            get
            {
                if (s_uniformMaterial == null)
                {
                    s_uniformMaterial = new Material(FindDefaultShader())
                    {
                        color = new Color(0.02f, 0.12f, 0.5f)
                    };
                }

                return s_uniformMaterial;
            }
        }

        private static Material SkinMaterial
        {
            get
            {
                if (s_skinMaterial == null)
                {
                    s_skinMaterial = new Material(FindDefaultShader())
                    {
                        color = new Color(0.95f, 0.72f, 0.55f)
                    };
                }

                return s_skinMaterial;
            }
        }

        private static Material BadgeMaterial
        {
            get
            {
                if (s_badgeMaterial == null)
                {
                    s_badgeMaterial = new Material(FindDefaultShader())
                    {
                        color = new Color(1f, 0.78f, 0.08f)
                    };
                }

                return s_badgeMaterial;
            }
        }

        private static Material BatMaterial
        {
            get
            {
                if (s_batMaterial == null)
                {
                    s_batMaterial = new Material(FindDefaultShader())
                    {
                        color = new Color(0.72f, 0.58f, 0.38f)
                    };
                }

                return s_batMaterial;
            }
        }

        private static Material BatHandleMaterial
        {
            get
            {
                if (s_batHandleMaterial == null)
                {
                    s_batHandleMaterial = new Material(FindDefaultShader())
                    {
                        color = new Color(0.18f, 0.12f, 0.08f)
                    };
                }

                return s_batHandleMaterial;
            }
        }

        private static Shader FindDefaultShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            return shader != null ? shader : Shader.Find("Standard");
        }
    }
}
