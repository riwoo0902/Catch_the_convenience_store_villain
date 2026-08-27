using UnityEngine;

namespace Lrw.Script.UI
{
    public class ExitMono : MonoBehaviour
    {
        public void Exit() 
            => Game.Exit();
    }
}