using Agents;
using Agents.FSM;
using UnityEngine;
using UnityEngine.Events;
using Villains.Animation;
using Villains.Combat;
using Villains.FSM;
using Villains.Movement;
using Villains.Targeting;

namespace Villains
{
    public class BrickVillain : Agent
    {
        [Header("FSM")]
        [SerializeField] private StateListSO stateList;
        [SerializeField] private VillainState initialState = VillainState.IDLE;

        [Header("Flee")]
        [SerializeField] private Transform fleeDestination;
        [SerializeField] private Vector3 fallbackFleeDestination = new Vector3(0f, 0f, -12f);
        [SerializeField] private bool disableOnFleeCompleted = true;
        [SerializeField] private UnityEvent onFleeCompleted;

        public VillainTargetProvider TargetProvider { get; private set; }
        public VillainTargetDetector TargetDetector { get; private set; }
        public VillainMovement Movement { get; private set; }
        public BrickThrowAttack ThrowAttack { get; private set; }
        public VillainAnimationEventRelay AnimationEvents { get; private set; }
        public Vector3 FleeDestination => fleeDestination != null ? fleeDestination.position : fallbackFleeDestination;
        public bool DisableOnFleeCompleted => disableOnFleeCompleted;

        private StateMachine _stateMachine;

        protected override void InitializeModules()
        {
            base.InitializeModules();

            TargetProvider = GetModule<VillainTargetProvider>();
            TargetDetector = GetModule<VillainTargetDetector>();
            Movement = GetModule<VillainMovement>();
            ThrowAttack = GetModule<BrickThrowAttack>();
            AnimationEvents = GetModule<VillainAnimationEventRelay>();

            Debug.Assert(stateList != null, $"{nameof(BrickVillain)} needs a StateListSO.", this);
            if (stateList != null)
                _stateMachine = new StateMachine(this, stateList.states);
        }

        private void Start()
        {
            ChangeState(initialState, 0f);
        }

        private void Update()
        {
            _stateMachine?.UpdateMachine();
        }

        public void ChangeState(VillainState state, float transitionDuration = 0.1f)
        {
            _stateMachine?.ChangeState((int)state, transitionDuration);
        }

        public void Stun(float duration)
        {
            BrickVillainStunState stunState = _stateMachine?.GetState<BrickVillainStunState>((int)VillainState.STUN);
            if (stunState == null)
                return;

            stunState.SetDuration(duration);
            ChangeState(VillainState.STUN);
        }

        public void FleeFromStore()
        {
            if (TargetDetector != null)
                TargetDetector.enabled = false;

            TargetProvider?.ClearTarget();
            ChangeState(VillainState.FLEE, 0.1f);
        }

        public void SetFleeDestination(Transform destination)
        {
            fleeDestination = destination;
        }

        public void SetFallbackFleeDestination(Vector3 destination)
        {
            fallbackFleeDestination = destination;
        }

        public void CompleteFlee()
        {
            Movement.Stop();
            onFleeCompleted?.Invoke();

            if (disableOnFleeCompleted)
                gameObject.SetActive(false);
        }
    }
}
