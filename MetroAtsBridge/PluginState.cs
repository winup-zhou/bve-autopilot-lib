using System.Collections.Generic;
using MetroAts;

namespace MetroAtsBridge {
    /// <summary>
    /// MetroAtsBridge（autopilot 输出代理）的平行状态暴露。
    /// 把本插件经 autopilot（C++）写出的面板/声音输出作为逻辑状态
    /// （键=autopilot.ini [panel]/[sound] 中的逻辑名）暴露给其它 BveEX 插件，
    /// 查询方持核心引用经 TryGetPluginState("MetroAtsBridge") 读取，无需解析物理面板数组。
    /// </summary>
    public partial class MetroAtsBridge : IPluginStateProvider {
        /// <summary>插件名（核心注册表中的注册键）。</summary>
        public string PluginName => "MetroAtsBridge";

        /// <summary>面板输出逻辑状态（逻辑名 → 值）。每帧由 Tick 经内部通道取 autopilot 逻辑值并落地时更新。</summary>
        public IReadOnlyDictionary<string, int> PanelStates => Config.PanelMap?.State;

        /// <summary>声音输出逻辑状态（逻辑名 → SoundPlayMode 数值，atostart / inchingstart）。</summary>
        public IReadOnlyDictionary<string, int> SoundStates => Config.SoundMap?.State;
    }
}
