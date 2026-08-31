using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private readonly Dictionary<Transform, LocalPose> _originalPoses = new();
        private Camera _viewCamera;

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
        }

        private void DisturbLookedProduct()
        {
            if (!TryGetLookedProduct(out Transform product))
            {
                return;
            }

            if (!_originalPoses.TryGetValue(product, out LocalPose originalPose))
            {
                originalPose = new LocalPose(product.localPosition, product.localRotation);
                _originalPoses.Add(product, originalPose);
            }

            Vector3 positionOffset = new(
                UnityEngine.Random.Range(-_maxPositionOffset.x, _maxPositionOffset.x),
                UnityEngine.Random.Range(0f, _maxPositionOffset.y),
                UnityEngine.Random.Range(-_maxPositionOffset.z, _maxPositionOffset.z));
            Vector3 rotationOffset = new(
                UnityEngine.Random.Range(-_maxRotationOffset.x, _maxRotationOffset.x),
                UnityEngine.Random.Range(-_maxRotationOffset.y, _maxRotationOffset.y),
                UnityEngine.Random.Range(-_maxRotationOffset.z, _maxRotationOffset.z));

            product.SetLocalPositionAndRotation(
                originalPose.Position + positionOffset,
                originalPose.Rotation * Quaternion.Euler(rotationOffset));
        }

        private void RestoreLookedProduct()
        {
            if (!TryGetLookedProduct(out Transform product)
                || !_originalPoses.Remove(product, out LocalPose originalPose))
            {
                return;
            }

            product.SetLocalPositionAndRotation(originalPose.Position, originalPose.Rotation);
        }

        private bool TryGetLookedProduct(out Transform product)
        {
            product = null;

            Ray viewRay = new(_viewCamera.transform.position, _viewCamera.transform.forward);
            if (!Physics.Raycast(
                    viewRay,
                    out RaycastHit hit,
                    _interactionDistance,
                    _raycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return TryResolveProductRoot(hit.transform, out product);
        }

        private static bool TryResolveProductRoot(Transform hitTransform, out Transform product)
        {
            product = null;

            for (Transform current = hitTransform; current != null && current.parent != null; current = current.parent)
            {
                Transform possibleProductGroup = current.parent;
                if (!IsProductGroup(possibleProductGroup) || !IsAllowedShelf(possibleProductGroup.parent))
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
