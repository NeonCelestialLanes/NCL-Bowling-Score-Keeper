using System;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using System.IO;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using Lumina.Text.ReadOnly;

namespace FFXIVBowling;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "NCL Bowling Score";
    private const string CommandName = "/nclbowling";

    [PluginService] internal IDalamudPluginInterface PluginInterface { get; init; } = null!;
    [PluginService] internal ICommandManager CommandManager { get; init; } = null!;
    [PluginService] internal IChatGui ChatGui { get; init; } = null!;
    [PluginService] internal IPartyList PartyList { get; init; } = null!;
    [PluginService] internal IObjectTable ObjectTable { get; init; } = null!;
    [PluginService] internal IPlayerState PlayerState { get; init; } = null!;
    [PluginService] internal ITextureProvider TextureProvider { get; init; } = null!;

    private BowlingWindow Window { get; set; } = null!;
    private WindowSystem WindowSystem { get; } = new("FFXIVBowlingSystem");

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IPartyList partyList,
        IObjectTable objectTable,
        IPlayerState playerState,
        ITextureProvider textureProvider)
    {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;
        this.ChatGui = chatGui;
        this.PartyList = partyList;
        this.ObjectTable = objectTable;
        this.PlayerState = playerState;
        this.TextureProvider = textureProvider;

        this.Window = new BowlingWindow(pluginInterface, partyList, objectTable, playerState, textureProvider);
        this.WindowSystem.AddWindow(this.Window);

        this.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
                HelpMessage = "Opens the NCL Bowling Score scorecard."
        });

        this.PluginInterface.UiBuilder.Draw += this.DrawUI;
        this.PluginInterface.UiBuilder.OpenMainUi += this.OnCommandOpen;
        this.ChatGui.ChatMessage += this.OnChatMessage;
        this.ChatGui.ChatMessageUnhandled += this.OnChatMessagePlain;
    }

    private void OnCommand(string command, string args) => this.Window.IsOpen = true;
    private void OnCommandOpen() => this.Window.IsOpen = true;
    private void DrawUI() => this.WindowSystem.Draw();

    private void OnChatMessage(IHandleableChatMessage message) => this.IngestChat(message);

    private void OnChatMessagePlain(IChatMessage message) => this.IngestChat(message);

    private void IngestChat(IChatMessage message)
    {
        var text = ReadSeText(message.Message.TextValue, message.OriginalMessage);
        var sender = ReadSeText(message.Sender.TextValue, message.OriginalSender);
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(sender))
            return;

        this.Window.HandleChat(text, sender);
    }

    private static string ReadSeText(string? textValue, ReadOnlySeString original)
    {
        if (!string.IsNullOrWhiteSpace(textValue))
            return textValue;

        var extracted = original.ExtractText();
        return string.IsNullOrWhiteSpace(extracted) ? string.Empty : extracted;
    }

    public void Dispose()
    {
        this.ChatGui.ChatMessageUnhandled -= this.OnChatMessagePlain;
        this.ChatGui.ChatMessage -= this.OnChatMessage;
        this.PluginInterface.UiBuilder.Draw -= this.DrawUI;
        this.PluginInterface.UiBuilder.OpenMainUi -= this.OnCommandOpen;
        this.CommandManager.RemoveHandler(CommandName);
        this.WindowSystem.RemoveAllWindows();
        this.Window.Dispose();
    }
}

