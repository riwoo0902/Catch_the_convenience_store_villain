using UnityEngine;

namespace CWH.Player.UI
{
    public sealed class PlayerHUDSettings : ScriptableObject
    {
        [SerializeField] private Sprite _phoneSprite;
        [SerializeField] private Sprite _phoneAppIcon;
        [SerializeField] private Sprite _emergencyCallIcon;
        [SerializeField] private Sprite _youtubeLogo;
        [SerializeField] private Sprite _mailIcon;

        public Sprite PhoneSprite => _phoneSprite;
        public Sprite PhoneAppIcon => _phoneAppIcon;
        public Sprite EmergencyCallIcon => _emergencyCallIcon;
        public Sprite YoutubeLogo => _youtubeLogo;
        public Sprite MailIcon => _mailIcon;
    }
}
