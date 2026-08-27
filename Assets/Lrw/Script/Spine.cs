using System;
using UnityEngine;

namespace Lrw.Script
{
    public class Spine : MonoBehaviour
    {
        [SerializeField] private float speed = 5;
        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            audioSource.Play();
        }

        private void FixedUpdate()
        {
            transform.Rotate(Vector3.up, speed * Time.fixedDeltaTime);
            
        }
        
    }
}