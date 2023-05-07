﻿using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.ConfigModule;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace RollForLoot;

public sealed class RollForLootPlugin : IDalamudPlugin, IDisposable
{
    public string Name => "Roll For Loot";

    private readonly WindowSystem _windowSystem;
    static ConfigWindow _configWindow;

    public RollForLootPlugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Config = (Service.Interface.GetPluginConfig() as Configuration) ?? new Configuration();

        _configWindow = new();

        _windowSystem = new WindowSystem(Name);
        _windowSystem.AddWindow(_configWindow);

        Service.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw += _windowSystem.Draw;
        Service.ChatGui.CheckMessageHandled += NoticeLoot;
        Service.Framework.Update += FrameworkUpdate;

        Roller.Init();

        Service.CommandManager.AddHandler("/rollforloot", new CommandInfo(OnCommand)
        {
            HelpMessage = "Roll for loot for you.",
            ShowInHelp = true,
        });
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Service.Interface.UiBuilder.Draw -= _windowSystem.Draw;
        Service.ChatGui.CheckMessageHandled -= NoticeLoot;
        Service.Framework.Update -= FrameworkUpdate;

        Service.CommandManager.RemoveHandler("/rollforloot");
    }

    private void OnOpenConfigUi()
    {
        _configWindow.IsOpen = true;
    }

    private unsafe void FrameworkUpdate(Framework framework)
    {
        if (!Service.Condition[ConditionFlag.BoundByDuty]) return;

        CloseWindow();
        OpenChest();
        RollLoot();
    }

    static bool _closeWindow = false;
    private unsafe static void CloseWindow()
    {
        if (!_closeWindow) return;

        var needGreedWindow = Service.GameGui.GetAddonByName("NeedGreed", 1);
        if (needGreedWindow == IntPtr.Zero) return;

        _closeWindow = false;

        var notification = (AtkUnitBase*)Service.GameGui.GetAddonByName("_Notification", 1);
        if (notification == null) return;

        var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
        atkValues[0].Type = atkValues[1].Type = ValueType.Int;
        atkValues[0].Int = 0;
        atkValues[1].Int = 2;
        try
        {
            notification->FireCallback(2, atkValues);
        }
        catch (Exception ex)
        {
            PluginLog.Warning(ex, "Failed to close the window!");
        }
        finally
        {
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    static DateTime _nextOpenTime = DateTime.Now;
    static uint _lastChest = 0;
    private unsafe static void OpenChest()
    {
        if (!Service.Config.Config.HasFlag(RollConfig.AutoOpenChest)) return;
        var player = Service.ClientState.LocalPlayer;
        if (player == null) return;

        var treasure = Service.ObjectTable.FirstOrDefault(o =>
        {
            if (o == null) return false;
            var dis = Vector3.Distance(player.Position, o.Position) - player.HitboxRadius - o.HitboxRadius;
            if (dis > 0.5f) return false;

            var address = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)o.Address;
            if ((ObjectKind)address->ObjectKind != ObjectKind.Treasure) return false;

            //Opened!
            foreach (var item in Loot.Instance()->ItemArraySpan)
            {
                if (item.ChestObjectId == o.ObjectId) return false;
            }

            return true;
        });

        if (treasure == null) return;
        if (DateTime.Now < _nextOpenTime) return;
        if (treasure.ObjectId == _lastChest && DateTime.Now - _nextOpenTime < TimeSpan.FromSeconds(10)) return;

        _nextOpenTime = DateTime.Now.AddSeconds(new Random().NextDouble() + 0.2);
        _lastChest = treasure.ObjectId;

        try
        {
            Service.TargetManager.SetTarget(treasure);

            TargetSystem.Instance()->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)treasure.Address);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to open the chest!");
        }

        if (!Service.Config.Config.HasFlag(RollConfig.AutoCloseWindow)) return;
        _closeWindow = true;
    }

    static DateTime _nextRollTime = DateTime.Now;
    static RollResult _rollOption = RollResult.UnAwarded;
    static int _need = 0, _greed = 0, _pass = 0;
    private static void RollLoot()
    {
        if (_rollOption == RollResult.UnAwarded) return;
        if (DateTime.Now < _nextRollTime) return;

        _nextRollTime = DateTime.Now.AddMilliseconds(Math.Max(150, new Random()
            .Next((int)(Service.Config.RollDelayMin * 1000),
            (int)(Service.Config.RollDelayMax * 1000))));

        try
        {
            if (!Roller.RollOneItem(_rollOption, ref _need, ref _greed, ref _pass))//Finish the loot
            {
                ShowResult(_need, _greed, _pass);
                _need = _greed = _pass = 0;
                _rollOption = RollResult.UnAwarded;
                Roller.Clear();
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Something Wrong with rolling!");
        }
    }

    private static void ShowResult(int need, int greed, int pass)
    {
        SeString seString = new(new List<Payload>()
        {
            new TextPayload("Need "),
            new UIForegroundPayload(575),
            new TextPayload(need.ToString()),
            new UIForegroundPayload(0),
            new TextPayload(" item" + (need == 1 ? "" : "s") + ", greed "),
            new UIForegroundPayload(575),
            new TextPayload(greed.ToString()),
            new UIForegroundPayload(0),
            new TextPayload(" item" + (greed == 1 ? "" : "s") + ", pass "),
            new UIForegroundPayload(575),
            new TextPayload(pass.ToString()),
            new UIForegroundPayload(0),
            new TextPayload(" item" + (pass == 1 ? "" : "s") + ".")
        });

        if (Service.Config.Config.HasFlag(RollConfig.ResultInChat))
        {
            Service.ChatGui.Print(seString);
        }
        if (Service.Config.Config.HasFlag(RollConfig.ResultInToast))
        {
            Service.ToastGui.ShowQuest(seString);
        }
    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments.Contains("need", StringComparison.OrdinalIgnoreCase))
        {
            _rollOption = RollResult.Needed;
        }
        else if (arguments.Contains("greed", StringComparison.OrdinalIgnoreCase))
        {
            _rollOption = RollResult.Greeded;
        }
        else if (arguments.Contains("pass", StringComparison.OrdinalIgnoreCase))
        {
            _rollOption = RollResult.Passed;
        }
        else if (arguments.Contains("autoRoll", StringComparison.OrdinalIgnoreCase))
        {
            Service.Config.Config ^= RollConfig.AutoRoll;
            Service.ChatGui.Print($"Set Auto Roll to {Service.Config.Config.HasFlag(RollConfig.AutoRoll)}");
            Service.Config.Save();
        }
        else if (arguments.Contains("autoOpen", StringComparison.OrdinalIgnoreCase))
        {
            Service.Config.Config ^= RollConfig.AutoOpenChest;
            Service.ChatGui.Print($"Set Auto Open to {Service.Config.Config.HasFlag(RollConfig.AutoOpenChest)}");
            Service.Config.Save();
        }
        else
        {
            OnOpenConfigUi();
        }
    }

    static readonly RollResult[] _rollArray = new RollResult[]
    {
        RollResult.Needed,
        RollResult.Greeded,
        RollResult.Passed,
    };

    private void NoticeLoot(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!Service.Config.Config.HasFlag(RollConfig.AutoRoll) || type != (XivChatType)2105) return;

        string textValue = message.TextValue;
        if (textValue == Service.ClientState.ClientLanguage switch
        {
            ClientLanguage.German => "Bitte um das Beutegut würfeln.",
            ClientLanguage.French => "Veuillez lancer les dés pour le butin.",
            ClientLanguage.Japanese => "ロットを行ってください。",
            _ => "Cast your lot.",
        })
        {
            Service.Interface.UiBuilder.AddNotification("Loot Time!", "Roll For Loot", NotificationType.Info);

            _nextRollTime = DateTime.Now.AddMilliseconds(new Random()
                .Next((int)(Service.Config.AutoRollDelayMin * 1000),
                (int)(Service.Config.AutoRollDelayMax * 1000)));

            _rollOption = _rollArray[(byte)(Service.Config.Config & RollConfig.DefaultStrategyMask) >> 5];
        }
    }
}
