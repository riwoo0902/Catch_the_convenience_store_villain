using CWH.Player.UI;
using CWH.Villains;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CWH.GameFlow
{
    public enum TutorialStep
    {
        OpenPhone, OpenClock, ReadClock, BackClock,
        OpenStocks, ReadStocks, BackStocks,
        OpenYoutube, ReadYoutube, BackYoutube,
        OpenMail, ReadMail, BackMail, ClosePhone,
        SpawnEncounter, OpenReportPhone, OpenDialer,
        DialFirstOne, DialSecondOne, DialTwo, Call, Police, Complete
    }

    /// <summary>One requested action at a time; reading requires confirmation, never a timeout.</summary>
    public sealed class ShiftTutorial : MonoBehaviour
    {
        private GameLoopController _loop;
        private GameFlowView _view;
        private PlayerHUDController _hud;
        private ConvenienceStoreVillainSpawner _spawner;
        private bool _spawned;
        public bool IsActive { get; private set; }
        public TutorialStep Step { get; private set; }

        public void Begin(GameLoopController loop, GameFlowView view)
        {
            _loop = loop;
            _view = view;
            _hud = FindFirstObjectByType<PlayerHUDController>();
            _spawner = FindFirstObjectByType<ConvenienceStoreVillainSpawner>();
            IsActive = true;
            Step = TutorialStep.OpenPhone;
            if (_hud != null) _hud.PhoneActionPerformed += OnPhoneAction;
            Present();
        }

        private void Update()
        {
            if (!IsActive || !_loop.IsPlaying) return;
            if (Step == TutorialStep.SpawnEncounter && !PoliceResponseController.IsResponseActive)
            {
                if (_spawner == null) _spawner = FindFirstObjectByType<ConvenienceStoreVillainSpawner>();
                if (!_spawned) _spawned = _spawner != null && _spawner.TrySpawnTutorialVillain();
                if (_spawned) Step = TutorialStep.OpenReportPhone;
            }
            if (Step == TutorialStep.Police && !PoliceResponseController.IsResponseActive
                && !RuntimePoliceOfficer.HasActiveVillains()) Step = TutorialStep.Complete;
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ConfirmCurrentStep();
            if (IsActive) Present();
        }

        private void OnPhoneAction(string action)
        {
            if (!IsActive || !_loop.IsPlaying || _hud == null) return;
            // A correct report still resolves the lesson if a player prepared 112 earlier.
            // Never strand the lesson after its only villain has already been handled.
            if (_spawned && action == "EmergencyCallButton" && PoliceResponseController.IsResponseActive
                && !PoliceResponseController.LastReportWasFalse) Step = TutorialStep.Police;
            else if (Step == TutorialStep.OpenPhone && action == "PhoneOpened") Step = TutorialStep.OpenClock;
            else if (Step == TutorialStep.ClosePhone && action == "PhoneClosed") Step = TutorialStep.SpawnEncounter;
            else if (Step == TutorialStep.OpenReportPhone && action == "PhoneOpened") Step = TutorialStep.OpenDialer;
            else if (IsOpenAppStep() && action == AppButton(RequiredApp()) && _hud.CurrentApp == RequiredApp()) Step++;
            else if (IsBackStep() && action == BackButton(RequiredApp()) && _hud.CurrentApp == PhoneApp.Home) Step++;
            else if (Step >= TutorialStep.DialFirstOne && Step <= TutorialStep.Call)
            {
                if (action == "DialKey_CLR") Step = TutorialStep.DialFirstOne;
                else if (Step == TutorialStep.DialFirstOne && action == "DialKey_1" && _hud.DialedNumber == "1") Step++;
                else if (Step == TutorialStep.DialSecondOne && action == "DialKey_1" && _hud.DialedNumber == "11") Step++;
                else if (Step == TutorialStep.DialTwo && action == "DialKey_2" && _hud.DialedNumber == "112") Step++;
            }
            Present();
        }

        public void ConfirmCurrentStep()
        {
            if (!IsActive || !_loop.IsPlaying) return;
            if (Step == TutorialStep.Complete) { Cancel(); return; }
            if (IsReadStep() && _hud != null && _hud.CurrentApp == RequiredApp())
            {
                Step++;
                Present();
            }
        }

        private bool IsOpenAppStep() => Step == TutorialStep.OpenClock || Step == TutorialStep.OpenStocks
            || Step == TutorialStep.OpenYoutube || Step == TutorialStep.OpenMail || Step == TutorialStep.OpenDialer;
        private bool IsReadStep() => Step == TutorialStep.ReadClock || Step == TutorialStep.ReadStocks
            || Step == TutorialStep.ReadYoutube || Step == TutorialStep.ReadMail;
        private bool IsBackStep() => Step == TutorialStep.BackClock || Step == TutorialStep.BackStocks
            || Step == TutorialStep.BackYoutube || Step == TutorialStep.BackMail;

        private PhoneApp RequiredApp()
        {
            if (Step >= TutorialStep.OpenClock && Step <= TutorialStep.BackClock) return PhoneApp.Clock;
            if (Step >= TutorialStep.OpenStocks && Step <= TutorialStep.BackStocks) return PhoneApp.Stocks;
            if (Step >= TutorialStep.OpenYoutube && Step <= TutorialStep.BackYoutube) return PhoneApp.Youtube;
            if (Step >= TutorialStep.OpenMail && Step <= TutorialStep.BackMail) return PhoneApp.Mail;
            if (Step >= TutorialStep.OpenDialer && Step <= TutorialStep.Call) return PhoneApp.Phone;
            return PhoneApp.None;
        }

        private static string AppButton(PhoneApp app) => app switch
        {
            PhoneApp.Clock => "ClockButton", PhoneApp.Stocks => "StocksButton",
            PhoneApp.Youtube => "YoutubeButton", PhoneApp.Mail => "MailButton", PhoneApp.Phone => "PhoneButton", _ => null
        };
        private static string BackButton(PhoneApp app) => app switch
        {
            PhoneApp.Clock => "ClockBackButton", PhoneApp.Stocks => "StocksBackButton",
            PhoneApp.Youtube => "BackButton", PhoneApp.Mail => "MailBackButton", PhoneApp.Phone => "PhoneBackButton", _ => null
        };
        private static string AppName(PhoneApp app) => app switch
        {
            PhoneApp.Clock => "시계", PhoneApp.Stocks => "주식", PhoneApp.Youtube => "YouTube",
            PhoneApp.Mail => "메일", PhoneApp.Phone => "전화", _ => "휴대폰"
        };
        private string ExpectedNumber() => Step switch
        {
            TutorialStep.DialFirstOne => "", TutorialStep.DialSecondOne => "1", TutorialStep.DialTwo => "11", _ => "112"
        };

        private void Present()
        {
            if (!IsActive || _view == null) return;
            PhoneApp app = RequiredApp();
            string objective;
            string hint = "말한 대로 하면 다음으로 넘어가.";
            string target = null;
            bool confirm = false;
            switch (Step)
            {
                case TutorialStep.OpenPhone:
                    objective = "TAB 눌러서 폰 열어 봐.";
                    hint = "교육 시간도 근무 시간이야. 시급 다 나가.";
                    break;
                case TutorialStep.OpenClock:
                case TutorialStep.OpenStocks:
                case TutorialStep.OpenYoutube:
                case TutorialStep.OpenMail:
                case TutorialStep.OpenDialer:
                    objective = $"{AppName(app)} 앱 눌러 봐.";
                    target = AppButton(app);
                    break;
                case TutorialStep.ReadClock:
                    objective = "지금 몇 시인지 봐.\n\n24:00 되면 바로 퇴근이야.\n그때까지만 버티면 돼.";
                    confirm = true;
                    break;
                case TutorialStep.ReadStocks:
                    objective = "이게 네 체력이야.\n\n주가처럼 오르내리지.\n0 되면 더는 못 서 있어.";
                    confirm = true;
                    break;
                case TutorialStep.ReadYoutube:
                    objective = "잠깐 유튜브라도 봐.\n\n보고 있으면 체력이 좀 돌아와.\n다 봤으면 확인 눌러.";
                    confirm = true;
                    break;
                case TutorialStep.ReadMail:
                    objective = "오늘 할 일은 메일로 보내 놨어.\n\n쓰러진 물건은 쳐다보고 E 눌러 정리해.\n그냥 두면 네 체력이 깎여.";
                    confirm = true;
                    break;
                case TutorialStep.BackClock:
                case TutorialStep.BackStocks:
                case TutorialStep.BackYoutube:
                case TutorialStep.BackMail:
                    objective = "뒤로 버튼 눌러.";
                    target = BackButton(app);
                    break;
                case TutorialStep.ClosePhone:
                    objective = "TAB 눌러서 폰 닫아.";
                    hint = "앱은 여기까지. 이제 진짜 손님 온다.";
                    break;
                case TutorialStep.SpawnEncounter:
                    objective = "입구 좀 보고 있어.";
                    hint = "경찰 가고 나면 하나 들어올 거야.";
                    break;
                case TutorialStep.OpenReportPhone:
                    objective = "왔다.\nTAB 눌러서 폰 열어.";
                    hint = "피하면서 신고해. 폰 열어도 움직일 수 있어.";
                    break;
                case TutorialStep.DialFirstOne:
                    objective = "1 눌러.";
                    target = "DialKey_1";
                    break;
                case TutorialStep.DialSecondOne:
                    objective = "1 한 번 더.";
                    target = "DialKey_1";
                    break;
                case TutorialStep.DialTwo:
                    objective = "이제 2.";
                    target = "DialKey_2";
                    break;
                case TutorialStep.Call:
                    objective = "초록색 통화 버튼 눌러.";
                    hint = "누르고 5초면 경찰 와.";
                    target = "EmergencyCallButton";
                    break;
                case TutorialStep.Police:
                    objective = "경찰이 정리할 때까지\n뛰면서 피해 있어.";
                    hint = "신고는 들어갔어. 또 안 걸어도 돼.";
                    break;
                default:
                    objective = "잘했어.\n\n이제 24:00까지만 하면 돼.\n아무 일 없는데 112 누르면 허위 신고야.\n그땐 네가 붙잡히고 체력도 20 깎여.";
                    confirm = true;
                    break;
            }
            // Recover guidance after unrelated navigation without locking controls or crediting a step.
            if (app != PhoneApp.None && _hud != null)
            {
                if (!_hud.IsPhoneOpen)
                {
                    objective = "TAB 눌러서 폰 다시 열어.";
                    hint = "하던 데부터 다시 하자.";
                    target = null;
                    confirm = false;
                }
                else if (_hud.CurrentApp != app && (!IsOpenAppStep() || _hud.CurrentApp != PhoneApp.Home))
                {
                    bool home = _hud.CurrentApp == PhoneApp.Home;
                    objective = home ? $"{AppName(app)} 앱 다시 눌러." : "뒤로 눌러서 앱 목록으로 나와.";
                    target = home ? AppButton(app) : BackButton(_hud.CurrentApp);
                    confirm = false;
                }
                else if (IsOpenAppStep() && _hud.CurrentApp == app)
                {
                    objective = "뒤로 눌러서 앱 목록으로 나와.";
                    target = BackButton(app);
                }
                else if (Step >= TutorialStep.DialFirstOne && Step <= TutorialStep.Call && _hud.DialedNumber != ExpectedNumber())
                {
                    objective = "지움 눌러서 지워.";
                    hint = "괜찮아. 112부터 다시 눌러 보자.";
                    target = "DialKey_CLR";
                }
            }
            if ((Step == TutorialStep.OpenReportPhone || Step == TutorialStep.OpenPhone) && _hud != null && _hud.IsPhoneOpen)
                objective = "TAB 눌러서 폰 닫아.";
            if (Step == TutorialStep.Complete && _hud != null && !_hud.IsPhoneOpen)
                objective += "\n\nEnter 누르면 교육 끝이야.";
            RectTransform anchor = _hud != null ? _hud.HighlightTutorialButton(target) : null;
            _view.ShowTutorial("사장님", objective, hint, confirm ? ConfirmCurrentStep : null, anchor);
        }

        public void Cancel()
        {
            if (!IsActive) return;
            IsActive = false;
            if (_hud != null)
            {
                _hud.PhoneActionPerformed -= OnPhoneAction;
                _hud.HighlightTutorialButton(null);
            }
            if (_view != null) _view.HideTutorial();
        }
        private void OnDestroy() => Cancel();
    }
}
