using UnityEngine;

namespace CWH.Quests
{
    [CreateAssetMenu(fileName = "NewQuest", menuName = "CWH/Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string _questId = "quest_id";
        [SerializeField] private int _sortOrder;
        [SerializeField] private string _title = "새 퀘스트";
        [SerializeField, TextArea(2, 5)] private string _description = "퀘스트 설명";
        [SerializeField] private string _objective = "목표";
        [SerializeField, Min(1)] private int _targetAmount = 1;
        [SerializeField] private string _reward = "없음";

        public string QuestId => _questId;
        public int SortOrder => _sortOrder;
        public string Title => _title;
        public string Description => _description;
        public string Objective => _objective;
        public int TargetAmount => _targetAmount;
        public string Reward => _reward;
    }
}
