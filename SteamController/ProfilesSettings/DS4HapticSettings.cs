using System.ComponentModel;

namespace SteamController.ProfilesSettings
{
    internal class DS4HapticSettings : HapticSettings
    {
        public const sbyte MinIntensity = -2;
        public const sbyte MaxIntensity = 10;

        public static DS4HapticSettings Default { get; } = new DS4HapticSettings(nameof(DS4HapticSettings));

        public DS4HapticSettings(string name) : base(name)
        {
        }

        public static bool GetHapticIntensity(byte? input, sbyte maxIntensity, out sbyte output)
        {
            output = default;
            if (input is null || input.Value == 0)
                return false;

            int value = MinIntensity + (maxIntensity - MinIntensity) * input.Value / 255;
            output = (sbyte)value;
            return true;
        }

        [Description("Haptic intensity between -2dB and 10dB")]
        public sbyte LeftIntensity
        {
            get { return Get<sbyte>("LeftIntensity", 2); }
            set { Set("LeftIntensity", Math.Clamp(value, MinIntensity, MaxIntensity)); }
        }

        [Description("Haptic intensity between -2dB and 10dB")]
        public sbyte RightIntensity
        {
            get { return Get<sbyte>("RightIntensity", 2); }
            set { Set("RightIntensity", Math.Clamp(value, MinIntensity, MaxIntensity)); }
        }
    }
}
