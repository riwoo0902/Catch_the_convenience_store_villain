using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CWH.Player.Test
{
    // 그냥 재미로 만든 당구 큐대 실험용 스크립트.
    // 좌클릭(Attack)을 누르고 있으면 z가 -0.5까지 천천히 뒤로 빠지고(차징),
    // 떼는 순간 z가 1까지 빠르게 튀어나간다(스트로크).
    public sealed class CueStickTest : MonoBehaviour
    {
        private enum ClickButton
        {
            Left,
            Right
        }

        [SerializeField] private ClickButton _button = ClickButton.Left;
        [SerializeField] private float _chargeZ = -0.5f;
        [SerializeField] private float _chargeSpeed = 1f;
        [SerializeField] private float _strikeZ = 1f;
        [SerializeField] private float _strikeSpeed = 15f;
        [SerializeField] private TMP_Text _chargingText;

        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var isCharging = _button == ClickButton.Left
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.rightButton.isPressed;

            var localPosition = transform.localPosition;
            var targetZ = isCharging ? _chargeZ : _strikeZ;
            var speed = isCharging ? _chargeSpeed : _strikeSpeed;
            localPosition.z = Mathf.MoveTowards(localPosition.z, targetZ, speed * Time.deltaTime);
            transform.localPosition = localPosition;

            if (_chargingText != null)
            {
                _chargingText.gameObject.SetActive(isCharging);
            }
        }
    }
}
