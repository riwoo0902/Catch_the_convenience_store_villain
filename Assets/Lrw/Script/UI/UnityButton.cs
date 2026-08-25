using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI
{
    [RequireComponent(typeof(Button))]
    public class UnityButton : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _tmp;
        
        public event Action OnClick;
        
        private string _text;

        public string Text
        {
            get => _text;
            set => SetText(value);
        }
        
        public void Awake()
        {
            Button button = GetComponent<Button>();
            
            if(button == null) throw new Exception("Button is null");
            
            _button = button;
            
            button.onClick.AddListener(Invoke);
            _tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            _text = _tmp != null ? _tmp.text : "";
        }

        private void Destroy()
        {
            _button?.onClick.RemoveListener(Invoke);
        }
        
        private void SetText(string text)
        {
            if(_tmp != null) _tmp.text = text;
        }

        private void Invoke() => OnClick?.Invoke();


        private void OnValidate()
        {
        
            
        }
        
    }
}