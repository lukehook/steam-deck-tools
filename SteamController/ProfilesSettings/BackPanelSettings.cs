using System.ComponentModel;
using System.Configuration;

namespace SteamController.ProfilesSettings
{
    [Category("2. Shortcuts")]
    internal abstract class BackPanelSettings : CommonHelpers.BaseSettings
    {
        private const string MappingsDescription = @"Only some of those keys do work. Allowed shortcuts are to be changed in future release.";

        public BackPanelSettings(string settingsKey) : base(settingsKey)
        {
        }

        [Description(MappingsDescription)]
        public VirtualKeyCode L4_KEY
        {
            get { return Get("L4_KEY", VirtualKeyCode.None); }
            set { Set("L4_KEY", value); }
        }

        [Description(MappingsDescription)]
        public VirtualKeyCode L5_KEY
        {
            get { return Get("L5_KEY", VirtualKeyCode.None); }
            set { Set("L5_KEY", value); }
        }

        [Description(MappingsDescription)]
        public VirtualKeyCode R4_KEY
        {
            get { return Get("R4_KEY", VirtualKeyCode.None); }
            set { Set("R4_KEY", value); }
        }

        [Description(MappingsDescription)]
        public VirtualKeyCode R5_KEY
        {
            get { return Get("R5_KEY", VirtualKeyCode.None); }
            set { Set("R5_KEY", value); }
        }
    }
}
