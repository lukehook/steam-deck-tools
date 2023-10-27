﻿using CommonHelpers;
using LibreHardwareMonitor.Hardware;
using System.ComponentModel;

namespace FanControl
{
    internal partial class FanController
    {
        private Dictionary<string, FanSensor> allSensors = new Dictionary<string, FanSensor>
        {
            {
                "APU", new FanSensor()
                {
                    // TODO: Is this correct?
                    HardwareName = "AMD Custom APU 0405",
                    HardwareType = HardwareType.Cpu,
                    SensorName = "Package",
                    SensorType = SensorType.Power,
                    ValueDeadZone = 0.1f,
                    AvgSamples = 20,
                    MaxValue = 25, // TODO: On resume a bogus value is returned
                    Profiles = new Dictionary<FanMode, FanSensor.Profile>()
                    {
                        {
                            FanMode.Max, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Constant,
                                MinRPM = CommonHelpers.Vlv0100.MAX_FAN_RPM
                            }
                        },
                        {
                            FanMode.SteamOS, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Constant,
                                MinRPM = 1500
                            }
                        },
                        {
                            FanMode.Silent, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Constant,
                                MinRPM = 1500
                            }
                        },
                    }
                }
            },
            {
                "CPU", new FanSensor()
                {
                    HardwareName = "AMD Custom APU 0405",
                    HardwareType = HardwareType.Cpu,
                    SensorName = "Core (Tctl/Tdie)",
                    SensorType = SensorType.Temperature,
                    ValueDeadZone = 0.0f,
                    AvgSamples = 20,
                    Profiles = new Dictionary<FanMode, FanSensor.Profile>()
                    {
                        {
                            FanMode.SteamOS, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Quadratic,
                                MinInput = 55,
                                MaxInput = 90,
                                A = 2.286f,
                                B = -188.6f,
                                C = 5457.0f
                            }
                        },
                        {
                            FanMode.Silent, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Exponential,
                                MinInput = 40,
                                MaxInput = 95,
                                A = 1.28f,
                                B = Settings.Default.Silent4000RPMTemp - 28,
                                C = 3000f
                            }
                        },
                    }
                }
            },
            {
                "GPU", new FanSensor()
                {
                    HardwareName = "AMD Custom GPU 0405",
                    HardwareType = HardwareType.GpuAmd,
                    SensorName = "GPU Core",
                    SensorType = SensorType.Temperature,
                    ValueDeadZone = 0.0f,
                    AvgSamples = 20,
                    Profiles = new Dictionary<FanMode, FanSensor.Profile>()
                    {
                        {
                            FanMode.SteamOS, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Quadratic,
                                MinInput = 55,
                                MaxInput = 90,
                                A = 2.286f,
                                B = -188.6f,
                                C = 5457.0f
                            }
                        },
                        {
                            FanMode.Silent, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Exponential,
                                MinInput = 40,
                                MaxInput = 95,
                                A = 1.28f,
                                B = Settings.Default.Silent4000RPMTemp - 28,
                                C = 3000f
                            }
                        },
                    }
                }
            },
            {
                "SSD", new FanSensor()
                {
                    HardwareType = HardwareType.Storage,
                    SensorName = "Temperature",
                    SensorType = SensorType.Temperature,
                    ValueDeadZone = 0.5f,
                    Profiles = new Dictionary<FanMode, FanSensor.Profile>()
                    {
                        {
                            FanMode.SteamOS, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Pid,
                                MinInput = 30,
                                MaxInput = 70,
                                MaxRPM = 3000,
                                PidSetPoint = 70,
                                Kp = 0,
                                Ki = -20,
                                Kd = 0
                            }
                        },
                        {
                            FanMode.Silent, new FanSensor.Profile()
                            {
                                Type = FanSensor.Profile.ProfileType.Pid,
                                MinInput = 30,
                                MaxInput = 70,
                                MaxRPM = 3000,
                                PidSetPoint = 70,
                                Kp = 0,
                                Ki = -20,
                                Kd = 0
                            }
                        }
                    }
                }
            },
            {
                "Batt", new FanSensor()
                {
                    HardwareType = HardwareType.Battery,
                    SensorName = "Temperature",
                    SensorType = SensorType.Temperature,
                    ValueDeadZone = 0.0f,
                    Profiles = new Dictionary<FanMode, FanSensor.Profile>()
                    {
                        {
                            FanMode.SteamOS, new FanSensor.Profile()
                            {
                                // If battery goes over 40oC require 2kRPM
                                Type = FanSensor.Profile.ProfileType.Constant,
                                MinInput = 0,
                                MaxInput = 40,
                                MinRPM = 0,
                                MaxRPM = 2000,
                            }
                        },
                        {
                            FanMode.Silent, new FanSensor.Profile()
                            {
                                // If battery goes over 40oC require 2kRPM
                                Type = FanSensor.Profile.ProfileType.Constant,
                                MinInput = 0,
                                MaxInput = 40,
                                MinRPM = 0,
                                MaxRPM = 2000,
                            }
                        }
                    }
                }
            }
        };

        #region Sensor Properties for Property Grid
        [Category("Sensor - APU"), DisplayName("Name")]
        public string? APUName { get { return allSensors["APU"].Name; } }
        [Category("Sensor - APU"), DisplayName("Power")]
        public string? APUPower { get { return allSensors["APU"].FormattedValue(); } }

        [Category("Sensor - CPU"), DisplayName("Name")]
        public string? CPUName { get { return allSensors["CPU"].Name; } }
        [Category("Sensor - CPU"), DisplayName("Temperature")]
        public string? CPUTemperature { get { return allSensors["CPU"].FormattedValue(); } }

        [Category("Sensor - GPU"), DisplayName("Name")]
        public string? GPUName { get { return allSensors["GPU"].Name; } }
        [Category("Sensor - GPU"), DisplayName("Temperature")]
        public string? GPUTemperature { get { return allSensors["GPU"].FormattedValue(); } }

        [Category("Sensor - SSD"), DisplayName("Name")]
        public string? SSDName { get { return allSensors["SSD"].Name; } }
        [Category("Sensor - SSD"), DisplayName("Temperature")]
        public string? SSDTemperature { get { return allSensors["SSD"].FormattedValue(); } }
        [Category("Sensor - Battery"), DisplayName("Name")]
        public string? BatteryName { get { return allSensors["Batt"].Name; } }
        [Category("Sensor - Battery"), DisplayName("Temperature")]
        public string? BatteryTemperature { get { return allSensors["Batt"].FormattedValue(); } }

        #endregion Sensor Properties for Property Grid
    }
}
