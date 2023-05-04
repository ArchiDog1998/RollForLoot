using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace RollForLoot;

public class ConfigWindow : Window
{
    public ConfigWindow()
        :base("Roll For Loot v" + typeof(ConfigWindow).Assembly.GetName().Version.ToString())
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(250, 480);
        RespectCloseHotkey = true;
    }

    public override void Draw()
    {
        foreach (var e in Enum.GetValues<RollConfig>())
        {
            var b = Service.Config.Config.HasFlag(e);
            if (ImGui.Checkbox(GetLabel(e), ref b))
            {
                Service.Config.Config ^= e;
                Service.Config.Save();
            }
        }

        ImGui.Separator();

        if (Service.Config.Config.HasFlag(RollConfig.AutoRoll))
        {
            ImGui.SetNextItemWidth(100);
            if (ImGui.Combo("Auto Roll Strategy", ref Service.Config.DefaultStrategy, new string[]
            {
            "Need",
            "Greed",
            "Pass",
            }, 3))
            {
                Service.Config.Save();
            }
        }

        ImGui.SetNextItemWidth(100);
        if(ImGui.DragFloatRange2("Delay for each roll", ref Service.Config.RollDelayMin,
            ref Service.Config.RollDelayMax, 0.05f, 0, 5)) Service.Config.Save();

        ImGui.SetNextItemWidth(100);
        if (ImGui.DragFloatRange2("Delay for auto roll", ref Service.Config.AutoRollDelayMin,
            ref Service.Config.AutoRollDelayMax, 0.05f, 0, 5)) Service.Config.Save();

        ImGui.Separator();

        ImGui.SetNextItemWidth(100);
        if (ImGui.DragInt("Min Gear Level", ref Service.Config.ItemLevel, 0.5f, 0, 90))
            Service.Config.Save();

        foreach (var e in Enum.GetValues<LootStrategy>())
        {
            var b = Service.Config.LootStrategy.HasFlag(e);
            if (ImGui.Checkbox(GetLabel(e), ref b))
            {
                Service.Config.LootStrategy ^= e;
                Service.Config.Save();
            }
        }
    }

    public static string GetLabel(LootStrategy strategy) => strategy switch
    {
        LootStrategy.IgnoreItemUnlocked => "Ignore Item Unlocked",
        LootStrategy.IgnoreMounts => "Ignore Mounts",
        LootStrategy.IgnoreMinions => "Ignore Minions",
        LootStrategy.IgnoreBardings => "Ignore Bardings",
        LootStrategy.IgnoreEmoteHairstyle => "Ignore Emote / Hairstyle",
        LootStrategy.IgnoreTripleTriadCards => "Ignore Triple Triad Cards",
        LootStrategy.IgnoreOrchestrionRolls => "Ignore Orchestrion Rolls",
        LootStrategy.IgnoreOtherJobItems => "Ignore Other Job Items",
        LootStrategy.IgnoreFadedCopy => "Ignore Faded Copy",
        _ => string.Empty,
    };

    public static string GetLabel(RollConfig strategy) => strategy switch
    {
        RollConfig.AutoOpenChest => "Auto Open the Chest",
        RollConfig.AutoRoll => "Auto Roll for Loot",
        RollConfig.ResultInChat => "Show Result In Chat",
        RollConfig.ResultInToast => "Show Result In Toast",
        _ => string.Empty,
    };
}
