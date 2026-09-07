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
        private GameObject _tutorialPanel;
        private TMP_Text _tutorialTitle;
        private TMP_Text _tutorialBody;
        private TMP_Text _tutorialHint;
        private TMP_Text _tutorialTitleShadow;
        private TMP_Text _tutorialBodyShadow;
        private TMP_Text _tutorialHintShadow;
        private GameObject _applicationPage;
        private bool _useOpeningImage;
        private Button _tutorialConfirm;
        private Action _onTutorialConfirm;
        private static readonly Vector2 TutorialSize = new(520f, 340f);
        private const float TutorialGap = 28f;
        private const float A4AspectRatio = 210f / 297f;
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
            _applicationPage = Rect("Employment Application", _panel.transform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.84f));
            GameObject paper = Rect("Application Image", _applicationPage.transform, Vector2.zero, Vector2.one);
            RawImage applicationImage = paper.AddComponent<RawImage>();
            applicationImage.texture = settings.OpeningApplication;
            applicationImage.raycastTarget = false;
            AspectRatioFitter paperFit = paper.AddComponent<AspectRatioFitter>();
            paperFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            paperFit.aspectRatio = A4AspectRatio;
            _applicationPage.SetActive(false);
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
            _tutorialPanel = Rect("Hands-on Tutorial", canvasObject.transform, Vector2.zero, Vector2.zero);
            RectTransform tutorialRect = (RectTransform)_tutorialPanel.transform;
            tutorialRect.anchorMin = tutorialRect.anchorMax = tutorialRect.pivot = new Vector2(0.5f, 0.5f);
            tutorialRect.sizeDelta = TutorialSize;
            _tutorialTitle = ShadowedText("Tutorial Title", new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f), 26,
                new Color(1f, 0.86f, 0.42f), TextAlignmentOptions.BottomLeft, out _tutorialTitleShadow);
            _tutorialBody = ShadowedText("Tutorial Objective", new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.76f), 26,
                Color.white, TextAlignmentOptions.TopLeft, out _tutorialBodyShadow);
            _tutorialHint = ShadowedText("Tutorial Progress", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.23f), 20,
                new Color(1f, 0.93f, 0.76f), TextAlignmentOptions.TopLeft, out _tutorialHintShadow);
            _tutorialConfirm = ActionButton("Tutorial Confirm", _tutorialPanel.transform, new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.22f), "알겠습니다 (Enter)", new Color(1f, 0.72f, 0.28f), Color.black);
            _tutorialConfirm.onClick.AddListener(() => _onTutorialConfirm?.Invoke());
            _tutorialConfirm.gameObject.SetActive(false);
            _tutorialPanel.SetActive(false);
        }

        public void ShowTutorial(string title, string objective, string hint, Action onConfirm = null, RectTransform anchor = null)
        {
            _tutorialPanel.SetActive(true);
            PlaceTutorial(anchor);
            _tutorialTitle.text = title;
            _tutorialBody.text = objective;
            _tutorialHint.text = hint;
            _tutorialTitleShadow.text = title;
            _tutorialBodyShadow.text = objective;
            _tutorialHintShadow.text = hint;
            _onTutorialConfirm = onConfirm;
            _tutorialConfirm.gameObject.SetActive(onConfirm != null);
            _tutorialHint.gameObject.SetActive(onConfirm == null);
            _tutorialHintShadow.gameObject.SetActive(onConfirm == null);
        }

        /// <summary>Float the guidance beside the button a step asks for so the words sit where the click belongs.</summary>
        private void PlaceTutorial(RectTransform anchor)
        {
            RectTransform panel = (RectTransform)_tutorialPanel.transform;
            Vector2 half = TutorialSize * 0.5f;
            Vector2 bounds = ((RectTransform)Canvas.transform).rect.size * 0.5f;
            if (anchor == null || !anchor.gameObject.activeInHierarchy)
            {
                panel.anchoredPosition = new Vector2(-bounds.x + half.x + TutorialGap, bounds.y - half.y - TutorialGap);
                return;
            }
            Rect target = ToCanvasRect(anchor);
            Rect block = ToCanvasRect(KeepOut(anchor));
            float x = block.xMax + TutorialGap + half.x;
            if (x + half.x > bounds.x) x = block.xMin - TutorialGap - half.x;
            panel.anchoredPosition = new Vector2(
                Mathf.Clamp(x, -bounds.x + half.x, bounds.x - half.x),
                Mathf.Clamp(target.center.y, -bounds.y + half.y, bounds.y - half.y));
        }

        /// <summary>Outermost panel the button lives in, so guidance sits beside the phone instead of over its screen.</summary>
        private static RectTransform KeepOut(RectTransform anchor)
        {
            Canvas canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return anchor;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            RectTransform result = anchor;
            for (Transform parent = anchor.parent; parent != null && parent != canvasRect; parent = parent.parent)
                if (parent is RectTransform rect && rect.rect.width < canvasRect.rect.width) result = rect;
            return result;
        }

        private Rect ToCanvasRect(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            RectTransform canvasRect = (RectTransform)Canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, corners[0], null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, corners[2], null, out Vector2 max);
            return new Rect(min, max - min);
        }

        public void HideTutorial()
        {
            _onTutorialConfirm = null;
            if (_tutorialPanel != null) _tutorialPanel.SetActive(false);
        }

        public void ShowStory(string chapter, string[] lines, Action onComplete, bool useOpeningImage = false)
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
            _useOpeningImage = useOpeningImage && _settings.OpeningApplication != null;
            if (_lines.Length == 0) { FinishStory(); return; }
            PresentLine();
        }

        private void PresentLine()
        {
            bool imagePage = _useOpeningImage && _lineIndex == 0;
            SetImagePage(imagePage);
            if (imagePage)
            {
                _characterCount = 0;
                _body.maxVisibleCharacters = int.MaxValue;
                _hint.text = "클릭하여 다음 이야기로";
                return;
            }
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
            SetImagePage(false);
            _storyActive = false;
            _panel.SetActive(true);
            _background.color = Color.clear;
            _advance.enabled = false;
            _buttons.SetActive(false);
            _page.text = "";
            _body.text = "";
            _eyebrow.text = "24:00   근무 종료";
            _hint.text = "손님, 지금부터는 제 알 바 아닙니다.";
        }

        public void ShowResults(bool won, float elapsedSeconds, int arrests, UnityAction restart, UnityAction exit)
        {
            SetImagePage(false);
            _storyActive = false;
            _panel.SetActive(true);
            _background.color = Color.black;
            _advance.enabled = false;
            _eyebrow.text = won ? "오늘의 근무 기록" : "중단된 근무 기록";
            _body.maxVisibleCharacters = int.MaxValue;
            _body.text = $"{(won ? "오늘도 무사히 퇴근했다." : "더는 버티지 못하고 근무가 끝났다.")}\n\n근무 시간   {Mathf.FloorToInt(elapsedSeconds / 60):00}:{Mathf.FloorToInt(elapsedSeconds % 60):00}\n신고 처리   {arrests}건";
            _page.text = won ? "무사 퇴근" : "근무 중단";
            _hint.text = "";
            _buttons.SetActive(true);
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(restart);
            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(exit);
            _exitButton.GetComponentInChildren<TMP_Text>().text = won ? "타이틀로" : "나가기";
        }

        private void SetImagePage(bool visible)
        {
            _applicationPage.SetActive(visible);
            _body.gameObject.SetActive(!visible);
            _eyebrow.gameObject.SetActive(!visible);
            _page.gameObject.SetActive(!visible);
            RectTransform hintRect = (RectTransform)_hint.transform;
            hintRect.anchorMin = new Vector2(0.12f, visible ? 0.09f : 0.17f);
            hintRect.anchorMax = new Vector2(0.88f, visible ? 0.15f : 0.23f);
        }

        /// <summary>
        /// Guidance floats over the store with no panel behind it, so every line is drawn twice:
        /// a black copy one pixel down and across, then the real one on top. Cheap, and it never
        /// depends on the font atlas having room for an outline.
        /// </summary>
        private TMP_Text ShadowedText(string name, Vector2 min, Vector2 max, float size, Color color,
            TextAlignmentOptions alignment, out TMP_Text shadow)
        {
            shadow = Text(name + " Shadow", _tutorialPanel.transform, min, max, size, new Color(0f, 0f, 0f, 0.85f));
            TMP_Text front = Text(name, _tutorialPanel.transform, min, max, size, color);
            foreach (TMP_Text text in new[] { shadow, front })
            {
                text.alignment = alignment;
                text.fontStyle = FontStyles.Bold;
                text.enableAutoSizing = true;
                text.fontSizeMin = size * 0.75f;
                text.fontSizeMax = size;
            }

            ((RectTransform)shadow.transform).anchoredPosition = new Vector2(2f, -2f);
            return front;
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
