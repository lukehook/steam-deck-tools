using SteamController.ProfilesSettings;
using HapticPad = SteamController.Devices.SteamController.HapticPad;

namespace SteamController.Profiles.Predefined
{
    public class X360HapticProfile : X360Profile
    {
        private X360HapticSettings HapticSettings
        {
            get { return X360HapticSettings.Default; }
        }

        public override Status Run(Context context)
        {
            if (base.Run(context).IsDone)
                return Status.Done;

            if (X360HapticSettings.GetHapticIntensity(context.X360.FeedbackLargeMotor, HapticSettings.LeftIntensity, out var leftIntensity))
                context.Steam.SendHaptic(HapticPad.Left, HapticSettings.HapticStyle, leftIntensity);

            if (X360HapticSettings.GetHapticIntensity(context.X360.FeedbackSmallMotor, HapticSettings.RightIntensity, out var rightIntensity))
                context.Steam.SendHaptic(HapticPad.Right, HapticSettings.HapticStyle, rightIntensity);

            context.X360.ResetFeedback();

            return Status.Continue;
        }
    }
}
