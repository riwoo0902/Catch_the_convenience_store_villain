using UnityEngine;

namespace DevLib.ObjectPool.Runtime
{
    public class PoolInitializer : MonoBehaviour
    {
        [field: SerializeField] public PoolManagerSO PoolManager { get;  private set; }

        private void Awake()
        {
            PoolManager.InitializePool(transform); //이 모노 비해비어가 풀매니저를 초기화해준다.
            
            PoolInitializer[] initializers = FindObjectsByType<PoolInitializer>(FindObjectsSortMode.None);
            Debug.Assert(initializers.Length == 1, "오직 하나의 풀 이니셜라이저만 씬에 존재할 수 있습니다.");
        }
        
        public T Pop<T>(PoolItemSO type) where T : IPoolable => PoolManager.Pop<T>(type);
        public void Push(IPoolable item) => PoolManager.Push(item);
    }
}