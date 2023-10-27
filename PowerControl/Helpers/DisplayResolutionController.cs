using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CommonHelpers;
using static PowerControl.Helpers.PhysicalMonitorBrightnessController;

namespace PowerControl.Helpers
{
    internal class DisplayResolutionController
    {
        public struct DisplayResolution : IComparable<DisplayResolution>
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public DisplayResolution() { Width = 0; Height = 0; }

            public DisplayResolution(int width, int height) { Width = width; Height = height; }

            public DisplayResolution(string text)
            {
                var options = text.Split("x", 2);
                Width = int.Parse(options[0]);
                Height = int.Parse(options[1]);
            }

            public static bool operator ==(DisplayResolution sz1, DisplayResolution sz2) => sz1.Width == sz2.Width && sz1.Height == sz2.Height;

            public static bool operator !=(DisplayResolution sz1, DisplayResolution sz2) => !(sz1 == sz2);

            public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is DisplayResolution && Equals((DisplayResolution)obj);
            public readonly bool Equals(DisplayResolution other) => this == other;

            public override readonly int GetHashCode() => HashCode.Combine(Width, Height);

            public int CompareTo(DisplayResolution other)
            {
                var index = Width.CompareTo(other.Width);
                if (index == 0) index = Height.CompareTo(other.Height);
                return index;
            }

            public override string ToString()
            {
                return string.Format("{0}x{1}", Width, Height);
            }
        }

        private static IEnumerable<DEVMODE> FindAllDisplaySettings()
        {
            DEVMODE dm = new DEVMODE();
            for (int i = 0; EnumDisplaySettings(null, i, ref dm); i++)
            {
                if (dm.dmFields.HasFlag(DM.PelsWidth) && dm.dmFields.HasFlag(DM.PelsHeight) && dm.dmFields.HasFlag(DM.PelsHeight))
                    yield return dm;

                dm = new DEVMODE();
            }
        }

        internal static DEVMODE? CurrentDisplaySettings()
        {
            DEVMODE dm = new DEVMODE();
            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
                return dm;
            return null;
        }

