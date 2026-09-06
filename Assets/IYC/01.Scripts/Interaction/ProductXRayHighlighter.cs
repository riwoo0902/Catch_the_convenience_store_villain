using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CWH.Player.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ProductXRayHighlighter : MonoBehaviour
    {
        private const string GlowShaderResourceName = "ProductXRayGlow";
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static Material s_glowMaterial;
        private static readonly Plane[] CameraFrustumPlanes = new Plane[6];

        [SerializeField] private bool _hideGlowWhenVisibleToPlayer = true;
        [SerializeField] private LayerMask _visibilityBlockingMask = ~0;

        private readonly List<MeshRenderer> _overlayRenderers = new();
        private readonly List<Renderer> _sourceRenderers = new();
        private MaterialPropertyBlock _propertyBlock;
        private bool _isHighlighted;

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted && _overlayRenderers.Count == 0)
            {
                BuildOverlays();
            }

            _isHighlighted = highlighted;
            RefreshOverlayVisibility();
        }

        private void Update()
        {
            if (!_isHighlighted)
            {
                return;
            }

            RefreshOverlayVisibility();

            float pulse = 0.55f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.2f;
            Color glowColor = new(2.6f, 0.55f, 0.04f, pulse);
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor(GlowColorId, glowColor);

            foreach (MeshRenderer overlayRenderer in _overlayRenderers)
            {
                if (overlayRenderer != null)
                {
                    overlayRenderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        private void BuildOverlays()
        {
            Material glowMaterial = GetGlowMaterial();
            if (glowMaterial == null)
            {
                Debug.LogWarning("Product X-ray glow shader could not be loaded.", this);
                return;
            }

            MeshRenderer[] sourceRenderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer sourceRenderer in sourceRenderers)
            {
                if (sourceRenderer.gameObject.name == "__ProductXRayGlow")
                {
                    continue;
                }

                MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
                if (sourceFilter == null || sourceFilter.sharedMesh == null)
                {
                    continue;
                }

                GameObject overlayObject = new("__ProductXRayGlow", typeof(MeshFilter), typeof(MeshRenderer));
                overlayObject.layer = sourceRenderer.gameObject.layer;
                Transform overlayTransform = overlayObject.transform;
                overlayTransform.SetParent(sourceRenderer.transform, false);

                overlayObject.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer overlayRenderer = overlayObject.GetComponent<MeshRenderer>();
                int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
                Material[] glowMaterials = new Material[materialCount];
                for (int index = 0; index < glowMaterials.Length; index++)
                {
                    glowMaterials[index] = glowMaterial;
                }

                overlayRenderer.sharedMaterials = glowMaterials;
                overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;
                overlayRenderer.lightProbeUsage = LightProbeUsage.Off;
                overlayRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                _sourceRenderers.Add(sourceRenderer);
                _overlayRenderers.Add(overlayRenderer);
            }
        }

        private void RefreshOverlayVisibility()
        {
            bool visibleToPlayer = _hideGlowWhenVisibleToPlayer && IsVisibleToPlayer();
            bool shouldShowOverlay = _isHighlighted && !visibleToPlayer;

            foreach (MeshRenderer overlayRenderer in _overlayRenderers)
            {
                if (overlayRenderer != null)
                {
                    overlayRenderer.enabled = shouldShowOverlay;
                }
            }
        }

        private bool IsVisibleToPlayer()
        {
            Camera playerCamera = FindPlayerCamera();
            if (playerCamera == null || !TryGetSourceBounds(out Bounds bounds))
            {
                return false;
            }

            GeometryUtility.CalculateFrustumPlanes(playerCamera, CameraFrustumPlanes);
            if (!GeometryUtility.TestPlanesAABB(CameraFrustumPlanes, bounds))
            {
                return false;
            }

            Vector3 origin = playerCamera.transform.position;
            Transform ignoredPlayerRoot = GetPlayerRoot(playerCamera.transform);
            if (HasClearLineOfSight(origin, bounds.center, ignoredPlayerRoot))
            {
                return true;
            }

            Vector3 extents = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + Vector3.Scale(extents, new Vector3(x, y, z));
                        if (HasClearLineOfSight(origin, corner, ignoredPlayerRoot))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasClearLineOfSight(Vector3 origin, Vector3 target, Transform ignoredPlayerRoot)
        {
            Vector3 toTarget = target - origin;
            float targetDistance = toTarget.magnitude;
            if (targetDistance <= Mathf.Epsilon)
            {
                return true;
            }

            Ray ray = new(origin, toTarget / targetDistance);
            RaycastHit[] hits = Physics.RaycastAll(ray, targetDistance, _visibilityBlockingMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return true;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || IsIgnoredVisibilityHit(hit.collider.transform, ignoredPlayerRoot))
                {
                    continue;
                }

                if (IsPartOfHighlightedProduct(hit.collider.transform))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private static bool IsIgnoredVisibilityHit(Transform hitTransform, Transform ignoredPlayerRoot)
        {
            return ignoredPlayerRoot != null && hitTransform.IsChildOf(ignoredPlayerRoot);
        }

        private bool IsPartOfHighlightedProduct(Transform hitTransform)
        {
            for (Transform current = hitTransform; current != null; current = current.parent)
            {
                if (current == transform)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform GetPlayerRoot(Transform cameraTransform)
        {
            Transform playerRoot = null;
            for (Transform current = cameraTransform; current != null; current = current.parent)
            {
                if (current.name == "Player")
                {
                    playerRoot = current;
                }
            }

            return playerRoot;
        }

        private bool TryGetSourceBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (Renderer sourceRenderer in _sourceRenderers)
            {
                if (sourceRenderer == null || !sourceRenderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = sourceRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(sourceRenderer.bounds);
                }
            }

            return hasBounds;
        }

        private static Camera FindPlayerCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.enabled && mainCamera.gameObject.activeInHierarchy)
            {
                return mainCamera;
            }

            Camera[] cameras = Camera.allCameras;
            Camera bestCamera = null;
            int bestScore = int.MinValue;

            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int score = camera.CompareTag("MainCamera") ? 10 : 0;
                for (Transform current = camera.transform; current != null; current = current.parent)
                {
                    if (current.name == "Player")
                    {
                        score += 100;
                        break;
                    }
                }

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestCamera = camera;
            }

            return bestCamera;
        }

        private static Material GetGlowMaterial()
        {
            if (s_glowMaterial != null)
            {
                return s_glowMaterial;
            }

            Shader glowShader = Resources.Load<Shader>(GlowShaderResourceName);
            if (glowShader == null)
            {
                return null;
            }

            s_glowMaterial = new Material(glowShader)
            {
                name = "Runtime Product X-Ray Glow",
                hideFlags = HideFlags.HideAndDontSave
            };
            return s_glowMaterial;
        }
    }
}
