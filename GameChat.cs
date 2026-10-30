using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace FFXIVBowling;

internal static class GameChat
{
    /// <summary>
    /// Submits a line as if it was typed in the chat box, so /random actually rolls.
    /// IChatGui.Print only echoes locally and will not produce a roll result.
    /// </summary>
    public static unsafe void SendCommand(string command)
    {
        var ui = UIModule.Instance();
        if (ui is null || string.IsNullOrWhiteSpace(command))
            return;

        var message = Utf8String.FromString(command);
        try
        {
            ui->ProcessChatBoxEntry(message, nint.Zero, false);
        }
        finally
        {
            if (message != null)
                message->Dtor(true);
        }
    }
}
