using System.ComponentModel;
using System.Configuration;

namespace SteamController.ProfilesSettings
{
    internal class DS4BackPanelSettings : BackPanelSettings
    {
        private const string MappingsDescription = @"Shortcuts are to be changed in future release.";

        public static DS4BackPanelSettings Default { get; } = new DS4BackPanelSettings(nameof(DS4BackPanelSettings));

        public DS4BackPanelSettings(string name) : base(name)
        {
        }

        [Description(MappingsDescription)]
        public VirtualDS4Code L4_DS4
        {
            get { return Get("L4_DS4", VirtualDS4Code.None); }
            set { Set("L4_DS4", value); }
        }

        [Description(MappingsDescription)]
        public VirtualDS4Code L5_DS4
        {
            get { return Get("L5_DS4", VirtualDS4Code.None); }
            set { Set("L5_DS4", value); }
        }

        [Description(MappingsDescription)]
        public VirtualDS4Code R4_DS4
        {
            get { return Get("R4_DS4", VirtualDS4Code.None); }
            set { Set("R4_DS4", value); }
        }

        [Description(MappingsDescription)]
        public VirtualDS4Code R5_DS4
        {
            get { return Get("R5_DS4", VirtualDS4Code.None); }
            set { Set("R5_DS4", value); }
        }
    }
}