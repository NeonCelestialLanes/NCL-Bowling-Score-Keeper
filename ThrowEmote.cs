using System;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace FFXIVBowling;

internal sealed class ThrowEmote
{
    private readonly IDataManager data;
    private readonly IUnlockState unlocks;

    public ThrowEmote(IDataManager data, IUnlockState unlocks)
    {
        this.data = data;
        this.unlocks = unlocks;
    }

    public bool IsUnlocked()
    {
        try
        {
            var sheet = this.data.GetExcelSheet<Emote>();
            if (sheet is null)
                return false;

            foreach (var emote in sheet)
            {
                if (!IsThrowCommand(emote))
                    continue;

                return this.unlocks.IsEmoteUnlocked(emote);
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }

    private static bool IsThrowCommand(Emote emote)
    {
        if (!emote.TextCommand.IsValid)
            return false;

        var command = emote.TextCommand.Value.Command.ExtractText().Trim();
        if (command.Equals("/throw", StringComparison.OrdinalIgnoreCase))
            return true;

        var alias = emote.TextCommand.Value.ShortCommand.ExtractText().Trim();
        return alias.Equals("/throw", StringComparison.OrdinalIgnoreCase);
    }
}
