using System.Collections.Generic;
using System.Reflection;
using System.IO;
using MetroAts;
using BveEx.PluginHost;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Linq;

namespace MetroAtsBridge {
    public static class Config {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileSection(string Section, StringBuilder RetVal, int Size, string FilePath);

        public const double LessInf = 0x7fffffff;
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string path;
        private const int buffer_size = 4096;

        public static List<KeyPosList> ATO_KeyPosLists = new List<KeyPosList>();
        public static List<KeyPosList> TASC_KeyPosLists = new List<KeyPosList>();

        /// <summary>
        /// 是否仍向 BVE 物理 panel/sound 写入（autopilot 输出的显示端子，落地由本 bridge 执行）。
        /// 默认 true；INI [output]writepanel / writesound = false 时仅保留状态暴露（内部通道不受影响）。
        /// </summary>
        public static bool PanelWriteEnabled = true;
        public static bool SoundWriteEnabled = true;

        /// <summary>
        /// autopilot（C++）逻辑输出的端子映射与状态（落地由本 bridge 执行，C++ 不直写 BVE）。
        /// 逻辑键 = C++ 面板論理名（パネル出力対象名簿）与音声論理名（atostart / inchingstart）；
        /// 默认端子 = autopilot.ini 的 [panel]/[sound]（"端子 = 逻辑名"）；
        /// MetroAtsBridgeConfig.ini [output] 可覆盖逻辑键端子（键=逻辑名，值=端子号）。
        /// 每帧经内部通道取逻辑值后 WritePanel/WriteSound 落地（受 writepanel/writesound 与重映射控制），
        /// 状态平行暴露：其它插件经核心 TryGetPluginState("MetroAtsBridge") 读 PanelStates/SoundStates。
        /// </summary>
        public static readonly MetroAts.OutputIndexMap PanelMap = new MetroAts.OutputIndexMap();
        public static readonly MetroAts.OutputIndexMap SoundMap = new MetroAts.OutputIndexMap();

        // C++ 逻辑名列表（顺序与 C++ 名簿一致，供每帧按索引取逻辑值）
        public static readonly List<string> PanelOutputNames = new List<string>();
        public static readonly List<string> SoundOutputNames = new List<string>();

        public static void Load() {
            path = new FileInfo(Path.Combine(PluginDir, "MetroAtsBridgeConfig.ini")).FullName;
            if (File.Exists(path)) {
                try {
                    var KeysString1 = "";
                    ReadConfig("keys", "atopositions", ref KeysString1);
                    foreach (var i in KeysString1.Split(',')) {
                        ATO_KeyPosLists.Add((KeyPosList)Enum.Parse(typeof(KeyPosList), i, true));
                    }

                    var KeysString2 = "";
                    ReadConfig("keys", "tascpositions", ref KeysString2);
                    foreach (var i in KeysString2.Split(',')) {
                        TASC_KeyPosLists.Add((KeyPosList)Enum.Parse(typeof(KeyPosList), i, true));
                    }

                    // 是否仍写入 BVE 物理 panel/sound（仅保留状态暴露）
                    ReadConfig("output", "writepanel", ref PanelWriteEnabled);
                    ReadConfig("output", "writesound", ref SoundWriteEnabled);
                } catch (Exception ex) {
                    throw ex;
                }
            } else throw new BveFileLoadException("Unable to find configuration file: MetroAtsBridgeConfig.ini", "MetroAtsBridge");
        }

        public static void Dispose() {
            ATO_KeyPosLists.Clear();
            TASC_KeyPosLists.Clear();
            PanelOutputNames.Clear();
            SoundOutputNames.Clear();
            if (PanelMap != null) PanelMap.Clear();
            if (SoundMap != null) SoundMap.Clear();
        }

