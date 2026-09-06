using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CWH.GameFlow
{
    /// <summary>A short, unscaled exit shot. It never resumes combat to play an ending.</summary>
    public sealed class ShiftCheckoutSequence : MonoBehaviour
    {
        private readonly List<Material> _materials = new();
        private readonly List<Camera> _previousCameras = new();
        private GameObject _actor;
        private Camera _camera;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftArm;
        private Transform _rightArm;

        public IEnumerator Play(float duration)
        {
            GameObject player = GameObject.Find("Player");
            Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;
            Vector3 center = playerPosition;
            Vector3 outward = Vector3.back;
            float floor = playerPosition.y - 1f;
            GameObject frame = GameObject.Find("automaticDoorFrame");
            if (frame == null) frame = GameObject.Find("automaticDoor_L_gp");
            if (frame != null)
            {
                Renderer[] renderers = frame.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    center = bounds.center;
                    floor = bounds.min.y;
                    // Exit axis is perpendicular to the thin dimension of the doorway.
                    outward = bounds.size.x > bounds.size.z ? Vector3.forward : Vector3.right;
                    GameObject shelf = GameObject.Find("shelfA_gp");
                    Vector3 storeInterior = shelf != null ? shelf.transform.position : playerPosition;
                    if (Vector3.Dot(outward, storeInterior - center) > 0) outward = -outward;
                }
            }
            Transform insideMarker = GameObject.Find("ShiftExitInside")?.transform;
            Transform outsideMarker = GameObject.Find("ShiftExitOutside")?.transform;
            Vector3 start = center + outward * 0.9f;
            start.y = floor;
            Vector3 end = start + outward * 5.5f;
            if (insideMarker != null && outsideMarker != null)
            {
                start = insideMarker.position;
                end = outsideMarker.position;
                outward = (end - start).normalized;
            }

            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!camera.enabled) continue;
                _previousCameras.Add(camera);
                camera.enabled = false;
            }
            _camera = new GameObject("Checkout Camera").AddComponent<Camera>();
            _camera.transform.SetParent(transform, false);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 300f;
            _camera.fieldOfView = 48f;
            Vector3 side = Vector3.Cross(Vector3.up, outward);
            Vector3 cameraPosition = start + outward * 9f + side * 3.5f + Vector3.up * 2.8f;
            _camera.transform.position = cameraPosition;
            BuildEmployee();
            _actor.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float step = Mathf.Sin(elapsed * 10f);
                _actor.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Abs(step) * 0.04f);
                _leftLeg.localRotation = Quaternion.Euler(step * 28f, 0, 0);
                _rightLeg.localRotation = Quaternion.Euler(-step * 28f, 0, 0);
                _leftArm.localRotation = Quaternion.Euler(-step * 22f, 0, -8f);
                _rightArm.localRotation = Quaternion.Euler(step * 22f, 0, 8f);
                _camera.transform.LookAt(Vector3.Lerp(start, _actor.transform.position, 0.7f) + Vector3.up * 1.05f);
                yield return null;
            }
        }

        private void BuildEmployee()
        {
            _actor = new GameObject("Off Duty Employee");
            _actor.transform.SetParent(transform, false);
            Material uniform = Material(new Color(0.12f, 0.66f, 0.52f));
            Material dark = Material(new Color(0.09f, 0.12f, 0.17f));
            Material skin = Material(new Color(1f, 0.76f, 0.56f));
            Material white = Material(Color.white);
            Part("Uniform", _actor.transform, PrimitiveType.Cube, new Vector3(0, 1.12f, 0), new Vector3(0.62f, 0.67f, 0.32f), uniform);
            Part("Head", _actor.transform, PrimitiveType.Sphere, new Vector3(0, 1.7f, 0), Vector3.one * 0.48f, skin);
            Part("Hair", _actor.transform, PrimitiveType.Cube, new Vector3(0, 1.92f, -0.02f), new Vector3(0.45f, 0.13f, 0.4f), dark);
            Part("Left Eye", _actor.transform, PrimitiveType.Cube, new Vector3(-0.09f, 1.72f, 0.224f), new Vector3(0.05f, 0.025f, 0.025f), dark);
            Part("Right Eye", _actor.transform, PrimitiveType.Cube, new Vector3(0.09f, 1.72f, 0.224f), new Vector3(0.05f, 0.025f, 0.025f), dark);
            Part("Badge", _actor.transform, PrimitiveType.Cube, new Vector3(0.16f, 1.29f, 0.17f), new Vector3(0.16f, 0.09f, 0.02f), white);
            _leftLeg = Limb("Left Leg", new Vector3(-0.17f, 0.84f, 0), new Vector3(0.22f, 0.72f, 0.27f), dark);
            _rightLeg = Limb("Right Leg", new Vector3(0.17f, 0.84f, 0), new Vector3(0.22f, 0.72f, 0.27f), dark);
            _leftArm = Limb("Left Arm", new Vector3(-0.4f, 1.42f, 0), new Vector3(0.18f, 0.62f, 0.22f), skin);
            _rightArm = Limb("Right Arm", new Vector3(0.4f, 1.42f, 0), new Vector3(0.18f, 0.62f, 0.22f), skin);
        }

        private Transform Limb(string name, Vector3 position, Vector3 size, Material material)
        {
            Transform pivot = new GameObject(name).transform;
            pivot.SetParent(_actor.transform, false);
            pivot.localPosition = position;
            Part("Mesh", pivot, PrimitiveType.Cube, Vector3.down * size.y * 0.5f, size, material);
            return pivot;
        }

        private static void Part(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
        }

        private Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material material = new(shader);
            material.color = color;
            _materials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Camera camera in _previousCameras) if (camera != null) camera.enabled = true;
            foreach (Material material in _materials) if (material != null) Destroy(material);
        }
    }
}
