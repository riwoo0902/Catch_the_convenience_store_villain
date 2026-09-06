using UnityEngine;
using Villains.Targeting;

namespace Villains.Visuals
{
    [RequireComponent(typeof(VillainTargetDetector))]
    public class VillainDetectionVisualizer : MonoBehaviour
    {
        [SerializeField] private bool showDetectionRange = true;
        [SerializeField] private Color detectionColor = new Color(1f, 0.86f, 0.1f, 0.85f);
        [SerializeField] private Color closeDetectionColor = new Color(1f, 0.2f, 0.15f, 0.9f);
        [SerializeField] private float lineWidth = 0.045f;
        [SerializeField, Range(12, 96)] private int segments = 56;
        [SerializeField] private float groundOffset = 0.04f;

        private VillainTargetDetector _detector;
        private LineRenderer _detectionLine;
        private LineRenderer _closeDetectionLine;
        private Material _lineMaterial;

        private void Awake()
        {
            _detector = GetComponent<VillainTargetDetector>();
            _lineMaterial = CreateLineMaterial();
            _detectionLine = CreateLineRenderer("Detection Range", detectionColor);
            _closeDetectionLine = CreateLineRenderer("Close Detection Range", closeDetectionColor);
        }

        private void LateUpdate()
        {
            bool shouldShow = showDetectionRange && _detector != null;
            _detectionLine.enabled = shouldShow;
            _closeDetectionLine.enabled = shouldShow;

            if (!shouldShow)
                return;

            DrawDetectionCone();
            DrawCircle(_closeDetectionLine, _detector.CloseDetectionRange);
        }

        private void DrawDetectionCone()
        {
            float viewAngle = Mathf.Clamp(_detector.ViewAngle, 0f, 360f);
            if (viewAngle >= 359.9f)
            {
                DrawCircle(_detectionLine, _detector.DetectionRange);
                return;
            }

            int pointCount = segments + 3;
            _detectionLine.positionCount = pointCount;

            Vector3 origin = GroundOrigin;
            _detectionLine.SetPosition(0, origin);

            float startAngle = -viewAngle * 0.5f;
            float step = viewAngle / segments;
            for (int i = 0; i <= segments; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + step * i, 0f) * transform.forward;
                _detectionLine.SetPosition(i + 1, origin + direction.normalized * _detector.DetectionRange);
            }

            _detectionLine.SetPosition(pointCount - 1, origin);
        }

        private void DrawCircle(LineRenderer lineRenderer, float radius)
        {
            int pointCount = segments + 1;
            lineRenderer.positionCount = pointCount;

            Vector3 origin = GroundOrigin;
            float step = 360f / segments;
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, step * i, 0f) * Vector3.forward;
                lineRenderer.SetPosition(i, origin + direction * radius);
            }
        }

        private Vector3 GroundOrigin => new Vector3(transform.position.x, transform.position.y + groundOffset, transform.position.z);

        private LineRenderer CreateLineRenderer(string objectName, Color color)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.material = _lineMaterial;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            return lineRenderer;
        }

        private static Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            return new Material(shader);
        }
    }
}
