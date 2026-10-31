using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace FFXIVBowling;

/// <summary>
/// Hands off to Aetherphone's MogCast. LIVE is on only when a house conductor
/// (Mikh, Uki, or Shin) is at NCL with you. Aetherphone does not publish party IPC.
/// </summary>
internal sealed class MogCast
{
    public const string OpenCommand = "/phone open aetherstream";

    private static readonly string[] Hosts =
    [
        "Mikh Alipoh",
        "Ukiri Morinou",
        "Shin Morinou",
    ];

    private readonly ICommandManager commands;
    private readonly IObjectTable objectTable;

    public MogCast(ICommandManager commands, IObjectTable objectTable)
    {
        this.commands = commands;
        this.objectTable = objectTable;
    }

    public bool PhoneInstalled => this.commands.Commands.ContainsKey("/phone");

    public bool CanJoinAtVenue(IPlayerState playerState, out string hostName)
    {
        hostName = string.Empty;
        if (!NclVenue.IsAt(playerState) || !this.PhoneInstalled)
            return false;

        return this.TryFindHost(out hostName);
    }

    public bool TryFindHost(out string hostName)
    {
        hostName = string.Empty;
        foreach (var obj in this.objectTable)
        {
            if (obj is not IPlayerCharacter player)
                continue;

            var name = player.Name.TextValue;
            if (!IsHouseHost(name))
                continue;

            hostName = RollParser.NormalizeName(name);
            return true;
        }

        return false;
    }

    public bool TryOpen()
    {
        if (!this.PhoneInstalled)
            return false;

        this.commands.ProcessCommand(OpenCommand);
        return true;
    }

    private static bool IsHouseHost(string name)
    {
        var normalized = RollParser.NormalizeName(name);
        foreach (var host in Hosts)
        {
            if (RollParser.NamesMatch(normalized, host))
                return true;
        }

        return false;
    }
}
