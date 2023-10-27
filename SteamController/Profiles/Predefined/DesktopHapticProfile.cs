using SteamController.ProfilesSettings;
using static SteamController.Devices.SteamController;
using HapticPad = SteamController.Devices.SteamController.HapticPad;

namespace SteamController.Profiles.Predefined
{
    public class DesktopHapticProfile : DesktopProfile
    {

        private HapticSettings HapticSettings
        {
            get { return DesktopHapticSettings.Default; }
        }

        public override Status Run(Context context)
        {
            if (base.Run(context).IsDone)
                return Status.Done;

            if (HapticSettings.HapticStyle == HapticStyle.Disabled)
                context.Steam.SendHaptic(HapticPad.Left, HapticSettings.HapticStyle, default);
                context.Steam.SendHaptic(HapticPad.Right, HapticSettings.HapticStyle, default);

            return Status.Continue;
        }
    }
}
