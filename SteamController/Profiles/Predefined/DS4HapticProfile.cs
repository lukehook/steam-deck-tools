using SteamController.ProfilesSettings;
using HapticPad = SteamController.Devices.SteamController.HapticPad;

namespace SteamController.Profiles.Predefined
{
    public class DS4HapticProfile : DS4Profile
    {
        private DS4HapticSettings HapticSettings
        {
            get { return DS4HapticSettings.Default; }
        }

        public override Status Run(Context context)
        {
            if (base.Run(context).IsDone)
                return Status.Done;

            if (DS4HapticSettings.GetHapticIntensity(context.DS4.FeedbackLargeMotor, HapticSettings.LeftIntensity, out var leftIntensity))
                context.Steam.SendHaptic(HapticPad.Left, HapticSettings.HapticStyle, leftIntensity);

            if (DS4HapticSettings.GetHapticIntensity(context.DS4.FeedbackSmallMotor, HapticSettings.RightIntensity, out var rightIntensity))
                context.Steam.SendHaptic(HapticPad.Right, HapticSettings.HapticStyle, rightIntensity);

            context.DS4.ResetFeedback();

            return Status.Continue;
        }
    }
}
