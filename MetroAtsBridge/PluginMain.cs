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

namespace MetroAtsBridge {
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

            // 核心必需：ATO/TASC 状态完全由 MetroAts 核心 + ATC 子插件决定（不再支持独立模式）
            if (corePlugin.isATO_TASCenabled && corePlugin.KeyPos != MetroAts.KeyPosList.None) {
                // ATO 可用性由 ATC 插件（MetroSignal/TokyuSignal）经核心 IsATOAvailable 上报，
                // 替代此前读取 ATC 面板灯端子(panel[263]/[274])的耦合方式。
                if (Config.ATO_KeyPosLists.Contains((KeyPosList)corePlugin.KeyPos) && corePlugin.IsATOAvailable) {
                    if (isAutopilotPluginLoaded) {
                        if (is64Bit) {
                            Sync64.setATOTASCStatus(2);
                            Sync64.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            ApplyAtoBlockTargets(pointer, nextSection);
                        } else {
                            Sync.setATOTASCStatus(2);
                            Sync.ATO_setATCLimit(nextSection.Location - state.Location, nextSection.CurrentSignalIndex);
                            ApplyAtoBlockTargets(pointer, nextSection);
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
                    var rtnVal = is64Bit ?
                    Sync64.Elapse(new AtsStruct.AtsVehicleState {
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
                    panelPtr, soundPtr) :
                    Sync.Elapse(new AtsStruct.AtsVehicleState {
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

                    // 落地 autopilot（C++）逻辑输出：内部通道取逻辑值 → 按映射写 BVE 物理数组（受开关/重映射），
                    // 同时记录平行状态供其它插件经核心读取。C++ 不直写 BVE，落地统一在此。
                    UpdateOutputStates(panel, sound);
                }

            } finally {
                // Ensure the handles are freed to avoid memory leaks
                if (panelHandle.IsAllocated) panelHandle.Free();
                if (soundHandle.IsAllocated) soundHandle.Free();
            }
        }

        /// <summary>
        /// 取 autopilot（C++）本帧全部面板/声音逻辑输出（内部通道），
        /// 经 PanelMap/SoundMap 按端子映射写入 BVE 物理数组（WritePanel/WriteSound：
        /// 受 writepanel/writesound 开关与 [output] 重映射控制，越界/未注册端子仅记录状态），
        /// 状态即 Config.PanelMap.State / SoundMap.State（平行暴露）。
        /// </summary>
        private void ApplyAtoBlockTargets(int pointer, Section nextSection) {
            // ATO 多模式目标速度：把当前闭塞与次闭塞按模式（0遅速/1平常/2回復）的目标速度下发 C++（index 级）
            int mode = corePlugin.ATORunningModeValue;

            int curIdx = -1;
            if (pointer > 0) {
                var cur = sectionManager.Sections[pointer - 1] as Section;
                if (cur != null) curIdx = cur.CurrentSignalIndex;
            }
            int nextIdx = nextSection.CurrentSignalIndex;

            if (curIdx >= 0) {
                int v = Config.AtoModeTargetSpeed(curIdx, mode);
                if (is64Bit) Sync64.ATO_setBlockTargetSpeed(curIdx, v);
                else Sync.ATO_setBlockTargetSpeed(curIdx, v);
            }
            int nv = Config.AtoModeTargetSpeed(nextIdx, mode);
            if (is64Bit) Sync64.ATO_setBlockTargetSpeed(nextIdx, nv);
            else Sync.ATO_setBlockTargetSpeed(nextIdx, nv);
        }

        /// <summary>
        /// 取 autopilot（C++）本帧全部面板/声音逻辑输出（内部通道），
        /// 经 PanelMap/SoundMap 按端子映射写入 BVE 物理数组（WritePanel/WriteSound：
        /// 受 writepanel/writesound 开关与 [output] 重映射控制，越界/未注册端子仅记录状态），
        /// 状态即 Config.PanelMap.State / SoundMap.State（平行暴露）。
        /// </summary>
        private void UpdateOutputStates(IList<int> panel, IList<int> sound) {
            var panelNames = Config.PanelOutputNames;
            for (int i = 0; i < panelNames.Count; i++) {
                int v = is64Bit ? Sync64.GetPanelOutputValue(i) : Sync.GetPanelOutputValue(i);
                Config.PanelMap.WritePanel(panel, panelNames[i], v);
            }

            var soundNames = Config.SoundOutputNames;
            for (int i = 0; i < soundNames.Count; i++) {
                int v = is64Bit ? Sync64.GetSoundOutputValue(i) : Sync.GetSoundOutputValue(i);
                Config.SoundMap.WriteSound(sound, soundNames[i], v);
            }
        }
    }
}
