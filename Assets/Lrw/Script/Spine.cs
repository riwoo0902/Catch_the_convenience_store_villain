using System;
using UnityEngine;

namespace Lrw.Script
{
    public class Spine : MonoBehaviour
    {
        [SerializeField] private float speed = 5;

        private void FixedUpdate()
        {
            transform.Rotate(Vector3.up, speed * Time.fixedDeltaTime);
        }
    }
}