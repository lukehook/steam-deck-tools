using System.ComponentModel;
namespace SteamController.ProfilesSettings
{
    [Category("3. Haptic")]
    internal abstract class HapticSettings : CommonHelpers.BaseSettings
    {
        public HapticSettings(string name) : base(name)
        {
        }

        public Devices.SteamController.HapticStyle HapticStyle
        {
            get { return Get("HapticStyle", Devices.SteamController.HapticStyle.Weak); }
            set { Set("HapticStyle", value); }
        }
    }
}
