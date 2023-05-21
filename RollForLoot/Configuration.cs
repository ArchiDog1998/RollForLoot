using Dalamud.Configuration;

namespace RollForLoot;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public RollConfig Config = RollConfig.AutoRoll
        | RollConfig.ResultInChat | RollConfig.ResultInToast;

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
    AutoRoll = 1 << 0,
    ResultInChat = 1 << 1,
    ResultInToast = 1 << 2,

    DefaultStrategyMask = 1 << 3 | 1 << 4,
}
