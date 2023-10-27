using System.ComponentModel;
namespace SteamController.ProfilesSettings
{
    internal class DesktopHapticSettings : HapticSettings
    {
        public static DesktopHapticSettings Default = new DesktopHapticSettings(nameof(DesktopHapticSettings));

        public DesktopHapticSettings(string name) : base(name)
        {
        }
    }
}
