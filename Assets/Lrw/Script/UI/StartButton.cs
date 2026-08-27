using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lrw.Script.UI
{
    public class StartButton : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "";
        
        public void GoNextScene()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}