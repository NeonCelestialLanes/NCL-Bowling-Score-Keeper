using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace FFXIVBowling;

internal sealed class BowlerLane
{
    public ulong ContentId { get; set; }
    public string Name { get; set; } = "Unknown";
    public bool IsLocal { get; set; }
    public bool IsLeader { get; set; }
    public bool IsManual { get; set; }
    public bool Present { get; set; }
    public bool SittingOut { get; set; }
    public BowlingGame Game { get; } = new();
    public ClockGame Clock { get; } = new();
}

/// <summary>
/// Party members are added automatically. Extra bowlers can be seated by name.
/// </summary>
internal sealed class PartyLanes
{
    private readonly Dictionary<ulong, BowlerLane> byId = new();
    private readonly List<BowlerLane> order = new();
    private ulong nextGuestId = 0xF000_0000_0000_0001;

    public IReadOnlyList<BowlerLane> Visible => this.order;
    public int PresentCount => this.order.Count;
    public bool HasManualGuest
    {
        get
        {
            foreach (var lane in this.order)
            {
                if (lane.IsManual && lane.Present)
                    return true;
            }

            return false;
        }
    }

    public void Sync(IPartyList party, IPlayerCharacter? localPlayer, ulong localContentId)
    {
        foreach (var lane in this.byId.Values)
        {
            if (!lane.IsManual)
                lane.Present = false;
        }

        this.order.Clear();

        var localName = localPlayer?.Name.TextValue ?? "You";
        var localId = localContentId != 0 ? localContentId : 1UL;

        if (party.Length > 0)
        {
            for (var i = 0; i < party.Length; i++)
            {
                var member = party[i];
                if (member is null)
                    continue;

                var name = member.Name.TextValue;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var id = member.ContentId != 0 ? member.ContentId : (ulong)(i + 100);
                var isLocal = (localContentId != 0 && member.ContentId == localContentId)
                    || (localPlayer is not null && RollParser.NamesMatch(name, localName));

                this.order.Add(this.Upsert(id, name, isLocal, i == party.PartyLeaderIndex, isManual: false));
            }
        }

        if (localPlayer is not null && !this.order.Exists(l => l.IsLocal))
            this.order.Insert(0, this.Upsert(localId, localName, isLocal: true, isLeader: this.order.Count == 0, isManual: false));

        foreach (var lane in this.byId.Values)
        {
            if (!lane.IsManual || !lane.Present)
                continue;

            if (this.order.Exists(l => ReferenceEquals(l, lane) || RollParser.NamesMatch(l.Name, lane.Name)))
                continue;

            this.order.Add(lane);
        }
    }

    public bool TryAddGuest(string rawName, out string? error)
    {
        error = null;
        var name = rawName.Trim();
        if (name.Length < 2)
        {
            error = "Type a character name to add a lane.";
            return false;
        }

        if (this.FindByName(name) is not null)
        {
            error = $"{name} is already on a lane.";
            return false;
        }

        var id = this.nextGuestId++;
        var lane = new BowlerLane
        {
            ContentId = id,
            Name = name,
            IsManual = true,
            Present = true
        };
        this.byId[id] = lane;
        this.order.Add(lane);
        return true;
    }

    public bool TryRemoveGuest(ulong contentId)
    {
        if (!this.byId.TryGetValue(contentId, out var lane) || !lane.IsManual)
            return false;

        this.byId.Remove(contentId);
        this.order.RemoveAll(l => l.ContentId == contentId);
        return true;
    }

    public BowlerLane? WhoseTurn()
    {
        foreach (var lane in this.order)
        {
            if (lane.SittingOut || lane.Game.IsComplete)
                continue;

            if (lane.Game.IsFrameInProgress)
                return lane;
        }

        BowlerLane? next = null;
        var fewest = int.MaxValue;
        foreach (var lane in this.order)
        {
            if (lane.SittingOut || lane.Game.IsComplete)
                continue;

            var done = lane.Game.CompletedFrames;
            if (done > fewest)
                continue;

            if (done < fewest)
            {
                fewest = done;
                next = lane;
            }
        }

        return next;
    }

