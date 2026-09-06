using CWH.Player.Animation;
using CWH.Player.Config;
using CWH.Player.Core;
using CWH.Player.Input;
using CWH.Player.States;
using UnityEngine;

namespace Branches.CWH.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterControllerMotor))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private GroundMovementConfig _groundConfig;
        [SerializeField] private JumpConfig _jumpConfig;
        [SerializeField] private SlideConfig _slideConfig;

        private CharacterControllerMotor _motor;
        private IMovementInputReader _inputReader;
        private MovementStateMachine _stateMachine;

        public IMovementStateSource StateSource { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<CharacterControllerMotor>();
            _inputReader = GetComponent<IMovementInputReader>();
        }

        private void Start()
        {
            var context = new MovementContext(_groundConfig, _jumpConfig, null, _slideConfig, _motor, null, transform);

            var library = new MovementStateLibrary();
            var grounded = new GroundedState(library);
            var airborne = new AirborneState(library);
            var sliding = new SlideState(library);
            library.Grounded = grounded;
            library.Airborne = airborne;
            library.Sliding = sliding;

            _stateMachine = new MovementStateMachine(context, grounded);
            StateSource = _stateMachine;
        }

        private void FixedUpdate()
        {
            var sample = _inputReader.Sample();
            _stateMachine.Tick(Time.fixedDeltaTime, sample);
        }
    }
}
