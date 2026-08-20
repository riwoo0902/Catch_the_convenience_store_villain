using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Lrw.Script.Map
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;


        private void OnTriggerEnter(Collider other)
        {
            
        }

        private void OnTriggerExit(Collider other)
        {
            
        }
        
        
    }
}