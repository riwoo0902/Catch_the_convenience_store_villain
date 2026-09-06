using System.Collections;
using System.Collections.Generic;
using Branches.CWH.Scripts.Player;
using CWH.Player.Health;
using CWH.Player.Interaction;
using CWH.Player.UI;
using CWH.Villains;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CWH.GameFlow
{
    public enum ShiftPhase { Intro, Playing, Checkout, DeathStory, Ending, Results }

    [DefaultExecutionOrder(-500)]
    public sealed class GameLoopController : MonoBehaviour
    {
        public const string GameplayScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
        public const string TitleScenePath = "Assets/Lrw/Scene/Lrw_Scene.unity";
        public static GameLoopController Instance { get; private set; }
        // Isolated actor test scenes continue to work without a shift controller.
        public static bool AllowsGameplay => Instance == null || Instance.IsPlaying;
        private static bool _skipNextIntro;
        private static bool _loading;
        private float _elapsedSeconds;
        private PlayerHealth _health;
        private GameFlowView _view;
        private readonly List<Behaviour> _pausedControls = new();
        private readonly List<Canvas> _hiddenCanvases = new();
        private bool _ownsFallbackSettings;
        private int _arrestCount;
        private bool _won;

        public GameFlowSettings Settings { get; private set; }
        public ShiftPhase Phase { get; private set; } = ShiftPhase.Intro;
        public bool IsPlaying => Phase == ShiftPhase.Playing;
        public float ElapsedSeconds => _elapsedSeconds;
        public int ArrestCount => _arrestCount;
        public float Progress01 => Settings != null ? Mathf.Clamp01(_elapsedSeconds / Settings.ShiftDurationSeconds) : 0f;
        public string ClockText
        {
            get
            {
                int startMinutes = (Settings != null ? Settings.StartHour : 20) * 60;
                int minutes = Mathf.FloorToInt(Mathf.Lerp(startMinutes, 24 * 60, Progress01));
                return $"{minutes / 60:00}:{minutes % 60:00}";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Instance = null;
            _skipNextIntro = false;
            _loading = false;
            Time.timeScale = 1f;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _loading = false;
            if (IsGameplayScene(scene))
            {
                if (FindFirstObjectByType<GameLoopController>() == null)
                    new GameObject("Game Loop").AddComponent<GameLoopController>();
            }
            else if (scene.path == TitleScenePath)
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public static bool IsGameplayScene(Scene scene) => scene.path == GameplayScenePath;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Settings = Resources.Load<GameFlowSettings>("GameFlowSettings");
            if (Settings == null)
            {
                Settings = ScriptableObject.CreateInstance<GameFlowSettings>();
                _ownsFallbackSettings = true;
            }
            _view = gameObject.AddComponent<GameFlowView>();
            _view.Build(Settings);
            Time.timeScale = 0f;
            PausePlayerControls();
        }

        private IEnumerator Start()
        {
            // Allow sceneLoaded installers and scene Start methods to finish before binding HUD.
            yield return null;
            _health = PlayerHealth.GetOrCreate();
            if (_health == null)
            {
                Debug.LogError("Game loop requires a Player with PlayerHealth.");
                yield break;
            }
            _health.Died += OnPlayerDied;
            FreezeGameplay();
            bool skip = _skipNextIntro;
            _skipNextIntro = false;
            if (skip) BeginShift();
            else _view.ShowStory("근무 전 안내", Settings.Opening, BeginShift);
        }

        private void Update()
        {
            if (!IsPlaying) return;
            _elapsedSeconds = Mathf.Min(_elapsedSeconds + Time.deltaTime, Settings.ShiftDurationSeconds);
            if (_elapsedSeconds >= Settings.ShiftDurationSeconds) BeginCheckout();
        }

        private void BeginShift()
        {
            if (Phase != ShiftPhase.Intro) return;
            Phase = ShiftPhase.Playing;
            _view.Hide();
            foreach (Behaviour control in _pausedControls)
                if (control != null) control.enabled = true;
            _pausedControls.Clear();
            foreach (Canvas canvas in _hiddenCanvases)
                if (canvas != null) canvas.enabled = true;
            _hiddenCanvases.Clear();
            FindFirstObjectByType<PlayerHUDController>()?.SetGameplayEnabled(true);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void PausePlayerControls()
        {
            PauseAll<PlayerCameraController>();
            // Keep movement Start available so dependent visual scripts can initialize.
            // Its FixedUpdate cannot run while the shift's time scale is zero.
            PauseAll<ShelfProductInteraction>();
            PauseAll<Branches.CWH.Scripts.Player.Test.PlayerPositionResetKeyTrigger>();
        }

        private void PauseAll<T>() where T : Behaviour
        {
            foreach (T control in FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!control.enabled) continue;
                _pausedControls.Add(control);
                control.enabled = false;
            }
        }

        private void FreezeGameplay()
        {
            FindFirstObjectByType<PlayerHUDController>()?.SetGameplayEnabled(false);
            PoliceResponseController.CancelEmergencyCall();
            Time.timeScale = 0f;
            PausePlayerControls();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!canvas.enabled || canvas == _view.Canvas) continue;
                _hiddenCanvases.Add(canvas);
                canvas.enabled = false;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnPlayerDied()
        {
            if (!IsPlaying) return;
            Phase = ShiftPhase.DeathStory;
            FreezeGameplay();
            _view.ShowStory("근무 실패", Settings.Death, ShowResults);
        }

        private void BeginCheckout()
        {
            if (!IsPlaying) return;
            Phase = ShiftPhase.Checkout;
            _won = true;
            FreezeGameplay();
            StartCoroutine(Checkout());
        }

        private IEnumerator Checkout()
        {
            _view.ShowCheckout();
            ShiftCheckoutSequence sequence = gameObject.AddComponent<ShiftCheckoutSequence>();
            yield return sequence.Play(Settings.CheckoutDurationSeconds);
            Phase = ShiftPhase.Ending;
            _view.ShowStory("24:00 · 퇴근 완료", Settings.Ending, ShowResults);
        }

        private void ShowResults()
        {
            Phase = ShiftPhase.Results;
            _view.ShowResults(_won, _elapsedSeconds, _arrestCount, RestartShift, _won ? ReturnToTitle : ExitGame);
        }

        public void RecordArrest() { if (IsPlaying) _arrestCount++; }
        public static void StartNewGame() => LoadScene(GameplayScenePath, false);
        public void RestartShift() => LoadScene(GameplayScenePath, true);
        public void ReturnToTitle() => LoadScene(TitleScenePath, false);
        public static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void LoadScene(string path, bool skipIntro)
        {
            if (_loading) return;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError($"Game flow scene is missing from the build scene list: {path}");
                return;
            }
            _loading = true;
            _skipNextIntro = skipIntro;
            PoliceResponseController.CancelEmergencyCall();
            // Synchronous scene replacement prevents live AI frames during transition.
            SceneManager.LoadScene(path);
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnPlayerDied;
            if (Instance != this) return;
            Instance = null;
            Time.timeScale = 1f;
            if (_ownsFallbackSettings) Destroy(Settings);
        }
    }
}
