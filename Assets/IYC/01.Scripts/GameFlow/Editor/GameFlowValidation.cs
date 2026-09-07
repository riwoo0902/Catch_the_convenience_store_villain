using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CWH.GameFlow.Editor
{
    /// <summary>
    /// Exercises the actual title and gameplay scenes. No gameplay assets are changed.
    /// Run without -quit: this runner exits batch mode after leaving Play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class GameFlowValidation
    {
        private const string SessionKey = "IYC.GameFlowValidation.State";
        private const string ReportKey = "IYC.GameFlowValidation.Report";
        private const string TitleScene = "Assets/Lrw/Scene/Lrw_Scene.unity";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Stack<IEnumerator> Routines = new();
        private static ValidationReport _report;
        private static bool _stepping;

        [Serializable]
        public sealed class ValidationReport
        {
            public string startedUtc;
            public string finishedUtc;
            public string unityVersion;
            public string outputDirectory;
            public string originalScene;
            public bool batch;
            public bool passed;
            public List<CheckResult> checks = new();
            public List<string> screenshots = new();
            public List<string> runtimeErrors = new();
            public List<string> notes = new();
        }

        [Serializable]
        public sealed class CheckResult
        {
            public string name;
            public bool passed;
            public string detail;
        }

        static GameFlowValidation()
        {
            EditorApplication.update += Update;
            Application.logMessageReceived += RecordRuntimeError;
        }

        [MenuItem("Tools/Game Flow/Validate Loop")]
        public static void RunInEditor() => Begin(false);

        public static void RunBatch() => Begin(true);

        private static void Begin(bool batch)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start game flow validation from Edit mode.");
            if (!batch && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save modified scenes before starting game flow validation.");

            string directory = Path.GetFullPath(Path.Combine("Logs", "GameFlowValidation", DateTime.Now.ToString("yyyyMMdd-HHmmss")));
            Directory.CreateDirectory(directory);
            _report = new ValidationReport
            {
                startedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                outputDirectory = directory,
                originalScene = SceneManager.GetActiveScene().path,
                batch = batch
            };
            _report.notes.Add("UI Button.onClick and the same story advance method used by clicks exercise the real scenes. Input hardware and external YouTube/network services are not automated.");
            _report.notes.Add("The false-report case pauses only the spawn coroutine and continuous health effects; the actual dialer, five-second timer, police visit, and one-time health penalty execute. Midnight is reached by advancing the in-memory elapsed timer, not by waiting ten minutes.");
            SessionState.SetString(SessionKey, "entering");
            Persist();
            EditorSceneManager.OpenScene(TitleScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Update()
        {
            string state = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(state) || _stepping || EditorApplication.isCompiling)
                return;
            _report ??= JsonUtility.FromJson<ValidationReport>(SessionState.GetString(ReportKey, "{}"));

            if (state == "finishing")
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.EraseString(SessionKey);
                    bool passed = _report.passed;
                    if (_report.batch)
                        EditorApplication.Exit(passed ? 0 : 1);
                    else if (!string.IsNullOrEmpty(_report.originalScene))
                        EditorSceneManager.OpenScene(_report.originalScene, OpenSceneMode.Single);
                }
                return;
            }

            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            if (state == "entering")
            {
                SessionState.SetString(SessionKey, "running");
                Routines.Push(RunFlow());
            }
            else if (Routines.Count == 0)
            {
                Finish("The validation coroutine was interrupted by a domain reload.");
                return;
            }

            _stepping = true;
            try
            {
                IEnumerator current = Routines.Peek();
                if (!current.MoveNext())
                {
                    Routines.Pop();
                    if (Routines.Count == 0)
                        Finish(null);
                }
                else if (current.Current is IEnumerator nested)
                    Routines.Push(nested);
            }
            catch (Exception error)
            {
                Finish(error.ToString());
            }
            finally
            {
                _stepping = false;
            }
        }

        private static IEnumerator RunFlow()
        {
            yield return Wait(0.5f);
            Require(SceneManager.GetActiveScene().path == TitleScene, "Actual title scene opens");
            Require(Find("CWH.Player.UI.PlayerHUDController") == null, "Gameplay HUD is absent on title");
            yield return Capture("01-title");

            Component startTarget = Find("Lrw.Script.UI.StartButton");
            Require(startTarget != null, "Title has a StartButton handler");
            Button startButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(index => button.onClick.GetPersistentTarget(index) == startTarget));
            startButton ??= startTarget.GetComponent<Button>();
            Require(startButton != null && startButton.gameObject.activeInHierarchy, "Title start button is wired and visible");
            startButton.onClick.Invoke();

            yield return Until(() => Phase() == "Intro", 30f, "Start button loads gameplay intro");
            object loop = Loop();
            Require(ReadString(loop, "ClockText") == "20:00", "New shift starts at 20:00");
            Require(Math.Abs(ReadFloat(loop, "ElapsedSeconds")) < 0.01f, "Intro begins before shift time elapses");
            yield return Wait(0.75f);
            Require(ReadFloat(loop, "ElapsedSeconds") < 0.01f, "Story does not consume work time");
            Require(!ReadBool(loop, "IsPlaying"), "Player is not in gameplay during intro");
            Component view = RequireView();
            GameObject applicationPage = (GameObject)Read(view, "_applicationPage");
            Require(applicationPage.activeInHierarchy && applicationPage.GetComponentInChildren<RawImage>().texture != null,
                "First story page displays the supplied application image");
            Require(!((TMP_Text)Read(view, "_body")).gameObject.activeInHierarchy, "Application image replaces the first text page");
            yield return Capture("02-employment-application");
            Call(view, "AdvanceStory");
            Require(!applicationPage.activeSelf && Convert.ToInt32(Read(view, "_lineIndex")) == 1,
                "One click continues from the image to the second story sentence");
            Require(((TMP_Text)Read(view, "_body")).text == GameLoopController.Instance.Settings.Opening[1],
                "Remaining opening story text is preserved");
            yield return Capture("02-intro-typewriter");
            TMP_Text narrative = (TMP_Text)Read(view, "_body");
            // A slow graphics frame may have completed the first line before capture.
            // In that case the next click starts the next actual narrative line.
            if (narrative.maxVisibleCharacters >= Convert.ToInt32(Read(view, "_characterCount")))
                Call(view, "AdvanceStory");
            int lineBeforeCompletion = Convert.ToInt32(Read(view, "_lineIndex"));
            Call(view, "AdvanceStory");
            Require(Phase() == "Intro" && Convert.ToInt32(Read(view, "_lineIndex")) == lineBeforeCompletion
                && narrative.maxVisibleCharacters == Convert.ToInt32(Read(view, "_characterCount")), "Story click completes the current line without skipping it");
            yield return Capture("03-intro-completed-line");
            yield return AdvanceNarrative("Intro", "Playing");

            loop = Loop();
            Require(ReadBool(loop, "IsPlaying"), "Final intro click starts gameplay");
            Require(ReadString(loop, "ClockText") == "20:00", "Intro completion preserves start time");
            object settings = Read(loop, "Settings");
            float duration = ReadDuration(settings);
            Require(Math.Abs(duration - 600f) < 0.01f, "Default shift lasts ten real minutes", duration.ToString("0.###"));
            yield return Wait(1f);
            Require(ReadFloat(loop, "ElapsedSeconds") > 0.1f, "Clock advances during gameplay");

            Component hud = Find("CWH.Player.UI.PlayerHUDController");
            Require(hud != null, "Gameplay HUD installs after title scene transition");
            yield return ExerciseTutorial(hud);
            Call(hud, "SetPhoneOpen", true);
            Click(hud, "ClockButton");
            yield return Wait(0.2f);
            GameObject clockScreen = Read(hud, "_clockScreen") as GameObject;
            TMP_Text clockText = Read(hud, "_clockTimeText") as TMP_Text;
            Require(clockScreen != null && clockScreen.activeInHierarchy, "Clock app opens from its home icon");
            Require(clockText != null && clockText.text == ReadString(loop, "ClockText"), "Clock app displays the shift time");
            float beforePhoneWait = ReadFloat(loop, "ElapsedSeconds");
            yield return Wait(0.7f);
            Require(ReadFloat(loop, "ElapsedSeconds") > beforePhoneWait + 0.1f, "Time continues with phone open");
            yield return Capture("04-phone-clock");

            // Keep the no-villain fixture deterministic without changing scene or settings assets.
            Component spawner = Find("CWH.Villains.ConvenienceStoreVillainSpawner");
            if (spawner is MonoBehaviour spawnBehaviour)
                spawnBehaviour.StopAllCoroutines();
            // The manager's errands pay out health, which would move the numbers this section measures.
            if (Find("CWH.Quests.ShiftQuestBoard") is MonoBehaviour questBoard)
                questBoard.enabled = false;
            Type policeType = TypeOf("CWH.Villains.PoliceResponseController");
            Require(!(bool)CallStatic(TypeOf("CWH.Villains.RuntimePoliceOfficer"), "HasActiveVillains"), "False-report fixture has no active villain");
            Call(hud, "ShowHomeScreen");
            Click(hud, "PhoneButton");
            Click(hud, "DialKey_CLR");
            Click(hud, "DialKey_1");
            Button emergency = FindButton(hud, "EmergencyCallButton");
            Require(!emergency.interactable, "Incomplete phone number cannot be called");
            Call(hud, "BeginEmergencyCall");
            Require(!(bool)ReadStatic(policeType, "IsResponseActive"), "Invalid number does not dispatch police");
            Click(hud, "DialKey_1");
            Click(hud, "DialKey_2");
            Require(ReadString(hud, "_dialedNumber") == "112" && emergency.interactable, "Keypad accepts 1, 1, 2 in order");
            float callStarted = Time.time;
            emergency.onClick.Invoke();
            Require((bool)ReadStatic(policeType, "IsResponseActive"), "Call button starts a response immediately");
            Require((bool)ReadStatic(policeType, "LastReportWasFalse"), "No-villain report is classified at call time");
            Require(!(bool)CallStatic(policeType, "TryBeginEmergencyCall"), "Duplicate emergency report is rejected");
            Require(Find("CWH.Villains.RuntimePoliceOfficer") == null, "Police is not spawned before five seconds");
            Component movement = FindByShortName("PlayerMovementController");
            Require(movement is Behaviour movementBehaviour && movementBehaviour.enabled, "Phone use leaves movement enabled");
            yield return Wait(0.5f);
            Require(ReadBool(hud, "_policeCallPending"), "Countdown stays active while using phone");
            yield return Capture("05-police-countdown");
            yield return Until(() => Find("CWH.Villains.RuntimePoliceOfficer") != null, 12f, "Five-second countdown dispatches police");
            Require(Time.time - callStarted >= 4.95f, "Police never dispatches before the five-second boundary", (Time.time - callStarted).ToString("0.000"));
            Component health = Find("CWH.Player.Health.PlayerHealth");
            Require(health != null, "Player health exists after scene transition");
            ((Behaviour)health).enabled = false;
            Call(health, "TakeDamage", 1);
            float beforePenalty = ReadFloat(health, "CurrentHealth");
            Component officer = Find("CWH.Villains.RuntimePoliceOfficer");
            GameObject player = GameObject.Find("Player");
            Require(player != null, "Gameplay player exists");
            officer.transform.position = player.transform.position + Vector3.right * 0.4f;
            yield return Until(() => ReadBool(officer, "_isAttacking"), 2f, "False report begins the police melee attack");
            Require(ReadFloat(health, "CurrentHealth") == beforePenalty, "False-report damage waits for the attack hit frame");
            Require(Convert.ToInt32(Read(officer, "_currentAnimationHash")) == Animator.StringToHash("Standing Melee Attack Downward"), "False report uses the villain-suppression attack animation");
            yield return Until(() => ReadFloat(health, "CurrentHealth") < beforePenalty, 4f, "Police false-report visit applies its penalty");
            Require(Math.Abs(beforePenalty - ReadFloat(health, "CurrentHealth") - 20f) < 0.01f, "False report costs exactly 20 HP even during hit invulnerability");
            float afterPenalty = ReadFloat(health, "CurrentHealth");
            yield return Wait(0.5f);
            Require(Math.Abs(ReadFloat(health, "CurrentHealth") - afterPenalty) < 0.01f, "False-report penalty is applied once");
            ((Behaviour)health).enabled = true;
            Call(hud, "SetPhoneOpen", false);

            // Restore only height at the fallen location, without respawning at the origin.
            Component recovery = Find("CWH.Player.PlayerFallRecovery");
            Require(recovery != null, "Fall recovery is automatically installed");
            CharacterController character = player.GetComponent<CharacterController>();
            Vector3 beforeFall = player.transform.position;
            Vector3 fallen = new(beforeFall.x + 0.4f, -20f, beforeFall.z + 0.3f);
            character.enabled = false;
            player.transform.position = fallen;
            character.enabled = true;
            Call(recovery, "LateUpdate");
            Require(Mathf.Abs(player.transform.position.x - fallen.x) < 0.0001f
                && Mathf.Abs(player.transform.position.z - fallen.z) < 0.0001f
                && player.transform.position.y >= 2f, "Falling below the floor preserves XZ and raises only Y");
            object movementSnapshot = Call(Read(movement, "StateSource"), "GetSnapshot");
            Vector3 recoveredVelocity = (Vector3)Read(movementSnapshot, "Velocity");
            Require(Mathf.Abs(recoveredVelocity.y) < 0.0001f, "Fall recovery clears downward velocity");

            Call(health, "TakeDamage", 10000);
            yield return Until(() => Phase() == "DeathStory", 3f, "Zero health opens death story");
            float deathTime = ReadFloat(Loop(), "ElapsedSeconds");
            yield return Wait(0.5f);
            Require(Math.Abs(ReadFloat(Loop(), "ElapsedSeconds") - deathTime) < 0.01f, "Clock freezes after death");
            Require(!(bool)ReadStatic(policeType, "IsResponseActive"), "Death cancels active police response");
            yield return Capture("06-death-story");
            yield return AdvanceNarrative("DeathStory", "Results");
            yield return Capture("07-game-over-results");
            Button restart = FindResultButton("Restart");
            Require(restart != null, "Game-over result has a restart button");
            restart.onClick.Invoke();
            yield return Until(() => Phase() == "Playing" && ReadFloat(Loop(), "ElapsedSeconds") < 5f, 30f, "Restart reloads the shift and skips intro");
            Require(ReadString(Loop(), "ClockText") == "20:00", "Restart resets the clock to 20:00");
            Require(!GameLoopController.Instance.TutorialActive, "Restart skips the hands-on tutorial");
            health = Find("CWH.Player.Health.PlayerHealth");
            Require(Math.Abs(ReadFloat(health, "CurrentHealth") - ReadFloat(health, "MaxHealth")) < 0.01f, "Restart restores full health");

            // Exercise both sides of the final-minute boundary without a ten-minute test.
            SetElapsed(Loop(), duration - 0.8f);
            yield return null;
            Require(Phase() == "Playing" && ReadString(Loop(), "ClockText") == "23:59", "Shift remains playable immediately before midnight");
            SetElapsed(Loop(), duration);
            yield return Until(() => Phase() == "Checkout", 4f, "Midnight immediately starts checkout");
            Require(ReadString(Loop(), "ClockText") == "24:00" && !ReadBool(Loop(), "IsPlaying"), "Midnight shows 24:00 and stops gameplay");
            Require((bool)CallStatic(policeType, "TryBeginEmergencyCall") == false, "Calls cannot start during checkout");
            yield return Capture("08-midnight-checkout");
            yield return Until(() => Phase() == "Ending", 25f, "Checkout presentation reaches ending story");
            yield return Capture("09-ending-story");
            yield return AdvanceNarrative("Ending", "Results");
            yield return Capture("10-shift-complete-results");
            Button returnToTitle = FindResultButton("Exit");
            Require(returnToTitle != null && returnToTitle.GetComponentInChildren<TMP_Text>().text == "타이틀로", "Success result has a title button");
            returnToTitle.onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().path == TitleScene, 30f, "Result title button returns to the real title scene");
            Require(Math.Abs(Time.timeScale - 1f) < 0.01f, "Returning to title restores normal time scale");
            Require(Find("CWH.Player.UI.PlayerHUDController") == null, "Gameplay HUD is removed when returning to title");
            yield return Capture("11-returned-title");

            GameLoopController.StartNewGame();
            yield return Until(() => Phase() == "Intro", 30f, "New game restores the story after returning to title");
            yield return AdvanceNarrative("Intro", "Playing");
            Require(GameLoopController.Instance.TutorialActive, "New game restores the tutorial");
            CWH.Player.Health.PlayerHealth.GetOrCreate().TakeDamage(10000);
            yield return Until(() => Phase() == "DeathStory", 3f, "Death can interrupt the hands-on tutorial");
            Require(!GameLoopController.Instance.TutorialActive, "Death dismisses the tutorial objective");
            yield return AdvanceNarrative("DeathStory", "Results");
            FindResultButton("Restart").onClick.Invoke();
            yield return Until(() => Phase() == "Playing", 30f, "Restart after tutorial death resumes the shift");
            Require(!GameLoopController.Instance.TutorialActive, "Restart skips even an unfinished tutorial");

            GameLoopController.StartNewGame();
            yield return Until(() => Phase() == "Intro", 30f, "Midnight tutorial fixture opens");
            yield return AdvanceNarrative("Intro", "Playing");
            Require(GameLoopController.Instance.TutorialActive, "Midnight fixture starts inside tutorial");
            SetElapsed(Loop(), duration);
            yield return Until(() => Phase() == "Checkout", 4f, "Midnight interrupts even an unfinished tutorial");
            Require(!GameLoopController.Instance.TutorialActive, "Midnight dismisses tutorial guidance");
            yield return Capture("tutorial-06-midnight-interruption");
        }

        private static IEnumerator ExerciseTutorial(Component hud)
        {
            var loop = GameLoopController.Instance;
            var tutorial = Object.FindFirstObjectByType<ShiftTutorial>();
            Require(tutorial != null && loop.TutorialActive && tutorial.Step == TutorialStep.OpenPhone,
                "Story starts with only the open-phone instruction");
            Require(!GameLoopController.AllowsRandomSpawns, "Random arrivals are held during the tutorial");
            float started = loop.ElapsedSeconds;
            yield return Capture("tutorial-01-open-phone");
            Call(hud, "SetPhoneOpen", true);
            Require(tutorial.Step == TutorialStep.OpenClock, "Opening the phone advances exactly one action");
            Transform highlight = FindButton(hud, "ClockButton").transform.Find("Tutorial Button Highlight");
            Require(highlight != null && highlight.gameObject.activeInHierarchy
                && highlight.GetComponentsInChildren<Image>().All(border => !border.raycastTarget),
                "Requested app has a visible non-blocking highlight");
            yield return Capture("tutorial-02-highlight-clock");
            Click(hud, "MailButton");
            Require(tutorial.Step == TutorialStep.OpenClock, "Wrong app does not advance the requested action");
            Click(hud, "MailBackButton");
            Click(hud, "ClockButton");
            Require(tutorial.Step == TutorialStep.ReadClock, "Clock click opens a separate reading step");
            yield return Wait(4f);
            Require(tutorial.Step == TutorialStep.ReadClock, "Reading never completes automatically after a timer");
            Call(hud, "SetPhoneOpen", false);
            tutorial.ConfirmCurrentStep();
            Require(tutorial.Step == TutorialStep.ReadClock, "Hidden app cannot be confirmed");
            Call(hud, "SetPhoneOpen", true);
            Click(hud, "ClockButton");
            Click(RequireView(), "Tutorial Confirm");
            Require(tutorial.Step == TutorialStep.BackClock, "Confirming clock information requests only the back button");
            yield return Capture("tutorial-03-highlight-back");
            Call(hud, "SetPhoneOpen", false);
            Call(hud, "SetPhoneOpen", true);
            Click(hud, "MailButton");
            Click(hud, "MailBackButton");
            Require(tutorial.Step == TutorialStep.BackClock, "Unrelated back button cannot complete the current back step");
            Click(hud, "ClockButton");
            Click(hud, "ClockBackButton");
            Require(tutorial.Step == TutorialStep.OpenStocks, "Correct back button unlocks the next app lesson");
            Click(hud, "StocksButton");
            Require(tutorial.Step == TutorialStep.ReadStocks, "Health app requires explicit reading confirmation");
            Click(RequireView(), "Tutorial Confirm");
            Click(hud, "StocksBackButton");

            // A non-initialized WebView exercises the button without contacting an external service.
            GameObject webFixture = new("Validation WebView (no network)");
            webFixture.transform.SetParent(hud.transform, false);
            Component webView = webFixture.AddComponent(TypeOf("Gree.UnityWebView.WebViewObject"));
            hud.GetType().GetField("_youtubeWebView", InstanceFlags).SetValue(hud, webView);
            hud.GetType().GetField("_youtubePageRequested", InstanceFlags).SetValue(hud, true);
            Click(hud, "YoutubeButton");
            Require(tutorial.Step == TutorialStep.ReadYoutube, "YouTube opens as its own action");
            yield return Wait(5.2f);
            Require(tutorial.Step == TutorialStep.ReadYoutube, "YouTube also waits for user confirmation, not five seconds");
            Click(RequireView(), "Tutorial Confirm");
            Click(hud, "BackButton");
            Click(hud, "MailButton");
            Require(tutorial.Step == TutorialStep.ReadMail, "Mail is a separate reading step");
            Click(RequireView(), "Tutorial Confirm");
            Click(hud, "MailBackButton");
            Require(tutorial.Step == TutorialStep.ClosePhone, "App lessons end with a separate close-phone instruction");
            Require(!(bool)CallStatic(TypeOf("CWH.Villains.RuntimePoliceOfficer"), "HasActiveVillains"), "No villain appears during app instruction");
            Call(hud, "SetPhoneOpen", false);
            yield return Until(() => tutorial.Step == TutorialStep.OpenReportPhone, 5f, "Closing the phone introduces one villain");
            var spawner = Object.FindFirstObjectByType<CWH.Villains.ConvenienceStoreVillainSpawner>();
            GameObject firstVillain = spawner.TutorialVillain;
            Require(firstVillain != null, "Tutorial has a real villain");
            spawner.TrySpawnTutorialVillain();
            Require(spawner.TutorialVillain == firstVillain, "Tutorial never duplicates its villain");
            Require(loop.ElapsedSeconds > started + 8f, "Work time continues throughout individual tutorial actions");
            Call(hud, "SetPhoneOpen", true);
            Require(tutorial.Step == TutorialStep.OpenDialer, "Reporting starts with a separate phone-app instruction");
            Click(hud, "PhoneButton");
            Require(tutorial.Step == TutorialStep.DialFirstOne, "Phone app first requests only digit one");
            Click(hud, "DialKey_2");
            Require(tutorial.Step == TutorialStep.DialFirstOne, "Wrong digit does not advance the lesson");
            highlight = FindButton(hud, "DialKey_CLR").transform.Find("Tutorial Button Highlight");
            Require(highlight != null && highlight.gameObject.activeInHierarchy, "Wrong number highlights clear for recovery");
            Click(hud, "DialKey_CLR");
            Click(hud, "DialKey_1");
            Require(tutorial.Step == TutorialStep.DialSecondOne, "First one requests the second one separately");
            Click(hud, "DialKey_1");
            Require(tutorial.Step == TutorialStep.DialTwo, "Second one requests digit two separately");
            yield return Capture("tutorial-04-highlight-digit-two");
            Click(hud, "DialKey_2");
            Require(tutorial.Step == TutorialStep.Call, "Completed 112 requests the call button separately");
            Click(hud, "EmergencyCallButton");
            Require(tutorial.Step == TutorialStep.Police && !CWH.Villains.PoliceResponseController.LastReportWasFalse,
                "Actual call advances to evading until police finish");
            yield return Until(() => tutorial.Step == TutorialStep.Complete, 40f, "Police resolves the single tutorial villain");
            yield return Wait(4.2f);
            Require(loop.TutorialActive, "Completion also waits for explicit confirmation");
            yield return Capture("tutorial-05-complete");
            float beforeCompletion = loop.ElapsedSeconds;
            tutorial.ConfirmCurrentStep();
            Require(!loop.TutorialActive && loop.ElapsedSeconds >= beforeCompletion && GameLoopController.AllowsRandomSpawns,
                "Confirmation ends the tutorial without resetting time and releases random arrivals");
        }

        private static IEnumerator AdvanceNarrative(string from, string to)
        {
            int clicks = 0;
            while (Phase() == from && clicks++ < 100)
            {
                Call(RequireView(), "AdvanceStory");
                yield return null;
            }
            yield return Until(() => Phase() == to, 5f, from + " advances to " + to);
        }

        private static IEnumerator Wait(float seconds)
        {
            double finish = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < finish)
                yield return null;
        }

        private static IEnumerator Until(Func<bool> predicate, float timeout, string description)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while (!predicate() && EditorApplication.timeSinceStartup < deadline)
                yield return null;
            Require(predicate(), description, "Phase: " + Phase());
        }

        private static IEnumerator Capture(string name)
        {
            yield return Wait(0.25f);
            Canvas.ForceUpdateCanvases();
            string path = Path.Combine(_report.outputDirectory, name + ".png");
            CaptureRenderedFrame(path);
            _report.screenshots.Add(path);
            Persist();
            yield return Wait(0.35f);
        }

        private static void CaptureRenderedFrame(string path)
        {
            Camera camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(candidate => candidate.enabled && candidate.gameObject.activeInHierarchy)
                .OrderByDescending(candidate => candidate.depth).FirstOrDefault();
            if (camera == null) throw new InvalidOperationException("No active camera for screenshot.");
            RenderTexture texture = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(canvas => canvas.enabled && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var previousCameras = overlays.Select(canvas => canvas.worldCamera).ToArray();
            var previousDistances = overlays.Select(canvas => canvas.planeDistance).ToArray();
            try
            {
                camera.targetTexture = texture;
                foreach (Canvas canvas in overlays)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    // The gameplay camera's 0.01 near plane is too close for SDF text
                    // when temporarily converting overlay UI for an offscreen capture.
                    canvas.planeDistance = Mathf.Max(1f, camera.nearClipPlane + 0.1f);
                }
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                    text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                for (int index = 0; index < overlays.Length; index++)
                {
                    overlays[index].renderMode = RenderMode.ScreenSpaceOverlay;
                    overlays[index].worldCamera = previousCameras[index];
                    overlays[index].planeDistance = previousDistances[index];
                }
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(texture);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static void Require(bool condition, string name, string detail = "")
        {
            _report.checks.Add(new CheckResult { name = name, passed = condition, detail = detail });
            Persist();
            if (!condition)
                throw new InvalidOperationException(name + (string.IsNullOrEmpty(detail) ? string.Empty : ": " + detail));
            Debug.Log("[GameFlowValidation] PASS " + name);
        }

        private static void Finish(string error)
        {
            Routines.Clear();
            if (!string.IsNullOrEmpty(error))
                _report.checks.Add(new CheckResult { name = "Scenario completed", passed = false, detail = error });
            foreach (string screenshot in _report.screenshots)
            {
                if (!File.Exists(screenshot))
                    _report.notes.Add("Screenshot was not produced by this rendering environment: " + screenshot);
            }
            _report.finishedUtc = DateTime.UtcNow.ToString("O");
            _report.passed = _report.checks.Count > 0 && _report.checks.All(check => check.passed) && _report.runtimeErrors.Count == 0;
            Persist();
            string reportPath = Path.Combine(_report.outputDirectory, "report.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(_report, true), Encoding.UTF8);
            string text = string.Join(Environment.NewLine, _report.checks.Select(check => (check.passed ? "PASS " : "FAIL ") + check.name + " " + check.detail));
            File.WriteAllText(Path.Combine(_report.outputDirectory, "report.txt"), text + Environment.NewLine + string.Join(Environment.NewLine, _report.notes), Encoding.UTF8);
            SessionState.SetString(SessionKey, "finishing");
            Debug.Log("[GameFlowValidation] " + (_report.passed ? "PASSED " : "FAILED ") + reportPath);
            EditorApplication.ExitPlaymode();
        }

        private static void Persist() => SessionState.SetString(ReportKey, JsonUtility.ToJson(_report));

        private static void RecordRuntimeError(string condition, string stackTrace, LogType type)
        {
            if (SessionState.GetString(SessionKey, string.Empty) != "running" || _report == null || _report.runtimeErrors.Count >= 50)
                return;
            if (type is not (LogType.Error or LogType.Exception or LogType.Assert))
                return;
            if (stackTrace.Contains("UnityEditor.Search.SearchDatabase") && !stackTrace.Contains("Assets/"))
            {
                string note = "Unity Editor search-index error (outside game scripts): " + condition;
                if (!_report.notes.Contains(note)) _report.notes.Add(note);
                Persist();
                return;
            }
            string message = condition + Environment.NewLine + stackTrace;
            if (!_report.runtimeErrors.Contains(message))
            {
                _report.runtimeErrors.Add(message);
                Persist();
            }
        }

        private static Type TypeOf(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName)).FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException("Missing type: " + fullName);

        private static Component Find(string fullName)
        {
            Type type = TypeOf(fullName);
            return Object.FindFirstObjectByType(type, FindObjectsInactive.Exclude) as Component;
        }

        private static Component FindByShortName(string name) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(component => component.GetType().Name == name);

        private static object Loop() => ReadStatic(TypeOf("CWH.GameFlow.GameLoopController"), "Instance");
        private static string Phase() => Loop() == null ? "None" : ReadString(Loop(), "Phase");
        private static Component RequireView() => Find("CWH.GameFlow.GameFlowView")
            ?? throw new InvalidOperationException("Missing game flow view.");

        private static object Read(object target, string name)
        {
            if (target == null)
                throw new InvalidOperationException("Cannot read " + name + " from null.");
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null)
                return property.GetValue(target);
            FieldInfo field = type.GetField(name, InstanceFlags)
                ?? throw new MissingFieldException(type.FullName, name);
            return field.GetValue(target);
        }

        private static object ReadStatic(Type type, string name) => type.GetProperty(name, StaticFlags)?.GetValue(null)
            ?? type.GetField(name, StaticFlags)?.GetValue(null);
        private static string ReadString(object target, string name) => Read(target, name)?.ToString();
        private static bool ReadBool(object target, string name) => Convert.ToBoolean(Read(target, name));
        private static float ReadFloat(object target, string name) => Convert.ToSingle(Read(target, name));

        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, InstanceFlags)
            ?.Invoke(target, args) ?? EnsureVoidMethodExists(target.GetType(), target, name, InstanceFlags);

        private static object CallStatic(Type type, string name, params object[] args) => type.GetMethod(name, StaticFlags)
            ?.Invoke(null, args) ?? EnsureVoidMethodExists(type, null, name, StaticFlags);

        private static object EnsureVoidMethodExists(Type type, object target, string name, BindingFlags flags)
        {
            if (type.GetMethod(name, flags) == null)
                throw new MissingMethodException(type.FullName, name);
            return null;
        }

        private static Button FindButton(Component root, string name) => root.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.name == name)
            ?? throw new InvalidOperationException("Missing button: " + name);

        private static void Click(Component root, string name)
        {
            Button button = FindButton(root, name);
            Require(button.gameObject.activeInHierarchy && button.interactable, "Button is usable: " + name);
            button.onClick.Invoke();
        }

        private static Button FindResultButton(string name) => RequireView().GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.gameObject.activeInHierarchy && button.interactable
                && button.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

        private static float ReadDuration(object settings)
        {
            foreach (string name in new[] { "ShiftDurationSeconds", "RealShiftDurationSeconds", "ShiftDuration", "DurationSeconds" })
            {
                if (settings.GetType().GetProperty(name, InstanceFlags) != null || settings.GetType().GetField(name, InstanceFlags) != null)
                    return ReadFloat(settings, name);
            }
            throw new InvalidOperationException("Shift settings duration member was not found.");
        }

        private static void SetElapsed(object loop, float seconds)
        {
            FieldInfo field = loop.GetType().GetField("_elapsedSeconds", InstanceFlags)
                ?? throw new MissingFieldException(loop.GetType().FullName, "_elapsedSeconds");
            field.SetValue(loop, seconds);
        }
    }
}
