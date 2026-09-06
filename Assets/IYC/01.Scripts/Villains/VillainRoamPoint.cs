using UnityEngine;

namespace CWH.Villains
{
    [DisallowMultipleComponent]
    [AddComponentMenu("CWH/Villains/Villain Roam Point")]
    public sealed class VillainRoamPoint : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float gizmoRadius = 0.35f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
