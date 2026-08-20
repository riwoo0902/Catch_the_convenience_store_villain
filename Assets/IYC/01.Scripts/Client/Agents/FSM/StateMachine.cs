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
                int paramHash = stateData.stateParam != null ? stateData.stateParam.ParamHash : 0;
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
    }
}