public static class RollParser
{
    // World /random:
    //   "Random! You roll a 8 (out of 11)."
    //   "Random! Y'alyth Morinou <world> Goblin rolls a 11 (out of 11)."
    // Party /dice party 11, seen by everyone in the party:
    //   sender "Mikh Aliapoh ★"  body "Random! (1-11) 4"
    private static readonly Regex WorldRoll = new(
        @"(?:(?:Random|Dice)!?\s*)?(?:(?<you>You)\s+roll\s+a|(?<name>.+?)\s+rolls\s+a)\D*(?<pins>\d+)\s*\(\s*(?:out of\s*\d+|0\s*[-–]\s*\d+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PartyDice = new(
        @"(?:\((?<paren>[^)]+)\)\s*)?(?:Random|Dice)!?\s*\(\s*\d+\s*[-–]\s*\d+\s*\)\D*(?<pins>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string message, string? sender, out string? rolledName, out bool isYou, out int pins)
    {
        rolledName = null;
        isYou = false;
        pins = 0;

        var haystack = string.IsNullOrWhiteSpace(sender) ? message : $"{sender} {message}";
        var match = WorldRoll.Match(message);
        if (match.Success && TryReadPins(match.Groups["pins"].Value, out pins))
        {
            isYou = match.Groups["you"].Success;
            if (!isYou)
                rolledName = match.Groups["name"].Value.Trim();
            else if (!string.IsNullOrWhiteSpace(sender))
                rolledName = sender.Trim();
            return true;
        }

        match = PartyDice.Match(haystack);
        if (!match.Success)
            match = PartyDice.Match(message);
        if (!match.Success || !TryReadPins(match.Groups["pins"].Value, out pins))
            return false;

        if (match.Groups["paren"].Success)
            rolledName = match.Groups["paren"].Value.Trim();
        else if (!string.IsNullOrWhiteSpace(sender))
            rolledName = sender.Trim();

        return true;
    }

    private static bool TryReadPins(string raw, out int pins)
    {
        if (!int.TryParse(raw, out pins))
            return false;

        return pins is >= 0 and <= 11;
    }

    internal static bool NamesMatch(string rolledAs, string localName)
    {
        var left = NormalizeName(rolledAs);
        var right = NormalizeName(localName);
        return left.Length > 0 && left.Equals(right, StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch == '@')
                break;
            if (ch is '★' or '☆' || char.IsControl(ch))
                continue;
            if (ch >= '\uE000' && ch <= '\uF8FF')
                continue;

            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }
}

public sealed class BowlingWindow : Window, IDisposable
{
    private readonly IPartyList partyList;
    private readonly IObjectTable objectTable;
    private readonly IPlayerState playerState;
    private readonly LifestreamTravel lifestream;
    private readonly IFontHandle labelFont;
    private readonly ISharedImmediateTexture? logo;
    private readonly PartyLanes lanes = new();
    private string status = "Walk up. Tell us you're ready to dominate the lanes.";
    private string guestName = string.Empty;
    private bool awaitingLocalRoll;
    private bool rollClickArmed = true;
    private string lastRollKey = string.Empty;
    private long lastRollTicks;
    private string? pendingChat;
    private long pendingChatAt;
    private long commandReadyAt;

