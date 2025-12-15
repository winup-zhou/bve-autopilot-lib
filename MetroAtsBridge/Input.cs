using BveEx.Extensions.Native.Input;
using BveEx.Extensions.Native;
using BveEx.PluginHost.Input;
using BveEx.PluginHost.Plugins;
using BveEx.PluginHost;
using BveTypes.ClassWrappers;
using MetroAts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MetroAtsBridge
{
    public partial class MetroAtsBridge : AssemblyPluginBase {

        private void Initialize(object sender, StartedEventArgs e) {
            if (isAutopilotPluginLoaded) Sync.Initialize((int)e.DefaultBrakePosition);
        }

        private void DoorOpened(object sender, EventArgs e) {
            if (isAutopilotPluginLoaded) Sync.DoorOpen();
        }

        private void DoorClosed(object sender, EventArgs e) {
            if (isAutopilotPluginLoaded) Sync.DoorClose();
        }

        private void OnKeyUp(object sender, KeyEventArgs e) {

        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            var handles = BveHacker.Scenario.Vehicle.Instruments.AtsPlugin.Handles;
            if (StandAloneMode && e.KeyCode == Config.StandAloneTASCKey && handles.BrakeNotch >= vehicleSpec.BrakeNotches && Keyin) {
                TASCenable = !TASCenable;
            }
        }

        private void KeyUp(object sender, AtsKeyEventArgs e) {
            if (isAutopilotPluginLoaded) Sync.KeyUp((int)e.KeyName);
        }

        private void KeyDown(object sender, AtsKeyEventArgs e) {
            var handles = BveHacker.Scenario.Vehicle.Instruments.AtsPlugin.Handles;
            if (handles.BrakeNotch == vehicleSpec.BrakeNotches + 1) {
                if (StandAloneMode && e.KeyName == AtsKeyName.I && handles.ReverserPosition == ReverserPosition.N) {
                    Keyin = false;
                } else if (StandAloneMode && e.KeyName == AtsKeyName.J && handles.ReverserPosition == ReverserPosition.N) {
                    Keyin = true;
                }
            }
            
            if (isAutopilotPluginLoaded) Sync.KeyDown((int)e.KeyName);
        }

        private void SetBeaconData(object sender, BeaconPassedEventArgs e) {
            if (isAutopilotPluginLoaded) Sync.SetBeaconData(new Sync.AtsBeaconData {
                Type = e.Type,
                Signal = e.SignalIndex,
                Distance = e.Distance,
                Optional = e.Optional,
            });
        }

        private void SetVehicleSpec(object sender, EventArgs e) {
            vehicleSpec = Native.VehicleSpec;
            if (isAutopilotPluginLoaded) Sync.SetVehicleSpec(new Sync.AtsVehicleSpec {
                BrakeNotches = vehicleSpec.BrakeNotches,
                PowerNotches = vehicleSpec.PowerNotches,
                AtsNotch = vehicleSpec.AtsNotch,
                B67Notch = vehicleSpec.B67Notch,
                Cars = vehicleSpec.Cars
            });
        }

        private void HornBlow(object sender, HornBlownEventArgs e) {
            if (isAutopilotPluginLoaded) Sync.HornBlow((int)e.HornType);
        }

        private void SetSignal(object sender, SignalUpdatedEventArgs e) {
            if (isAutopilotPluginLoaded) Sync.SetSignal(e.SignalIndex);
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e) {
            sectionManager = e.Scenario.SectionManager;
        }
    }
}
