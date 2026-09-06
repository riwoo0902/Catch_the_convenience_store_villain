using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lrw.Script.UI
{
    public class StartButton : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "";
        
        public void GoNextScene()
        {
            if (string.IsNullOrEmpty(nextSceneName) || nextSceneName == CWH.GameFlow.GameLoopController.GameplayScenePath)
                CWH.GameFlow.GameLoopController.StartNewGame();
            else
                SceneManager.LoadScene(nextSceneName);
        }
    }
}
