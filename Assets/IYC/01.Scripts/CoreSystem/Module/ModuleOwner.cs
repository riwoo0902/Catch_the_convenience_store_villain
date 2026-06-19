using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IYC._01.Scripts.CoreSystem.Module
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        protected Dictionary<Type, IModule> modules = new Dictionary<Type, IModule>();

        protected void Awake()
        {
            modules = GetComponentsInChildren<IModule>().ToDictionary(module => module.GetType());

            InitializeModules();
            AfterInitializeModules();
        }

        protected virtual void InitializeModules()
        {
            foreach (IModule module in modules.Values)
            {
                module.Init(this);
            }
        }

        protected virtual void AfterInitializeModules()
        {
            foreach (IAfterInitModule afterModule in modules.Values)
            {
                afterModule.AfterInit();
            }
        }

        public T GetModule<T>()
        {
            if (modules.TryGetValue(typeof(T), out IModule module))
            {
                return (T)module;
            }
            
            IModule findedModule = modules.Values.FirstOrDefault();
            
            if(findedModule is T castedModule)
            {
                return castedModule;
            }
            
            return default;
        }
    }
}