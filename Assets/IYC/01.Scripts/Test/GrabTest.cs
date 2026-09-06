using UnityEngine;
using UnityEngine.InputSystem;

namespace CWH.Player.Test
{
    // 파피 플레이타임 그랩팩처럼, 닿은 리지드바디 오브젝트를 잡았다 놓는 실험용 스크립트.
    // 이 오브젝트의 Collider는 Is Trigger 체크 필요.
    [RequireComponent(typeof(Collider))]
    public sealed class GrabTest : MonoBehaviour
    {
        [SerializeField] private Transform _holdPoint;

        private Rigidbody _touchingRigidbody;
        private Rigidbody _heldRigidbody;
        private Transform _heldOriginalParent;
        private bool _heldWasKinematic;

        private void Awake()
        {
            if (_holdPoint == null)
            {
                _holdPoint = transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null && _heldRigidbody == null)
            {
                _touchingRigidbody = rb;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody == _touchingRigidbody)
            {
                _touchingRigidbody = null;
            }
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (_heldRigidbody != null)
                {
                    Release();
                }
                else if (_touchingRigidbody != null)
                {
                    Grab(_touchingRigidbody);
                }
            }

            if (_heldRigidbody != null)
            {
                _heldRigidbody.transform.position = _holdPoint.position;
                _heldRigidbody.transform.rotation = _holdPoint.rotation;
            }
        }

        private void Grab(Rigidbody rb)
        {
            _heldRigidbody = rb;
            _heldOriginalParent = rb.transform.parent;
            _heldWasKinematic = rb.isKinematic;

            rb.isKinematic = true;
            rb.transform.SetParent(_holdPoint);
        }

        private void Release()
        {
            _heldRigidbody.transform.SetParent(_heldOriginalParent);
            _heldRigidbody.isKinematic = _heldWasKinematic;
            _heldRigidbody.linearVelocity = Vector3.zero;
            _heldRigidbody.angularVelocity = Vector3.zero;
            _heldRigidbody = null;
        }
    }
}
