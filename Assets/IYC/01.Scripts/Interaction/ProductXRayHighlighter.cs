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

        private readonly List<MeshRenderer> _overlayRenderers = new();
        private MaterialPropertyBlock _propertyBlock;
        private bool _isHighlighted;

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted && _overlayRenderers.Count == 0)
            {
                BuildOverlays();
            }

            _isHighlighted = highlighted;
            foreach (MeshRenderer overlayRenderer in _overlayRenderers)
            {
                if (overlayRenderer != null)
                {
                    overlayRenderer.enabled = highlighted;
                }
            }
        }

        private void Update()
        {
            if (!_isHighlighted)
            {
                return;
            }

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
                _overlayRenderers.Add(overlayRenderer);
            }
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
