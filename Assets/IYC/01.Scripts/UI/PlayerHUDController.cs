using System.Collections;
using CWH.Player.Health;
using CWH.Quests;
using CWH.Villains;
using Gree.UnityWebView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CWH.Player.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerHUDController : MonoBehaviour
    {
        private const string SettingsResourceName = "PlayerHUDSettings";
        private const string YoutubeUrl = "https://m.youtube.com/";
        private static TMP_FontAsset _runtimeUiFont;

        private PlayerHealth _health;
        private TextMeshProUGUI _healthText;
        private GameObject _phoneOverlay;
        private GameObject _homeScreen;
        private GameObject _youtubeScreen;
        private GameObject _phoneDialerScreen;
        private GameObject _mailScreen;
        private RectTransform _phoneRect;
        private RectTransform _youtubeWebViewViewport;
        private RectTransform _mailContent;
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
        private string _dialedNumber = string.Empty;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnCanvas()
        {
            if (FindFirstObjectByType<PlayerHUDController>() != null)
            {
                return;
            }

            Canvas targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = CreateFallbackCanvas();
            }

            targetCanvas.gameObject.AddComponent<PlayerHUDController>();
        }

        private static Canvas CreateFallbackCanvas()
        {
            GameObject canvasObject = new("Player HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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
                _healthText.SetText("HP -- / --");
            }

            _phoneOverlay.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                SetPhoneOpen(!_phoneOverlay.activeSelf);
            }

            if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
            {
                RefreshPhoneSize();
            }
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
            BuildMailQuestScreen();
            BuildHealthDisplay(canvasRect);
            RefreshPhoneSize();
        }

        private void BuildHomeScreen(PlayerHUDSettings settings)
        {
            _homeScreen = CreateRectObject("PhoneHome", _phoneRect);
            RectTransform homeRect = (RectTransform)_homeScreen.transform;
            SetPhoneContentAnchors(homeRect);

            GameObject title = CreateTextObject("HomeTitle", homeRect, "APPS", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0.1f, 0.78f);
            titleRect.anchorMax = new Vector2(0.9f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            title.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject phoneButtonObject = CreateRectObject("PhoneButton", homeRect);
            RectTransform phoneButtonRect = (RectTransform)phoneButtonObject.transform;
            SetCenteredRect(phoneButtonRect, new Vector2(-108f, 40f), new Vector2(92f, 84f));
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

            GameObject phoneLabel = CreateTextObject("PhoneLabel", homeRect, "Phone", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetCenteredRect((RectTransform)phoneLabel.transform, new Vector2(-108f, -25f), new Vector2(100f, 34f));
            phoneLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject youtubeButtonObject = CreateRectObject("YoutubeButton", homeRect);
            RectTransform buttonRect = (RectTransform)youtubeButtonObject.transform;
            SetCenteredRect(buttonRect, new Vector2(0f, 40f), new Vector2(92f, 84f));

            Image youtubeImage = youtubeButtonObject.AddComponent<Image>();
            youtubeImage.sprite = settings != null ? settings.YoutubeLogo : null;
            youtubeImage.preserveAspect = true;
            youtubeImage.color = youtubeImage.sprite != null ? Color.white : new Color(1f, 0f, 0f, 1f);

            Button youtubeButton = youtubeButtonObject.AddComponent<Button>();
            youtubeButton.targetGraphic = youtubeImage;
            youtubeButton.onClick.AddListener(ShowYoutubeScreen);

            GameObject appLabel = CreateTextObject("YoutubeLabel", homeRect, "YouTube", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform appLabelRect = (RectTransform)appLabel.transform;
            SetCenteredRect(appLabelRect, new Vector2(0f, -25f), new Vector2(100f, 34f));
            appLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject mailButtonObject = CreateRectObject("MailButton", homeRect);
            RectTransform mailButtonRect = (RectTransform)mailButtonObject.transform;
            SetCenteredRect(mailButtonRect, new Vector2(108f, 40f), new Vector2(92f, 84f));
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

            GameObject mailLabel = CreateTextObject("MailLabel", homeRect, "메일", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetCenteredRect((RectTransform)mailLabel.transform, new Vector2(108f, -25f), new Vector2(100f, 34f));
            mailLabel.GetComponent<TextMeshProUGUI>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            GameObject hint = CreateTextObject("CloseHint", homeRect, "TAB  CLOSE", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            RectTransform hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = new Vector2(0.15f, 0.06f);
            hintRect.anchorMax = new Vector2(0.85f, 0.14f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            hint.GetComponent<TextMeshProUGUI>().color = new Color(0.35f, 0.35f, 0.4f, 1f);
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
            headerRect.anchorMin = new Vector2(0f, 0.82f);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            header.AddComponent<Image>().color = new Color(0.92f, 0.05f, 0.05f, 1f);

            GameObject headerText = CreateTextObject("HeaderText", headerRect, "YouTube", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)headerText.transform);

            GameObject webViewViewportObject = CreateRectObject("WebViewViewport", youtubeRect);
            _youtubeWebViewViewport = (RectTransform)webViewViewportObject.transform;
            _youtubeWebViewViewport.anchorMin = new Vector2(0.025f, 0.24f);
            _youtubeWebViewViewport.anchorMax = new Vector2(0.975f, 0.8f);
            _youtubeWebViewViewport.offsetMin = Vector2.zero;
            _youtubeWebViewViewport.offsetMax = Vector2.zero;
            Image webViewBackground = webViewViewportObject.AddComponent<Image>();
            webViewBackground.color = new Color(0.08f, 0.08f, 0.09f, 1f);
            webViewBackground.raycastTarget = false;

            GameObject loadingText = CreateTextObject("WebViewStatus", _youtubeWebViewViewport, "YOUTUBE READY", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)loadingText.transform);
            _youtubeStatusText = loadingText.GetComponent<TextMeshProUGUI>();

            GameObject recoveryText = CreateTextObject("RecoveryText", youtubeRect, "HP RECOVERING", 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform recoveryRect = (RectTransform)recoveryText.transform;
            recoveryRect.anchorMin = new Vector2(0.08f, 0.155f);
            recoveryRect.anchorMax = new Vector2(0.92f, 0.225f);
            recoveryRect.offsetMin = Vector2.zero;
            recoveryRect.offsetMax = Vector2.zero;
            recoveryText.GetComponent<TextMeshProUGUI>().color = new Color(0.25f, 0.65f, 0.3f, 1f);

            GameObject backButtonObject = CreateRectObject("BackButton", youtubeRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.5f, 0.09f);
            backRect.anchorMax = new Vector2(0.5f, 0.09f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(180f, 48f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);

            GameObject backText = CreateTextObject("BackText", backRect, "BACK", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
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
            header.AddComponent<Image>().color = new Color(0.12f, 0.58f, 0.27f, 1f);

            GameObject headerText = CreateTextObject("PhoneHeaderText", headerRect, "PHONE", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)headerText.transform);

            GameObject numberPanel = CreateRectObject("NumberPanel", dialerRect);
            RectTransform numberPanelRect = (RectTransform)numberPanel.transform;
            numberPanelRect.anchorMin = new Vector2(0.08f, 0.69f);
            numberPanelRect.anchorMax = new Vector2(0.92f, 0.81f);
            numberPanelRect.offsetMin = Vector2.zero;
            numberPanelRect.offsetMax = Vector2.zero;
            numberPanel.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.13f, 1f);

            GameObject numberText = CreateTextObject("DialedNumber", numberPanelRect, "---", 38f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)numberText.transform);
            _dialedNumberText = numberText.GetComponent<TextMeshProUGUI>();

            GameObject statusText = CreateTextObject("DialerStatus", dialerRect, "ENTER 112", 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform statusRect = (RectTransform)statusText.transform;
            statusRect.anchorMin = new Vector2(0.08f, 0.61f);
            statusRect.anchorMax = new Vector2(0.92f, 0.68f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            _dialerStatusText = statusText.GetComponent<TextMeshProUGUI>();
            _dialerStatusText.color = new Color(0.18f, 0.38f, 0.23f, 1f);

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
            backImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);
            GameObject backText = CreateTextObject("PhoneBackText", backRect, "BACK", 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);

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
                ? new Color(0.32f, 0.35f, 0.34f, 1f)
                : new Color(0.12f, 0.58f, 0.27f, 1f);
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

            GameObject textObject = CreateTextObject($"DialKeyText_{key}", buttonRect, key, key.Length > 1 ? 14f : 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchToParent((RectTransform)textObject.transform);
        }

        private void AppendDialedDigit(string digit)
        {
            if (_policeCallPending || _dialedNumber.Length >= 3)
            {
                return;
            }

            _dialedNumber += digit;
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void ClearDialedNumber()
        {
            if (_policeCallPending)
            {
                return;
            }

            _dialedNumber = string.Empty;
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void DeleteLastDialedDigit()
        {
            if (_policeCallPending || _dialedNumber.Length == 0)
            {
                return;
            }

            _dialedNumber = _dialedNumber.Substring(0, _dialedNumber.Length - 1);
            RefreshDialedNumber();
            RefreshEmergencyCallButton();
        }

        private void RefreshDialedNumber()
        {
            _dialedNumberText.SetText(string.IsNullOrEmpty(_dialedNumber) ? "---" : _dialedNumber);
        }

        private void RefreshEmergencyCallButton()
        {
            bool canCall = !_policeCallPending && _dialedNumber == "112";
            if (_emergencyCallButton != null)
            {
                _emergencyCallButton.interactable = canCall;
            }

            _dialerStatusText.SetText(canCall ? "PRESS CALL" : "ENTER 112");
            _dialerStatusText.color = new Color(0.18f, 0.38f, 0.23f, 1f);
        }

        private void BeginEmergencyCall()
        {
            if (_policeCallPending || _dialedNumber != "112")
            {
                return;
            }

            StartCoroutine(CompleteEmergencyCall());
        }

        private IEnumerator CompleteEmergencyCall()
        {
            _policeCallPending = true;
            if (_emergencyCallButton != null)
            {
                _emergencyCallButton.interactable = false;
            }

            for (int seconds = 5; seconds > 0; seconds--)
            {
                _dialerStatusText.SetText("POLICE ARRIVING IN {0}", seconds);
                yield return new WaitForSeconds(1f);
            }

            ConvenienceStoreVillainSpawner.RequestAllVillainsFlee();
            _dialerStatusText.SetText("POLICE ARRIVED");
            _dialerStatusText.color = new Color(0.05f, 0.55f, 0.2f, 1f);
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
            header.AddComponent<Image>().color = new Color(0.1f, 0.42f, 0.82f, 1f);

            GameObject headerText = CreateTextObject(
                "MailHeaderText",
                headerRect,
                "퀘스트 메일",
                28f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            StretchToParent((RectTransform)headerText.transform);

            GameObject viewportObject = CreateRectObject("QuestViewport", mailRect);
            RectTransform viewportRect = (RectTransform)viewportObject.transform;
            viewportRect.anchorMin = new Vector2(0.055f, 0.18f);
            viewportRect.anchorMax = new Vector2(0.945f, 0.81f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportObject.AddComponent<Image>().color = new Color(0.84f, 0.89f, 0.97f, 1f);
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

            PopulateQuestMail();

            GameObject backButtonObject = CreateRectObject("MailBackButton", mailRect);
            RectTransform backRect = (RectTransform)backButtonObject.transform;
            backRect.anchorMin = new Vector2(0.5f, 0.09f);
            backRect.anchorMax = new Vector2(0.5f, 0.09f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.sizeDelta = new Vector2(180f, 48f);
            Image backImage = backButtonObject.AddComponent<Image>();
            backImage.color = new Color(0.12f, 0.2f, 0.34f, 1f);
            Button backButton = backButtonObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(ShowHomeScreen);

            GameObject backText = CreateTextObject(
                "MailBackText",
                backRect,
                "뒤로",
                21f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            StretchToParent((RectTransform)backText.transform);

            _mailScreen.SetActive(false);
        }

        private void PopulateQuestMail()
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
                GameObject emptyText = CreateTextObject(
                    "NoQuestText",
                    _mailContent,
                    "퀘스트 메일이 없습니다",
                    22f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center);
                emptyText.AddComponent<LayoutElement>().preferredHeight = 120f;
                emptyText.GetComponent<TextMeshProUGUI>().color = new Color(0.28f, 0.35f, 0.46f, 1f);
                return;
            }

            foreach (QuestDefinition quest in quests)
            {
                CreateQuestMailCard(quest);
            }
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
                ? $"목표: {quest.Objective}  x{quest.TargetAmount}"
                : $"목표: {quest.Objective}";
            if (!string.IsNullOrWhiteSpace(quest.Reward))
            {
                objectiveText += $"\n보상: {quest.Reward}";
            }

            GameObject objective = CreateTextObject(
                "QuestObjective",
                cardRect,
                objectiveText,
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft);
            RectTransform objectiveRect = (RectTransform)objective.transform;
            objectiveRect.anchorMin = new Vector2(0.06f, 0.05f);
            objectiveRect.anchorMax = new Vector2(0.94f, 0.29f);
            objectiveRect.offsetMin = Vector2.zero;
            objectiveRect.offsetMax = Vector2.zero;
            objective.GetComponent<TextMeshProUGUI>().color = new Color(0.08f, 0.48f, 0.3f, 1f);
        }

        private void BuildHealthDisplay(RectTransform canvasRect)
        {
            GameObject healthBackgroundObject = CreateRectObject("PlayerHealth", canvasRect);
            RectTransform backgroundRect = (RectTransform)healthBackgroundObject.transform;
            backgroundRect.anchorMin = new Vector2(0f, 1f);
            backgroundRect.anchorMax = new Vector2(0f, 1f);
            backgroundRect.pivot = new Vector2(0f, 1f);
            backgroundRect.anchoredPosition = new Vector2(24f, -24f);
            backgroundRect.sizeDelta = new Vector2(330f, 64f);

            Image background = healthBackgroundObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.025f, 0.78f);
            background.raycastTarget = false;

            GameObject textObject = CreateTextObject("HealthText", backgroundRect, "HP 100 / 100", 32f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform textRect = (RectTransform)textObject.transform;
            StretchToParent(textRect);
            textRect.offsetMin = new Vector2(18f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);
            _healthText = textObject.GetComponent<TextMeshProUGUI>();
        }

        private void RefreshHealthText(float currentHealth, float maxHealth)
        {
            int displayedCurrent = Mathf.CeilToInt(currentHealth);
            int displayedMax = Mathf.CeilToInt(maxHealth);
            _healthText.SetText("HP {0} / {1}", displayedCurrent, displayedMax);

            float healthRatio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            _healthText.color = Color.Lerp(
                new Color(1f, 0.2f, 0.2f, 1f),
                new Color(0.35f, 1f, 0.5f, 1f),
                healthRatio);
        }

        private void SetPhoneOpen(bool isOpen)
        {
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
        }

        private void ShowYoutubeScreen()
        {
            _homeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
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
            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _mailScreen.SetActive(false);
            _phoneDialerScreen.SetActive(true);
        }

        private void ShowMailScreen()
        {
            HideYoutubeWebView();
            _health?.SetYoutubeHealing(false);
            _homeScreen.SetActive(false);
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(true);
        }

        private void ShowHomeScreen()
        {
            HideYoutubeWebView();
            _youtubeScreen.SetActive(false);
            _phoneDialerScreen.SetActive(false);
            _mailScreen.SetActive(false);
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
                    zoom: true);
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

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.HealthChanged -= RefreshHealthText;
                _health.SetYoutubeHealing(false);
            }

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
