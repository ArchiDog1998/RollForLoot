namespace RollForLoot;

[Flags]
public enum LootStrategy : ushort
{
    IgnoreItemUnlocked = 1 << 0,

    IgnoreMounts = 1 << 1,

    IgnoreMinions = 1 << 2,

    IgnoreBardings = 1 << 3,

    IgnoreEmoteHairstyle = 1 << 4,

    IgnoreTripleTriadCards = 1 << 5,

    IgnoreOrchestrionRolls = 1 << 6,

    IgnoreOtherJobItems = 1 << 7,

    IgnoreFadedCopy = 1 << 8,
}
