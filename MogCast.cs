using Dalamud.Plugin.Services;

namespace FFXIVBowling;

/// <summary>
/// Hands off to Aetherphone's MogCast app. Aetherphone does not publish
/// a live-party IPC, so "on air" here means: you are at NCL and the phone is installed.
/// Opening MogCast is what actually lists nearby screens.
/// </summary>
internal sealed class MogCast
{
    public const string OpenCommand = "/phone open aetherstream";
    private readonly ICommandManager commands;

    public MogCast(ICommandManager commands)
    {
        this.commands = commands;
    }

    public bool PhoneInstalled => this.commands.Commands.ContainsKey("/phone");

    public bool CanJoinAtVenue(IPlayerState playerState) =>
        NclVenue.IsAt(playerState) && this.PhoneInstalled;

    public bool TryOpen()
    {
        if (!this.PhoneInstalled)
            return false;

        this.commands.ProcessCommand(OpenCommand);
        return true;
    }
}