        private static bool SetDisplaySettings(string type, DEVMODE? best)
        {
            if (best == null)
                return false;

            DEVMODE oldDm = new DEVMODE();
            if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref oldDm))
                return false;

            Log.TraceObject("SetDisplaySettings:" + type + ":IN", oldDm);

            DEVMODE dm = best.Value;

            if (dm.dmPelsWidth == oldDm.dmPelsWidth &&
                dm.dmPelsHeight == oldDm.dmPelsHeight &&
                dm.dmDisplayFrequency == oldDm.dmDisplayFrequency)
            {
                Log.TraceLine(
                    "DispChange: {0}, already set: {1}x{2}@{3}",
                    type,
                    oldDm.dmPelsWidth, oldDm.dmPelsHeight, oldDm.dmDisplayFrequency);
                return true;
            }

            var testChange = ChangeDisplaySettingsEx(
                null, ref dm, IntPtr.Zero,
                ChangeDisplaySettingsFlags.CDS_TEST, IntPtr.Zero);
            var applyChange = DISP_CHANGE.NotUpdated;
            Log.TraceObject("SetDisplaySettings:" + type + ":REQ", dm);

            if (testChange == DISP_CHANGE.Successful)
            {
                applyChange = ChangeDisplaySettingsEx(
                    null, ref dm, IntPtr.Zero,
                    ChangeDisplaySettingsFlags.CDS_RESET, IntPtr.Zero);
            }

            DEVMODE newDm = new DEVMODE();
            if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref newDm))
                return false;

            Log.TraceObject("SetDisplaySettings:" + type + ":OUT", newDm);

            Log.TraceLine(
                "DispChange: {0}, Test: {1}, Set: {8}, from: {2}x{3}@{4}, to: {5}x{6}@{7}",
                type, testChange,
                oldDm.dmPelsWidth, oldDm.dmPelsHeight, oldDm.dmDisplayFrequency,
                newDm.dmPelsWidth, newDm.dmPelsHeight, newDm.dmDisplayFrequency,
                applyChange
            );

            return applyChange == DISP_CHANGE.Successful;
        }

        public static bool SetDisplaySettings(DisplayResolution size, int hz, string type = "DisplaySettings")
        {
            DEVMODE? best = FindAllDisplaySettings()
                .Where((dm) => dm.dmPelsWidth == size.Width && dm.dmPelsHeight == size.Height)
                .Where((dm) => dm.dmDisplayFrequency == hz)
                .First();

            if (best is null)
                return false;

            return SetDisplaySettings(type, best);
        }

        public static bool ResetCurrentResolution()
        {
            try
            {
                var dm = CurrentDisplaySettings();

                // Reset to best default
                var bestResolution = GetAllResolutions().Last();
                var bestRefreshRate = GetRefreshRates(bestResolution).Max();
                SetDisplaySettings(bestResolution, bestRefreshRate, "ResetToDefault");

                return SetDisplaySettings("Reset", dm);
            }
            catch (Exception e)
            {
                Log.TraceException("ResetResolution", e);
                return false;
            }
        }

        public static DisplayResolution[] GetAllResolutions()
        {
            return FindAllDisplaySettings()
                .Select((dm) => new DisplayResolution(dm.dmPelsWidth, dm.dmPelsHeight))
                .ToImmutableSortedSet()
                .ToArray();
        }

        public static DisplayResolution? GetResolution()
        {
            var dm = CurrentDisplaySettings();
            if (dm is not null)
                return new DisplayResolution(dm.Value.dmPelsWidth, dm.Value.dmPelsHeight);

            return null;
        }

        public static bool SetResolution(DisplayResolution size)
        {
            DEVMODE? best = FindAllDisplaySettings()
                .Where((dm) => dm.dmPelsWidth == size.Width && dm.dmPelsHeight == size.Height)
                .MaxBy((dm) => dm.dmDisplayFrequency);

            if (best == null)
                return false;

            return SetDisplaySettings("Resolution", best);
        }

        public static int[] GetRefreshRates(DisplayResolution? size = null)
        {
            if (size is null)
                size = GetResolution();
            if (size is null)
                return new int[0];

            return FindAllDisplaySettings()
                .Where((dm) => dm.dmPelsWidth == size?.Width && dm.dmPelsHeight == size?.Height)
                .Select((dm) => dm.dmDisplayFrequency)
                .ToHashSet()
                .ToArray();
        }

        public static int GetRefreshRate()
        {
            var dm = CurrentDisplaySettings();

            if (dm is not null)
                return dm.Value.dmDisplayFrequency;

            return -1;
        }

        public static bool SetRefreshRate(int hz)
        {
            var current = GetResolution();
            if (current is null)
                return false;

            return SetDisplaySettings(current.Value, hz, "SetRefreshRate");
        }


        enum DISP_CHANGE : int
        {
            Successful = 0,
            Restart = 1,
            Failed = -1,
            BadMode = -2,
            NotUpdated = -3,
            BadFlags = -4,
            BadParam = -5,
            BadDualView = -6
        }

        [Flags()]
        internal enum DM : int
        {
            Orientation = 0x1,
            PaperSize = 0x2,
            PaperLength = 0x4,
            PaperWidth = 0x8,
            Scale = 0x10,
            Position = 0x20,
            NUP = 0x40,
            DisplayOrientation = 0x80,
            Copies = 0x100,
            DefaultSource = 0x200,
            PrintQuality = 0x400,
            Color = 0x800,
            Duplex = 0x1000,
            YResolution = 0x2000,
            TTOption = 0x4000,
            Collate = 0x8000,
            FormName = 0x10000,
            LogPixels = 0x20000,
            BitsPerPixel = 0x40000,
            PelsWidth = 0x80000,
            PelsHeight = 0x100000,
            DisplayFlags = 0x200000,
            DisplayFrequency = 0x400000,
            ICMMethod = 0x800000,
            ICMIntent = 0x1000000,
            MeduaType = 0x2000000,
            DitherType = 0x4000000,
            PanningWidth = 0x8000000,
            PanningHeight = 0x10000000,
            DisplayFixedOutput = 0x20000000
        }

        internal struct POINTL
        {
            public int x;
            public int y;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        internal struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            [FieldOffset(0)]
            public string dmDeviceName;
            [FieldOffset(32)]
            public short dmSpecVersion;
            [FieldOffset(34)]
            public short dmDriverVersion;
            [FieldOffset(36)]
            public short dmSize;
            [FieldOffset(38)]
            public short dmDriverExtra;
            [FieldOffset(40)]
            public DM dmFields;

            [FieldOffset(44)]
            short dmOrientation;
            [FieldOffset(46)]
            short dmPaperSize;
            [FieldOffset(48)]
            short dmPaperLength;
            [FieldOffset(50)]
            short dmPaperWidth;
            [FieldOffset(52)]
            short dmScale;
            [FieldOffset(54)]
            short dmCopies;
            [FieldOffset(56)]
            short dmDefaultSource;
            [FieldOffset(58)]
            short dmPrintQuality;

            [FieldOffset(44)]
            public POINTL dmPosition;
            [FieldOffset(52)]
            public int dmDisplayOrientation;
            [FieldOffset(56)]
            public int dmDisplayFixedOutput;

            [FieldOffset(60)]
            public short dmColor; // See note below!
            [FieldOffset(62)]
            public short dmDuplex; // See note below!
            [FieldOffset(64)]
            public short dmYResolution;
            [FieldOffset(66)]
            public short dmTTOption;
            [FieldOffset(68)]
            public short dmCollate; // See note below!
            //[System.Runtime.InteropServices.FieldOffset(70)]
            //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            //public string dmFormName;
            [FieldOffset(102)]
            public short dmLogPixels;
            [FieldOffset(104)]
            public int dmBitsPerPel;
            [FieldOffset(108)]
            public int dmPelsWidth;
            [FieldOffset(112)]
            public int dmPelsHeight;
            [FieldOffset(116)]
            public int dmDisplayFlags;
            [FieldOffset(116)]
            public int dmNup;
            [FieldOffset(120)]
            public int dmDisplayFrequency;
        }

        [Flags()]
        public enum ChangeDisplaySettingsFlags : uint
        {
            CDS_NONE = 0,
            CDS_UPDATEREGISTRY = 0x00000001,
            CDS_TEST = 0x00000002,
            CDS_FULLSCREEN = 0x00000004,
            CDS_GLOBAL = 0x00000008,
            CDS_SET_PRIMARY = 0x00000010,
            CDS_VIDEOPARAMETERS = 0x00000020,
            CDS_ENABLE_UNSAFE_MODES = 0x00000100,
            CDS_DISABLE_UNSAFE_MODES = 0x00000200,
            CDS_RESET = 0x40000000,
            CDS_RESET_EX = 0x20000000,
            CDS_NORESET = 0x10000000
        }

        const int ENUM_CURRENT_SETTINGS = -1;
        const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        static extern DISP_CHANGE ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);
    }
}
