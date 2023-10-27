using System.ComponentModel;
using System.Configuration;

namespace SteamController.ProfilesSettings
{
    internal class X360BackPanelSettings : BackPanelSettings
    {
        private const string MappingsDescription = @"Shortcuts are to be changed in future release.";

        public static X360BackPanelSettings Default { get; } = new X360BackPanelSettings(nameof(X360BackPanelSettings));

        public X360BackPanelSettings(string name) : base(name)
        {
        }

        [Description(MappingsDescription)]
        public VirtualX360Code L4_X360
        {
            get { return Get("L4_X360", VirtualX360Code.None); }
            set { Set("L4_X360", value); }
        }

        [Description(MappingsDescription)]
        public VirtualX360Code L5_X360
        {
            get { return Get("L5_X360", VirtualX360Code.None); }
            set { Set("L5_X360", value); }
        }

        [Description(MappingsDescription)]
        public VirtualX360Code R4_X360
        {
            get { return Get("R4_X360", VirtualX360Code.None); }
            set { Set("R4_X360", value); }
        }

        [Description(MappingsDescription)]
        public VirtualX360Code R5_X360
        {
            get { return Get("R5_X360", VirtualX360Code.None); }
            set { Set("R5_X360", value); }
        }
    }
}