using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Agents.FSM
{
    public class StateMachine
    {
        public AgentState CurrentState { get; private set; }
        private Dictionary<int, AgentState> _stateDict;

        public StateMachine(Agent agent, StateSO[] stateList)
        {
            _stateDict = new Dictionary<int, AgentState>();
            foreach (StateSO stateData in stateList)
            {
                Type type = ResolveStateType(stateData.className);
                Debug.Assert(type != null, $"찾고자 하는 타입이 없습니다. : {stateData.className}");
                int paramHash = GetStateClipHash(stateData);
                AgentState state = (AgentState)Activator.CreateInstance(type, agent, paramHash);

                _stateDict.Add(stateData.assetIndex, state);
            }
        }

        public void ChangeState(int newStateIndex, float transitionDuration)
        {
            CurrentState?.Exit();
            AgentState newState = _stateDict.GetValueOrDefault(newStateIndex);
            Debug.Assert(newState != null, $"new State is null {newStateIndex}");

            CurrentState = newState;
            CurrentState.Enter(transitionDuration);
        }

        public TState GetState<TState>(int stateIndex) where TState : AgentState
        {
            if (_stateDict.TryGetValue(stateIndex, out AgentState state))
                return state as TState;

            return null;
        }

        public void UpdateMachine() => CurrentState?.Update();

        private Type ResolveStateType(string className)
        {
            Type type = Type.GetType(className);
            if (type != null)
                return type;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(className))
                .FirstOrDefault(foundType => foundType != null);
        }

        private int GetStateClipHash(StateSO stateData)
        {
            if (stateData.stateParam == null)
                return 0;

            if (stateData.stateParam.ParamHash != 0)
                return stateData.stateParam.ParamHash;

            return string.IsNullOrEmpty(stateData.stateParam.ParamName)
                ? 0
                : Animator.StringToHash(stateData.stateParam.ParamName);
        }
    }
}
