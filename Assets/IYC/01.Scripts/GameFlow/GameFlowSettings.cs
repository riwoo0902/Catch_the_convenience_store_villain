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
        private Sprite _clockIcon;

        [Header("Opening — each entry is one click")]
        [SerializeField, TextArea(2, 6)] private string[] _opening =
        {
            "야간 편의점 알바 구함.\n업무: 계산, 상품 정리, 그리고 생존.\n마지막 항목은 면접 때 못 들었다.",
            "봉투값 100원에 인류애가 무너지고,\n젓가락 하나에 진열대가 뒤집힌다.\n손님은 왕이라더니 여기는 폭군만 온다.",
            "벽돌을 던져도 서비스. 물건을 엎어도 서비스.\n죄송합니다를 무한 리필로 처먹는 진상들.\n시급에는 샌드백 이용료가 없는데?",
            "사장님: 진상 오면 112 눌러. 경찰 5초면 와.\n나: 그 5초 동안은요?\n사장님: 달려.",
            "근무는 20:00부터 24:00까지.\n현실 시간 10분만 살아남으면 바로 퇴근이다.\nTAB으로 휴대폰을 열고 시계 앱에서 시간을 확인하자.",
            "엎어진 물건을 바라보고 E로 정리하자.\n어질러진 물건이 남아 있으면 체력이 줄어든다.\n휴대폰으로 유튜브를 보면 체력이 회복된다.",
            "빌런이 오면 달리면서 전화 앱을 켜자.\n1 → 1 → 2 → 통화. 누른 순간부터 5초를 버티자.\n아무도 없는데 신고하면? 경찰 방문 + 체력 20 차감.",
            "목표: 친절왕 말고 생존왕.\n진상에게 오늘의 내 인생까지 반품해 줄 순 없다.\n24:00. 그때는 누가 깽판을 치든 퇴근이다."
        };

        [Header("Game over")]
        [SerializeField, TextArea(2, 6)] private string[] _death =
        {
            "영수증도 없이 상식을 환불하러 온 인간들.\n오늘은 내 체력이 먼저 품절됐다.",
            "진열대는 넘어지고, 벽돌은 날아오고,\n진상은 끝까지 말했다.\n\"손님한테 그게 무슨 태도야?\"",
            "폭언에 폭행까지 해 놓고 서비스가 별로란다.\n별점 1점. 인간성은 0점.\n다음 근무에선 반드시 살아서 퇴근하자."
        };

        [Header("Ending")]
        [SerializeField, TextArea(2, 6)] private string[] _ending =
        {
            "24:00. 퇴근.\n\"야! 손님 아직 있잖아!\"\n그래서요. 저는 이제 없는데요.",
            "진열대 뒤집기, 벽돌 던지기, 고성방가.\n오늘의 진상 종합선물세트는 여기까지.\n저런 짓은 고객의 권리가 아니라 민폐다.",
            "손님은 왕?\n남의 일터에서 폭언하고 폭행하는 왕은 필요 없다.\n알바도 사람이다. 그리고 지금은 퇴근한 사람이다."
        };

        public float ShiftDurationSeconds => Mathf.Max(1f, _shiftDurationSeconds);
        public int StartHour => _startHour;
        public float CharactersPerSecond => Mathf.Max(1f, _charactersPerSecond);
        public float CheckoutDurationSeconds => Mathf.Max(2f, _checkoutDurationSeconds);
        public string[] Opening => _opening;
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
