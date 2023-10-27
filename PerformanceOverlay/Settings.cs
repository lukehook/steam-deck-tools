using CommonHelpers;

namespace PerformanceOverlay
{
    internal sealed class Settings : BaseSettings
    {
        public static readonly Settings Default = new Settings();

        public Settings() : base("Settings")
        {
            TouchSettings = true;
        }

        public OverlayMode OSDMode
        {
            get { return Get("OSDMode", OverlayMode.FPS); }
            set { Set("OSDMode", value); }
        }

        public string ShowOSDShortcut
        {
            get { return Get("ShowOSDShortcut", "Shift+F11"); }
            set { Set("ShowOSDShortcut", value); }
        }

        public string CycleOSDShortcut
        {
            get { return Get("CycleOSDShortcut", "Alt+Shift+F11"); }
            set { Set("CycleOSDShortcut", value); }
        }

        public bool ShowOSD
        {
            get { return Get("ShowOSD", true); }
            set { Set("ShowOSD", value); }
        }

        public bool EnableFullOnPowerControl
        {
            get { return Get("EnableFullOnPowerControl", false); }
            set { Set("EnableFullOnPowerControl", value); }
        }

        public bool EnableKernelDrivers
        {
            get { return Get("EnableKernelDrivers", false); }
            set { Set("EnableKernelDrivers", value); }
        }

        public bool EnableExperimentalFeatures
        {
            get { return Instance.IsDEBUG; }
        }
    }
}
