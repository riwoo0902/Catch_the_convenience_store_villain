using System.Collections;
using CWH.GameFlow;
using CWH.Player.Health;
using CWH.Quests;
using CWH.Villains;
using Gree.UnityWebView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CWH.Player.UI
{
    public enum PhoneApp { None, Home, Clock, Stocks, Youtube, Mail, Phone }

    [DisallowMultipleComponent]
    public sealed class PlayerHUDController : MonoBehaviour
    {
        private const string SettingsResourceName = "PlayerHUDSettings";
        private const string YoutubeUrl = "https://m.youtube.com/?persist_app=1&app=m";
        private const string MobileYoutubeUserAgent =
            "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36";
        private static TMP_FontAsset _runtimeUiFont;

        private PlayerHealth _health;
        private TextMeshProUGUI _healthText;
        private GameObject _mailToastPanel;
        private TextMeshProUGUI _mailToastText;
        private float _mailToastHideTime;
        private int _questRevision = -1;
        private GameObject _villainAlertPanel;
        private TextMeshProUGUI _villainAlertText;
        private Coroutine _villainAlertRoutine;
        private GameObject _policeCountdownPanel;
        private TextMeshProUGUI _policeCountdownText;
        private GameObject _phoneOverlay;
        private GameObject _homeScreen;
        private GameObject _youtubeScreen;
        private GameObject _phoneDialerScreen;
        private GameObject _mailScreen;
        private GameObject _stocksScreen;
        private GameObject _clockScreen;
        private TextMeshProUGUI _homeStatusText;
        private TextMeshProUGUI _clockTimeText;
        private TextMeshProUGUI _clockRemainingText;
        private RectTransform _clockProgressFill;
        private RectTransform _phoneRect;
        private RectTransform _youtubeWebViewViewport;
        private RectTransform _mailContent;
        private Image _healthFill;
        private WebViewObject _youtubeWebView;
        private TextMeshProUGUI _youtubeStatusText;
        private TextMeshProUGUI _dialedNumberText;
        private TextMeshProUGUI _dialerStatusText;
        private Button _emergencyCallButton;
        private Behaviour _playerLookController;
        private GameObject _crosshair;
        private bool _lookControllerWasEnabled;
        private bool _crosshairWasActive;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;
        private bool _youtubePageRequested;
        private bool _policeCallPending;
        private bool _gameplayEnabled = true;
        private bool _responseWasActive;
        private Coroutine _policeCallCoroutine;
        private string _dialedNumber = string.Empty;
        private string _lastClockText;
        private int _lastRemainingMinutes = -1;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private readonly System.Collections.Generic.Dictionary<string, Button> _tutorialButtons = new();
        private RectTransform _tutorialHighlight;

        public event System.Action<string> PhoneActionPerformed;
        public string DialedNumber => _dialedNumber;

        public bool IsPhoneOpen => _phoneOverlay != null && _phoneOverlay.activeInHierarchy;
        public PhoneApp CurrentApp
        {
            get
            {
                if (!IsPhoneOpen) return PhoneApp.None;
                if (_clockScreen.activeSelf) return PhoneApp.Clock;
                if (_stocksScreen.activeSelf) return PhoneApp.Stocks;
                if (_youtubeScreen.activeSelf) return PhoneApp.Youtube;
                if (_mailScreen.activeSelf) return PhoneApp.Mail;
                if (_phoneDialerScreen.activeSelf) return PhoneApp.Phone;
                return PhoneApp.Home;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneInstallation()
        {
            SceneManager.sceneLoaded -= InstallOnScene;
            SceneManager.sceneLoaded += InstallOnScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnInitialScene()
        {
            InstallOnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void InstallOnScene(Scene scene, LoadSceneMode mode)
        {
            if (!GameLoopController.IsGameplayScene(scene))
            {
                return;
            }

            foreach (PlayerHUDController existing in FindObjectsByType<PlayerHUDController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing.gameObject.scene == scene)
                {
                    return;
                }
            }

            Canvas targetCanvas = CreateFallbackCanvas();
            SceneManager.MoveGameObjectToScene(targetCanvas.gameObject, scene);
            targetCanvas.gameObject.AddComponent<PlayerHUDController>();
        }

        private static Canvas CreateFallbackCanvas()
        {
            GameObject canvasObject = new("Player HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            return canvas;
        }

        private void Awake()
        {
            PlayerHUDSettings settings = Resources.Load<PlayerHUDSettings>(SettingsResourceName);
            _health = PlayerHealth.GetOrCreate();
            BuildInterface(settings);

            if (_health != null)
            {
                _health.HealthChanged += RefreshHealthText;
                RefreshHealthText(_health.CurrentHealth, _health.MaxHealth);
            }
            else
            {
                _healthText.SetText("-- / --");
            }

            ConvenienceStoreVillainSpawner.VillainEnteredStore += ShowVillainEntryAlert;
            ConvenienceStoreVillainSpawner.VillainBecameAngry += ShowVillainAngryAlert;
            _phoneOverlay.SetActive(false);
            SetGameplayEnabled(GameLoopController.AllowsGameplay);
        }

        private void Update()
        {
            if (_mailToastPanel != null && _mailToastPanel.activeSelf && Time.unscaledTime >= _mailToastHideTime)
            {
                _mailToastPanel.SetActive(false);
            }

            if (_questRevision != ShiftQuestBoard.Revision)
            {
                string announcement = ShiftQuestBoard.TakeAnnouncement();
                if (!string.IsNullOrEmpty(announcement))
                {
                    ShowMailToast(announcement);
                }

                // Repaint only while the inbox is open; otherwise wait until the player opens it.
                if (_mailScreen != null && _mailScreen.activeInHierarchy)
                {
                    _questRevision = ShiftQuestBoard.Revision;
                    RefreshQuestMail();
                }
            }

            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay)
            {
                if (_phoneOverlay.activeSelf || _policeCallPending)
                {
                    SetGameplayEnabled(false);
                }

                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                SetPhoneOpen(!_phoneOverlay.activeSelf);
            }

            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
            {
                RefreshPhoneSize();
            }

            RefreshClock();
            bool responseActive = PoliceResponseController.IsResponseActive;
            if (_responseWasActive != responseActive)
            {
                _responseWasActive = responseActive;
                RefreshEmergencyCallButton();
            }
        }

        public void SetGameplayEnabled(bool isEnabled)
        {
            _gameplayEnabled = isEnabled;
            if (!isEnabled)
            {
                if (_phoneOverlay != null && _phoneOverlay.activeSelf)
                {
                    SetPhoneOpen(false);
                }

                _health?.SetYoutubeHealing(false);
                HideYoutubeWebView();
                CancelPendingEmergencyCall();
            }

            RefreshEmergencyCallButton();
            RefreshClock();
        }

        private void BuildInterface(PlayerHUDSettings settings)
        {
            RectTransform canvasRect = (RectTransform)transform;

            _phoneOverlay = CreateRectObject("PhoneOverlay", canvasRect);
            StretchToParent((RectTransform)_phoneOverlay.transform);

            Image dimBackground = _phoneOverlay.AddComponent<Image>();
            dimBackground.color = new Color(0f, 0f, 0f, 0.42f);

            GameObject phoneObject = CreateRectObject("Phone", _phoneOverlay.transform);
            _phoneRect = (RectTransform)phoneObject.transform;
            _phoneRect.anchorMin = new Vector2(0.5f, 0.5f);
            _phoneRect.anchorMax = new Vector2(0.5f, 0.5f);
            _phoneRect.pivot = new Vector2(0.5f, 0.5f);
            _phoneRect.anchoredPosition = Vector2.zero;

            Image phoneImage = phoneObject.AddComponent<Image>();
            phoneImage.sprite = settings != null ? settings.PhoneSprite : null;
            phoneImage.preserveAspect = true;
            phoneImage.raycastTarget = false;
            if (phoneImage.sprite == null)
            {
                phoneImage.color = new Color(0.05f, 0.05f, 0.06f, 1f);
            }

            BuildHomeScreen(settings);
            BuildYoutubeScreen();
            BuildPhoneDialerScreen(settings);
            BuildStocksScreen();
            BuildMailQuestScreen();
            BuildClockScreen();
            BuildVillainAlertDisplay(canvasRect);
            BuildPoliceCountdownDisplay(canvasRect);
            BuildMailToast(canvasRect);
            RefreshPhoneSize();
            foreach (Button button in _phoneOverlay.GetComponentsInChildren<Button>(true))
            {
                _tutorialButtons[button.name] = button;
                button.onClick.AddListener(() => PhoneActionPerformed?.Invoke(button.name));
            }
        }

        /// <summary>Returns the highlighted button so guidance text can be placed beside it.</summary>
        public RectTransform HighlightTutorialButton(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName) || !_tutorialButtons.TryGetValue(buttonName, out Button button)
                || !button.gameObject.activeInHierarchy)
            {
                if (_tutorialHighlight != null) _tutorialHighlight.gameObject.SetActive(false);
                return null;
            }
            if (_tutorialHighlight == null)
            {
                _tutorialHighlight = (RectTransform)CreateRectObject("Tutorial Button Highlight", button.transform).transform;
                for (int edge = 0; edge < 4; edge++)
                {
                    RectTransform border = (RectTransform)CreateRectObject("Border", _tutorialHighlight).transform;
                    StretchToParent(border);
                    if (edge < 2)
                    {
                        border.anchorMin = new Vector2(0f, edge);
                        border.anchorMax = new Vector2(1f, edge);
                        border.sizeDelta = new Vector2(0f, 5f);
                    }
                    else
                    {
                        border.anchorMin = new Vector2(edge - 2, 0f);
                        border.anchorMax = new Vector2(edge - 2, 1f);
                        border.sizeDelta = new Vector2(5f, 0f);
                    }
                    Image outline = border.gameObject.AddComponent<Image>();
                    outline.color = new Color(1f, 0.72f, 0.15f);
                    outline.raycastTarget = false;
                }
            }
            _tutorialHighlight.SetParent(button.transform, false);
            StretchToParent(_tutorialHighlight);
            _tutorialHighlight.offsetMin = new Vector2(-7f, -7f);
            _tutorialHighlight.offsetMax = new Vector2(7f, 7f);
            _tutorialHighlight.gameObject.SetActive(true);
            return (RectTransform)button.transform;
        }

        private void BuildHomeScreen(PlayerHUDSettings settings)
        {
            _homeScreen = CreateRectObject("PhoneHome", _phoneRect);
            RectTransform homeRect = (RectTransform)_homeScreen.transform;
            SetPhoneContentAnchors(homeRect);

            GameObject statusBar = CreateTextObject("HomeStatusBar", homeRect, "20:00", 17f, FontStyles.Normal, TextAlignmentOptions.Right);
            RectTransform statusBarRect = (RectTransform)statusBar.transform;
            statusBarRect.anchorMin = new Vector2(0.12f, 0.88f);
            statusBarRect.anchorMax = new Vector2(0.88f, 0.95f);
            statusBarRect.offsetMin = Vector2.zero;
            statusBarRect.offsetMax = Vector2.zero;
            _homeStatusText = statusBar.GetComponent<TextMeshProUGUI>();
            _homeStatusText.color = new Color(0.4f, 0.41f, 0.45f, 1f);

            GameObject phoneButtonObject = CreateRectObject("PhoneButton", homeRect);
            RectTransform phoneButtonRect = (RectTransform)phoneButtonObject.transform;
            SetHomeAppRect(phoneButtonRect, 0, 0, false);
            Image phoneButtonImage = phoneButtonObject.AddComponent<Image>();
            phoneButtonImage.sprite = settings != null ? settings.PhoneAppIcon : null;
            phoneButtonImage.preserveAspect = true;
            phoneButtonImage.color = phoneButtonImage.sprite != null
                ? Color.white
                : new Color(0.15f, 0.7f, 0.3f, 1f);
            Button phoneButton = phoneButtonObject.AddComponent<Button>();
            phoneButton.targetGraphic = phoneButtonImage;
            phoneButton.onClick.AddListener(ShowPhoneDialerScreen);
            if (phoneButtonImage.sprite == null)
            {
                CreatePhoneHandsetIcon(phoneButtonRect);
            }

            GameObject phoneLabel = CreateTextObject("PhoneLabel", homeRect, "전화", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetHomeAppRect((RectTransform)phoneLabel.transform, 0, 0, true);
            phoneLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject youtubeButtonObject = CreateRectObject("YoutubeButton", homeRect);
            RectTransform buttonRect = (RectTransform)youtubeButtonObject.transform;
            SetHomeAppRect(buttonRect, 1, 0, false);

            Image youtubeImage = youtubeButtonObject.AddComponent<Image>();
            youtubeImage.sprite = settings != null ? settings.YoutubeLogo : null;
            youtubeImage.preserveAspect = true;
            youtubeImage.color = youtubeImage.sprite != null ? Color.white : new Color(1f, 0f, 0f, 1f);

            Button youtubeButton = youtubeButtonObject.AddComponent<Button>();
            youtubeButton.targetGraphic = youtubeImage;
            youtubeButton.onClick.AddListener(ShowYoutubeScreen);

            GameObject appLabel = CreateTextObject("YoutubeLabel", homeRect, "YouTube", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform appLabelRect = (RectTransform)appLabel.transform;
            SetHomeAppRect(appLabelRect, 1, 0, true);
            appLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject stocksButtonObject = CreateRectObject("StocksButton", homeRect);
            RectTransform stocksButtonRect = (RectTransform)stocksButtonObject.transform;
            SetHomeAppRect(stocksButtonRect, 0, 1, false);
            Image stocksButtonImage = stocksButtonObject.AddComponent<Image>();
            stocksButtonImage.sprite = LoadStocksIcon();
            stocksButtonImage.preserveAspect = true;
            stocksButtonImage.color = stocksButtonImage.sprite != null
                ? Color.white
                : new Color(0.08f, 0.62f, 0.38f, 1f);
            Button stocksButton = stocksButtonObject.AddComponent<Button>();
            stocksButton.targetGraphic = stocksButtonImage;
            stocksButton.onClick.AddListener(ShowStocksScreen);
            if (stocksButtonImage.sprite == null)
            {
                CreateStocksChartIcon(stocksButtonRect);
            }

            GameObject stocksLabel = CreateTextObject("StocksLabel", homeRect, "주식", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetHomeAppRect((RectTransform)stocksLabel.transform, 0, 1, true);
            stocksLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject mailButtonObject = CreateRectObject("MailButton", homeRect);
            RectTransform mailButtonRect = (RectTransform)mailButtonObject.transform;
            SetHomeAppRect(mailButtonRect, 1, 1, false);
            Image mailButtonImage = mailButtonObject.AddComponent<Image>();
            mailButtonImage.sprite = settings != null ? settings.MailIcon : null;
            mailButtonImage.preserveAspect = true;
            mailButtonImage.color = mailButtonImage.sprite != null
                ? Color.white
                : new Color(0.12f, 0.48f, 0.9f, 1f);
            Button mailButton = mailButtonObject.AddComponent<Button>();
            mailButton.targetGraphic = mailButtonImage;
            mailButton.onClick.AddListener(ShowMailScreen);
            if (mailButtonImage.sprite == null)
            {
                CreateMailEnvelopeIcon(mailButtonRect);
            }

            GameObject mailLabel = CreateTextObject("MailLabel", homeRect, "메일", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetHomeAppRect((RectTransform)mailLabel.transform, 1, 1, true);
            mailLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameFlowSettings gameFlowSettings = Resources.Load<GameFlowSettings>("GameFlowSettings");
            GameObject clockButtonObject = CreateRectObject("ClockButton", homeRect);
            RectTransform clockButtonRect = (RectTransform)clockButtonObject.transform;
            SetHomeAppRect(clockButtonRect, 0, 2, false);
            Image clockButtonImage = clockButtonObject.AddComponent<Image>();
            clockButtonImage.sprite = gameFlowSettings != null ? gameFlowSettings.ClockIcon : null;
            if (clockButtonImage.sprite == null && settings != null)
            {
                clockButtonImage.sprite = settings.ClockIcon;
            }

            clockButtonImage.preserveAspect = true;
            clockButtonImage.color = clockButtonImage.sprite != null ? Color.white : new Color(0.1f, 0.18f, 0.27f, 1f);
            Button clockButton = clockButtonObject.AddComponent<Button>();
            clockButton.targetGraphic = clockButtonImage;
            clockButton.onClick.AddListener(ShowClockScreen);
            if (clockButtonImage.sprite == null)
            {
                GameObject clockFallback = CreateTextObject("ClockFallback", clockButtonRect, "24:00", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
                StretchToParent((RectTransform)clockFallback.transform);
            }

            GameObject clockLabel = CreateTextObject("ClockLabel", homeRect, "시계", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            SetHomeAppRect((RectTransform)clockLabel.transform, 0, 2, true);
            clockLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject hint = CreateTextObject("CloseHint", homeRect, "TAB 키를 누르면 닫힙니다", 16f, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = new Vector2(0.15f, 0.025f);
            hintRect.anchorMax = new Vector2(0.85f, 0.085f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            hint.GetComponent<TextMeshProUGUI>().color = new Color(0.45f, 0.45f, 0.5f, 1f);
        }

        private void BuildYoutubeScreen()
        {
            _youtubeScreen = CreateRectObject("YoutubeScreen", _phoneRect);
            RectTransform youtubeRect = (RectTransform)_youtubeScreen.transform;
            SetPhoneContentAnchors(youtubeRect);

            Image background = _youtubeScreen.AddComponent<Image>();
            background.color = new Color(0.97f, 0.97f, 0.97f, 1f);

            GameObject header = CreateRectObject("Header", youtubeRect);
            RectTransform headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 0.88f);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color(0.92f, 0.05f, 0.05f, 1f);

            GameObject headerText = CreateTextObject("HeaderText", headerRect, "YouTube", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform headerTextRect = (RectTransform)headerText.transform;
            StretchToParent(headerTextRect);
            headerTextRect.offsetMin = new Vector2(58f, 0f);

            GameObject webViewViewportObject = CreateRectObject("WebViewViewport", youtubeRect);
            _youtubeWebViewViewport = (RectTransform)webViewViewportObject.transform;
            _youtubeWebViewViewport.anchorMin = new Vector2(0.01f, 0.01f);
            _youtubeWebViewViewport.anchorMax = new Vector2(0.99f, 0.88f);
            _youtubeWebViewViewport.offsetMin = Vector2.zero;
            _youtubeWebViewViewport.offsetMax = Vector2.zero;
            Image webViewBackground = webViewViewportObject.AddComponent<Image>();
            webViewBackground.color = new Color(0.08f, 0.08f, 0.09f, 1f);
            webViewBackground.raycastTarget = false;

            GameObject loadingText = CreateTextObject("WebViewStatus", _youtubeWebViewViewport, "YOUTUBE READY", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)loadingText.transform);
            _youtubeStatusText = loadingText.GetComponent<TextMeshProUGUI>();

            GameObject backButtonObject = CreateRectObject("BackButton", headerRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.1f, 0.5f);
            backRect.anchorMax = new Vector2(0.1f, 0.5f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(52f, 42f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.68f, 0.02f, 0.02f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);

            GameObject backText = CreateTextObject("BackText", backRect, "<", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);

            _youtubeScreen.SetActive(false);
        }

        private void BuildPhoneDialerScreen(PlayerHUDSettings settings)
        {
            _phoneDialerScreen = CreateRectObject("PhoneDialerScreen", _phoneRect);
            RectTransform dialerRect = (RectTransform)_phoneDialerScreen.transform;
            SetPhoneContentAnchors(dialerRect);
            _phoneDialerScreen.AddComponent<Image>().color = new Color(0.95f, 0.97f, 0.96f, 1f);

            GameObject header = CreateRectObject("PhoneHeader", dialerRect);
            RectTransform headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 0.84f);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color(0.95f, 0.97f, 0.96f, 1f);
            CreateDivider(headerRect, new Color(0.84f, 0.86f, 0.85f, 1f));

            GameObject headerText = CreateTextObject("PhoneHeaderText", headerRect, "전화", 23f, FontStyles.Normal, TextAlignmentOptions.Left);
            RectTransform phoneHeaderTextRect = (RectTransform)headerText.transform;
            StretchToParent(phoneHeaderTextRect);
            phoneHeaderTextRect.offsetMin = new Vector2(20f, 0f);
            headerText.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.14f, 0.13f, 1f);

            GameObject numberPanel = CreateRectObject("NumberPanel", dialerRect);
            RectTransform numberPanelRect = (RectTransform)numberPanel.transform;
            numberPanelRect.anchorMin = new Vector2(0.08f, 0.69f);
            numberPanelRect.anchorMax = new Vector2(0.92f, 0.81f);
            numberPanelRect.offsetMin = Vector2.zero;
            numberPanelRect.offsetMax = Vector2.zero;
            numberPanel.AddComponent<Image>().color = new Color(0.95f, 0.97f, 0.96f, 1f);

            GameObject numberText = CreateTextObject("DialedNumber", numberPanelRect, string.Empty, 40f, FontStyles.Normal, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)numberText.transform);
            _dialedNumberText = numberText.GetComponent<TextMeshProUGUI>();
            _dialedNumberText.color = new Color(0.1f, 0.11f, 0.1f, 1f);
            _dialedNumberText.characterSpacing = 10f;

            GameObject statusText = CreateTextObject("DialerStatus", dialerRect, "112를 누르세요", 16f, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform statusRect = (RectTransform)statusText.transform;
            statusRect.anchorMin = new Vector2(0.08f, 0.61f);
            statusRect.anchorMax = new Vector2(0.92f, 0.68f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            _dialerStatusText = statusText.GetComponent<TextMeshProUGUI>();
            _dialerStatusText.color = new Color(0.45f, 0.48f, 0.46f, 1f);

            string[,] keys =
            {
                { "1", "2", "3" },
                { "4", "5", "6" },
                { "7", "8", "9" },
                { "CLR", "0", "DEL" }
            };

            for (int row = 0; row < keys.GetLength(0); row++)
            {
                for (int column = 0; column < keys.GetLength(1); column++)
                {
                    string key = keys[row, column];
                    CreateDialerKey(dialerRect, key, row, column);
                }
            }

            GameObject callButtonObject = CreateRectObject("EmergencyCallButton", dialerRect);
            RectTransform callButtonRect = (RectTransform)callButtonObject.transform;
            SetCenteredRect(callButtonRect, new Vector2(0f, -205f), new Vector2(88f, 58f));
            Image callButtonImage = callButtonObject.AddComponent<Image>();
            callButtonImage.sprite = settings != null ? settings.EmergencyCallIcon : null;
            callButtonImage.preserveAspect = true;
            callButtonImage.color = callButtonImage.sprite != null
                ? Color.white
                : new Color(0.08f, 0.7f, 0.25f, 1f);
            _emergencyCallButton = callButtonObject.AddComponent<Button>();
            _emergencyCallButton.targetGraphic = callButtonImage;
            _emergencyCallButton.onClick.AddListener(BeginEmergencyCall);
            _emergencyCallButton.interactable = false;
            if (callButtonImage.sprite == null)
            {
                CreatePhoneHandsetIcon(callButtonRect);
            }

            GameObject backButtonObject = CreateRectObject("PhoneBackButton", dialerRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            SetCenteredRect(backRect, new Vector2(0f, -285f), new Vector2(180f, 46f));
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.9f, 0.91f, 0.9f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);
            GameObject backText = CreateTextObject("PhoneBackText", backRect, "뒤로", 19f, FontStyles.Normal, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);
            backText.GetComponent<TextMeshProUGUI>().color = new Color(0.14f, 0.15f, 0.14f, 1f);

            _phoneDialerScreen.SetActive(false);
        }

        private void CreateDialerKey(RectTransform parent, string key, int row, int column)
        {
            GameObject buttonObject = CreateRectObject($"DialKey_{key}", parent);
            RectTransform buttonRect = (RectTransform)buttonObject.transform;
            float x = (column - 1) * 82f;
            float y = 72f - row * 66f;
            SetCenteredRect(buttonRect, new Vector2(x, y), new Vector2(68f, 52f));

            Image image = buttonObject.AddComponent<Image>();
            image.color = key == "CLR" || key == "DEL"
                ? new Color(0.87f, 0.88f, 0.87f, 1f)
                : new Color(0.93f, 0.94f, 0.93f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (key == "CLR")
            {
                button.onClick.AddListener(ClearDialedNumber);
            }
            else if (key == "DEL")
            {
                button.onClick.AddListener(DeleteLastDialedDigit);
            }
            else
            {
                button.onClick.AddListener(() => AppendDialedDigit(key));
            }

            string label = key == "CLR" ? "지움" : key == "DEL" ? "삭제" : key;
            GameObject textObject = CreateTextObject($"DialKeyText_{key}", buttonRect, label, key.Length > 1 ? 17f : 25f, FontStyles.Normal, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)textObject.transform);
            textObject.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.13f, 0.12f, 1f);
        }

        private void AppendDialedDigit(string digit)
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay || _policeCallPending || _dialedNumber.Length >= 3)
            {
                return;
            }

            _dialedNumber += digit;
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void ClearDialedNumber()
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay || _policeCallPending)
            {
                return;
            }

            _dialedNumber = string.Empty;
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void DeleteLastDialedDigit()
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay || _policeCallPending || _dialedNumber.Length == 0)
            {
                return;
            }

            _dialedNumber = _dialedNumber.Substring(0, _dialedNumber.Length - 1);
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void RefreshDialedNumber()
        {
            _dialedNumberText.SetText(_dialedNumber);
        }

        private void RefreshEmergencyCallButton()
        {
            bool responseActive = PoliceResponseController.IsResponseActive;
            bool canCall = _gameplayEnabled && GameLoopController.AllowsGameplay && !_policeCallPending && !responseActive && _dialedNumber == "112";
            if (_emergencyCallButton != null)
            {
                _emergencyCallButton.interactable = canCall;
            }

            if (_dialerStatusText != null && !_policeCallPending)
            {
                _dialerStatusText.SetText(responseActive ? "경찰이 출동했습니다" : canCall ? "통화 버튼을 누르세요" : "112를 누르세요");
                _dialerStatusText.color = new Color(0.45f, 0.48f, 0.46f, 1f);
            }
        }

        private void BeginEmergencyCall()
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay || _policeCallPending || _dialedNumber != "112")
            {
                return;
            }

            // Reserve the response at CALL press so later arrivals cannot turn a false report into a valid one.
            if (!PoliceResponseController.TryBeginEmergencyCall())
            {
                RefreshEmergencyCallButton();
                return;
            }

            _policeCallPending = true;
            _policeCallCoroutine = StartCoroutine(CompleteEmergencyCall());
            SetPhoneOpen(false);
        }

        private IEnumerator CompleteEmergencyCall()
        {
            if (_emergencyCallButton != null)
            {
                _emergencyCallButton.interactable = false;
            }

            ShowPoliceCountdown(true);
            float arrivalTime = Time.time + 5f;
            int previousSeconds = -1;
            while (Time.time < arrivalTime)
            {
                if (!_gameplayEnabled || !GameLoopController.AllowsGameplay)
                {
                    CancelPendingEmergencyCall();
                    yield break;
                }

                int seconds = Mathf.CeilToInt(arrivalTime - Time.time);
                if (seconds != previousSeconds)
                {
                    previousSeconds = seconds;
                    _dialerStatusText.SetText("{0}초 뒤 경찰 도착", seconds);
                    SetPoliceCountdown(seconds);
                }

                yield return null;
            }

            PoliceResponseController.CompleteEmergencyCall();
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay)
            {
                yield break;
            }

            _dialerStatusText.SetText("경찰 도착");
            _dialerStatusText.color = new Color(0.05f, 0.55f, 0.2f, 1f);
            SetPoliceCountdown(0);
            yield return new WaitForSeconds(0.65f);
            ShowPoliceCountdown(false);
            _policeCallPending = false;
            _policeCallCoroutine = null;
            _dialedNumber = string.Empty;
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void CancelPendingEmergencyCall()
        {
            if (_policeCallCoroutine != null)
            {
                StopCoroutine(_policeCallCoroutine);
                _policeCallCoroutine = null;
            }

            if (_policeCallPending)
            {
                PoliceResponseController.CancelEmergencyCall();
            }

            _policeCallPending = false;
            _dialedNumber = string.Empty;
            if (_dialedNumberText != null)
            {
                RefreshDialedNumber();
            }

            ShowPoliceCountdown(false);
        }

        private void BuildStocksScreen()
        {
            _stocksScreen = CreateRectObject("StocksScreen", _phoneRect);
            RectTransform stocksRect = (RectTransform)_stocksScreen.transform;
            SetPhoneContentAnchors(stocksRect);
            _stocksScreen.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f);

            GameObject header = CreateRectObject("StocksHeader", stocksRect);
            RectTransform headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 0.84f);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f);
            CreateDivider(headerRect, new Color(0.11f, 0.16f, 0.2f, 1f));

            GameObject headerText = CreateTextObject(
                "StocksHeaderText",
                headerRect,
                "주식",
                23f,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            RectTransform stocksHeaderTextRect = (RectTransform)headerText.transform;
            StretchToParent(stocksHeaderTextRect);
            stocksHeaderTextRect.offsetMin = new Vector2(20f, 0f);

            GameObject healthCard = CreateRectObject("HealthCard", stocksRect);
            RectTransform healthCardRect = (RectTransform)healthCard.transform;
            healthCardRect.anchorMin = new Vector2(0.07f, 0.3f);
            healthCardRect.anchorMax = new Vector2(0.93f, 0.76f);
            healthCardRect.offsetMin = Vector2.zero;
            healthCardRect.offsetMax = Vector2.zero;
            healthCard.AddComponent<Image>().color = new Color(0.07f, 0.1f, 0.13f, 1f);

            GameObject caption = CreateTextObject(
                "HealthCaption",
                healthCardRect,
                "내 체력",
                17f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            RectTransform captionRect = (RectTransform)caption.transform;
            captionRect.anchorMin = new Vector2(0.08f, 0.7f);
            captionRect.anchorMax = new Vector2(0.92f, 0.9f);
            captionRect.offsetMin = Vector2.zero;
            captionRect.offsetMax = Vector2.zero;
            caption.GetComponent<TextMeshProUGUI>().color = new Color(0.55f, 0.68f, 0.74f, 1f);

            GameObject healthTextObject = CreateTextObject(
                "StocksHealthText",
                healthCardRect,
                "100 / 100",
                38f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            RectTransform healthTextRect = (RectTransform)healthTextObject.transform;
            healthTextRect.anchorMin = new Vector2(0.04f, 0.36f);
            healthTextRect.anchorMax = new Vector2(0.96f, 0.7f);
            healthTextRect.offsetMin = Vector2.zero;
            healthTextRect.offsetMax = Vector2.zero;
            _healthText = healthTextObject.GetComponent<TextMeshProUGUI>();

            GameObject healthBar = CreateRectObject("HealthBar", healthCardRect);
            RectTransform healthBarRect = (RectTransform)healthBar.transform;
            healthBarRect.anchorMin = new Vector2(0.1f, 0.18f);
            healthBarRect.anchorMax = new Vector2(0.9f, 0.31f);
            healthBarRect.offsetMin = Vector2.zero;
            healthBarRect.offsetMax = Vector2.zero;
            healthBar.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 1f);

            GameObject healthFillObject = CreateRectObject("HealthFill", healthBarRect);
            RectTransform healthFillRect = (RectTransform)healthFillObject.transform;
            StretchToParent(healthFillRect);
            _healthFill = healthFillObject.AddComponent<Image>();
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _healthFill.fillAmount = 1f;
            _healthFill.raycastTarget = false;

            GameObject backButtonObject = CreateRectObject("StocksBackButton", stocksRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.5f, 0.09f);
            backRect.anchorMax = new Vector2(0.5f, 0.09f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(180f, 48f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.1f, 0.14f, 0.18f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);

            GameObject backText = CreateTextObject(
                "StocksBackText",
                backRect,
                "뒤로",
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);

            _stocksScreen.SetActive(false);
        }

        private void BuildClockScreen()
        {
            _clockScreen = CreateRectObject("ClockScreen", _phoneRect);
            RectTransform clockRect = (RectTransform)_clockScreen.transform;
            SetPhoneContentAnchors(clockRect);
            _clockScreen.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 1f);

            Color muted = new(0.5f, 0.64f, 0.72f, 1f);
            Color accent = new(0.48f, 0.92f, 0.82f, 1f);
            CreateClockLabel("ClockHeader", clockRect, "시계", 28f, 0.82f, 0.97f, Color.white);
            CreateClockLabel("ClockCaption", clockRect, "현재 시각", 19f, 0.69f, 0.79f, muted);
            _clockTimeText = CreateClockLabel("CurrentTime", clockRect, "20:00", 68f, 0.52f, 0.72f, accent);
            _clockTimeText.enableAutoSizing = true;
            _clockTimeText.fontSizeMin = 35f;
            _clockTimeText.fontSizeMax = 68f;

            GameObject progressTrack = CreateRectObject("ShiftProgressTrack", clockRect);
            RectTransform trackRect = (RectTransform)progressTrack.transform;
            trackRect.anchorMin = new Vector2(0.1f, 0.46f);
            trackRect.anchorMax = new Vector2(0.9f, 0.475f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            progressTrack.AddComponent<Image>().color = new Color(0.12f, 0.19f, 0.23f, 1f);
            GameObject progress = CreateRectObject("ShiftProgress", trackRect);
            _clockProgressFill = (RectTransform)progress.transform;
            StretchToParent(_clockProgressFill);
            Image fill = progress.AddComponent<Image>();
            fill.color = accent;
            fill.raycastTarget = false;

            CreateClockLabel("ShiftHours", clockRect, "20:00  —  24:00", 16f, 0.39f, 0.45f, muted);
            _clockRemainingText = CreateClockLabel("TimeUntilCheckout", clockRect, "퇴근까지 4시간 00분", 21f, 0.29f, 0.39f, Color.white);
            _clockRemainingText.enableAutoSizing = true;
            _clockRemainingText.fontSizeMin = 15f;
            _clockRemainingText.fontSizeMax = 21f;
            CreateClockLabel("ClockHint", clockRect, "24:00이 되면 퇴근합니다.\n그때까지 살아남으세요.", 17f, 0.16f, 0.29f, muted);

            GameObject backButtonObject = CreateRectObject("ClockBackButton", clockRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.5f, 0.09f);
            backRect.anchorMax = backRect.anchorMin;
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(180f, 48f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.11f, 0.26f, 0.3f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);
            GameObject backText = CreateTextObject("ClockBackText", backRect, "뒤로", 21f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);
            _clockScreen.SetActive(false);
            RefreshClock();
        }

        private static TextMeshProUGUI CreateClockLabel(string name, RectTransform parent, string text, float fontSize, float bottom, float top, Color color)
        {
            GameObject labelObject = CreateTextObject(name, parent, text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(0.07f, bottom);
            rect.anchorMax = new Vector2(0.93f, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.color = color;
            return label;
        }

        private void RefreshClock()
        {
            if (_clockTimeText == null)
            {
                return;
            }

            GameLoopController loop = GameLoopController.Instance;
            string clockText = loop != null ? loop.ClockText : "20:00";
            float progress = loop != null ? Mathf.Clamp01(loop.Progress01) : 0f;
            if (_lastClockText != clockText)
            {
                _lastClockText = clockText;
                _clockTimeText.SetText(clockText);
                if (_homeStatusText != null) _homeStatusText.SetText(clockText);
            }

            int remainingMinutes = Mathf.CeilToInt((1f - progress) * 240f);
            if (_lastRemainingMinutes != remainingMinutes)
            {
                _lastRemainingMinutes = remainingMinutes;
                _clockRemainingText.SetText($"퇴근까지 {remainingMinutes / 60}시간 {remainingMinutes % 60:00}분");
            }

            _clockProgressFill.anchorMax = new Vector2(progress, 1f);
        }

        private void BuildMailQuestScreen()
        {
            _mailScreen = CreateRectObject("MailQuestScreen", _phoneRect);
            RectTransform mailRect = (RectTransform)_mailScreen.transform;
            SetPhoneContentAnchors(mailRect);
            _mailScreen.AddComponent<Image>().color = new Color(0.94f, 0.96f, 1f, 1f);

            GameObject header = CreateRectObject("MailHeader", mailRect);
            RectTransform headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 0.84f);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color(0.94f, 0.96f, 1f, 1f);
            CreateDivider(headerRect, new Color(0.83f, 0.86f, 0.92f, 1f));

            GameObject headerText = CreateTextObject(
                "MailHeaderText",
                headerRect,
                "받은 메일함",
                23f,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            RectTransform mailHeaderTextRect = (RectTransform)headerText.transform;
            StretchToParent(mailHeaderTextRect);
            mailHeaderTextRect.offsetMin = new Vector2(20f, 0f);
            headerText.GetComponent<TextMeshProUGUI>().color = new Color(0.13f, 0.16f, 0.22f, 1f);

            GameObject viewportObject = CreateRectObject("QuestViewport", mailRect);
            RectTransform viewportRect = (RectTransform)viewportObject.transform;
            viewportRect.anchorMin = new Vector2(0.055f, 0.18f);
            viewportRect.anchorMax = new Vector2(0.945f, 0.81f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportObject.AddComponent<Image>().color = new Color(0.91f, 0.93f, 0.97f, 1f);
            viewportObject.AddComponent<RectMask2D>();

            GameObject contentObject = CreateRectObject("QuestContent", viewportRect);
            _mailContent = (RectTransform)contentObject.transform;
            _mailContent.anchorMin = new Vector2(0f, 1f);
            _mailContent.anchorMax = Vector2.one;
            _mailContent.pivot = new Vector2(0.5f, 1f);
            _mailContent.offsetMin = Vector2.zero;
            _mailContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = _mailScreen.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = _mailContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            RefreshQuestMail();

            GameObject backButtonObject = CreateRectObject("MailBackButton", mailRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.5f, 0.09f);
            backRect.anchorMax = new Vector2(0.5f, 0.09f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(180f, 48f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.88f, 0.9f, 0.94f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);

            GameObject backText = CreateTextObject(
                "MailBackText",
                backRect,
                "뒤로",
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);
            backText.GetComponent<TextMeshProUGUI>().color = new Color(0.15f, 0.18f, 0.24f, 1f);

            _mailScreen.SetActive(false);
        }

        /// <summary>The jobs that stand all shift. Returns how many cards were added.</summary>
        private int PopulateStandingQuests()
        {
            QuestDefinition[] quests = Resources.LoadAll<QuestDefinition>("Quests");
            System.Array.Sort(quests, static (left, right) =>
            {
                int orderComparison = left.SortOrder.CompareTo(right.SortOrder);
                return orderComparison != 0
                    ? orderComparison
                    : string.Compare(left.Title, right.Title, System.StringComparison.Ordinal);
            });

            if (quests.Length == 0)
            {
                return 0;
            }

            CreateMailSectionLabel("기본 업무");
            foreach (QuestDefinition quest in quests)
            {
                CreateQuestMailCard(quest);
            }

            return quests.Length;
        }

        /// <summary>Repaints the inbox: the manager's extra work on top, newest first, then the standing jobs.</summary>
        private void RefreshQuestMail()
        {
            if (_mailContent == null)
            {
                return;
            }

            for (int index = _mailContent.childCount - 1; index >= 0; index--)
            {
                Transform child = _mailContent.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            int cards = 0;
            ShiftQuestBoard board = ShiftQuestBoard.Instance;
            if (board != null && board.Tasks.Count > 0)
            {
                CreateMailSectionLabel("점장님 추가 업무");
                for (int index = board.Tasks.Count - 1; index >= 0; index--)
                {
                    CreateTaskMailCard(board.Tasks[index]);
                    cards++;
                }
            }

            cards += PopulateStandingQuests();
            if (cards > 0)
            {
                return;
            }

            GameObject emptyText = CreateTextObject(
                "NoQuestText",
                _mailContent,
                "받은 메일이 없습니다",
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            emptyText.AddComponent<LayoutElement>().preferredHeight = 120f;
            emptyText.GetComponent<TextMeshProUGUI>().color = new Color(0.28f, 0.35f, 0.46f, 1f);
        }

        private void CreateMailSectionLabel(string text)
        {
            GameObject label = CreateTextObject($"Section_{text}", _mailContent, text, 15f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            label.AddComponent<LayoutElement>().preferredHeight = 26f;
            label.GetComponent<TextMeshProUGUI>().color = new Color(0.42f, 0.48f, 0.58f, 1f);
        }

        private void CreateTaskMailCard(ShiftTask task)
        {
            GameObject card = CreateRectObject($"Task_{task.Title}", _mailContent);
            card.AddComponent<Image>().color = task.IsDone ? new Color(0.93f, 0.97f, 0.93f, 1f) : Color.white;
            card.AddComponent<LayoutElement>().preferredHeight = 186f;
            RectTransform cardRect = (RectTransform)card.transform;

            GameObject sender = CreateTextObject("TaskSender", cardRect, "점장님", 15f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            SetCardRow((RectTransform)sender.transform, 0.79f, 0.95f);
            sender.GetComponent<TextMeshProUGUI>().color = new Color(0.45f, 0.5f, 0.58f, 1f);

            GameObject title = CreateTextObject("TaskTitle", cardRect, task.Title, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            SetCardRow((RectTransform)title.transform, 0.58f, 0.79f);
            title.GetComponent<TextMeshProUGUI>().color = new Color(0.08f, 0.25f, 0.52f, 1f);

            GameObject note = CreateTextObject("TaskNote", cardRect, task.Note, 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetCardRow((RectTransform)note.transform, 0.29f, 0.57f);
            note.GetComponent<TextMeshProUGUI>().color = new Color(0.18f, 0.2f, 0.24f, 1f);

            string statusLine = task.IsDone
                ? $"완료했습니다.  체력 +{task.HealthReward}"
                : $"{task.ProgressText}    보상 체력 +{task.HealthReward}";
            GameObject status = CreateTextObject("TaskStatus", cardRect, statusLine, 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetCardRow((RectTransform)status.transform, 0.05f, 0.28f);
            status.GetComponent<TextMeshProUGUI>().color = task.IsDone
                ? new Color(0.12f, 0.45f, 0.24f, 1f)
                : new Color(0.35f, 0.4f, 0.46f, 1f);
        }

        private static void SetCardRow(RectTransform rectTransform, float bottom, float top)
        {
            rectTransform.anchorMin = new Vector2(0.06f, bottom);
            rectTransform.anchorMax = new Vector2(0.94f, top);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildMailToast(RectTransform canvasRect)
        {
            _mailToastPanel = CreateRectObject("MailToast", canvasRect);
            RectTransform panelRect = (RectTransform)_mailToastPanel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            // Stacks under the police countdown and main's villain-entry banner.
            panelRect.anchoredPosition = new Vector2(0f, -178f);
            panelRect.sizeDelta = new Vector2(520f, 56f);

            Image panelImage = _mailToastPanel.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.05f, 0.86f);
            panelImage.raycastTarget = false;

            GameObject textObject = CreateTextObject("MailToastText", panelRect, string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform textRect = (RectTransform)textObject.transform;
            StretchToParent(textRect);
            textRect.offsetMin = new Vector2(16f, 4f);
            textRect.offsetMax = new Vector2(-16f, -4f);
            _mailToastText = textObject.GetComponent<TextMeshProUGUI>();
            _mailToastText.color = new Color(1f, 0.82f, 0.4f, 1f);

            _mailToastPanel.SetActive(false);
        }

        private void ShowMailToast(string message)
        {
            if (_mailToastPanel == null)
            {
                return;
            }

            _mailToastText.SetText(message);
            _mailToastPanel.SetActive(true);
            _mailToastHideTime = Time.unscaledTime + 4.5f;
        }

        private void CreateQuestMailCard(QuestDefinition quest)
        {
            GameObject card = CreateRectObject($"Quest_{quest.QuestId}", _mailContent);
            card.AddComponent<Image>().color = Color.white;
            card.AddComponent<LayoutElement>().preferredHeight = 172f;
            RectTransform cardRect = (RectTransform)card.transform;

            GameObject title = CreateTextObject(
                "QuestTitle",
                cardRect,
                quest.Title,
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0.06f, 0.68f);
            titleRect.anchorMax = new Vector2(0.94f, 0.94f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.GetComponent<TextMeshProUGUI>().color = new Color(0.08f, 0.25f, 0.52f, 1f);

            GameObject description = CreateTextObject(
                "QuestDescription",
                cardRect,
                quest.Description,
                16f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            RectTransform descriptionRect = (RectTransform)description.transform;
            descriptionRect.anchorMin = new Vector2(0.06f, 0.29f);
            descriptionRect.anchorMax = new Vector2(0.94f, 0.68f);
            descriptionRect.offsetMin = Vector2.zero;
            descriptionRect.offsetMax = Vector2.zero;
            description.GetComponent<TextMeshProUGUI>().color = new Color(0.18f, 0.2f, 0.24f, 1f);

            string objectiveText = quest.TargetAmount > 1
                ? $"할 일: {quest.Objective}  {quest.TargetAmount}개"
                : $"할 일: {quest.Objective}";
            if (!string.IsNullOrWhiteSpace(quest.Reward))
            {
                objectiveText += $"\n보상: {quest.Reward}";
            }

            GameObject objective = CreateTextObject(
                "QuestObjective",
                cardRect,
                objectiveText,
                15f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            RectTransform objectiveRect = (RectTransform)objective.transform;
            objectiveRect.anchorMin = new Vector2(0.06f, 0.05f);
            objectiveRect.anchorMax = new Vector2(0.94f, 0.29f);
            objectiveRect.offsetMin = Vector2.zero;
            objectiveRect.offsetMax = Vector2.zero;
            objective.GetComponent<TextMeshProUGUI>().color = new Color(0.22f, 0.4f, 0.29f, 1f);
        }

        private void BuildPoliceCountdownDisplay(RectTransform canvasRect)
        {
            _policeCountdownPanel = CreateRectObject("PoliceCountdown", canvasRect);
            RectTransform panelRect = (RectTransform)_policeCountdownPanel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -26f);
            panelRect.sizeDelta = new Vector2(390f, 72f);

            Image panelImage = _policeCountdownPanel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.04f, 0.82f);
            panelImage.raycastTarget = false;

            GameObject textObject = CreateTextObject(
                "PoliceCountdownText",
                panelRect,
                "5초 뒤 경찰 도착",
                26f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            RectTransform textRect = (RectTransform)textObject.transform;
            StretchToParent(textRect);
            textRect.offsetMin = new Vector2(18f, 6f);
            textRect.offsetMax = new Vector2(-18f, -6f);
            _policeCountdownText = textObject.GetComponent<TextMeshProUGUI>();
            _policeCountdownText.color = new Color(0.65f, 0.9f, 1f, 1f);

            _policeCountdownPanel.SetActive(false);
        }

        private void BuildVillainAlertDisplay(RectTransform canvasRect)
        {
            _villainAlertPanel = CreateRectObject("VillainEntryAlert", canvasRect);
            RectTransform panelRect = (RectTransform)_villainAlertPanel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -104f);
            panelRect.sizeDelta = new Vector2(520f, 66f);

            Image panelImage = _villainAlertPanel.AddComponent<Image>();
            panelImage.color = new Color(0.18f, 0.025f, 0.025f, 0.88f);
            panelImage.raycastTarget = false;

            GameObject textObject = CreateTextObject(
                "VillainEntryAlertText",
                panelRect,
                "진상 입장!",
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            RectTransform textRect = (RectTransform)textObject.transform;
            StretchToParent(textRect);
            textRect.offsetMin = new Vector2(18f, 6f);
            textRect.offsetMax = new Vector2(-18f, -6f);
            _villainAlertText = textObject.GetComponent<TextMeshProUGUI>();
            _villainAlertText.color = new Color(1f, 0.82f, 0.32f, 1f);

            _villainAlertPanel.SetActive(false);
        }

        private void ShowVillainEntryAlert(string villainName)
        {
            if (_villainAlertPanel == null || _villainAlertText == null)
            {
                return;
            }

            string cleanedName = string.IsNullOrWhiteSpace(villainName)
                ? "진상"
                : villainName.Replace("(Clone)", string.Empty).Trim();
            _villainAlertText.text = $"{cleanedName} 입장!";

            if (_villainAlertRoutine != null)
            {
                StopCoroutine(_villainAlertRoutine);
            }

            _villainAlertRoutine = StartCoroutine(ShowVillainAlertRoutine());
        }

        private void ShowVillainAngryAlert()
        {
            if (_villainAlertPanel == null || _villainAlertText == null)
            {
                return;
            }

            _villainAlertText.text = "진상이 개빡쳤다!";

            if (_villainAlertRoutine != null)
            {
                StopCoroutine(_villainAlertRoutine);
            }

            _villainAlertRoutine = StartCoroutine(ShowVillainAlertRoutine());
        }

        private IEnumerator ShowVillainAlertRoutine()
        {
            _villainAlertPanel.SetActive(true);
            yield return new WaitForSeconds(3f);
            _villainAlertPanel.SetActive(false);
            _villainAlertRoutine = null;
        }

        private void ShowPoliceCountdown(bool isVisible)
        {
            if (_policeCountdownPanel != null)
            {
                _policeCountdownPanel.SetActive(isVisible);
            }
        }

        private void SetPoliceCountdown(int seconds)
        {
            if (_policeCountdownText == null)
            {
                return;
            }

            if (seconds <= 0)
            {
                _policeCountdownText.SetText("경찰 도착");
                _policeCountdownText.color = new Color(0.35f, 1f, 0.5f, 1f);
                return;
            }

            _policeCountdownText.SetText("{0}초 뒤 경찰 도착", seconds);
            _policeCountdownText.color = new Color(0.65f, 0.9f, 1f, 1f);
        }

        private void RefreshHealthText(float currentHealth, float maxHealth)
        {
            if (_healthText == null)
            {
                return;
            }

            int displayedCurrent = Mathf.CeilToInt(currentHealth);
            int displayedMax = Mathf.CeilToInt(maxHealth);
            _healthText.SetText("{0} / {1}", displayedCurrent, displayedMax);

            float healthRatio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            Color healthColor = Color.Lerp(
                new Color(1f, 0.2f, 0.2f, 1f),
                new Color(0.35f, 1f, 0.5f, 1f),
                healthRatio);
            _healthText.color = healthColor;
            if (_healthFill != null)
            {
                _healthFill.fillAmount = healthRatio;
                _healthFill.color = healthColor;
            }
        }

        private void SetPhoneOpen(bool isOpen)
        {
            if (_phoneOverlay == null || _phoneOverlay.activeSelf == isOpen || (isOpen && (!_gameplayEnabled || !GameLoopController.AllowsGameplay)))
            {
                return;
            }

            if (isOpen)
            {
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _playerLookController = FindPlayerLookController();
                _lookControllerWasEnabled = _playerLookController != null && _playerLookController.enabled;
                if (_playerLookController != null)
                {
                    _playerLookController.enabled = false;
                }

                _crosshair = GameObject.Find("CrossHead");
                _crosshairWasActive = _crosshair != null && _crosshair.activeSelf;
                if (_crosshair != null)
                {
                    _crosshair.SetActive(false);
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ShowHomeScreen();
            }
            else
            {
                _health?.SetYoutubeHealing(false);
                HideYoutubeWebView();
                if (_playerLookController != null)
                {
                    _playerLookController.enabled = _lookControllerWasEnabled;
                }

                if (_crosshair != null)
                {
                    _crosshair.SetActive(_crosshairWasActive);
                }

                Cursor.lockState = _previousCursorLockMode;
                Cursor.visible = _previousCursorVisible;
            }

            _phoneOverlay.SetActive(isOpen);
            PhoneActionPerformed?.Invoke(isOpen ? "PhoneOpened" : "PhoneClosed");
        }

        private void ShowYoutubeScreen()
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay)
            {
                return;
            }

            _clockScreen.SetActive(false);
            _homeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _stocksScreen.SetActive(false);
            _youtubeScreen.SetActive(true);
            EnsureYoutubeWebView();
            RefreshYoutubeWebViewBounds();

            if (_youtubeWebView != null)
            {
                _youtubeWebView.SetVisibility(true);
                if (!_youtubePageRequested)
                {
                    _youtubePageRequested = true;
                    SetYoutubeStatus("LOADING YOUTUBE...");
                    _youtubeWebView.LoadURL(YoutubeUrl);
                }
            }

            _health?.SetYoutubeHealing(_youtubeWebView != null);
        }

        private void ShowPhoneDialerScreen()
        {
            _clockScreen.SetActive(false);
            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _stocksScreen.SetActive(false);
            _phoneDialerScreen.SetActive(true);
            RefreshEmergencyCallButton();
        }

        private void ShowStocksScreen()
        {
            _clockScreen.SetActive(false);
            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _stocksScreen.SetActive(true);

            if (_health != null)
            {
                RefreshHealthText(_health.CurrentHealth, _health.MaxHealth);
            }
        }

        private void ShowMailScreen()
        {
            _clockScreen.SetActive(false);
            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _stocksScreen.SetActive(false);
            _mailScreen.SetActive(true);
        }

        private void ShowClockScreen()
        {
            if (!_gameplayEnabled || !GameLoopController.AllowsGameplay)
            {
                return;
            }

            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _stocksScreen.SetActive(false);
            _clockScreen.SetActive(true);
            RefreshClock();
        }

        private void ShowHomeScreen()
        {
            _clockScreen.SetActive(false);
            HideYoutubeWebView();
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _stocksScreen.SetActive(false);
            _homeScreen.SetActive(true);
            _health?.SetYoutubeHealing(false);
        }

        private void EnsureYoutubeWebView()
        {
            if (_youtubeWebView != null)
            {
                return;
            }

            GameObject webViewObject = new("YouTube WebView");
            webViewObject.transform.SetParent(transform, false);

            try
            {
                _youtubeWebView = webViewObject.AddComponent<WebViewObject>();
                _youtubeWebView.bitmapRefreshCycle = 3;
                _youtubeWebView.devicePixelRatio = 1;
                _youtubeWebView.Init(
                    err: message =>
                    {
                        Debug.LogWarning($"YouTube WebView error: {message}");
                        SetYoutubeStatus("WEBVIEW ERROR");
                    },
                    httpErr: message =>
                    {
                        Debug.LogWarning($"YouTube HTTP error: {message}");
                        SetYoutubeStatus("NETWORK ERROR");
                    },
                    ld: _ => SetYoutubeStatus("HP RECOVERING"),
                    zoom: true,
                    ua: MobileYoutubeUserAgent,
                    wkContentMode: 1);
                _youtubeWebView.SetVisibility(false);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Could not initialize YouTube WebView: {exception.Message}");
                Destroy(webViewObject);
                _youtubeWebView = null;
                SetYoutubeStatus("WEBVIEW UNAVAILABLE");
            }
        }

        private void RefreshYoutubeWebViewBounds()
        {
            if (_youtubeWebView == null || _youtubeWebViewViewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Vector3[] worldCorners = new Vector3[4];
            _youtubeWebViewViewport.GetWorldCorners(worldCorners);

            Canvas parentCanvas = _youtubeWebViewViewport.GetComponentInParent<Canvas>();
            Camera uiCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null;
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[2]);

            int left = Mathf.Clamp(Mathf.RoundToInt(bottomLeft.x), 0, Screen.width - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(bottomLeft.y), 0, Screen.height - 1);
            int right = Mathf.Clamp(Screen.width - Mathf.RoundToInt(topRight.x), 0, Screen.width - 1);
            int top = Mathf.Clamp(Screen.height - Mathf.RoundToInt(topRight.y), 0, Screen.height - 1);

            if (Screen.width - left - right > 1 && Screen.height - top - bottom > 1)
            {
                _youtubeWebView.SetMargins(left, top, right, bottom);
            }
        }

        private void HideYoutubeWebView()
        {
            if (_youtubeWebView == null)
            {
                return;
            }

            _youtubeWebView.EvaluateJS("document.querySelectorAll('video').forEach(function(video){video.pause();});");
            _youtubeWebView.SetVisibility(false);
        }

        private void SetYoutubeStatus(string status)
        {
            if (_youtubeStatusText != null)
            {
                _youtubeStatusText.SetText(status);
            }
        }

        private static Behaviour FindPlayerLookController()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "PlayerCameraController")
                {
                    return behaviour;
                }
            }

            return null;
        }

        private void RefreshPhoneSize()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            Canvas.ForceUpdateCanvases();
            RectTransform overlayRect = _phoneRect.parent as RectTransform;
            float phoneHeight = overlayRect != null && overlayRect.rect.height > 0f
                ? overlayRect.rect.height
                : Screen.height;
            _phoneRect.sizeDelta = new Vector2(phoneHeight * 0.523f, phoneHeight);
            RefreshYoutubeWebViewBounds();
        }

        private static void SetPhoneContentAnchors(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.145f, 0.08f);
            rectTransform.anchorMax = new Vector2(0.855f, 0.9f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Sprite LoadStocksIcon()
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("UI/stocks");
            return sprites.Length > 0 ? sprites[0] : null;
        }

        private static void CreateStocksChartIcon(RectTransform parent)
        {
            Vector2[] points =
            {
                new(-27f, -18f),
                new(-9f, -2f),
                new(7f, -10f),
                new(27f, 20f)
            };

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[i + 1];
                Vector2 delta = end - start;
                GameObject segment = CreateRectObject($"ChartSegment{i}", parent);
                RectTransform segmentRect = (RectTransform)segment.transform;
                SetCenteredRect(segmentRect, (start + end) * 0.5f, new Vector2(delta.magnitude, 7f));
                segmentRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                Image segmentImage = segment.AddComponent<Image>();
                segmentImage.color = Color.white;
                segmentImage.raycastTarget = false;
            }
        }

        private static void CreatePhoneHandsetIcon(RectTransform parent)
        {
            GameObject center = CreateRectObject("HandsetCenter", parent);
            RectTransform centerRect = (RectTransform)center.transform;
            SetCenteredRect(centerRect, Vector2.zero, new Vector2(18f, 56f));
            centerRect.localRotation = Quaternion.Euler(0f, 0f, -42f);
            Image centerImage = center.AddComponent<Image>();
            centerImage.color = Color.white;
            centerImage.raycastTarget = false;

            GameObject upperEnd = CreateRectObject("HandsetUpperEnd", parent);
            RectTransform upperRect = (RectTransform)upperEnd.transform;
            SetCenteredRect(upperRect, new Vector2(-20f, 20f), new Vector2(30f, 18f));
            upperRect.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Image upperImage = upperEnd.AddComponent<Image>();
            upperImage.color = Color.white;
            upperImage.raycastTarget = false;

            GameObject lowerEnd = CreateRectObject("HandsetLowerEnd", parent);
            RectTransform lowerRect = (RectTransform)lowerEnd.transform;
            SetCenteredRect(lowerRect, new Vector2(20f, -20f), new Vector2(30f, 18f));
            lowerRect.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Image lowerImage = lowerEnd.AddComponent<Image>();
            lowerImage.color = Color.white;
            lowerImage.raycastTarget = false;
        }

        private static void CreateMailEnvelopeIcon(RectTransform parent)
        {
            GameObject envelope = CreateRectObject("Envelope", parent);
            RectTransform envelopeRect = (RectTransform)envelope.transform;
            SetCenteredRect(envelopeRect, Vector2.zero, new Vector2(62f, 44f));
            Image envelopeImage = envelope.AddComponent<Image>();
            envelopeImage.color = Color.white;
            envelopeImage.raycastTarget = false;

            GameObject leftFold = CreateRectObject("EnvelopeLeftFold", parent);
            RectTransform leftFoldRect = (RectTransform)leftFold.transform;
            SetCenteredRect(leftFoldRect, new Vector2(-14f, 5f), new Vector2(38f, 5f));
            leftFoldRect.localRotation = Quaternion.Euler(0f, 0f, -34f);
            Image leftFoldImage = leftFold.AddComponent<Image>();
            leftFoldImage.color = new Color(0.12f, 0.48f, 0.9f, 1f);
            leftFoldImage.raycastTarget = false;

            GameObject rightFold = CreateRectObject("EnvelopeRightFold", parent);
            RectTransform rightFoldRect = (RectTransform)rightFold.transform;
            SetCenteredRect(rightFoldRect, new Vector2(14f, 5f), new Vector2(38f, 5f));
            rightFoldRect.localRotation = Quaternion.Euler(0f, 0f, 34f);
            Image rightFoldImage = rightFold.AddComponent<Image>();
            rightFoldImage.color = new Color(0.12f, 0.48f, 0.9f, 1f);
            rightFoldImage.raycastTarget = false;
        }

        private static void SetCenteredRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void SetHomeAppRect(RectTransform rectTransform, int column, int row, bool isLabel)
        {
            Vector2 anchor = new(column == 0 ? 0.28f : 0.72f, 0.73f - row * 0.22f);
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = isLabel ? new Vector2(0f, -58f) : Vector2.zero;
            rectTransform.sizeDelta = isLabel ? new Vector2(110f, 34f) : new Vector2(88f, 80f);
        }

        private static GameObject CreateRectObject(string objectName, Transform parent)
        {
            GameObject gameObject = new(objectName, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreateTextObject(
            string objectName,
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateRectObject(objectName, parent);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.font = GetUiFont();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            return textObject;
        }

        private static TMP_FontAsset GetUiFont()
        {
            if (_runtimeUiFont != null)
            {
                return _runtimeUiFont;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _runtimeUiFont = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 64);
            if (_runtimeUiFont != null)
            {
                _runtimeUiFont.name = "Runtime Malgun Gothic UI";
                _runtimeUiFont.hideFlags = HideFlags.DontSave;
                return _runtimeUiFont;
            }
#endif

            _runtimeUiFont = TMP_Settings.defaultFontAsset;
            return _runtimeUiFont;
        }

        /// <summary>A one-pixel rule under a header, the way a real app separates its title bar.</summary>
        private static void CreateDivider(RectTransform parent, Color color)
        {
            GameObject line = CreateRectObject("Divider", parent);
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0f);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0f, 1.5f);
            Image image = line.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            CancelPendingEmergencyCall();
            if (_health != null)
            {
                _health.HealthChanged -= RefreshHealthText;
                _health.SetYoutubeHealing(false);
            }

            ConvenienceStoreVillainSpawner.VillainEnteredStore -= ShowVillainEntryAlert;
            ConvenienceStoreVillainSpawner.VillainBecameAngry -= ShowVillainAngryAlert;

            HideYoutubeWebView();
            if (_youtubeWebView != null)
            {
                Destroy(_youtubeWebView.gameObject);
            }

            if (_phoneOverlay != null && _phoneOverlay.activeSelf)
            {
                Cursor.lockState = _previousCursorLockMode;
                Cursor.visible = _previousCursorVisible;
            }
        }
    }
}
