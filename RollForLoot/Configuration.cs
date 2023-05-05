using Dalamud.Configuration;

namespace RollForLoot;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public RollConfig Config = RollConfig.AutoOpenChest | RollConfig.AutoRoll
        | RollConfig.ResultInChat | RollConfig.ResultInToast | RollConfig.AutoCloseWindow;

    public int DefaultStrategy = 0;

    public LootStrategy LootStrategy = 0;

    public int ItemLevel = 0;

    public float RollDelayMin = 1;
    public float RollDelayMax = 1.5f;

    public float AutoRollDelayMin = 1.5f;
    public float AutoRollDelayMax = 2f;
    public void Save()
    {
        Service.Interface.SavePluginConfig(this);
    }
}

[Flags]
public enum RollConfig : byte
{
    AutoOpenChest = 1 << 0,
    AutoRoll = 1 << 1,
    ResultInChat = 1 << 2,
    ResultInToast = 1 << 3,
    AutoCloseWindow = 1 << 4,
}
