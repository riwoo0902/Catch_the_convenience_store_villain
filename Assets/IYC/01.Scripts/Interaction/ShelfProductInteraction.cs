using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Villains.Projectiles;

namespace CWH.Player.Interaction
{
    /// <summary>
    /// Lets the player disturb or restore the single shelf product at the center of the view.
    /// The component installs itself on the active player camera at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ShelfProductInteraction : MonoBehaviour
    {
        private const string ShelfAName = "shelfA_gp";
        private const string ShelfBName = "shelfB_gp";
        private const string ProductGroupPrefix = "produce";
        private const string GroupSuffix = "_gp";

        [Header("Raycast")]
        [SerializeField, Min(0.1f)] private float _interactionDistance = 8f;
        [SerializeField] private LayerMask _raycastLayers = ~0;

        [Header("Disturbance")]
        [SerializeField] private Vector3 _maxPositionOffset = new(0.14f, 0.04f, 0.12f);
        [SerializeField] private Vector3 _maxRotationOffset = new(22f, 35f, 22f);

        [Header("Unorganized Detection")]
        [SerializeField, Min(0.0001f)] private float _positionTolerance = 0.001f;
        [SerializeField, Range(0.1f, 15f)] private float _rotationTolerance = 2f;
        [SerializeField, Min(0.02f)] private float _poseCheckInterval = 0.12f;

        private readonly Dictionary<Transform, LocalPose> _initialPoses = new();
        private readonly HashSet<Transform> _unorganizedProducts = new();
        private Camera _viewCamera;
        private float _nextPoseCheckTime;

        public static int UnorganizedProductCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetUnorganizedProductCount()
        {
            UnorganizedProductCount = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnPlayerCamera()
        {
            if (FindFirstObjectByType<ShelfProductInteraction>() != null)
            {
                return;
            }

            Camera playerCamera = FindBestPlayerCamera();
            if (playerCamera != null)
            {
                playerCamera.gameObject.AddComponent<ShelfProductInteraction>();
            }
        }

        private static Camera FindBestPlayerCamera()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Camera bestCamera = null;
            int bestScore = int.MinValue;

            foreach (Camera candidate in cameras)
            {
                if (!candidate.enabled || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int score = candidate.CompareTag("MainCamera") ? 10 : 0;
                if (HasPlayerAncestor(candidate.transform))
                {
                    score += 100;
                }

                if (score > bestScore)
                {
                    bestCamera = candidate;
                    bestScore = score;
                }
            }

            return bestCamera;
        }

        private static bool HasPlayerAncestor(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (string.Equals(current.name, "Player", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            _viewCamera = GetComponent<Camera>();
            CaptureInitialProductPoses();
            BrickProjectile.HitTarget += HandleBrickHit;
        }

        private void CaptureInitialProductPoses()
        {
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in sceneTransforms)
            {
                if (!TryResolveProductRoot(candidate, out Transform product)
                    || _initialPoses.ContainsKey(product))
                {
                    continue;
                }

                _initialPoses.Add(product, new LocalPose(product.localPosition, product.localRotation));
                EnableProductPhysics(product);
            }
        }

        private static void EnableProductPhysics(Transform product)
        {
            Rigidbody[] rigidbodies = product.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;
                rigidbody.detectCollisions = true;
            }
        }

        private static void ResetProductPhysics(Transform product)
        {
            Rigidbody[] rigidbodies = product.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                if (rigidbody.isKinematic)
                {
                    continue;
                }

                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.WakeUp();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                DisturbLookedProduct();
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                RestoreLookedProduct();
            }

            if (Time.unscaledTime >= _nextPoseCheckTime)
            {
                _nextPoseCheckTime = Time.unscaledTime + _poseCheckInterval;
                RefreshUnorganizedProducts();
            }
        }

        private void DisturbLookedProduct()
        {
            if (!TryGetLookedProduct(out Transform product))
            {
                return;
            }

            DisturbProduct(product);
        }

        private void HandleBrickHit(Transform hitTransform)
        {
            if (TryResolveProductRoot(hitTransform, out Transform product))
            {
                DisturbProduct(product);
            }
        }

        private void DisturbProduct(Transform product)
        {
            if (product == null)
            {
                return;
            }

            if (!_initialPoses.TryGetValue(product, out LocalPose initialPose))
            {
                initialPose = new LocalPose(product.localPosition, product.localRotation);
                _initialPoses.Add(product, initialPose);
                EnableProductPhysics(product);
            }

            SetUnorganized(product, true);

            Vector3 positionOffset = new(
                UnityEngine.Random.Range(-_maxPositionOffset.x, _maxPositionOffset.x),
                UnityEngine.Random.Range(0f, _maxPositionOffset.y),
                UnityEngine.Random.Range(-_maxPositionOffset.z, _maxPositionOffset.z));
            Vector3 rotationOffset = new(
                UnityEngine.Random.Range(-_maxRotationOffset.x, _maxRotationOffset.x),
                UnityEngine.Random.Range(-_maxRotationOffset.y, _maxRotationOffset.y),
                UnityEngine.Random.Range(-_maxRotationOffset.z, _maxRotationOffset.z));

            product.SetLocalPositionAndRotation(
                initialPose.Position + positionOffset,
                initialPose.Rotation * Quaternion.Euler(rotationOffset));
            ResetProductPhysics(product);
            Physics.SyncTransforms();
        }

        private void RestoreLookedProduct()
        {
            if (!TryGetLookedProduct(out Transform product)
                || !_initialPoses.TryGetValue(product, out LocalPose initialPose))
            {
                return;
            }

            product.SetLocalPositionAndRotation(initialPose.Position, initialPose.Rotation);
            ResetProductPhysics(product);
            Physics.SyncTransforms();
            SetUnorganized(product, false);
        }

        private void RefreshUnorganizedProducts()
        {
            foreach (KeyValuePair<Transform, LocalPose> entry in _initialPoses)
            {
                Transform product = entry.Key;
                if (product == null)
                {
                    continue;
                }

                LocalPose initialPose = entry.Value;
                bool positionChanged = (product.localPosition - initialPose.Position).sqrMagnitude
                                       > _positionTolerance * _positionTolerance;
                bool rotationChanged = Quaternion.Angle(product.localRotation, initialPose.Rotation)
                                       > _rotationTolerance;
                SetUnorganized(product, positionChanged || rotationChanged);
            }
        }

        private void SetUnorganized(Transform product, bool isUnorganized)
        {
            bool changed = isUnorganized
                ? _unorganizedProducts.Add(product)
                : _unorganizedProducts.Remove(product);

            ProductXRayHighlighter highlighter = product.GetComponent<ProductXRayHighlighter>();
            if (isUnorganized && highlighter == null)
            {
                highlighter = product.gameObject.AddComponent<ProductXRayHighlighter>();
            }

            if (highlighter != null)
            {
                highlighter.SetHighlighted(isUnorganized);
            }

            if (changed)
            {
                UnorganizedProductCount = _unorganizedProducts.Count;
            }
        }

        private void OnDestroy()
        {
            BrickProjectile.HitTarget -= HandleBrickHit;
            foreach (Transform product in _unorganizedProducts)
            {
                if (product != null && product.TryGetComponent(out ProductXRayHighlighter highlighter))
                {
                    highlighter.SetHighlighted(false);
                }
            }

            UnorganizedProductCount = 0;
        }

        private bool TryGetLookedProduct(out Transform product)
        {
            product = null;

            Ray viewRay = new(_viewCamera.transform.position, _viewCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                viewRay,
                _interactionDistance,
                _raycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, static (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (TryResolveProductRoot(hit.transform, out product))
                {
                    return true;
                }
            }

            product = null;
            return false;
        }

        private static bool TryResolveProductRoot(Transform hitTransform, out Transform product)
        {
            product = null;

            for (Transform current = hitTransform; current != null && current.parent != null; current = current.parent)
            {
                Transform possibleProductGroup = current.parent;
                if (!IsProductGroup(possibleProductGroup)
                    || !IsAllowedShelf(possibleProductGroup.parent))
                {
                    continue;
                }

                product = current;
                return true;
            }

            return false;
        }

        private static bool IsProductGroup(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            return transform.name.StartsWith(ProductGroupPrefix, StringComparison.OrdinalIgnoreCase)
                   && transform.name.EndsWith(GroupSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowedShelf(Transform transform)
        {
            return transform != null
                   && (string.Equals(transform.name, ShelfAName, StringComparison.Ordinal)
                       || string.Equals(transform.name, ShelfBName, StringComparison.Ordinal));
        }

        private readonly struct LocalPose
        {
            public LocalPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }
    }
}
