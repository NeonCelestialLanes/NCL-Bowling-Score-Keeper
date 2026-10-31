using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using System.IO;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
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
    [PluginService] internal IDataManager DataManager { get; init; } = null!;
    [PluginService] internal IUnlockState UnlockState { get; init; } = null!;

    private BowlingWindow Window { get; set; } = null!;
    private WindowSystem WindowSystem { get; } = new("FFXIVBowlingSystem");

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IPartyList partyList,
        IObjectTable objectTable,
        IPlayerState playerState,
        ITextureProvider textureProvider,
        IDataManager dataManager,
        IUnlockState unlockState)
    {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;
        this.ChatGui = chatGui;
        this.PartyList = partyList;
        this.ObjectTable = objectTable;
        this.PlayerState = playerState;
        this.TextureProvider = textureProvider;
        this.DataManager = dataManager;
        this.UnlockState = unlockState;

        this.Window = new BowlingWindow(pluginInterface, partyList, objectTable, playerState, textureProvider, dataManager, unlockState);
        this.WindowSystem.AddWindow(this.Window);

        if (this.CommandManager.Commands.ContainsKey(CommandName))
            this.CommandManager.RemoveHandler(CommandName);

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
        @"(?:(?:Random|Dice)!?\s*)?(?:(?<you>You)\s+roll\s+a|(?<name>.+?)\s+rolls\s+a)\D*(?<pins>\d+)\s*\(\s*(?:out of\s*(?<die>\d+)|0\s*[-–]\s*(?<die2>\d+))\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PartyDice = new(
        @"(?:\((?<paren>[^)]+)\)\s*)?(?:Random|Dice)!?\s*\(\s*\d+\s*[-–]\s*(?<die>\d+)\s*\)\D*(?<pins>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string message, string? sender, out string? rolledName, out bool isYou, out int pins, out int dieSides)
    {
        rolledName = null;
        isYou = false;
        pins = 0;
        dieSides = 0;

        var haystack = string.IsNullOrWhiteSpace(sender) ? message : $"{sender} {message}";
        var match = WorldRoll.Match(message);
        if (match.Success && TryReadPins(match.Groups["pins"].Value, out pins))
        {
            dieSides = ReadDie(match);
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

        dieSides = ReadDie(match);
        if (match.Groups["paren"].Success)
            rolledName = match.Groups["paren"].Value.Trim();
        else if (!string.IsNullOrWhiteSpace(sender))
            rolledName = sender.Trim();

        return true;
    }

    private static int ReadDie(Match match)
    {
        if (match.Groups["die"].Success && int.TryParse(match.Groups["die"].Value, out var die) && die > 0)
            return die;
        if (match.Groups["die2"].Success && int.TryParse(match.Groups["die2"].Value, out die) && die > 0)
            return die;
        return 0;
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
    private readonly ThrowEmote throwEmote;
    private readonly IFontHandle labelFont;
    private readonly IFontHandle tabFont;
    private readonly ISharedImmediateTexture? logo;
    private readonly ISharedImmediateTexture? ballPin;
    private readonly ISharedImmediateTexture? dartsArt;
    private readonly ISharedImmediateTexture? billiardsArt;
    private readonly ISharedImmediateTexture? jukeboxArt;
    private readonly PartyLanes lanes = new();
    private string status = "Walk up. Tell us you're ready to dominate the lanes.";
    private string guestName = string.Empty;
    private bool awaitingLocalRoll;
    private bool rollClickArmed = true;
    private string lastRollKey = string.Empty;
    private long lastRollTicks;
    private readonly List<(string Command, long At)> outgoing = new();
    private readonly Dictionary<ulong, bool> sheetOpen = new();
    private long commandReadyAt;
    private int markPins;
    private ulong? lastScoredId;
    private bool awaitingLocalDart;
    private bool dartClickArmed = true;
    private int houseTab;
    private bool compactHud;
    private Vector2 savedWindowSize = new(860, 900);
    private Vector2 liveWindowSize = new(860, 900);
    private Vector2? pendingSize;
    private Vector2? holdUntilSize;
    private string lastDartKey = string.Empty;
    private long lastDartTicks;
    private int markDart = 2;
    private ulong? lastDartId;
    private readonly ISharedImmediateTexture? menuFood;
    private readonly ISharedImmediateTexture? menuDrink;
    private readonly NclHours hours = new();
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly NclConfig config;
    private long lastSizeSave;
    private const string CarrdUrl = "https://neoncelestial.carrd.co/";
    private const string BilliardsUrl = "https://bloob.io/8ballpool";
    private const string AetherphoneUrl = "https://aetherphone.net/";

    public BowlingWindow(
        IDalamudPluginInterface pluginInterface,
        IPartyList partyList,
        IObjectTable objectTable,
        IPlayerState playerState,
        ITextureProvider textureProvider,
        IDataManager dataManager,
        IUnlockState unlockState)
        : base("NCL Bowling Score", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.pluginInterface = pluginInterface;
        this.config = pluginInterface.GetPluginConfig() as NclConfig ?? new NclConfig();
        this.partyList = partyList;
        this.objectTable = objectTable;
        this.playerState = playerState;
        this.lifestream = new LifestreamTravel(pluginInterface);
        this.throwEmote = new ThrowEmote(dataManager, unlockState);
        var dir = pluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;
        var fontPath = Path.Combine(dir, "Assets", "LuckiestGuy-Regular.ttf");
        var logoPath = Path.Combine(dir, "Assets", "ncl-logo.png");
        var ballPinPath = Path.Combine(dir, "Assets", "ncl-ball-pin.png");
        var dartsArtPath = Path.Combine(dir, "Assets", "ncl-darts-neon.png");
        var billiardsArtPath = Path.Combine(dir, "Assets", "ncl-billiards-neon.png");
        var jukeboxArtPath = Path.Combine(dir, "Assets", "ncl-jukebox-neon.png");
        var foodPath = Path.Combine(dir, "Assets", "menu-food.jpg");
        var drinkPath = Path.Combine(dir, "Assets", "menu-drink.jpg");
        this.labelFont = pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(toolkit =>
        {
            if (File.Exists(fontPath))
                toolkit.Font = toolkit.AddFontFromFile(fontPath, new SafeFontConfig { SizePx = 17 });
            else
                toolkit.AddDalamudDefaultFont(17);
        }));
        this.tabFont = pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(toolkit =>
        {
            if (File.Exists(fontPath))
                toolkit.Font = toolkit.AddFontFromFile(fontPath, new SafeFontConfig { SizePx = 15 });
            else
                toolkit.AddDalamudDefaultFont(15);
        }));
        this.logo = File.Exists(logoPath) ? textureProvider.GetFromFile(logoPath) : null;
        this.ballPin = File.Exists(ballPinPath) ? textureProvider.GetFromFile(ballPinPath) : null;
        this.dartsArt = File.Exists(dartsArtPath) ? textureProvider.GetFromFile(dartsArtPath) : null;
        this.billiardsArt = File.Exists(billiardsArtPath) ? textureProvider.GetFromFile(billiardsArtPath) : null;
        this.jukeboxArt = File.Exists(jukeboxArtPath) ? textureProvider.GetFromFile(jukeboxArtPath) : null;
        this.menuFood = File.Exists(foodPath) ? textureProvider.GetFromFile(foodPath) : null;
        this.menuDrink = File.Exists(drinkPath) ? textureProvider.GetFromFile(drinkPath) : null;
        this.savedWindowSize = EvenSize(new Vector2(this.config.FullWidth, this.config.FullHeight));
        this.liveWindowSize = this.savedWindowSize;
        this.Size = this.savedWindowSize;
        this.pendingSize = this.savedWindowSize;
        this.SizeCondition = ImGuiCond.Always;
        this.SizeConstraints = FullWindowLimits;
    }

    private static readonly WindowSizeConstraints FullWindowLimits = new()
    {
        MinimumSize = new Vector2(540, 280),
        MaximumSize = new Vector2(1400, 980)
    };

    private static readonly WindowSizeConstraints CompactWindowLimits = new()
    {
        MinimumSize = new Vector2(420, 200),
        MaximumSize = new Vector2(900, 720)
    };

    public override void PreDraw()
    {
        this.SizeConstraints = this.compactHud ? CompactWindowLimits : FullWindowLimits;
        var target = this.pendingSize ?? this.holdUntilSize;
        if (target is Vector2 size)
        {
            this.Size = size;
            this.SizeCondition = ImGuiCond.Always;
            this.holdUntilSize = size;
            this.pendingSize = null;
        }
        else
        {
            this.SizeCondition = ImGuiCond.Appearing;
        }
    }

    public bool HandleChat(string text, string? sender = null)
    {
        this.SyncRoster();
        if (!NclVenue.IsAt(this.playerState))
            return false;

        if (IsCommandRestricted(text))
        {
            this.awaitingLocalRoll = false;
            this.awaitingLocalDart = false;
            this.outgoing.Clear();
            this.commandReadyAt = Environment.TickCount64 + 1500;
            this.status = "The game blocked that throw. Wait it out, then roll again.";
            return true;
        }

        if (!RollParser.TryParse(text, sender, out var rolledName, out var isYou, out var pins, out var dieSides))
            return false;

        if (dieSides == 10 || (dieSides == 0 && this.awaitingLocalDart))
            return this.ApplyHeardDart(rolledName, sender, text, isYou, pins);

        return this.ApplyHeardBowl(rolledName, sender, text, isYou, pins);
    }

    public override void Draw()
    {
        this.SyncRoster();
        this.FlushPendingChat();
        this.liveWindowSize = ImGui.GetWindowSize();
        if (this.holdUntilSize is Vector2 target)
        {
            ImGui.SetWindowSize(target);
            if (MathF.Abs(this.liveWindowSize.X - target.X) < 8f && MathF.Abs(this.liveWindowSize.Y - target.Y) < 8f)
                this.holdUntilSize = null;
        }
        else if (!this.compactHud)
        {
            this.RememberFullSize(this.liveWindowSize);
        }

        NeonTheme.Push();
        NeonTheme.DrawBackdrop();

        if (this.compactHud)
        {
            this.DrawCompactHud();
            NeonTheme.Pop();
            return;
        }

        NeonTheme.BrandHeader(this.logo?.GetWrapOrEmpty(), this.labelFont, this.hours.Line());

        this.tabFont.Push();
        this.houseTab = NeonTheme.GlowTabBar(["BOWLING", "DARTS", "BILLIARDS", "JUKEBOX", "MENU", "CARRD"], this.houseTab);
        this.tabFont.Pop();

        var pinStatus = this.houseTab is 0 or 1;
        const float barH = 34f;
        var avail = ImGui.GetContentRegionAvail().Y;
        var gap = ImGui.GetStyle().ItemSpacing.Y;
        var bodyH = pinStatus ? MathF.Max(1f, avail - barH - gap) : MathF.Max(1f, avail);

        ImGui.BeginChild("ncl_body", new Vector2(0f, bodyH), false, ImGuiWindowFlags.NoScrollbar);
        ImGui.Dummy(new Vector2(0f, 8f));
        switch (this.houseTab)
        {
            case 1:
                if (NclVenue.IsAt(this.playerState))
                    this.DrawDartsTab();
                else
                    this.DrawVenueLock("then throw.");
                break;
            case 2:
                this.DrawBilliardsTab();
                break;
            case 3:
                this.DrawJukeboxTab();
                break;
            case 4:
                this.DrawMenuTab();
                break;
            case 5:
                this.DrawCarrdTab();
                break;
            default:
                if (NclVenue.IsAt(this.playerState))
                    this.DrawBowlingTab();
                else
                    this.DrawVenueLock("then bowl.");
                break;
        }

        ImGui.EndChild();
        if (pinStatus)
            NeonTheme.StatusBar(this.status);
        NeonTheme.Pop();
    }

    private void SetCompact(bool on)
    {
        if (on == this.compactHud)
            return;

        if (on)
        {
            this.RememberFullSize(this.liveWindowSize);
            if (this.houseTab > 1)
                this.houseTab = 0;
            this.pendingSize = this.MiniWindowSize();
        }
        else
        {
            this.pendingSize = this.savedWindowSize;
        }

        this.compactHud = on;
    }

    private void RememberFullSize(Vector2 size)
    {
        size = EvenSize(size);
        if (size.X < 200f || size.Y < 160f)
            return;

        this.savedWindowSize = size;
        var now = Environment.TickCount64;
        if (now - this.lastSizeSave < 400)
            return;
        if (Math.Abs(this.config.FullWidth - size.X) < 1f && Math.Abs(this.config.FullHeight - size.Y) < 1f)
            return;

        this.lastSizeSave = now;
        this.config.FullWidth = size.X;
        this.config.FullHeight = size.Y;
        this.pluginInterface.SavePluginConfig(this.config);
    }

    private static Vector2 EvenSize(Vector2 size) =>
        new(MathF.Round(size.X * 0.5f) * 2f, MathF.Round(size.Y * 0.5f) * 2f);

    private Vector2 MiniWindowSize()
    {
        var rows = Math.Max(1, this.lanes.PresentCount);
        var height = 188f + (rows * 50f);
        height = Math.Clamp(height, 240f, 380f);
        return new Vector2(540f, height);
    }

    private void DrawCompactHud()
    {
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  FULL  ", NeonTheme.Purple))
            this.SetCompact(false);
        ImGui.SameLine();
        if (NeonTheme.GlowButton(this.houseTab == 1 ? "  DARTS  " : "  BOWLING  ", NeonTheme.Cyan))
            this.houseTab = this.houseTab == 1 ? 0 : 1;
        ImGui.PopStyleColor();
        this.labelFont.Pop();

        if (!NclVenue.IsAt(this.playerState))
        {
            this.DrawVenueLock(this.houseTab == 1 ? "then throw." : "then bowl.");
            return;
        }

        if (this.houseTab == 1)
            this.DrawCompactDarts();
        else
            this.DrawCompactBowling();

        ImGui.TextColored(NeonTheme.Muted, this.status);
    }

    private void DrawCompactBowling()
    {
        var local = this.LocalLane();
        var onApproach = this.lanes.WhoseTurn();
        var localDone = local?.Game.IsComplete == true;
        var localTurn = local is not null && onApproach is not null && ReferenceEquals(onApproach, local);
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            this.rollClickArmed = true;

        var cooldownMs = this.commandReadyAt - Environment.TickCount64;
        var onCooldown = cooldownMs > 0;
        var canRoll = localTurn && !localDone && !this.awaitingLocalRoll && this.rollClickArmed && !onCooldown;

        if (onApproach is not null)
            ImGui.TextColored(NeonTheme.Gold, $"Up: {onApproach.Name}");
        else if (this.lanes.PresentCount > 0)
            ImGui.TextColored(NeonTheme.Magenta, "That's the game.");

        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        var rollColor = this.awaitingLocalRoll || onCooldown ? NeonTheme.Gold : canRoll ? NeonTheme.Cyan : NeonTheme.Muted;
        var rollLabel = this.awaitingLocalRoll
            ? "  WAITING…  ##miniroll"
            : onCooldown
                ? $"  WAIT {cooldownMs / 1000f:0.0}s  ##miniroll"
                : localDone
                    ? "  GAME OVER  ##miniroll"
                    : !localTurn
                        ? "  WAIT  ##miniroll"
                        : "  ROLL BALL  ##miniroll";
        ImGui.BeginDisabled(!canRoll);
        var clicked = NeonTheme.GlowButton(rollLabel, rollColor);
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (NeonTheme.GlowButton("  NEW GAME  ##mininew", NeonTheme.Magenta))
        {
            this.lanes.ResetVisible();
            this.awaitingLocalRoll = false;
            this.rollClickArmed = true;
            this.outgoing.Clear();
            this.commandReadyAt = 0;
            this.lastScoredId = null;
            this.status = "Fresh lanes. Don't choke.";
        }

        ImGui.PopStyleColor();
        this.labelFont.Pop();
        if (clicked && canRoll)
        {
            this.rollClickArmed = false;
            this.awaitingLocalRoll = true;
            this.EnqueueChat(this.RollCommand());
            this.status = $"Bowling… {local!.Game.RemainingPins} standing.";
        }

        if (!ImGui.BeginChild("mini_lanes", new Vector2(0f, -22f), false))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var lane in this.lanes.Visible)
        {
            var isTurn = onApproach is not null && ReferenceEquals(onApproach, lane);
            var tag = lane.IsLocal ? "YOU" : lane.IsManual ? "GUEST" : string.Empty;
            var name = string.IsNullOrEmpty(tag) ? lane.Name : $"{lane.Name}  {tag}";
            ImGui.TextColored(isTurn ? NeonTheme.Gold : NeonTheme.Ice, name);
            NeonTheme.MiniSheet(lane.Game.GetFrames(), lane.Game.TotalScore, lane.Game.IsComplete);
        }

        ImGui.EndChild();
    }

    private void DrawCompactDarts()
    {
        var local = this.LocalLane();
        var onOche = this.lanes.WhoseDartTurn();
        var localDone = local?.Clock.IsComplete == true;
        var localTurn = local is not null && onOche is not null && ReferenceEquals(onOche, local);
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            this.dartClickArmed = true;

        var cooldownMs = this.commandReadyAt - Environment.TickCount64;
        var onCooldown = cooldownMs > 0;
        var canThrow = localTurn && !localDone && !this.awaitingLocalDart && this.dartClickArmed && !onCooldown;

        if (onOche is not null)
            ImGui.TextColored(NeonTheme.Gold, $"Up: {onOche.Name}  ·  {onOche.Clock.AimLabel}  ·  dart {onOche.Clock.DartsThisTurn + 1}/3");
        else if (this.lanes.PresentCount > 0)
            ImGui.TextColored(NeonTheme.Magenta, "Clock's done.");

        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        var throwColor = this.awaitingLocalDart || onCooldown ? NeonTheme.Gold : canThrow ? NeonTheme.Cyan : NeonTheme.Muted;
        var throwLabel = this.awaitingLocalDart
            ? "  WAITING…  ##minithrow"
            : onCooldown
                ? $"  WAIT {cooldownMs / 1000f:0.0}s  ##minithrow"
                : localDone
                    ? "  CLOCK DONE  ##minithrow"
                    : !localTurn
                        ? "  WAIT  ##minithrow"
                        : "  THROW DART  ##minithrow";
        ImGui.BeginDisabled(!canThrow);
        var clicked = NeonTheme.GlowButton(throwLabel, throwColor);
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (NeonTheme.GlowButton("  NEW CLOCK  ##miniclock", NeonTheme.Magenta))
        {
            this.lanes.ResetClocks();
            this.awaitingLocalDart = false;
            this.dartClickArmed = true;
            this.outgoing.Clear();
            this.commandReadyAt = 0;
            this.lastDartId = null;
            this.status = "Fresh clocks. Start at 1.";
        }

        ImGui.PopStyleColor();
        this.labelFont.Pop();
        if (clicked && canThrow)
        {
            this.dartClickArmed = false;
            this.awaitingLocalDart = true;
            this.EnqueueDartThrow();
            this.status = $"Darts… aiming {local!.Clock.AimLabel}.";
        }

        if (!ImGui.BeginChild("mini_clocks", new Vector2(0f, -22f), false))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var lane in this.lanes.Visible)
        {
            var isTurn = onOche is not null && ReferenceEquals(onOche, lane);
            var clock = lane.Clock;
            var line = clock.IsComplete
                ? $"{lane.Name}  ·  DONE"
                : $"{lane.Name}  ·  {clock.AimLabel}  ·  {clock.PointsEarned}/20";
            ImGui.TextColored(isTurn ? NeonTheme.Gold : NeonTheme.Ice, line);
            if (clock.LastTurnMarks.Count > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(NeonTheme.Gold, string.Join(" ", clock.LastTurnMarks));
            }
        }

        ImGui.EndChild();
    }

    private void DrawVenueLock(string verb)
    {
        this.outgoing.Clear();
        this.awaitingLocalRoll = false;
        this.awaitingLocalDart = false;
        this.labelFont.Push();
        ImGui.TextColored(NeonTheme.Magenta, "Scoring only works at Neon Celestial Lanes.");
        ImGui.TextColored(NeonTheme.Gold, NclVenue.Address);
        ImGui.TextColored(NeonTheme.Muted, $"Step inside the house — yard or lanes — {verb}");
        ImGui.Dummy(new Vector2(0, 8));
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  GO TO NCL  ", NeonTheme.Cyan))
            this.lifestream.TryGoToNcl(out this.status);
        ImGui.PopStyleColor();
        this.labelFont.Pop();
        if (this.status.StartsWith("Walk up", StringComparison.Ordinal))
            this.status = "Come bowl at NCL. " + NclVenue.Address + ".";
    }

    private void DrawBowlingTab()
    {
        var local = this.LocalLane();
        var onApproach = this.lanes.WhoseTurn();
        var localDone = local?.Game.IsComplete == true;
        var localTurn = local is not null && onApproach is not null && ReferenceEquals(onApproach, local);
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            this.rollClickArmed = true;

        var cooldownMs = this.commandReadyAt - Environment.TickCount64;
        var onCooldown = cooldownMs > 0;
        var canRoll = localTurn && !localDone && !this.awaitingLocalRoll && this.rollClickArmed && !onCooldown;

        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Cyan, this.RosterHeadline());
        this.tabFont.Pop();
        if (onApproach is not null)
            ImGui.TextColored(NeonTheme.Gold, $"On the approach: {onApproach.Name}");
        else if (this.lanes.PresentCount > 0)
            ImGui.TextColored(NeonTheme.Magenta, "That's the game. Bragging rights until someone beats you.");

        ImGui.Dummy(new Vector2(0f, 8f));
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        var rollColor = this.awaitingLocalRoll || onCooldown ? NeonTheme.Gold : canRoll ? NeonTheme.Cyan : NeonTheme.Muted;
        var rollLabel = this.awaitingLocalRoll
            ? "  WAITING FOR ROLL…  ##rollball"
            : onCooldown
                ? $"  WAIT {cooldownMs / 1000f:0.0}s  ##rollball"
                : localDone
                    ? "  GAME OVER  ##rollball"
                    : !localTurn
                        ? "  WAIT YOUR TURN  ##rollball"
                        : "  ROLL BALL  ##rollball";
        ImGui.BeginDisabled(!canRoll);
        var clickedRoll = NeonTheme.GlowButton(rollLabel, rollColor);
        ImGui.EndDisabled();
        if (clickedRoll && canRoll)
        {
            this.rollClickArmed = false;
            this.awaitingLocalRoll = true;
            this.EnqueueChat(this.RollCommand());
            this.status = $"Bowling… {local!.Game.RemainingPins} pin(s) still standing. Make it dramatic.";
        }

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  NEW GAME  ", NeonTheme.Magenta))
        {
            this.lanes.ResetVisible();
            this.awaitingLocalRoll = false;
            this.rollClickArmed = true;
            this.outgoing.Clear();
            this.commandReadyAt = 0;
            this.lastScoredId = null;
            this.status = "Fresh lanes. Don't choke.";
        }

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  RULES  ", NeonTheme.Gold))
            ImGui.OpenPopup("HOUSE RULES##ncl_bowl_rules");

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  MINI  ", NeonTheme.Purple))
            this.SetCompact(true);

        NeonTheme.DrawBallAndPin(this.ballPin?.GetWrapOrEmpty());

        ImGui.PopStyleColor();
        this.labelFont.Pop();

        this.DrawBowlingRulesPopup();
        this.DrawLaneFixes(onApproach);
        NeonTheme.GlowSeparator();
        this.DrawGuestRow();
        NeonTheme.GlowSeparator();
        this.DrawLaneList(onApproach);
    }

    private void DrawBowlingRulesPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(560, 0), ImGuiCond.Always);
        if (!ImGui.BeginPopupModal("HOUSE RULES##ncl_bowl_rules", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextColored(NeonTheme.Magenta, "NEON CELESTIAL LANES  ·  BOWLING FOR HUMANS");
        ImGui.TextColored(NeonTheme.Muted, "Never bowled? Start here. One click is one throw. Wait your turn.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "The idea");
        ImGui.TextWrapped("There are 10 pins. A game is 10 frames. In each frame you get up to two throws. The number you roll is how many pins fell. We use /random 11 (or /dice party 11) so 0 through 11 can come up.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "What's on the scorecard");
        ImGui.BulletText("X  —  strike. You knocked down all 10 pins on the first ball. That frame is over. You do not throw a second ball.");
        ImGui.BulletText("/  —  spare. Your two balls in that frame add up to exactly 10. Example: 7 then 3.");
        ImGui.BulletText("-  —  gutter / miss. Zero pins. Either you rolled a 0, or you rolled more pins than were still standing.");
        ImGui.BulletText("A number (1–9)  —  that many pins fell on that ball.");
        ImGui.TextWrapped("The bigger number under the marks is your running score. Strikes and spares get bonus points from later throws — the scorecard does that math.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "House rule: overshoot is a gutter");
        ImGui.TextWrapped("If you roll higher than the pins still standing, that ball is a gutter (0). The extra pins don't count. 11 is always a gutter on a first ball, because only 10 pins exist.");
        ImGui.BulletText("First ball 11 →  -   then you still get a second ball at a full rack.");
        ImGui.BulletText("First ball 6, then 8 (only 4 standing) →  6  -   not a spare.");
        ImGui.BulletText("First ball 7, then 3 →  7  /   that's a spare. Exactly 10.");
        ImGui.BulletText("First ball 7, then 4 (only 3 standing) →  7  -   gutter. Not a spare.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "Whose turn");
        ImGui.TextWrapped("Bowl in order. A strike ends your frame immediately — next player. After two balls (or a spare), next player. 10th frame: two balls, plus a third if you strike or spare.");
        ImGui.Dummy(new Vector2(0, 10));

        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  GOT IT  ", NeonTheme.Cyan))
            ImGui.CloseCurrentPopup();
        ImGui.PopStyleColor();
        ImGui.EndPopup();
    }

    private void DrawDartsTab()
    {
        var local = this.LocalLane();
        var onOche = this.lanes.WhoseDartTurn();
        var localDone = local?.Clock.IsComplete == true;
        var localTurn = local is not null && onOche is not null && ReferenceEquals(onOche, local);
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            this.dartClickArmed = true;

        var cooldownMs = this.commandReadyAt - Environment.TickCount64;
        var onCooldown = cooldownMs > 0;
        var canThrow = localTurn && !localDone && !this.awaitingLocalDart && this.dartClickArmed && !onCooldown;

        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Cyan, "AROUND THE CLOCK  ·  1–20, then bullseye  ·  3 darts each turn");
        this.tabFont.Pop();
        ImGui.TextColored(NeonTheme.Muted, "Even 2/4/6/8 hits a number. Odd 3/5/7/9 misses. 1 and 10 only count on bullseye.");
        if (onOche is not null)
            ImGui.TextColored(NeonTheme.Gold, $"On the oche: {onOche.Name}  ·  aiming {onOche.Clock.AimLabel}  ·  dart {onOche.Clock.DartsThisTurn + 1}/3");
        else if (this.lanes.PresentCount > 0)
            ImGui.TextColored(NeonTheme.Magenta, "Clock's done. First one to bullseye keeps the bragging rights.");

        ImGui.Dummy(new Vector2(0f, 8f));
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        var throwColor = this.awaitingLocalDart || onCooldown ? NeonTheme.Gold : canThrow ? NeonTheme.Cyan : NeonTheme.Muted;
        var throwLabel = this.awaitingLocalDart
            ? "  WAITING FOR DART…  ##throwdart"
            : onCooldown
                ? $"  WAIT {cooldownMs / 1000f:0.0}s  ##throwdart"
                : localDone
                    ? "  CLOCK DONE  ##throwdart"
                    : !localTurn
                        ? "  WAIT YOUR TURN  ##throwdart"
                        : "  THROW DART  ##throwdart";
        ImGui.BeginDisabled(!canThrow);
        var clickedThrow = NeonTheme.GlowButton(throwLabel, throwColor);
        ImGui.EndDisabled();
        if (clickedThrow && canThrow)
        {
            this.dartClickArmed = false;
            this.awaitingLocalDart = true;
            this.EnqueueDartThrow();
            this.status = $"Darts… aiming {local!.Clock.AimLabel}. Make it stick.";
        }

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  NEW CLOCK  ", NeonTheme.Magenta))
        {
            this.lanes.ResetClocks();
            this.awaitingLocalDart = false;
            this.dartClickArmed = true;
            this.outgoing.Clear();
            this.commandReadyAt = 0;
            this.lastDartId = null;
            this.status = "Fresh clocks. Start at 1.";
        }

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  RULES  ", NeonTheme.Gold))
            ImGui.OpenPopup("HOUSE RULES##ncl_dart_rules");

        ImGui.SameLine();
        if (NeonTheme.GlowButton("  MINI  ", NeonTheme.Purple))
            this.SetCompact(true);

        NeonTheme.DrawBallAndPin(this.dartsArt?.GetWrapOrEmpty(), 24f);

        ImGui.PopStyleColor();
        this.labelFont.Pop();

        this.DrawDartsRulesPopup();
        this.DrawDartFixes(onOche);
        NeonTheme.GlowSeparator();
        this.DrawGuestRow();
        NeonTheme.GlowSeparator();
        this.DrawDartList(onOche);
    }

    private void DrawDartsRulesPopup()
    {
        const float rulesWidth = 600f;
        ImGui.SetNextWindowSize(new Vector2(rulesWidth, 0), ImGuiCond.Always);
        if (!ImGui.BeginPopupModal("HOUSE RULES##ncl_dart_rules", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + rulesWidth - 36f);

        ImGui.TextColored(NeonTheme.Magenta, "NEON CELESTIAL LANES  ·  DARTS FOR HUMANS");
        ImGui.TextColored(NeonTheme.Muted, "This is not real-world darts. You are not aiming at a board. You're collecting points in order.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "The idea");
        ImGui.TextWrapped("Around the Clock, easy mode: earn points 1 through 20 in order, then hit the bullseye. First person to finish wins. Your progress never resets. If you end a turn working on 7, next turn you start on 7 — you do not go back to 1.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "A turn");
        ImGui.TextWrapped("When it's your turn you throw 3 darts. Each dart is one click. If you have the /throw emote, THROW DART plays that first, then rolls /random 10 (or /dice party 10). No throw emote? Just the roll. Hits let you keep going up the numbers on that same turn until you miss or run out of darts.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "What the roll means (numbers 1–20)");
        RulesBullet("2, 4, 6, or 8  —  hit. You earn that point and immediately move to the next number.");
        RulesHangBullet("3, 5, 7, or 9  —  ", "miss. You stay on the same number. Throw the rest of your darts if you have any left.");
        RulesBullet("1 or 10  —  miss while you're on 1–20. Those two numbers are only for bullseye.");
        ImGui.Dummy(new Vector2(0, 6));

        ImGui.TextColored(NeonTheme.Gold, "Bullseye (after 20)");
        ImGui.TextWrapped("Once you've earned 1 through 20, you only need the bullseye. Now 1 or 10 is a hit and you win. 2–9 miss.");
        ImGui.Dummy(new Vector2(0, 6));

        ImGui.TextColored(NeonTheme.Gold, "What's on the scorecard");
        RulesBullet("The highlighted number is what you're aiming at right now.");
        RulesBullet("Filled numbers are points you've already earned. They stay.");
        RulesBullet("BULL is last. Finish that and the clock is done.");
        ImGui.Dummy(new Vector2(0, 4));
        ImGui.TextWrapped("Example: you're on 1, roll a 4 (hit) → you have 1. Next dart is for 2. Roll a 6 (hit) → you have 2. If you stop on 3, you stay on 3 next turn.");
        ImGui.Dummy(new Vector2(0, 10));

        ImGui.PopTextWrapPos();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  GOT IT  ", NeonTheme.Cyan))
            ImGui.CloseCurrentPopup();
        ImGui.PopStyleColor();
        ImGui.EndPopup();
    }

    private static void RulesBullet(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
    }

    private static void RulesHangBullet(string lead, string rest)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextUnformatted(lead);
        ImGui.SameLine(0, 0);
        ImGui.TextWrapped(rest);
    }

    private void DrawBilliardsTab()
    {
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        const float artW = 328f;
        NeonTheme.DrawOverlayArt(
            this.billiardsArt?.GetWrapOrEmpty(),
            new Vector2(origin.X + avail - artW - 38f, origin.Y + 200f),
            artW);

        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Gold, "NEON CELESTIAL LANES  ·  BILLIARDS");
        this.tabFont.Pop();
        ImGui.TextColored(NeonTheme.Muted, "Standard 8-ball. Play on the site or your Aetherphone, hang at the in-game table.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Cyan, "How it works");
        ImGui.TextWrapped("When you're ready to play, open bloob.io/8ballpool. One player can create a room and share the code, or you can join a public match together. Once you're connected, that's your table. Play your turns on the website — or on your Aetherphone if you have one — while roleplaying at the in-game pool table for the full immersive vibe.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Cyan, "House rules");
        ImGui.BulletText("Standard 8-Ball rules apply.");
        ImGui.BulletText("Call your shots if you're feeling fancy.");
        ImGui.BulletText("No rage quitting (we will absolutely notice).");
        ImGui.BulletText("Winner gets bragging rights until someone beats them.");
        ImGui.Dummy(new Vector2(0, 4));
        ImGui.TextColored(NeonTheme.Ice, "Simple. Smooth. Slightly competitive.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Cyan, "Rentals");
        ImGui.BulletText("Single match — 2 players, one full 8-ball game.");
        ImGui.BulletText("Private table — reserve ahead via Discord, 2-hour block during open hours.");
        ImGui.Dummy(new Vector2(0, 12));

        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  OPEN TABLE  ", NeonTheme.Gold))
            Util.OpenLink(BilliardsUrl);
        ImGui.PopStyleColor();
        this.labelFont.Pop();
        ImGui.Dummy(new Vector2(0, 8));
        ImGui.TextColored(NeonTheme.Lavender, BilliardsUrl);
    }

    private void DrawJukeboxTab()
    {
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        const float artW = 248f;
        NeonTheme.DrawOverlayArt(
            this.jukeboxArt?.GetWrapOrEmpty(),
            new Vector2(origin.X + avail - artW - 36f, origin.Y + 268f),
            artW);

        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Magenta, "NEON CELESTIAL LANES  ·  JUKEBOX");
        this.tabFont.Pop();
        ImGui.TextColored(NeonTheme.Muted, "We broadcast on MogCast. That's the watch-party app on Aetherphone.");
        ImGui.Dummy(new Vector2(0, 8));

        ImGui.TextColored(NeonTheme.Gold, "How to listen");
        ImGui.TextWrapped("The house screen is on MogCast, so everyone hears and sees the same thing. Tune in on your Aetherphone and you're on the broadcast with the rest of the house.");
        ImGui.Dummy(new Vector2(0, 6));
        ImGui.BulletText("If you have an Aetherphone: type /phone, open MogCast, and join the NCL broadcast.");
        ImGui.BulletText("Look for the in-world screen at the lanes. Playback stays in sync.");
        ImGui.BulletText("No phone yet? Grab Aetherphone, then come back and hop on MogCast.");
        ImGui.Dummy(new Vector2(0, 12));

        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  GET AETHERPHONE  ", NeonTheme.Magenta))
            Util.OpenLink(AetherphoneUrl);
        ImGui.PopStyleColor();
        this.labelFont.Pop();
        ImGui.Dummy(new Vector2(0, 8));
        ImGui.TextColored(NeonTheme.Lavender, "aetherphone.net  ·  /phone  ·  MogCast");
    }

    private void DrawMenuTab()
    {
        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Cyan, "FOOD  ·  DRINK");
        this.tabFont.Pop();
        ImGui.TextColored(NeonTheme.Muted, "Straight from the NCL Carrd. Tell the bar what you want.");
        if (!ImGui.BeginChild("ncl_menus", new Vector2(0, -8), true))
        {
            ImGui.EndChild();
            return;
        }

        var food = this.menuFood?.GetWrapOrEmpty();
        var drink = this.menuDrink?.GetWrapOrEmpty();
        var avail = ImGui.GetContentRegionAvail();
        var gap = 14f;
        var colW = MathF.Max(180f, (avail.X - gap) * 0.5f);
        this.DrawMenuPage(food, colW);
        ImGui.SameLine(0, gap);
        this.DrawMenuPage(drink, colW);
        ImGui.EndChild();
    }

    private void DrawMenuPage(IDalamudTextureWrap? wrap, float width)
    {
        if (wrap is null || wrap.Handle == nint.Zero || wrap.Width <= 0)
        {
            ImGui.BeginChild("menu_missing" + width, new Vector2(width, 240), true);
            ImGui.TextColored(NeonTheme.Muted, "Menu art didn't load. Use the Carrd tab.");
            ImGui.EndChild();
            return;
        }

        var height = width * wrap.Height / wrap.Width;
        ImGui.Image(wrap.Handle, new Vector2(width, height));
    }

    private void DrawCarrdTab()
    {
        this.tabFont.Push();
        ImGui.TextColored(NeonTheme.Magenta, "NEON CELESTIAL LANES");
        this.tabFont.Pop();
        ImGui.TextColored(NeonTheme.Gold, NclVenue.Address);
        ImGui.TextColored(NeonTheme.Muted, "Bowling, billiards, darts, menu, gallery — the whole house lives on Carrd.");
        ImGui.Dummy(new Vector2(0, 12));
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);
        if (NeonTheme.GlowButton("  OPEN CARRD  ", NeonTheme.Gold))
            Util.OpenLink(CarrdUrl);
        ImGui.SameLine();
        if (NeonTheme.GlowButton("  GO TO NCL  ", NeonTheme.Cyan))
            this.lifestream.TryGoToNcl(out this.status);
        ImGui.PopStyleColor();
        this.labelFont.Pop();
        ImGui.Dummy(new Vector2(0, 8));
        ImGui.TextColored(NeonTheme.Lavender, CarrdUrl);
    }

    private void DrawGuestRow()
    {
        this.labelFont.Push();
        ImGui.TextColored(NeonTheme.Cyan, "ADD A PLAYER");
        ImGui.SetNextItemWidth(280);
        var addGuest = ImGui.InputTextWithHint("##guest_name", "First Last", ref this.guestName, 40, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if (NeonTheme.GlowButton("  ADD  ", NeonTheme.Cyan) || addGuest)
            this.TryAddGuest();
        this.labelFont.Pop();
    }

    private void DrawLaneList(BowlerLane? onApproach)
    {
        if (!ImGui.BeginChild("neon_lanes", new Vector2(0, -8), true))
        {
            ImGui.EndChild();
            return;
        }

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

        ImGui.EndChild();
    }

    private void DrawDartList(BowlerLane? onOche)
    {
        if (!ImGui.BeginChild("neon_clocks", new Vector2(0, -8), true))
        {
            ImGui.EndChild();
            return;
        }

        ulong? removeId = null;
        string? removedName = null;
        foreach (var lane in this.lanes.Visible)
        {
            if (this.DrawDartLane(lane, onOche, out var drop) && drop)
            {
                removeId = lane.ContentId;
                removedName = lane.Name;
                break;
            }
        }

        if (removeId is ulong id && removedName is not null)
        {
            this.lanes.TryRemoveGuest(id);
            this.status = $"{removedName} left the oche.";
        }

        ImGui.EndChild();
    }

    private void SyncRoster() =>
        this.lanes.Sync(this.partyList, this.objectTable.LocalPlayer, this.playerState.ContentId);

    private bool SheetOpen(ulong key) =>
        !this.sheetOpen.TryGetValue(key, out var open) || open;

    private void ToggleSheet(ulong key) =>
        this.sheetOpen[key] = !this.SheetOpen(key);

    private BowlerLane? LocalLane()
    {
        foreach (var lane in this.lanes.Visible)
        {
            if (lane.IsLocal)
                return lane;
        }

        return null;
    }

    private void EnqueueChat(string command, long at)
    {
        this.outgoing.Add((command, at));
    }

    private void EnqueueChat(string command)
    {
        this.EnqueueChat(command, Environment.TickCount64 + Random.Shared.Next(100, 301));
    }

    private void EnqueueDartThrow()
    {
        var dice = this.DartCommand();
        var firstAt = Environment.TickCount64 + Random.Shared.Next(100, 301);
        if (this.throwEmote.IsUnlocked())
        {
            this.EnqueueChat("/throw", firstAt);
            this.EnqueueChat(dice, firstAt + 1300);
        }
        else
        {
            this.EnqueueChat(dice, firstAt);
        }
    }

    private void FlushPendingChat()
    {
        if (this.outgoing.Count == 0)
            return;

        var now = Environment.TickCount64;
        if (now < this.outgoing[0].At || now < this.commandReadyAt)
            return;

        if (!NclVenue.IsAt(this.playerState))
        {
            this.outgoing.Clear();
            this.awaitingLocalRoll = false;
            this.awaitingLocalDart = false;
            return;
        }

        var command = this.outgoing[0].Command;
        this.outgoing.RemoveAt(0);
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

    private string DartCommand()
    {
        if (this.lanes.HasManualGuest || this.partyList.Length <= 1)
            return "/random 10";

        return "/dice party 10";
    }

    private bool ApplyHeardBowl(string? rolledName, string? sender, string text, bool isYou, int pins)
    {
        var local = this.LocalLane();
        if (isYou && local is not null)
            rolledName = local.Name;

        var lane = this.lanes.FindBowler(rolledName, sender, text);
        if (lane is null && isYou)
            lane = local;

        if (lane is null)
        {
            this.status = $"Heard a roll ({pins}) from {rolledName ?? sender ?? "party chat"} but they're not on a lane.";
            return true;
        }

        var onApproach = this.lanes.WhoseTurn();
        if (onApproach is null || !ReferenceEquals(onApproach, lane))
        {
            if (lane.IsLocal)
                this.awaitingLocalRoll = false;

            var offTurn = lane.IsLocal ? "You" : lane.Name;
            var waitFor = onApproach is null ? "the game is over" : $"it's {onApproach.Name}'s turn";
            this.status = $"{offTurn} rolled {pins}, but {waitFor}. That throw doesn't count.";
            return true;
        }

        if (lane.IsLocal)
            this.awaitingLocalRoll = false;

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

        var who = lane.IsLocal ? "You" : lane.Name;
        var extra = overshot
            ? $" Rolled {pins} with fewer pins standing. Gutter."
            : string.Empty;
        this.lastScoredId = lane.ContentId;
        this.status = $"{who}: {lane.Game.DescribeLastMark(counted)}{extra}";
        if (!lane.Game.IsFrameInProgress)
        {
            var next = this.lanes.WhoseTurn();
            if (next is not null)
                this.status += $"  Next up: {next.Name}.";
        }

        return true;
    }

    private bool ApplyHeardDart(string? rolledName, string? sender, string text, bool isYou, int pins)
    {
        var local = this.LocalLane();
        if (isYou && local is not null)
            rolledName = local.Name;

        var lane = this.lanes.FindBowler(rolledName, sender, text);
        if (lane is null && isYou)
            lane = local;

        if (lane is null)
        {
            this.status = $"Heard a dart ({pins}) from {rolledName ?? sender ?? "party chat"} but they're not on the board.";
            return true;
        }

        var onOche = this.lanes.WhoseDartTurn();
        if (onOche is null || !ReferenceEquals(onOche, lane))
        {
            if (lane.IsLocal)
                this.awaitingLocalDart = false;

            var offTurn = lane.IsLocal ? "You" : lane.Name;
            var waitFor = onOche is null ? "the clock is done" : $"it's {onOche.Name}'s darts";
            this.status = $"{offTurn} threw {pins}, but {waitFor}. That dart doesn't count.";
            return true;
        }

        if (lane.IsLocal)
            this.awaitingLocalDart = false;

        var key = $"{lane.ContentId}:d:{pins}";
        var now = Environment.TickCount64;
        if (key == this.lastDartKey && now - this.lastDartTicks < 800)
            return true;

        this.lastDartKey = key;
        this.lastDartTicks = now;

        if (!this.lanes.TryApplyDart(lane, pins, out var hit, out var turnOver, out var error))
        {
            this.status = error ?? "Could not apply that dart.";
            return true;
        }

        this.lastDartId = lane.ContentId;
        var who = lane.IsLocal ? "You" : lane.Name;
        var mark = hit
            ? (lane.Clock.IsComplete ? "BULLSEYE — clock finished!" : $"HIT  ·  now aiming {lane.Clock.AimLabel}")
            : "miss";
        this.status = $"{who}: {pins} → {mark}";
        if (turnOver)
        {
            var next = this.lanes.WhoseDartTurn();
            if (next is not null)
                this.status += $"  Next up: {next.Name}.";
        }

        return true;
    }

    private string RosterHeadline()
    {
        var count = this.lanes.PresentCount;
        if (count <= 1)
            return "LANES  ·  solo (party members and guests can be added)";
        return $"LANES  ·  {count} bowlers (party + guests)";
    }

    private void DrawLaneFixes(BowlerLane? onApproach)
    {
        var canFix = onApproach is not null && !onApproach.Game.IsComplete;
        var canUndo = this.FindLastScoredLane() is not null;

        NeonTheme.BeginInsetBar();
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);

        ImGui.BeginDisabled(!canFix);
        if (NeonTheme.Chip("  GUTTER  ##gutter", NeonTheme.Magenta) && canFix)
            this.ApplyManualThrow(onApproach!, 0);
        ImGui.SameLine(0, 8f);
        if (NeonTheme.Chip("  PASS  ##pass", NeonTheme.Cyan) && canFix)
            this.PassOpenFrame(onApproach!);
        ImGui.SameLine(0, 8f);
        if (NeonTheme.Chip("  SKIP  ##fixskip", NeonTheme.Gold) && canFix)
            this.SkipAfk(onApproach!);
        ImGui.EndDisabled();

        ImGui.SameLine(0, 8f);
        ImGui.BeginDisabled(!canUndo);
        if (NeonTheme.Chip("  UNDO  ##undo", NeonTheme.Gold) && canUndo)
            this.UndoLastThrow();
        ImGui.EndDisabled();

        ImGui.SameLine(0, 8f);
        ImGui.SetNextItemWidth(52);
        ImGui.InputInt("##mark_pins", ref this.markPins, 0);
        if (this.markPins < 0)
            this.markPins = 0;
        if (this.markPins > 11)
            this.markPins = 11;

        ImGui.SameLine(0, 8f);
        ImGui.BeginDisabled(!canFix);
        if (NeonTheme.Chip("  MARK  ##mark", NeonTheme.Cyan) && canFix)
            this.ApplyManualThrow(onApproach!, this.markPins);
        ImGui.EndDisabled();

        ImGui.PopStyleColor();
        this.labelFont.Pop();
        NeonTheme.EndInsetBar();
        ImGui.TextColored(
            NeonTheme.Muted,
            onApproach is null
                ? "Gutter, pass, skip an AFK bowler, undo, or mark a missed throw."
                : $"Gutter, pass, skip, undo, or mark — only for {onApproach.Name} (on the approach).");
    }

    private void DrawDartFixes(BowlerLane? onOche)
    {
        var canFix = onOche is not null && !onOche.Clock.IsComplete;

        NeonTheme.BeginInsetBar();
        this.labelFont.Push();
        ImGui.PushStyleColor(ImGuiCol.Text, NeonTheme.Ice);

        ImGui.BeginDisabled(!canFix);
        if (NeonTheme.Chip("  MISS  ##dmiss", NeonTheme.Magenta) && canFix)
            this.ApplyManualDart(onOche!, 3);
        ImGui.SameLine(0, 8f);
        if (NeonTheme.Chip("  HIT  ##dhit", NeonTheme.Cyan) && canFix)
            this.ApplyManualDart(onOche!, onOche!.Clock.IsBullseye ? 10 : 2);
        ImGui.SameLine(0, 8f);
        if (NeonTheme.Chip("  SKIP  ##dfixskip", NeonTheme.Gold) && canFix)
            this.SkipDartAfk(onOche!);
        ImGui.EndDisabled();

        ImGui.SameLine(0, 8f);
        ImGui.SetNextItemWidth(52);
        ImGui.InputInt("##mark_dart", ref this.markDart, 0);
        if (this.markDart < 1)
            this.markDart = 1;
        if (this.markDart > 10)
            this.markDart = 10;

        ImGui.SameLine(0, 8f);
        ImGui.BeginDisabled(!canFix);
        if (NeonTheme.Chip("  MARK  ##dmark", NeonTheme.Cyan) && canFix)
            this.ApplyManualDart(onOche!, this.markDart);
        ImGui.EndDisabled();

        ImGui.PopStyleColor();
        this.labelFont.Pop();
        NeonTheme.EndInsetBar();
        ImGui.TextColored(
            NeonTheme.Muted,
            onOche is null
                ? "Hit, miss, skip an AFK thrower, or mark a /random 10 the scorecard missed."
                : $"Only {onOche.Name}'s darts — aiming {onOche.Clock.AimLabel}.");
    }

    private void ApplyManualDart(BowlerLane lane, int roll)
    {
        this.awaitingLocalDart = false;
        this.outgoing.Clear();
        if (!this.lanes.TryApplyDart(lane, roll, out var hit, out var turnOver, out var error))
        {
            this.status = error ?? "Could not mark that dart.";
            return;
        }

        this.lastDartId = lane.ContentId;
        var mark = hit
            ? (lane.Clock.IsComplete ? "BULLSEYE — clock finished!" : $"HIT  ·  now aiming {lane.Clock.AimLabel}")
            : "miss";
        this.status = $"{lane.Name}: {roll} → {mark}";
        if (turnOver)
        {
            var next = this.lanes.WhoseDartTurn();
            if (next is not null)
                this.status += $"  Next up: {next.Name}.";
        }
    }

    private void SkipDartAfk(BowlerLane lane)
    {
        this.awaitingLocalDart = false;
        this.outgoing.Clear();
        lane.Clock.SkipRestOfTurn();
        lane.SittingOut = true;
        var next = this.lanes.WhoseDartTurn();
        this.status = $"{lane.Name} is sitting out (AFK).";
        if (next is not null)
            this.status += $"  Next up: {next.Name}.";
    }

    private void ApplyManualThrow(BowlerLane lane, int pins)
    {
        this.awaitingLocalRoll = false;
        this.outgoing.Clear();
        if (!this.lanes.TryApplyRoll(lane, pins, out var counted, out var overshot, out var error))
        {
            this.status = error ?? "Could not mark that throw.";
            return;
        }

        this.lastScoredId = lane.ContentId;
        var extra = overshot
            ? $" Rolled {pins} with fewer pins standing. Gutter."
            : string.Empty;
        this.status = $"{lane.Name}: {lane.Game.DescribeLastMark(counted)}{extra}";
        if (!lane.Game.IsFrameInProgress)
        {
            var next = this.lanes.WhoseTurn();
            if (next is not null)
                this.status += $"  Next up: {next.Name}.";
        }
    }

    private void SkipAfk(BowlerLane lane)
    {
        this.awaitingLocalRoll = false;
        this.outgoing.Clear();
        if (lane.Game.IsFrameInProgress)
            lane.Game.TryPassFrame(out _, out _);

        lane.SittingOut = true;
        var next = this.lanes.WhoseTurn();
        this.status = $"{lane.Name} is sitting out (AFK).";
        if (next is not null)
            this.status += $"  Next up: {next.Name}.";
    }

    private void PassOpenFrame(BowlerLane lane)
    {
        this.awaitingLocalRoll = false;
        this.outgoing.Clear();
        if (!lane.Game.TryPassFrame(out var gutters, out var error))
        {
            this.status = error ?? "Could not pass that frame.";
            return;
        }

        this.lastScoredId = lane.ContentId;
        var next = this.lanes.WhoseTurn();
        this.status = $"{lane.Name} passed ({gutters} gutter{(gutters == 1 ? string.Empty : "s")}).";
        if (next is not null)
            this.status += $"  Next up: {next.Name}.";
    }

    private void UndoLastThrow()
    {
        var lane = this.FindLastScoredLane();
        if (lane is null)
        {
            this.status = "Nothing to undo.";
            return;
        }

        if (!lane.Game.TryUndo(out var error))
        {
            this.status = error ?? "Could not undo that throw.";
            return;
        }

        this.awaitingLocalRoll = false;
        this.outgoing.Clear();
        this.status = $"Undid the last ball on {lane.Name}'s lane.";
        if (lane.Game.Rolls.Count == 0)
            this.lastScoredId = null;
    }

    private BowlerLane? FindLastScoredLane()
    {
        if (this.lastScoredId is ulong id)
        {
            foreach (var lane in this.lanes.Visible)
            {
                if (lane.ContentId == id && lane.Game.Rolls.Count > 0)
                    return lane;
            }
        }

        BowlerLane? fallback = null;
        foreach (var lane in this.lanes.Visible)
        {
            if (lane.Game.Rolls.Count == 0)
                continue;
            fallback = lane;
        }

        return fallback;
    }

    private void TryAddGuest()
    {
        if (!this.lanes.TryAddGuest(this.guestName, out var error))
        {
            this.status = error ?? "Could not add that bowler.";
            return;
        }

        this.status = $"{this.guestName.Trim()} has a lane. We'll score their bowling /random 11 and darts /random 10.";
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
        if (lane.SittingOut)
            title += "  ·  AFK";
        else if (isTurn)
            title += "  ·  UP";

        NeonTheme.BeginLaneCard(lane.ContentId, isTurn);

        if (lane.SittingOut)
        {
            if (NeonTheme.Chip($"Back in##back_{lane.ContentId}", NeonTheme.Cyan))
            {
                lane.SittingOut = false;
                this.status = $"{lane.Name} is back on the approach.";
            }

            ImGui.SameLine(0, 8f);
        }
        else if (!lane.Game.IsComplete)
        {
            if (NeonTheme.Chip($"Skip##skip_{lane.ContentId}", NeonTheme.Gold))
            {
                this.SkipAfk(lane);
                NeonTheme.EndLaneCard(isTurn);
                return false;
            }

            ImGui.SameLine(0, 8f);
        }

        if (lane.IsManual)
        {
            if (NeonTheme.Chip($"Remove##rm_{lane.ContentId}", NeonTheme.Magenta))
            {
                NeonTheme.EndLaneCard(isTurn);
                removeGuest = true;
                return true;
            }

            ImGui.SameLine(0, 8f);
        }

        this.tabFont.Push();
        if (NeonTheme.NamePlate($"lane_name_{lane.ContentId}", title, NeonTheme.Purple, ImGui.GetContentRegionAvail().X))
            this.ToggleSheet(lane.ContentId);
        this.tabFont.Pop();
        var open = this.SheetOpen(lane.ContentId);

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

    private bool DrawDartLane(BowlerLane lane, BowlerLane? onOche, out bool removeGuest)
    {
        removeGuest = false;
        var clock = lane.Clock;
        var isTurn = onOche is not null && ReferenceEquals(onOche, lane);
        var title = lane.Name;
        if (lane.IsLocal)
            title += "  ·  YOU";
        if (lane.IsManual)
            title += "  ·  GUEST";
        if (lane.SittingOut)
            title += "  ·  AFK";
        else if (isTurn)
            title += "  ·  UP";
        if (clock.IsComplete)
            title += "  ·  FINISHED";

        NeonTheme.BeginLaneCard(lane.ContentId + 17, isTurn);

        if (lane.SittingOut)
        {
            if (NeonTheme.Chip($"Back in##dback_{lane.ContentId}", NeonTheme.Cyan))
            {
                lane.SittingOut = false;
                this.status = $"{lane.Name} is back on the oche.";
            }

            ImGui.SameLine(0, 8f);
        }
        else if (!clock.IsComplete)
        {
            if (NeonTheme.Chip($"Skip##dskip_{lane.ContentId}", NeonTheme.Gold))
            {
                this.SkipDartAfk(lane);
                NeonTheme.EndLaneCard(isTurn);
                return false;
            }

            ImGui.SameLine(0, 8f);
        }

        if (lane.IsManual)
        {
            if (NeonTheme.Chip($"Remove##drm_{lane.ContentId}", NeonTheme.Magenta))
            {
                NeonTheme.EndLaneCard(isTurn);
                removeGuest = true;
                return true;
            }

            ImGui.SameLine(0, 8f);
        }

        var dartKey = lane.ContentId ^ 0xD000_0000UL;
        this.tabFont.Push();
        if (NeonTheme.NamePlate($"dart_name_{lane.ContentId}", title, NeonTheme.Purple, ImGui.GetContentRegionAvail().X))
            this.ToggleSheet(dartKey);
        this.tabFont.Pop();
        var open = this.SheetOpen(dartKey);

        var dartLine = clock.IsComplete
            ? "Around the clock — finished."
            : $"Aiming {clock.AimLabel}  ·  dart {clock.DartsThisTurn + 1}/3  ·  points {clock.PointsEarned}/20";
        ImGui.TextColored(NeonTheme.Lavender, dartLine);
        if (clock.LastTurnMarks.Count > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(NeonTheme.Gold, "  " + string.Join("  ", clock.LastTurnMarks));
        }

        if (open)
            this.DrawClockPips(clock, $"pips_{lane.ContentId}");

        NeonTheme.EndLaneCard(isTurn);
        return false;
    }

    private void DrawClockPips(ClockGame clock, string id)
    {
        ImGui.PushID(id);
        for (var n = 1; n <= ClockGame.LastNumber; n++)
        {
            if (n > 1)
                ImGui.SameLine(0, 4f);
            NeonTheme.ClockPip(n.ToString(), n < clock.AimingAt, n == clock.AimingAt && !clock.IsComplete);
        }

        ImGui.SameLine(0, 8f);
        NeonTheme.ClockPip("BULL", clock.IsComplete, clock.IsBullseye);
        ImGui.PopID();
    }

    private void DrawScorecard(BowlingGame game, string id)
    {
        ImGui.PushID(id);
        NeonTheme.BowlingSheet(game.GetFrames(), game.TotalScore, game.IsComplete);
        ImGui.PopID();
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
        this.config.FullWidth = this.savedWindowSize.X;
        this.config.FullHeight = this.savedWindowSize.Y;
        this.pluginInterface.SavePluginConfig(this.config);
        this.hours.Dispose();
        this.labelFont.Dispose();
        this.tabFont.Dispose();
    }
}
