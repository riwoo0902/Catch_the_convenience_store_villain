using System;
using Agents;
using Agents.FSM;
using Player.FSM;
using UnityEngine;

namespace Player
{
    public class PlayerController : Agent
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO playerStates;

        private StateMachine _stateMachine;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _stateMachine = new StateMachine(this, playerStates.states);

            PlayerInput.OnJumpKeyPressed += PlayerInput_OnJumpKeyPressed;
        }

        private void PlayerInput_OnJumpKeyPressed()
        {
            Debug.Log("Jump Key Pressed");
            ChangeState(PlayerState.JUMP, transitionDuration: 0);
        }

        private void Start()
        {
            ChangeState(PlayerState.IDLE, transitionDuration: 0); //즉시 IDLE로 변경한다.
        }

        private void Update()
        {
            _stateMachine.UpdateMachine();
        }

        public void ChangeState(PlayerState newState, float transitionDuration)
            => _stateMachine.ChangeState((int)newState, transitionDuration);
    }
}