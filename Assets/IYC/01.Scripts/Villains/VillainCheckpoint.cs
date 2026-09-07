using UnityEngine;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    [AddComponentMenu("CWH/Villains/Villain Checkpoint")]
    public sealed class VillainCheckpoint : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float gizmoRadius = 0.45f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.25f);
            Gizmos.DrawSphere(transform.position, gizmoRadius);
        }
    }
}
