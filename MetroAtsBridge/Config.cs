using System.Collections.Generic;
using System.Reflection;
using System.IO;
using BveEx.PluginHost;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Linq;

namespace MetroAtsBridge {
    public static class Config {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public const double LessInf = 0x7fffffff;
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string path;
        private const int buffer_size = 4096;

        public static List<KeyPosList> ATO_KeyPosLists = new List<KeyPosList>();
        public static List<KeyPosList> TASC_KeyPosLists = new List<KeyPosList>();

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
                } catch (Exception ex) {
                    throw ex;
                }
            } else throw new BveFileLoadException("Unable to find configuration file: MetroAtsBridgeConfig.ini", "MetroAtsBridge");
        }

        public static void Dispose() {
            ATO_KeyPosLists.Clear();
            TASC_KeyPosLists.Clear();
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
