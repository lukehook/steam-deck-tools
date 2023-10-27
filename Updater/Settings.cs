using CommonHelpers;

namespace Updater
{
    internal sealed class Settings : BaseSettings
    {
        public static readonly Settings Default = new Settings();

        public Settings() : base("Settings")
        {
        }

        public int GetRunTimes(string key, long match)
        {
            if (Get<long>(key + "Match", 0) != match)
                return 0;
            return Get(key + "Counter", 0);
        }

        public void SetRunTimes(string key, long match, int value)
        {
            Set(key + "Match", match);
            Set(key + "Counter", value);
        }
    }
}
