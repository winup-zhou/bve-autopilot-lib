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

namespace MetroAtsBridge
{
    public partial class MetroAtsBridge : AssemblyPluginBase {

        private void Initialize(object sender, StartedEventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.Initialize((int)e.DefaultBrakePosition);
                else Sync.Initialize((int)e.DefaultBrakePosition);
            } 
        }

        private void DoorOpened(object sender, EventArgs e) {
            if (isAutopilotPluginLoaded) { 
                if (is64Bit) Sync64.DoorOpen();
                else Sync.DoorOpen();
            } 
        }

        private void DoorClosed(object sender, EventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.DoorClose();
                else Sync.DoorClose();
            }
        }

        private void KeyUp(object sender, AtsKeyEventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.KeyUp((int)e.KeyName);
                else Sync.KeyUp((int)e.KeyName);
            } 
        }

        private void KeyDown(object sender, AtsKeyEventArgs e) {
            // 钥匙/档位/ATO-TASC 切换均由 MetroAts 核心处理（不再支持独立模式的 I/J 本地控制）
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.KeyDown((int)e.KeyName);
                else Sync.KeyDown((int)e.KeyName);
            }
        }

        private void SetBeaconData(object sender, BeaconPassedEventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.SetBeaconData(new AtsStruct.AtsBeaconData {
                    Type = e.Type,
                    Signal = e.SignalIndex,
                    Distance = e.Distance,
                    Optional = e.Optional,
                });
                else Sync.SetBeaconData(new AtsStruct.AtsBeaconData {
                    Type = e.Type,
                    Signal = e.SignalIndex,
                    Distance = e.Distance,
                    Optional = e.Optional,
                });
            }
        }

        private void SetVehicleSpec(object sender, EventArgs e) {
            vehicleSpec = Native.VehicleSpec;
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.SetVehicleSpec(new AtsStruct.AtsVehicleSpec {
                    BrakeNotches = vehicleSpec.BrakeNotches,
                    PowerNotches = vehicleSpec.PowerNotches,
                    AtsNotch = vehicleSpec.AtsNotch,
                    B67Notch = vehicleSpec.B67Notch,
                    Cars = vehicleSpec.Cars
                });
                else Sync.SetVehicleSpec(new AtsStruct.AtsVehicleSpec {
                    BrakeNotches = vehicleSpec.BrakeNotches,
                    PowerNotches = vehicleSpec.PowerNotches,
                    AtsNotch = vehicleSpec.AtsNotch,
                    B67Notch = vehicleSpec.B67Notch,
                    Cars = vehicleSpec.Cars
                });
            }
        }

        private void HornBlow(object sender, HornBlownEventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.HornBlow((int)e.HornType);
                else Sync.HornBlow((int)e.HornType);
            }
        }

        private void SetSignal(object sender, SignalUpdatedEventArgs e) {
            if (isAutopilotPluginLoaded) {
                if (is64Bit) Sync64.SetSignal(e.SignalIndex);
                else Sync.SetSignal(e.SignalIndex);
            }
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e) {
            sectionManager = e.Scenario.SectionManager;
        }
    }
}
