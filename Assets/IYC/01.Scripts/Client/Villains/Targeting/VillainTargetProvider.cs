using IYC._01.Scripts.CoreSystem.Module;
using Player;
using UnityEngine;

namespace Villains.Targeting
{
    public class VillainTargetProvider : MonoBehaviour, IModule
    {
        [SerializeField] private float targetForgetDuration = 4f;
        [SerializeField] private bool findPlayerOnInit = true;

        public Transform Target { get; private set; }
        public Transform CurrentTarget { get; private set; }
        public Vector3 LastTargetPosition { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        private float _forgetTimer;

        public void Init(ModuleOwner owner)
        {
            LastTargetPosition = owner.transform.position;

            if (!findPlayerOnInit)
                return;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                SetTarget(player.transform);
        }

        private void Update()
        {
            UpdateTargetMemory();
        }

        public void SetTarget(Transform target)
        {
            Target = target;
            if (target == null)
                return;

            CurrentTarget = target;
            LastTargetPosition = target.position;
            _forgetTimer = 0f;
        }

        public void LoseVisibleTarget()
        {
            Target = null;
        }

        public void ClearTarget()
        {
            Target = null;
            CurrentTarget = null;
            _forgetTimer = 0f;
        }

        public float GetTargetDistance(Vector3 origin)
        {
            if (CurrentTarget == null)
                return float.MaxValue;

            Vector3 targetPosition = Target != null ? Target.position : LastTargetPosition;
            return Vector3.Distance(origin, targetPosition);
        }

        private void UpdateTargetMemory()
        {
            if (Target != null)
            {
                CurrentTarget = Target;
                LastTargetPosition = Target.position;
                _forgetTimer = 0f;
                return;
            }

            if (CurrentTarget == null)
                return;

            _forgetTimer += Time.deltaTime;
            if (_forgetTimer >= targetForgetDuration)
                ClearTarget();
        }
    }
}
