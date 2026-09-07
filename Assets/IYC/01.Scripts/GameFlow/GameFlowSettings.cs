using UnityEngine;

namespace CWH.GameFlow
{
    [CreateAssetMenu(menuName = "CWH/Game Flow Settings", fileName = "GameFlowSettings")]
    public sealed class GameFlowSettings : ScriptableObject
    {
        [Header("Shift")]
        [SerializeField, Min(1f)] private float _shiftDurationSeconds = 600f;
        [SerializeField, Range(0, 23)] private int _startHour = 20;
        [SerializeField, Min(1f)] private float _charactersPerSecond = 28f;
        [SerializeField, Min(2f)] private float _checkoutDurationSeconds = 7f;
        [SerializeField] private Texture2D _clockTexture;
        [SerializeField] private Texture2D _openingApplication;
        private Sprite _clockIcon;

        [Header("Opening — each entry is one click")]
        [SerializeField, TextArea(2, 6)] private string[] _opening =
        {
            "야간 편의점 알바 구함.\n시급은 최저임금. 야간수당은 5인 미만 사업장이라 없다.\n업무: 계산, 상품 정리, 그리고 참기.",
            "면접은 십 분 만에 끝났다.\n사장님은 천장의 CCTV를 가리키며 말했다.\n\"무슨 일 생기면 저게 다 봐 줄 거야.\"",
            "봉투값 백 원에 욕이 날아오고,\n계산이 느리다며 진열대가 넘어간다.\n손님은 왕이라는데, 왕은 왜 밤에만 올까.",
            "참으면 넘어가고, 대들면 내 잘못이 된다.\n그래서 다들 참는다. 나도 참았다.\n참는 게 업무인 줄 알았다.",
            "근로계약서엔 없지만 배운 게 하나 있다.\n폭언과 폭행은 서비스가 아니라 범죄다.\n112는 손님보다 먼저 부를 수 있다.",
            "오늘 목표는 친절왕이 아니라 무사 퇴근.\n24:00까지 버틴다.\n내 하루까지 반품해 줄 순 없으니까."
        };

        [Header("Game over")]
        [SerializeField, TextArea(2, 6)] private string[] _death =
        {
            "참는 게 최선이라고 배운 밤이었다.\n오늘은 내 몸이 먼저 접혔다.",
            "진열대가 넘어가고 유리가 깨지는 동안에도\n그 사람은 끝까지 이렇게 말했다.\n\"손님한테 그게 무슨 태도야?\"",
            "맞은 사람이 먼저 사과하는 게 이상하다는 걸\n쓰러지고 나서야 알았다.\n다음엔 참지 말고, 먼저 신고하자."
        };

        [Header("Ending")]
        [SerializeField, TextArea(2, 6)] private string[] _ending =
        {
            "24:00. 교대 시간이다.\n\"야, 손님 아직 있잖아!\"\n계산은 다음 근무자가 합니다. 저는 퇴근이고요.",
            "오늘 누른 112는 가게에 미안한 일이 아니었다.\n손님 응대하는 사람을 보호하는 건\n원래 일 시킨 쪽의 의무라고 법에 적혀 있다.",
            "손님은 왕이 아니다. 그냥 손님이다.\n알바도 을이 아니다. 일하는 사람이다.\n내일도 같은 시간에, 무사히 출근하겠습니다."
        };

        public float ShiftDurationSeconds => Mathf.Max(1f, _shiftDurationSeconds);
        public int StartHour => _startHour;
        public float CharactersPerSecond => Mathf.Max(1f, _charactersPerSecond);
        public float CheckoutDurationSeconds => Mathf.Max(2f, _checkoutDurationSeconds);
        public string[] Opening => _opening;
        public Texture2D OpeningApplication => _openingApplication;
        public string[] Death => _death;
        public string[] Ending => _ending;
        public Sprite ClockIcon
        {
            get
            {
                if (_clockIcon == null && _clockTexture != null)
                    _clockIcon = Sprite.Create(_clockTexture, new Rect(0, 0, _clockTexture.width, _clockTexture.height), Vector2.one * 0.5f);
                return _clockIcon;
            }
        }

        private void OnDisable()
        {
            if (_clockIcon != null) Destroy(_clockIcon);
        }
    }
}
