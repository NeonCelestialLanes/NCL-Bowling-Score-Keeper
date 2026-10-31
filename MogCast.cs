using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace FFXIVBowling;

/// <summary>
/// Hands off to Aetherphone MogCast only when a house conductor
/// (Mikh, Uki, or Shin) is at NCL. Aetherphone has no join-by-name IPC,
/// so we target that conductor then open MogCast — guests pick their nearby screen.
/// </summary>
internal sealed class MogCast
{
    public const string OpenCommand = "/phone open aetherstream";

    private static readonly string[] Hosts =
    [
        "Mikh Aliapoh",
        "Ukiri Morinou",
        "Shin Morinou",
    ];

    private readonly ICommandManager commands;
    private readonly IObjectTable objectTable;
    private readonly ITargetManager targets;

    public MogCast(ICommandManager commands, IObjectTable objectTable, ITargetManager targets)
    {
        this.commands = commands;
        this.objectTable = objectTable;
        this.targets = targets;
    }

    public bool PhoneInstalled => this.commands.Commands.ContainsKey("/phone");

    public bool CanJoinAtVenue(IPlayerState playerState, out string hostName)
    {
        hostName = string.Empty;
        if (!NclVenue.IsAt(playerState) || !this.PhoneInstalled)
            return false;

        return this.TryFindHost(out _, out hostName);
    }

    public bool TryFindHost(out IPlayerCharacter? host, out string hostName)
    {
        host = null;
        hostName = string.Empty;
        foreach (var wanted in Hosts)
        {
            foreach (var obj in this.objectTable)
            {
                if (obj is not IPlayerCharacter player)
                    continue;

                var name = player.Name.TextValue;
                if (!RollParser.NamesMatch(name, wanted))
                    continue;

                host = player;
                hostName = RollParser.NormalizeName(name);
                return true;
            }
        }

        return false;
    }

    public bool TryOpenHouseCast()
    {
        if (!this.PhoneInstalled)
            return false;

        if (!this.TryFindHost(out var host, out _))
            return false;

        this.targets.Target = host;
        this.commands.ProcessCommand(OpenCommand);
        return true;
    }
}
