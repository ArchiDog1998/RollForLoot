using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace RollForLoot;

public class Service
{
    public static Configuration Config { get; set; }

    [PluginService]
    public static ChatGui ChatGui { get; private set; }

    [PluginService]
    public static ClientState ClientState { get; private set; }

    [PluginService]
    public static CommandManager CommandManager { get; private set; }

    [PluginService]
    public static DataManager Data { get; private set; }

    [PluginService]
    public static Framework Framework { get; private set; }

    [PluginService]
    public static DalamudPluginInterface Interface { get; private set; }

    [PluginService]
    public static SigScanner SigScanner { get; private set; }

    [PluginService]
    public static ToastGui ToastGui { get; private set; }

    [PluginService]
    public static Condition Condition { get; private set; }

    [PluginService]
    public static ObjectTable ObjectTable { get; private set; }

    [PluginService]
    public static TargetManager TargetManager { get; private set; }
}
