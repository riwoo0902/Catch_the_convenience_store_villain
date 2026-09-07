using System.Collections.Generic;
using CWH.GameFlow;
using CWH.Player.Health;
using CWH.Player.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CWH.Quests
{
    public enum ShiftTaskKind { Tidy, Report, Endure }

    /// <summary>A kind of errand the manager hands out, with the wording he uses for it.</summary>
    public readonly struct ShiftTaskTemplate
    {
        public ShiftTaskTemplate(ShiftTaskKind kind, string title, string note, string objective, string unit,
            int minTarget, int maxTarget, int healthReward)
        {
            Kind = kind;
            Title = title;
            Note = note;
            Objective = objective;
            Unit = unit;
            MinTarget = minTarget;
            MaxTarget = maxTarget;
            HealthReward = healthReward;
        }

        public ShiftTaskKind Kind { get; }
        public string Title { get; }
        public string Note { get; }
        public string Objective { get; }
        public string Unit { get; }
        public int MinTarget { get; }
        public int MaxTarget { get; }
        public int HealthReward { get; }
    }

    /// <summary>One extra job the manager mailed in, and the health he pays for finishing it.</summary>
    public sealed class ShiftTask
    {
        public ShiftTask(ShiftTaskTemplate template, int target, int baseline)
        {
            Template = template;
            Target = target;
            Baseline = baseline;
        }

        public ShiftTaskTemplate Template { get; }
        public int Target { get; }
        public int Baseline { get; }
        public int Progress { get; private set; }
        public float EndureSeconds { get; set; }
        public bool IsDone { get; private set; }
        public float DoneAt { get; private set; }

        public string Title => Template.Title;
        public string Note => Template.Note;
        public int HealthReward => Template.HealthReward;
        public string ProgressText => $"{Template.Objective}  {Mathf.Min(Progress, Target)} / {Target}{Template.Unit}";

        public void SetProgress(int value) => Progress = Mathf.Max(0, value);

        public void MarkDone(float time)
        {
            IsDone = true;
            DoneAt = time;
        }
    }

    /// <summary>
    /// The manager keeps mailing extra work through the shift. Finishing a job pays back health,
    /// which is what makes the rest of the night survivable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShiftQuestBoard : MonoBehaviour
    {
        private const float FirstMailDelay = 15f;
        private const float MinMailInterval = 45f;
        private const float MaxMailInterval = 75f;
        private const int MaxOpenTasks = 3;
        private const float DoneCardLifetime = 15f;

        private static readonly ShiftTaskTemplate[] Templates =
        {
            new(ShiftTaskKind.Tidy, "진열대 정리",
                "3번 매대 또 엎어졌더라. 손님 오기 전에 좀 세워 놔.", "상품 정리", "개", 3, 5, 12),
            new(ShiftTaskKind.Tidy, "매대 재정비",
                "CCTV로 보니까 바닥에 물건 굴러다니던데. 정리 부탁해.", "상품 정리", "개", 4, 6, 14),
            new(ShiftTaskKind.Report, "난동 신고",
                "또 난동 부리면 참지 말고 바로 112 눌러. 책임은 내가 져.", "112 신고", "건", 1, 2, 15),
            new(ShiftTaskKind.Report, "재발 방지",
                "어제 그 손님 또 온다는데, 오면 참지 말고 신고해.", "112 신고", "건", 1, 1, 12),
            new(ShiftTaskKind.Endure, "매장 지키기",
                "나 물건 받으러 잠깐 나갔다 올게. 자리 좀 지켜 줘.", "매장 지키기", "초", 60, 90, 10),
            new(ShiftTaskKind.Endure, "야간 대기",
                "이 시간이 제일 조용해. 별일 없으면 그냥 서 있어도 돼.", "매장 지키기", "초", 45, 70, 8)
        };

        private readonly List<ShiftTask> _tasks = new();
        private readonly List<int> _templateOrder = new();
        private int _orderIndex;
        private float _nextMailTime;

        public static ShiftQuestBoard Instance { get; private set; }

        /// <summary>Bumped whenever a card is added, advanced, or cleared, so the mail app can repaint.</summary>
        public static int Revision { get; private set; }

        /// <summary>The last thing worth showing on the HUD, until someone takes it.</summary>
        public static string Announcement { get; private set; }

        public IReadOnlyList<ShiftTask> Tasks => _tasks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneInstallation()
        {
            Instance = null;
            Revision = 0;
            Announcement = null;
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
            if (!GameLoopController.IsGameplayScene(scene) || Instance != null)
            {
                return;
            }

            GameObject host = new("Shift Quest Board");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<ShiftQuestBoard>();
        }

        public static string TakeAnnouncement()
        {
            string message = Announcement;
            Announcement = null;
            return message;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _nextMailTime = Time.time + FirstMailDelay;
            ReshuffleTemplates();
        }

        private void Update()
        {
            // The manager stays quiet through training and once the shift is over.
            if (!GameLoopController.AllowsRandomSpawns)
            {
                _nextMailTime = Mathf.Max(_nextMailTime, Time.time + FirstMailDelay);
                return;
            }

            AdvanceOpenTasks(Time.deltaTime);
            ClearFinishedCards();
            if (Time.time >= _nextMailTime && OpenTaskCount() < MaxOpenTasks)
            {
                IssueTask();
            }
        }

        private int OpenTaskCount()
        {
            int open = 0;
            foreach (ShiftTask task in _tasks)
            {
                if (!task.IsDone)
                {
                    open++;
                }
            }

            return open;
        }

        private void IssueTask()
        {
            if (_orderIndex >= _templateOrder.Count)
            {
                ReshuffleTemplates();
            }

            ShiftTaskTemplate template = Templates[_templateOrder[_orderIndex++]];
            int target = Random.Range(template.MinTarget, template.MaxTarget + 1);
            _tasks.Add(new ShiftTask(template, target, CurrentCount(template.Kind)));
            _nextMailTime = Time.time + Random.Range(MinMailInterval, MaxMailInterval);
            Announcement = $"점장님 추가 업무: {template.Title}";
            Revision++;
        }

        private void ReshuffleTemplates()
        {
            _templateOrder.Clear();
            for (int i = 0; i < Templates.Length; i++)
            {
                _templateOrder.Add(i);
            }

            for (int i = _templateOrder.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (_templateOrder[i], _templateOrder[swap]) = (_templateOrder[swap], _templateOrder[i]);
            }

            _orderIndex = 0;
        }

        private static int CurrentCount(ShiftTaskKind kind) => kind switch
        {
            ShiftTaskKind.Tidy => ShelfProductInteraction.RestoredProductCount,
            ShiftTaskKind.Report => GameLoopController.Instance != null ? GameLoopController.Instance.ArrestCount : 0,
            _ => 0
        };

        private void AdvanceOpenTasks(float deltaTime)
        {
            foreach (ShiftTask task in _tasks)
            {
                if (task.IsDone)
                {
                    continue;
                }

                int previous = task.Progress;
                if (task.Template.Kind == ShiftTaskKind.Endure)
                {
                    task.EndureSeconds += deltaTime;
                    task.SetProgress(Mathf.FloorToInt(task.EndureSeconds));
                }
                else
                {
                    task.SetProgress(CurrentCount(task.Template.Kind) - task.Baseline);
                }

                if (task.Progress != previous)
                {
                    Revision++;
                }

                if (task.Progress < task.Target)
                {
                    continue;
                }

                task.MarkDone(Time.time);
                PlayerHealth.Instance?.Heal(task.HealthReward);
                Announcement = $"업무 완료!  체력 +{task.HealthReward}";
                Revision++;
            }
        }

        private void ClearFinishedCards()
        {
            for (int index = _tasks.Count - 1; index >= 0; index--)
            {
                if (!_tasks[index].IsDone || Time.time - _tasks[index].DoneAt < DoneCardLifetime)
                {
                    continue;
                }

                _tasks.RemoveAt(index);
                Revision++;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
