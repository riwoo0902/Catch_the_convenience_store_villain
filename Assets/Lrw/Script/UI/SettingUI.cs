using System;
using DG.Tweening;
using UnityEngine;

namespace Lrw.Script.UI
{
    public class SettingUI : MonoBehaviour
    {
        [SerializeField] private Vector2 hidePos;
        [SerializeField] private float hideDuration = 1f;
        [SerializeField] private Ease ease = Ease.Linear;
        
        private Vector2 _showPos;
        private bool _isHide;
        private void Awake()
        {
            _showPos = transform.position;
            transform.position = hidePos;
            _isHide = true;
        }

        public void ShowHide()
        {
            Vector2 targetPos = _isHide ? _showPos : hidePos;
            
            transform.DOKill();
            transform.DOMove(targetPos, hideDuration).SetEase(ease);
            
            _isHide = !_isHide;
        }
        
        
    }
}