using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Lumina.Excel.GeneratedSheets;
using System.Runtime.InteropServices;

namespace RollForLoot;

internal static class Roller
{
    unsafe delegate bool RollItemRaw(Loot* lootIntPtr, RollResult option, uint lootItemIndex);
    static RollItemRaw _rollItemRaw;

    static uint _itemId = 0, _index = 0;
    public static void Clear()
    {
        _itemId = _index = 0;
    }

    public static bool RollOneItem(RollResult option, ref int need, ref int greed, ref int pass)
    {
        if (!GetNextLootItem(out var index, out var loot)) return false;

        //Make option valid.
        option = ResultMerge(option, GetRestrictResult(loot), GetPlayerRestrict(loot));

        if (_itemId == loot.ItemId && index == _index)
        {
            PluginLog.Warning($"Item [{loot.ItemId}] roll {option} failed, please contract to the author. Or lower you delay.");
            switch (option)
            {
                case RollResult.Needed:
                    need--;
                    break;
                case RollResult.Greeded:
                    greed--;
                    break;
                default:
                    pass--;
                    break;
            }
            option = RollResult.Passed;
        }

        RollItem(option, index);
        _itemId = loot.ItemId;
        _index = index;

        switch (option)
        {
            case RollResult.Needed:
                need++;
                break;
            case RollResult.Greeded:
                greed++;
                break;
            default:
                pass++;
                break;
        }
        return true;
    }

    private static RollResult GetRestrictResult(LootItem loot)
    {
         var stateMax = loot.RollState switch
         {
             RollState.UpToNeed => RollResult.Needed,
             RollState.UpToGreed => RollResult.Greeded,
             _ => RollResult.Passed,
         };

        var ruleMax = loot.LootMode switch
        {
            LootMode.Normal => RollResult.Needed,
            LootMode.GreedOnly => RollResult.Greeded,
            _ => RollResult.Passed,
        };
        return ResultMerge(stateMax, ruleMax);
    }

    private static RollResult GetPlayerRestrict(LootItem loot)
    {
        var item = Service.Data.GetExcelSheet<Item>().GetRow(loot.ItemId);
        if (item == null) return RollResult.Passed;

        //Unique.
        if (item.IsUnique && ItemCount(loot.ItemId) > 0) return RollResult.Passed;

        var strategy = Service.Config.LootStrategy;

        if (IsItemUnlocked(loot.ItemId))
        {
            if (strategy.HasFlag(LootStrategy.IgnoreItemUnlocked))
            {
                return RollResult.Passed;
            }

            if ((strategy.HasFlag(LootStrategy.IgnoreMounts) || item.IsUnique)
                && item.ItemAction?.Value.Type == 1322)
            {
                return RollResult.Passed;
            }

            if ((strategy.HasFlag(LootStrategy.IgnoreMinions) || item.IsUnique)
                && item.ItemAction?.Value.Type == 853)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreBardings)
                && item.ItemAction?.Value.Type == 1013)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreEmoteHairstyle)
                && item.ItemAction?.Value.Type == 2633)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreTripleTriadCards)
                && item.ItemAction?.Value.Type == 3357)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreOrchestrionRolls)
                && item.ItemAction?.Value.Type == 25183)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreFadedCopy)
                && item.Icon == 25958)
            {
                return RollResult.Passed;
            }
        }

        if(item.EquipSlotCategory.Row != 0)
        {
            if (item.LevelItem.Row <= Service.Config.ItemLevel)
            {
                return RollResult.Passed;
            }

            if (strategy.HasFlag(LootStrategy.IgnoreOtherJobItems)
                && loot.RollState != RollState.UpToNeed)
            {
                return RollResult.Passed;
            }
        }

        //PLD set.
        if (strategy.HasFlag(LootStrategy.IgnoreOtherJobItems)
            && item.ItemAction?.Value.Type == 29153 
            && !(Service.ClientState.LocalPlayer?.ClassJob?.Id is 1 or 19))
        {
            return RollResult.Passed;
        }

        return RollResult.Needed;
    }

    private static RollResult ResultMerge(params RollResult[] results)
       => results.Max() switch
       {
           RollResult.Needed => RollResult.Needed,
           RollResult.Greeded => RollResult.Greeded,
           _ => RollResult.Passed,
       };

    public static unsafe bool GetNextLootItem(out uint i, out LootItem loot)
    {
        var span = Loot.Instance()->ItemArraySpan;
        for (i = 0; i < span.Length; i++)
        {
            loot = span[(int)i];
            if (loot.ChestObjectId is 0 or GameObject.InvalidGameObjectId) continue;
            if ((RollResult)loot.RollResult != RollResult.UnAwarded) continue;
            if (loot.RollState is RollState.Rolled or RollState.Unavailable or RollState.Unknown) continue;
            if (loot.ItemId == 0) continue;
            if (loot.LootMode is LootMode.LootMasterGreedOnly or LootMode.Unavailable) continue;

            return true;
        }

        loot = default;
        return false;
    }

    private static unsafe void RollItem(RollResult option, uint index)
    {
        try
        {
            _rollItemRaw ??= Marshal.GetDelegateForFunctionPointer<RollItemRaw>(Service.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            _rollItemRaw?.Invoke(Loot.Instance(), option, index).ToString();
        }
        catch (Exception ex)
        {
            PluginLog.Warning(ex, "Warning at roll");
        }
    }

    private static unsafe int ItemCount(uint itemId)
        => InventoryManager.Instance()->GetInventoryItemCount(itemId);

    private static unsafe bool IsItemUnlocked(uint itemId)
        => UIState.Instance()->IsItemActionUnlocked(ExdModule.GetItemRowById(itemId)) == 1;
}