    public BowlingWindow(
        IDalamudPluginInterface pluginInterface,
        IPartyList partyList,
        IObjectTable objectTable,
        IPlayerState playerState,
        ITextureProvider textureProvider)
        : base("NCL Bowling Score")
    {
        this.partyList = partyList;
        this.objectTable = objectTable;
        this.playerState = playerState;
        this.lifestream = new LifestreamTravel(pluginInterface);
        var dir = pluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;
        var fontPath = Path.Combine(dir, "Assets", "LuckiestGuy-Regular.ttf");
        var logoPath = Path.Combine(dir, "Assets", "ncl-logo.png");
        this.labelFont = pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(toolkit =>
        {
            if (File.Exists(fontPath))
                toolkit.Font = toolkit.AddFontFromFile(fontPath, new SafeFontConfig { SizePx = 17 });
            else
                toolkit.AddDalamudDefaultFont(17);
        }));
        this.logo = File.Exists(logoPath) ? textureProvider.GetFromFile(logoPath) : null;
        this.Size = new Vector2(780, 520);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 340),
            MaximumSize = new Vector2(1200, 900)
        };
    }

    public bool HandleChat(string text, string? sender = null)
    {
        this.SyncRoster();
        if (!NclVenue.IsAt(this.playerState))
            return false;

        if (IsCommandRestricted(text))
        {
            this.awaitingLocalRoll = false;
            this.pendingChat = null;
            this.commandReadyAt = Environment.TickCount64 + 1500;
            this.status = "The game blocked that throw. Wait it out, then roll again.";
            return true;
        }

        if (!RollParser.TryParse(text, sender, out var rolledName, out var isYou, out var pins))
            return false;

        var local = this.LocalLane();
        if (isYou && local is not null)
            rolledName = local.Name;

        var lane = this.lanes.FindBowler(rolledName, sender, text);
        if (lane is null && isYou)
            lane = local;

        var isLocalThrow = lane?.IsLocal == true;
        if (isLocalThrow)
        {
            if (!this.awaitingLocalRoll)
                return true;

            this.awaitingLocalRoll = false;
        }

        if (lane is null)
        {
            this.status = $"Heard a roll ({pins}) from {rolledName ?? sender ?? "party chat"} but they're not on a lane.";
            return true;
        }

        var key = $"{lane.ContentId}:{pins}";
        var now = Environment.TickCount64;
        if (key == this.lastRollKey && now - this.lastRollTicks < 800)
            return true;

        this.lastRollKey = key;
        this.lastRollTicks = now;

        if (!this.lanes.TryApplyRoll(lane, pins, out var counted, out var overshot, out var error))
        {
            this.status = error ?? "Could not apply that roll.";
            return true;
        }

        var who = lane!.IsLocal ? "You" : lane.Name;
        var extra = overshot
            ? $" Rolled {pins} with fewer pins standing. Gutter."
            : string.Empty;
        this.status = $"{who}: {lane.Game.DescribeLastMark(counted)}{extra}";
        if (!lane.Game.IsFrameInProgress)
        {
            var next = this.lanes.WhoseTurn();
            if (next is not null)
                this.status += $"  Next up: {next.Name}.";
        }

        return true;
    }

    public override void Draw()
    {
        this.SyncRoster();

        NeonTheme.Push();
        NeonTheme.DrawBackdrop();
        NeonTheme.BrandHeader(this.logo?.GetWrapOrEmpty(), this.labelFont);

        if (!NclVenue.IsAt(this.playerState))
        {
            this.pendingChat = null;
            this.awaitingLocalRoll = false;
            this.labelFont.Push();
            ImGui.TextColored(NeonTheme.Magenta, "This scorecard only works at Neon Celestial Lanes.");
            ImGui.TextColored(NeonTheme.Gold, NclVenue.Address);
            ImGui.TextColored(NeonTheme.Muted, "Step inside the house — yard or lanes — then bowl.");
            ImGui.Dummy(new Vector2(0, 8));
            ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
            if (NeonTheme.GlowButton("  GO TO NCL  ", NeonTheme.Cyan))
                this.lifestream.TryGoToNcl(out this.status);
            ImGui.SameLine();
            if (NeonTheme.GlowButton("  CARRD  ", NeonTheme.Gold))
                Util.OpenLink("https://neoncelestial.carrd.co/");
            ImGui.PopStyleColor();
            this.labelFont.Pop();
            NeonTheme.StatusBar(
                this.status.StartsWith("Walk up", StringComparison.Ordinal)
                    ? "Come bowl at NCL. " + NclVenue.Address + "."
                    : this.status);
            NeonTheme.Pop();
            return;
        }

        var local = this.LocalLane();
        var onApproach = this.lanes.WhoseTurn();
        var localDone = local?.Game.IsComplete == true;
        var localTurn = local is not null && onApproach is not null && ReferenceEquals(onApproach, local);
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            this.rollClickArmed = true;

        var cooldownMs = this.commandReadyAt - Environment.TickCount64;
        var onCooldown = cooldownMs > 0;
        var canRoll = localTurn && !localDone && !this.awaitingLocalRoll && this.rollClickArmed && !onCooldown;

        ImGui.TextColored(NeonTheme.Cyan, this.RosterHeadline());
        if (onApproach is not null)
            ImGui.TextColored(NeonTheme.Gold, $"On the approach: {onApproach.Name}");
        else if (this.lanes.PresentCount > 0)
            ImGui.TextColored(NeonTheme.Magenta, "That's the game. Bragging rights until someone beats you.");

        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        var rollColor = this.awaitingLocalRoll || onCooldown ? NeonTheme.Gold : canRoll ? NeonTheme.Cyan : NeonTheme.Muted;
        var rollLabel = this.awaitingLocalRoll
            ? "  WAITING FOR ROLL…  ##rollball"
            : onCooldown
                ? $"  WAIT {cooldownMs / 1000f:0.0}s  ##rollball"
                : localDone
                    ? "  GAME OVER  ##rollball"
                    : "  ROLL BALL  ##rollball";
        ImGui.BeginDisabled(!canRoll);
        var clickedRoll = NeonTheme.GlowButton(rollLabel, rollColor);
        ImGui.EndDisabled();
        if (clickedRoll && canRoll)
        {
            this.rollClickArmed = false;
            this.awaitingLocalRoll = true;
            this.pendingChat = this.RollCommand();
            this.pendingChatAt = Environment.TickCount64 + Random.Shared.Next(100, 301);
            this.status = $"Bowling… {local!.Game.RemainingPins} pin(s) still standing. Make it dramatic.";
        }

        this.FlushPendingChat();

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  NEW GAME  ", NeonTheme.Magenta))
        {
            this.lanes.ResetVisible();
            this.awaitingLocalRoll = false;
            this.rollClickArmed = true;
            this.pendingChat = null;
            this.commandReadyAt = 0;
            this.status = "Fresh lanes. Don't choke.";
        }

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  CARRD  ", NeonTheme.Gold))
            Util.OpenLink("https://neoncelestial.carrd.co/");

        ImGui.PopStyleColor();
        this.labelFont.Pop();
        NeonTheme.GlowSeparator();

        this.labelFont.Push();
        ImGui.TextColored(NeonTheme.Cyan, "ADD A BOWLER");
        ImGui.SetNextItemWidth(280);
        var addGuest = ImGui.InputTextWithHint("##guest_name", "First Last", ref this.guestName, 40, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (NeonTheme.GlowButton("  ADD  ", NeonTheme.Cyan) || addGuest)
            this.TryAddGuest();
        this.labelFont.Pop();

        NeonTheme.GlowSeparator();

        if (ImGui.BeginChild("neon_lanes", new Vector2(0, -42), true))
        {
            ulong? removeId = null;
            string? removedName = null;
            foreach (var lane in this.lanes.Visible)
            {
                if (this.DrawLane(lane, onApproach, out var drop) && drop)
                {
                    removeId = lane.ContentId;
                    removedName = lane.Name;
                    break;
                }
            }

            if (removeId is ulong id && removedName is not null)
            {
                this.lanes.TryRemoveGuest(id);
                this.status = $"{removedName} left the lanes.";
            }
        }

        ImGui.EndChild();
        NeonTheme.StatusBar(this.status);
        NeonTheme.Pop();
    }

    private void SyncRoster() =>
        this.lanes.Sync(this.partyList, this.objectTable.LocalPlayer, this.playerState.ContentId);

    private BowlerLane? LocalLane()
    {
        foreach (var lane in this.lanes.Visible)
        {
            if (lane.IsLocal)
                return lane;
        }

        return null;
    }

    private void FlushPendingChat()
    {
        if (this.pendingChat is null || Environment.TickCount64 < this.pendingChatAt)
            return;

        if (!NclVenue.IsAt(this.playerState))
        {
            this.pendingChat = null;
            this.awaitingLocalRoll = false;
            return;
        }

        var command = this.pendingChat;
        this.pendingChat = null;
        GameChat.SendCommand(command);
        this.commandReadyAt = Environment.TickCount64 + 1200;
    }

    private static bool IsCommandRestricted(string text) =>
        text.Contains("Consecutive text command input is currently restricted", StringComparison.OrdinalIgnoreCase);

    private string RollCommand()
    {
        // Guests may not be in the party, so those games stay on world /random.
        // A recognized party with no guests uses /dice party so every client sees the throw.
        if (this.lanes.HasManualGuest || this.partyList.Length <= 1)
            return "/random 11";

        return "/dice party 11";
    }

    private string RosterHeadline()
    {
        var count = this.lanes.PresentCount;
        if (count <= 1)
            return "LANES  ·  solo (party members and guests can be added)";
        return $"LANES  ·  {count} bowlers (party + guests)";
    }

    private void TryAddGuest()
    {
        if (!this.lanes.TryAddGuest(this.guestName, out var error))
        {
            this.status = error ?? "Could not add that bowler.";
            return;
        }

        this.status = $"{this.guestName.Trim()} has a lane. We'll score their /random 11.";
        this.guestName = string.Empty;
    }

    private bool DrawLane(BowlerLane lane, BowlerLane? onApproach, out bool removeGuest)
    {
        removeGuest = false;
        var game = lane.Game;
        var isTurn = onApproach is not null && ReferenceEquals(onApproach, lane);
        var title = lane.Name;
        if (lane.IsLocal)
            title += "  ·  YOU";
        if (lane.IsLeader)
            title += "  ·  LEADER";
        if (lane.IsManual)
            title += "  ·  GUEST";
        if (isTurn)
            title += "  ·  UP";

        NeonTheme.BeginLaneCard(lane.ContentId, isTurn);

        if (lane.IsManual)
        {
            if (ImGui.SmallButton($"Remove##rm_{lane.ContentId}"))
            {
                NeonTheme.EndLaneCard(isTurn);
                removeGuest = true;
                return true;
            }

            ImGui.SameLine();
        }

        ImGui.PushStyleColor(ImGuiCol.Header, isTurn ? NeonTheme.CurrentFrame : NeonTheme.Header);
        var open = ImGui.CollapsingHeader($"{title}###lane_{lane.ContentId}", ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.PopStyleColor();

        ImGui.TextColored(NeonTheme.Lavender,
            game.IsComplete
                ? $"Frame 10 (finished)  ·  standing: —  ·  {this.PreviousFrameSummary(game)}"
                : $"Frame {game.CurrentFrameNumber} / 10  ·  ball {game.RollsInCurrentFrame + 1}  ·  standing: {game.RemainingPins}");
        ImGui.SameLine();
        ImGui.TextColored(NeonTheme.Gold, $"  Score {game.TotalScore}");

        if (open)
            this.DrawScorecard(game, $"card_{lane.ContentId}");

        NeonTheme.EndLaneCard(isTurn);
        return false;
    }

    private void DrawScorecard(BowlingGame game, string id)
    {
        const ImGuiTableFlags flags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.SizingStretchSame;

        if (!ImGui.BeginTable(id, 11, flags))
            return;

        for (var i = 1; i <= 10; i++)
            ImGui.TableSetupColumn($"F{i}");
        ImGui.TableSetupColumn("Total");
        ImGui.TableHeadersRow();

        var frames = game.GetFrames();

        ImGui.TableNextRow();
        foreach (var frame in frames)
        {
            ImGui.TableNextColumn();
            if (frame.IsCurrent && !game.IsComplete)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(NeonTheme.CurrentFrame));

            var marks = FormatRolls(frame);
            if (string.IsNullOrEmpty(marks))
                ImGui.Dummy(new Vector2(1f, 1f));
            else
                ImGui.TextUnformatted(marks);
        }

        ImGui.TableNextColumn();
        ImGui.Text(string.Empty);

        ImGui.TableNextRow();
        foreach (var frame in frames)
        {
            ImGui.TableNextColumn();
            ImGui.Text(frame.CumulativeScore?.ToString() ?? string.Empty);
        }

        ImGui.TableNextColumn();
        ImGui.TextColored(NeonTheme.Gold, game.TotalScore.ToString());

        ImGui.EndTable();
    }

    private string PreviousFrameSummary(BowlingGame game)
    {
        if (game.CurrentFrameNumber <= 1)
            return "Prev: —";

        var frame = game.GetFrames()[game.CurrentFrameNumber - 2];
        var rolls = FormatRolls(frame);
        if (string.IsNullOrWhiteSpace(rolls))
            return "Prev: —";

        var mark = frame.IsStrike ? "  Strike" : frame.IsSpare ? "  Spare" : string.Empty;
        return $"Prev: {rolls}{mark}";
    }

    private static string FormatRolls(FrameView frame)
    {
        if (frame.Number == 10)
            return JoinRolls(frame.Roll1, frame.Roll2, frame.Roll3);

        return JoinRolls(frame.Roll1, frame.Roll2);
    }

    private static string JoinRolls(params string[] parts)
    {
        var filled = Array.FindAll(parts, static p => !string.IsNullOrEmpty(p));
        return filled.Length == 0 ? string.Empty : string.Join("  ", filled);
    }

    public void Dispose()
    {
        this.labelFont.Dispose();
    }
}
