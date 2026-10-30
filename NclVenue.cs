using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace FFXIVBowling;

/// <summary>
/// Neon Celestial Lanes: Goblin (world 81), Lavender Beds, Ward 17 Plot 21.
/// Housing ward/plot from the game are 0-indexed.
/// Lifestream: /li goblin lb w17 p21
/// </summary>
internal static class NclVenue
{
    public const string WorldName = "Goblin";
    public const string DistrictName = "Lavender Beds";
    public const int Ward = 17;
    public const int Plot = 21;
    public const ushort GoblinWorldId = 81;
    public const uint LavenderBedsTerritory = 340;
    public const string Address = "Goblin · Lavender Beds · Ward 17 Plot 21";
    public const string LifestreamCommand = "goblin lb w17 p21";

    public static bool IsAt(IPlayerState playerState) =>
        IsGoblin(playerState) && IsNclPlot();

    public static bool IsGoblin(IPlayerState playerState)
    {
        if (playerState.CurrentWorld.RowId == GoblinWorldId)
            return true;

        if (playerState.CurrentWorld.IsValid)
        {
            var name = playerState.CurrentWorld.Value.Name.ToString();
            if (name.Equals(WorldName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static unsafe bool IsNclPlot()
    {
        var mgr = HousingManager.Instance();
        if (mgr is null)
            return false;

        var plot = mgr->GetCurrentPlot();
        var ward = mgr->GetCurrentWard();
        if (plot < 0 || ward < 0)
            return false;

        if (ward + 1 != Ward || plot + 1 != Plot)
            return false;

        var house = mgr->GetCurrentHouseId();
        if (house.WorldId != 0 && house.WorldId != GoblinWorldId)
            return false;

        return true;
    }
}