        /// <summary>
        /// 枚举 autopilot（C++）的全部面板/声音逻辑名，注册默认端子（autopilot.ini [panel]/[sound]）
        /// 并应用 MetroAtsBridgeConfig.ini [output] 覆盖与写入开关。
        /// 须在 Sync/Sync64.Load() 成功（C++ DLL 已加载）后调用（由构造函数调用）。
        /// </summary>
        public static void RegisterAutopilotOutputs() {
            PanelOutputNames.Clear();
            SoundOutputNames.Clear();
            PanelMap.Clear();
            SoundMap.Clear();

            bool is64 = Environment.Is64BitProcess;

            // 面板逻辑名 + autopilot.ini [panel] 默认端子
            var panelDefaults = ReadAutopilotTerminals("panel");
            int pc = is64 ? Sync64.GetPanelOutputCount() : Sync.GetPanelOutputCount();
            if (pc < 0) pc = 0;
            for (int i = 0; i < pc; i++) {
                string name = Marshal.PtrToStringUni(is64 ? Sync64.GetPanelOutputName(i) : Sync.GetPanelOutputName(i));
                if (string.IsNullOrEmpty(name)) continue;
                PanelOutputNames.Add(name);
                PanelMap.RegisterDefault(name, panelDefaults.TryGetValue(name, out int d) ? d : -1);
            }

            // 声音逻辑名 + autopilot.ini [sound] 默认端子
            var soundDefaults = ReadAutopilotTerminals("sound");
            int sc = is64 ? Sync64.GetSoundOutputCount() : Sync.GetSoundOutputCount();
            if (sc < 0) sc = 0;
            for (int i = 0; i < sc; i++) {
                string name = Marshal.PtrToStringUni(is64 ? Sync64.GetSoundOutputName(i) : Sync.GetSoundOutputName(i));
                if (string.IsNullOrEmpty(name)) continue;
                SoundOutputNames.Add(name);
                SoundMap.RegisterDefault(name, soundDefaults.TryGetValue(name, out int d) ? d : -1);
            }

            // [output] 覆盖端子（键=逻辑名，值=端子号；writepanel/writesound 布尔行被忽略）
            PanelMap.Override(OutputIndexMap.ReadSection(path, "output", PanelMap.Index.Keys));
            SoundMap.Override(OutputIndexMap.ReadSection(path, "output", SoundMap.Index.Keys));

            PanelMap.PanelWriteEnabled = PanelWriteEnabled;
            SoundMap.SoundWriteEnabled = SoundWriteEnabled;
        }

        /// <summary>读 autopilot.ini 指定段（"端子 = 逻辑名"），返回 逻辑名 → 端子 映射。</summary>
        private static Dictionary<string, int> ReadAutopilotTerminals(string section) {
            var result = new Dictionary<string, int>();
            string ini = Path.Combine(PluginDir, "autopilot.ini");
            if (!File.Exists(ini)) return result;

            var sb = new StringBuilder(buffer_size);
            int size = GetPrivateProfileSection(section, sb, buffer_size, ini);
            if (size <= 0 || size >= buffer_size - 1) return result;

            foreach (var line in sb.ToString().Split('\0')) {
                if (line.Length == 0) continue;
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                if (int.TryParse(line.Substring(0, eq).Trim(), out int index)) {
                    string name = line.Substring(eq + 1).Trim();
                    if (name.Length > 0) result[name] = index;
                }
            }
            return result;
        }

        // ---------- ATO 多模式目标速度表（表在 bridge；仅 ATO 生效，经 ATO_setBlockTargetSpeed 下发 C++） ----------
        // index = BVE 信号 index（CS-ATC 语义），mode：0=遅速 1=平常 2=回復（与核心 AtoModeList 一致）。
        // 数字现示(11..33)按现示速度查表；105/120 按同模式差推算；ORP(35) 用 ORP35 行（35/25 暂不可区分）；
        // 停止/構内(Sxx/SORP)/無効等一律 0。
        public static int AtoModeTargetSpeed(int index, int mode) {
            int vRef;
            switch (index) {
                case 11: case 12: vRef = 10; break;
                case 13: vRef = 15; break;
                case 14: vRef = 20; break;
                case 15: vRef = 25; break;
                case 16: vRef = 30; break;
                case 17: vRef = 35; break;
                case 18: vRef = 40; break;
                case 19: vRef = 45; break;
                case 20: vRef = 50; break;
                case 21: vRef = 55; break;
                case 22: vRef = 60; break;
                case 23: vRef = 65; break;
                case 24: vRef = 70; break;
                case 25: vRef = 75; break;
                case 26: vRef = 80; break;
                case 27: vRef = 85; break;
                case 28: vRef = 90; break;
                case 29: vRef = 95; break;
                case 30: vRef = 100; break;
                case 31: vRef = 105; break;
                case 32: vRef = 110; break;
                case 33: vRef = 120; break;
                case 35: return AtoTarget(20, 30, 32, mode); // ORP（ORP35 行）；ORP25 暂无法区分
                default: return 0; // 停止(01/02)・構内(Sxx/SORP)・無効等
            }
            return AtoDigitTarget(vRef, mode);
        }

