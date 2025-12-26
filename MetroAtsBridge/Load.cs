using BveEx.Extensions.Native;
using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using BveTypes.ClassWrappers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorePlugin = MetroAts.MetroAts;

namespace MetroAtsBridge
{
    public enum KeyPosList {
        Tokyu = 0,
        None = 1,
        Metro = 2,
        Tobu = 3,
        Seibu = 4,
        Sotetsu = 5,
        JR = 6,
        ToyoKosoku = 7,
        Odakyu = 8
    }


    [Plugin(PluginType.VehiclePlugin)]
    public partial class MetroAtsBridge : AssemblyPluginBase {
        private readonly INative Native;
        private static VehicleSpec vehicleSpec;
        private static bool isSpacePressed = false;
        private static bool StandAloneMode = false;
        private static bool Keyin = false;
        private static bool TASCenable = false;
        private static CorePlugin corePlugin;
        private static bool isAutopilotPluginLoaded = false;
        public static SectionManager sectionManager;
        bool is64Bit = false;

        public MetroAtsBridge(PluginBuilder services) : base(services) {
            is64Bit = Environment.Is64BitProcess;
            Config.Load();

            Native = Extensions.GetExtension<INative>();
            Native.Started += Initialize;
            Native.DoorClosed += DoorClosed;
            Native.DoorOpened += DoorOpened;
            Native.AtsKeys.AnyKeyPressed += KeyDown;
            Native.AtsKeys.AnyKeyReleased += KeyUp;
            Native.VehicleSpecLoaded += SetVehicleSpec;
            Native.BeaconPassed += SetBeaconData;
            Native.HornBlown += HornBlow;
            Native.SignalUpdated += SetSignal;
            Plugins.AllPluginsLoaded += OnAllPluginsLoaded;

            BveHacker.MainFormSource.KeyDown += OnKeyDown;
            BveHacker.MainFormSource.KeyUp += OnKeyUp;
            BveHacker.ScenarioCreated += OnScenarioCreated;

            if (is64Bit) {
                try {
                    Sync64.Load();
                    isAutopilotPluginLoaded = true;
                } catch (DllNotFoundException e) {
                    throw new BveFileLoadException("Unable to find bve-autopilot-lib64.dll. Details: " + e.Message, "MetroAtsBridge");
                } catch (Exception e) {
                    throw new BveFileLoadException(e.ToString(), "MetroAtsBridge");
                }
            } else {
                try {
                    Sync.Load();
                    isAutopilotPluginLoaded = true;
                } catch (DllNotFoundException e) {
                    throw new BveFileLoadException("Unable to find bve-autopilot-lib.dll. Details: " + e.Message, "MetroAtsBridge");
                } catch (Exception e) {
                    throw new BveFileLoadException(e.ToString(), "MetroAtsBridge");
                }
            }

        }

        private void OnAllPluginsLoaded(object sender, EventArgs e) {
            try {
                corePlugin = Plugins.VehiclePlugins["MetroAtsCore"] as CorePlugin;
                StandAloneMode = false;
            } catch (Exception ex) {
                StandAloneMode = true;
            }
        }

        public override void Dispose() {
            Config.Dispose();

            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.Dispose();
                else Sync.Dispose();
            }
            isAutopilotPluginLoaded = false; 
            
            TASCenable = false;

            Native.Started -= Initialize;
            Native.DoorClosed -= DoorClosed;
            Native.DoorOpened -= DoorOpened;

            Native.VehicleSpecLoaded -= SetVehicleSpec;
            Native.BeaconPassed -= SetBeaconData;
            Native.HornBlown -= HornBlow;
            Native.SignalUpdated -= SetSignal;
            Plugins.AllPluginsLoaded -= OnAllPluginsLoaded;
            BveHacker.ScenarioCreated -= OnScenarioCreated;

            BveHacker.MainFormSource.KeyDown -= OnKeyDown;
            BveHacker.MainFormSource.KeyUp -= OnKeyUp;
        }
    }
}