    public BowlerLane? WhoseDartTurn()
    {
        foreach (var lane in this.order)
        {
            if (lane.SittingOut || lane.Clock.IsComplete)
                continue;

            if (lane.Clock.IsTurnInProgress)
                return lane;
        }

        BowlerLane? next = null;
        var fewest = int.MaxValue;
        foreach (var lane in this.order)
        {
            if (lane.SittingOut || lane.Clock.IsComplete)
                continue;

            var done = lane.Clock.CompletedTurns;
            if (done > fewest)
                continue;

            if (done < fewest)
            {
                fewest = done;
                next = lane;
            }
        }

        return next;
    }

    public BowlerLane? FindBowler(params string?[] hints)
    {
        foreach (var hint in hints)
        {
            var lane = this.FindByName(hint);
            if (lane is not null)
                return lane;
        }

        foreach (var hint in hints)
        {
            if (string.IsNullOrWhiteSpace(hint))
                continue;

            foreach (var lane in this.order)
            {
                var laneName = RollParser.NormalizeName(lane.Name);
                if (laneName.Length < 2)
                    continue;

                if (hint.Contains(lane.Name, StringComparison.OrdinalIgnoreCase)
                    || hint.Contains(laneName, StringComparison.OrdinalIgnoreCase)
                    || RollParser.NormalizeName(hint).Contains(laneName, StringComparison.OrdinalIgnoreCase))
                    return lane;
            }
        }

        return null;
    }

    public bool TryApplyRoll(BowlerLane lane, int pins, out int counted, out bool overshot, out string? error)
    {
        counted = 0;
        overshot = false;
        var onApproach = lane.Game.IsFrameInProgress ? lane : this.WhoseTurn();
        if (onApproach is null || !ReferenceEquals(onApproach, lane))
        {
            error = onApproach is null
                ? "The game is over."
                : $"It's {onApproach.Name}'s turn.";
            return false;
        }

        return lane.Game.TryAddRoll(pins, out counted, out overshot, out error);
    }

    public bool TryApplyDart(BowlerLane lane, int roll, out bool hit, out bool turnOver, out string? error)
    {
        hit = false;
        turnOver = false;
        var onOche = this.WhoseDartTurn();
        if (onOche is null || !ReferenceEquals(onOche, lane))
        {
            error = onOche is null
                ? "That clock is finished."
                : $"It's {onOche.Name}'s darts.";
            return false;
        }

        return lane.Clock.TryThrow(roll, out hit, out turnOver, out error);
    }

    public void ResetVisible()
    {
        foreach (var lane in this.order)
            lane.Game.Reset();
    }

    public void ResetClocks()
    {
        foreach (var lane in this.order)
            lane.Clock.Reset();
    }

    private BowlerLane Upsert(ulong id, string name, bool isLocal, bool isLeader, bool isManual)
    {
        var lane = this.FindByName(name);
        if (lane is null)
            this.byId.TryGetValue(id, out lane);

        if (lane is null)
        {
            lane = new BowlerLane { ContentId = id };
            this.byId[id] = lane;
        }
        else if (lane.ContentId != id)
        {
            this.byId.Remove(lane.ContentId);
            lane.ContentId = id;
            this.byId[id] = lane;
        }

        lane.Name = name;
        lane.IsLocal = isLocal;
        lane.IsLeader = isLeader;
        if (!isManual)
            lane.IsManual = false;
        lane.Present = true;
        return lane;
    }

    private BowlerLane? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var rolled = RollParser.NormalizeName(name);
        if (rolled.Length == 0)
            return null;

        foreach (var lane in this.byId.Values)
        {
            var laneName = RollParser.NormalizeName(lane.Name);
            if (laneName.Length == 0)
                continue;
            if (rolled.Equals(laneName, StringComparison.OrdinalIgnoreCase))
                return lane;
            if (rolled.EndsWith(laneName, StringComparison.OrdinalIgnoreCase))
                return lane;
        }

        return null;
    }
}
