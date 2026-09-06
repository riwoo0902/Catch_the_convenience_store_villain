using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CWH.GameFlow
{
    public sealed class GameFlowView : MonoBehaviour
    {
        private GameObject _panel;
        private Image _background;
        private Button _advance;
        private TMP_Text _eyebrow;
        private TMP_Text _body;
        private TMP_Text _hint;
        private TMP_Text _page;
        private GameObject _buttons;
        private Button _restartButton;
        private Button _exitButton;
        private TMP_FontAsset _font;
        private GameFlowSettings _settings;
        private string[] _lines;
        private int _lineIndex;
        private float _visibleCharacters;
        private int _characterCount;
        private Action _onStoryComplete;
        private bool _storyActive;
        public Canvas Canvas { get; private set; }

        public void Build(GameFlowSettings settings)
        {
            _settings = settings;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 64);
#endif
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            GameObject canvasObject = new("Shift Presentation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas = canvasObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 30000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            _panel = Rect("Story Panel", canvasObject.transform, Vector2.zero, Vector2.one);
            _background = _panel.AddComponent<Image>();
            _background.color = Color.black;
            _advance = _panel.AddComponent<Button>();
            _advance.transition = Selectable.Transition.None;
            _advance.onClick.AddListener(AdvanceStory);
            _eyebrow = Text("Chapter", _panel.transform, new Vector2(0.12f, 0.77f), new Vector2(0.88f, 0.84f), 23, new Color(1f, 0.72f, 0.28f));
            _body = Text("Narrative", _panel.transform, new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.73f), 42, Color.white);
            _body.lineSpacing = 24;
            _body.enableAutoSizing = true;
            _body.fontSizeMin = 28;
            _body.fontSizeMax = 42;
            _hint = Text("Continue Hint", _panel.transform, new Vector2(0.12f, 0.17f), new Vector2(0.88f, 0.23f), 22, new Color(0.62f, 0.62f, 0.62f));
            _page = Text("Page", _panel.transform, new Vector2(0.12f, 0.86f), new Vector2(0.88f, 0.91f), 18, new Color(0.45f, 0.45f, 0.45f));
            _buttons = Rect("Result Actions", _panel.transform, new Vector2(0.24f, 0.16f), new Vector2(0.76f, 0.26f));
            _restartButton = ActionButton("Restart", _buttons.transform, new Vector2(0, 0), new Vector2(0.47f, 1), "다시 근무", new Color(1f, 0.72f, 0.28f), Color.black);
            _exitButton = ActionButton("Exit", _buttons.transform, new Vector2(0.53f, 0), Vector2.one, "나가기", new Color(0.15f, 0.15f, 0.15f), Color.white);
            _buttons.SetActive(false);
        }

        public void ShowStory(string chapter, string[] lines, Action onComplete)
        {
            _panel.SetActive(true);
            _background.color = Color.black;
            _advance.enabled = true;
            _buttons.SetActive(false);
            _eyebrow.text = chapter;
            _lines = lines ?? Array.Empty<string>();
            _lineIndex = 0;
            _onStoryComplete = onComplete;
            _storyActive = true;
            if (_lines.Length == 0) { FinishStory(); return; }
            PresentLine();
        }

        private void PresentLine()
        {
            _body.text = _lines[_lineIndex];
            _body.maxVisibleCharacters = int.MaxValue;
            _body.ForceMeshUpdate();
            _characterCount = _body.textInfo.characterCount;
            _visibleCharacters = 0f;
            _body.maxVisibleCharacters = 0;
            _hint.text = "클릭하면 문장 전체 보기";
            _page.text = $"{_lineIndex + 1:00} / {_lines.Length:00}";
        }

        private void Update()
        {
            if (!_storyActive || _body.maxVisibleCharacters >= _characterCount) return;
            _visibleCharacters += Time.unscaledDeltaTime * _settings.CharactersPerSecond;
            _body.maxVisibleCharacters = Mathf.Min(_characterCount, Mathf.FloorToInt(_visibleCharacters));
            if (_body.maxVisibleCharacters >= _characterCount) _hint.text = "클릭하여 계속";
        }

        public void AdvanceStory()
        {
            if (!_storyActive) return;
            if (_body.maxVisibleCharacters < _characterCount)
            {
                _visibleCharacters = _characterCount;
                _body.maxVisibleCharacters = _characterCount;
                _hint.text = "클릭하여 계속";
                return;
            }
            _lineIndex++;
            if (_lineIndex >= _lines.Length) FinishStory();
            else PresentLine();
        }

        private void FinishStory()
        {
            _storyActive = false;
            Action callback = _onStoryComplete;
            _onStoryComplete = null;
            callback?.Invoke();
        }

        public void Hide()
        {
            _storyActive = false;
            _panel.SetActive(false);
        }

        public void ShowCheckout()
        {
            _storyActive = false;
            _panel.SetActive(true);
            _background.color = Color.clear;
            _advance.enabled = false;
            _buttons.SetActive(false);
            _page.text = "";
            _body.text = "";
            _eyebrow.text = "24:00  ·  근무 종료";
            _hint.text = "손님, 지금부터는 제 알 바 아닙니다.";
        }

        public void ShowResults(bool won, float elapsedSeconds, int arrests, UnityAction restart, UnityAction exit)
        {
            _storyActive = false;
            _panel.SetActive(true);
            _background.color = Color.black;
            _advance.enabled = false;
            _eyebrow.text = won ? "근무 종료 보고서" : "근무 중단 보고서";
            _body.maxVisibleCharacters = int.MaxValue;
            _body.text = $"{(won ? "살아서 퇴근했다." : "체력 품절. 근무 종료.")}\n\n생존 시간  {Mathf.FloorToInt(elapsedSeconds / 60):00}:{Mathf.FloorToInt(elapsedSeconds % 60):00}     |     체포  {arrests}명";
            _page.text = won ? "SHIFT COMPLETE" : "GAME OVER";
            _hint.text = "";
            _buttons.SetActive(true);
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(restart);
            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(exit);
            _exitButton.GetComponentInChildren<TMP_Text>().text = won ? "타이틀로" : "나가기";
        }

        private TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, float size, Color color)
        {
            GameObject go = Rect(name, parent, min, max);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private Button ActionButton(string name, Transform parent, Vector2 min, Vector2 max, string label, Color bg, Color fg)
        {
            GameObject go = Rect(name, parent, min, max);
            go.AddComponent<Image>().color = bg;
            Button button = go.AddComponent<Button>();
            TMP_Text text = Text("Label", go.transform, Vector2.zero, Vector2.one, 28, fg);
            text.text = label;
            return button;
        }

        private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go;
        }

        private void OnDestroy()
        {
            if (_font != null && _font != TMP_Settings.defaultFontAsset) Destroy(_font);
        }
    }
}
