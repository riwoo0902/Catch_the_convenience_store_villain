using DG.Tweening;
using UnityEngine;

namespace Client.Visuals
{
    public class EyeBlinker : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField] private Vector3 openScale = new Vector3(0.05f, 0.05f, 0.05f);
        [SerializeField] private Vector3 closedScale = new Vector3(0.05f, 0.003f, 0.05f);
        [SerializeField] private float minBlinkInterval = 2f;
        [SerializeField] private float maxBlinkInterval = 5f;
        [SerializeField] private float closeDuration = 0.06f;
        [SerializeField] private float closedDuration = 0.04f;
        [SerializeField] private float openDuration = 0.08f;
        [SerializeField] private bool playOnStart = true;

        private Sequence _blinkSequence;
        private Tween _waitTween;

        private void Awake()
        {
            if (eye == null)
                eye = transform;

            eye.localScale = openScale;
        }

        private void Start()
        {
            if (playOnStart)
                ScheduleNextBlink();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void PlayBlink()
        {
            if (eye == null)
                return;

            _blinkSequence?.Kill();
            _blinkSequence = DOTween.Sequence()
                .Append(eye.DOScale(closedScale, closeDuration).SetEase(Ease.OutQuad))
                .AppendInterval(closedDuration)
                .Append(eye.DOScale(openScale, openDuration).SetEase(Ease.OutQuad))
                .OnComplete(ScheduleNextBlink)
                .SetTarget(eye);
        }

        public void StopBlinking()
        {
            KillTweens();

            if (eye != null)
                eye.localScale = openScale;
        }

        private void ScheduleNextBlink()
        {
            if (!isActiveAndEnabled)
                return;

            float interval = Random.Range(minBlinkInterval, maxBlinkInterval);
            _waitTween?.Kill();
            _waitTween = DOVirtual.DelayedCall(interval, PlayBlink).SetTarget(this);
        }

        private void KillTweens()
        {
            _waitTween?.Kill();
            _blinkSequence?.Kill();
            _waitTween = null;
            _blinkSequence = null;
        }

        private void OnValidate()
        {
            minBlinkInterval = Mathf.Max(0.01f, minBlinkInterval);
            maxBlinkInterval = Mathf.Max(minBlinkInterval, maxBlinkInterval);
            closeDuration = Mathf.Max(0.01f, closeDuration);
            closedDuration = Mathf.Max(0f, closedDuration);
            openDuration = Mathf.Max(0.01f, openDuration);
        }
    }
}
