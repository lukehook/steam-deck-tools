using System.ComponentModel;
using System.Configuration;

namespace SteamController.ProfilesSettings
{
    internal class DesktopBackPanelSettings : BackPanelSettings
    {
        public static DesktopBackPanelSettings Default { get; } = new DesktopBackPanelSettings(nameof(DesktopBackPanelSettings));

        public DesktopBackPanelSettings(string name) : base(name)
        {
        }
    }
}