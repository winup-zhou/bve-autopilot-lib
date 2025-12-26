using BveEx.Extensions.Native;
using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;
using BveTypes.ClassWrappers;
using MetroAts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetroAtsBridge
{
    public partial class MetroAtsBridge : AssemblyPluginBase {

        public override void Tick(TimeSpan elapsed) {
            var state = Native.VehicleState;
            var panel = Native.AtsPanelArray;
            var sound = Native.AtsSoundArray;
            var AtsHandles = BveHacker.Scenario.Vehicle.Instruments.AtsPlugin.AtsHandles;


            if (isAutopilotPluginLoaded) {
                if (is64Bit) {
                    Sync64.SetPower(AtsHandles.PowerNotch);
                    Sync64.SetBrake(AtsHandles.BrakeNotch);
                    Sync64.SetReverser((int)AtsHandles.ReverserPosition);
                } else {
                    Sync.SetPower(AtsHandles.PowerNotch);
                    Sync.SetBrake(AtsHandles.BrakeNotch);
                    Sync.SetReverser((int)AtsHandles.ReverserPosition);
                }
            }

            int pointer = 0;
            while (sectionManager.Sections[pointer].Location < state.Location) {
                pointer++;
                if (pointer >= sectionManager.Sections.Count) {
                    pointer = sectionManager.Sections.Count - 1;
                    break;
                }
            }

            var nextSection = sectionManager.Sections[pointer] as Section;

            if (!StandAloneMode) {
                if (corePlugin.isATO_TASCenabled && corePlugin.KeyPos != MetroAts.KeyPosList.None) {
                    if (Config.ATO_KeyPosLists.Contains((KeyPosList)corePlugin.KeyPos) && panel[263] == 1 && panel[274] == 0) {
                        if (isAutopilotPluginLoaded) {
                            if (is64Bit) {
                                Sync64.setATOTASCStatus(2);
                                Sync64.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            } else {
                                Sync.setATOTASCStatus(2);
                                Sync.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            }                               
                        }
                    } else {
                        if (Config.TASC_KeyPosLists.Contains((KeyPosList)corePlugin.KeyPos)) {
                            if (isAutopilotPluginLoaded) {
                                if (is64Bit) Sync64.setATOTASCStatus(1);
                                else Sync.setATOTASCStatus(1);
                            }
                            
                        } else {
                            if (isAutopilotPluginLoaded) {
                                if (is64Bit) Sync64.setATOTASCStatus(0);
                                else Sync.setATOTASCStatus(0);
                            }
                        }
                    }
                } else {
                    if (isAutopilotPluginLoaded) {
                        if (is64Bit) Sync64.setATOTASCStatus(0);
                        else Sync.setATOTASCStatus(0);
                    }
                }
            } else {
                if (TASCenable && Keyin) {
                    if (Config.ATO_KeyPosLists.Contains(Config.StandAloneKey) && panel[263] == 1 && panel[274] == 0) {
                        if (isAutopilotPluginLoaded) {
                            if (is64Bit) {
                                Sync64.setATOTASCStatus(2);
                                Sync64.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            } else {
                                Sync.setATOTASCStatus(2);
                                Sync.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            }
                        }
                    } else {
                        if (Config.TASC_KeyPosLists.Contains(Config.StandAloneKey)) {
                            if (isAutopilotPluginLoaded) {
                                if (is64Bit) Sync64.setATOTASCStatus(1);
                                else Sync.setATOTASCStatus(1);
                            }
                        } else {
                            if (isAutopilotPluginLoaded) {
                                if (is64Bit) Sync64.setATOTASCStatus(0);
                                else Sync.setATOTASCStatus(0);
                            }
                        }
                    }
                } else {
                    if (isAutopilotPluginLoaded) {
                        if (is64Bit) Sync64.setATOTASCStatus(0);
                        else Sync.setATOTASCStatus(0);
                    }
                }
            }
            int[] panel_ = new int[1024];
            int[] sound_ = new int[1024];

            for (int i = 0; i < 1024; ++i) {
                panel_[i] = panel[i];
                sound_[i] = sound[i];
            }

            GCHandle panelHandle = GCHandle.Alloc(panel_, GCHandleType.Pinned);
            GCHandle soundHandle = GCHandle.Alloc(sound_, GCHandleType.Pinned);
            
            try {
                IntPtr panelPtr = panelHandle.AddrOfPinnedObject();
                IntPtr soundPtr = soundHandle.AddrOfPinnedObject();

                if (isAutopilotPluginLoaded) {
                    var rtnVal = Sync.Elapse(new AtsStruct.AtsVehicleState {
                        Location = state.Location,
                        Speed = state.Speed,
                        Time = Convert.ToInt32(state.Time.TotalMilliseconds),
                        BcPressure = state.BcPressure,
                        MrPressure = state.MrPressure,
                        ErPressure = state.ErPressure,
                        BpPressure = state.BpPressure,
                        SapPressure = state.SapPressure,
                        Current = state.Current
                    },
                    panelPtr, soundPtr);

                    AtsHandles.PowerNotch = rtnVal.Power;
                    AtsHandles.BrakeNotch = rtnVal.Brake;
                    AtsHandles.ReverserPosition = (ReverserPosition)rtnVal.Reverser;
                    AtsHandles.ConstantSpeedMode = (ConstantSpeedMode)rtnVal.ConstantSpeed;

                    for (int i = 0; i < 1024; ++i) {
                        panel[i] = panel_[i];
                        sound[i] = sound_[i];
                    }
                }
                
            } finally {
                // Ensure the handles are freed to avoid memory leaks
                if (panelHandle.IsAllocated) panelHandle.Free();
                if (soundHandle.IsAllocated) soundHandle.Free();
            }
        }
    }
}