        // 现示速度 v 的数字现示行 → {遅速, 平常, 回復}（105/120 为推算行）
        private static int AtoDigitTarget(int v, int mode) {
            switch (v) {
                case 10: return AtoTarget(5, 5, 7, mode);
                case 15: return AtoTarget(10, 10, 12, mode);
                case 20: return AtoTarget(15, 15, 17, mode);
                case 25: return AtoTarget(20, 20, 22, mode);
                case 30: return AtoTarget(20, 25, 27, mode);
                case 35: return AtoTarget(20, 30, 32, mode);
                case 40: return AtoTarget(25, 35, 37, mode);
                case 45: return AtoTarget(30, 40, 42, mode);
                case 50: return AtoTarget(35, 45, 47, mode);
                case 55: return AtoTarget(40, 50, 52, mode);
                case 60: return AtoTarget(45, 55, 57, mode);
                case 65: return AtoTarget(50, 60, 62, mode);
                case 70: return AtoTarget(55, 65, 67, mode);
                case 75: return AtoTarget(60, 70, 72, mode);
                case 80: return AtoTarget(65, 75, 77, mode);
                case 85: return AtoTarget(70, 80, 82, mode);
                case 90: return AtoTarget(75, 85, 87, mode);
                case 95: return AtoTarget(80, 90, 92, mode);
                case 100: return AtoTarget(85, 95, 97, mode);
                case 105: return AtoTarget(90, 100, 102, mode); // 推算
                case 110: return AtoTarget(95, 105, 107, mode);
                case 120: return AtoTarget(105, 115, 117, mode); // 推算
                default: return 0;
            }
        }

        private static int AtoTarget(int slowKmh, int normalKmh, int recoveryKmh, int mode) {
            switch (mode) {
                case 0: return slowKmh;      // 遅速
                case 2: return recoveryKmh;  // 回復
                default: return normalKmh;   // 平常
            }
        }

        private static void ReadConfig(string Section, string Key, ref int Value) {
            var OriginalVal = Value;
            var RetVal = new StringBuilder(buffer_size);
            var Readsize = GetPrivateProfileString(Section, Key, "", RetVal, buffer_size, path);
            if (Readsize > 0 && Readsize < buffer_size - 1) {
                Value = Convert.ToInt32(RetVal.ToString());
            } else {
                Value = OriginalVal;
            }
        }

        private static void ReadConfig(string Section, string Key, ref double Value) {
            var OriginalVal = Value;
            var RetVal = new StringBuilder(buffer_size);
            var Readsize = GetPrivateProfileString(Section, Key, "", RetVal, buffer_size, path);
            if (Readsize > 0 && Readsize < buffer_size - 1) {
                Value = Convert.ToDouble(RetVal.ToString());
            } else {
                Value = OriginalVal;
            }
        }

        private static void ReadConfig(string Section, string Key, ref bool Value) {
            var OriginalVal = Value;
            var RetVal = new StringBuilder(buffer_size);
            var Readsize = GetPrivateProfileString(Section, Key, "", RetVal, buffer_size, path);
            if (Readsize > 0 && Readsize < buffer_size - 1) {
                Value = Convert.ToBoolean(RetVal.ToString());
            } else {
                Value = OriginalVal;
            }
        }

        private static void ReadConfig(string Section, string Key, ref string Value) {
            var OriginalVal = Value;
            var RetVal = new StringBuilder(buffer_size);
            var Readsize = GetPrivateProfileString(Section, Key, "", RetVal, buffer_size, path);
            if (Readsize > 0 && Readsize < buffer_size - 1) {
                Value = RetVal.ToString();
            } else {
                Value = OriginalVal;
            }
        }
    }
}
