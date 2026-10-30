using System;
using System.Collections.Generic;

namespace FFXIVBowling;

/// <summary>
/// Standard 10-frame bowling.
/// Frames 1–9: strike (X) ends the frame on one ball and scores 10 + next two balls;
/// spare (/) scores 10 + next one ball; open frame scores the two balls as-is.
/// Frame 10: two balls, plus a third if the first is a strike or the first two make a spare.
/// Rolling more pins than are standing (including an 11) is a gutter: 0 for that ball.
/// A 10 on the first ball is a strike. Two balls that add to exactly 10 is a spare.
/// </summary>
public sealed class BowlingGame
{
    public const int MaxFrames = 10;
    public const int PinsPerFrame = 10;

    private readonly List<int> rolls = new();

    public IReadOnlyList<int> Rolls => this.rolls;
    public bool IsComplete { get; private set; }
    public int CurrentFrameNumber => Math.Min(this.GetOpenFrameIndex() + 1, MaxFrames);
    public int RollsInCurrentFrame => this.GetRollsInOpenFrame();
    public int RemainingPins => this.GetRemainingPins();
    public int TotalScore => this.ComputeResolvedScore();
    public int CompletedFrames => this.IsComplete ? MaxFrames : this.GetOpenFrameIndex();
    public bool IsFrameInProgress => !this.IsComplete && this.GetRollsInOpenFrame() > 0;

    public void Reset()
    {
        this.rolls.Clear();
        this.IsComplete = false;
    }

    /// <summary>
    /// Applies a knock-down result. Rolling more than the pins still standing is a gutter (0).
    /// </summary>
    public bool TryAddRoll(int rawPins, out int countedPins, out bool overshot, out string? error)
    {
        countedPins = 0;
        overshot = false;
        error = null;

        if (this.IsComplete)
        {
            error = "The game is already complete.";
            return false;
        }

        if (rawPins < 0)
        {
            error = "Roll cannot be negative.";
            return false;
        }

        var remaining = this.GetRemainingPins();
        if (rawPins > remaining)
        {
            countedPins = 0;
            overshot = true;
        }
        else
        {
            countedPins = rawPins;
        }

        this.rolls.Add(countedPins);

        if (this.HasFinishedTenthFrame())
            this.IsComplete = true;

        return true;
    }

    public bool TryUndo(out string? error)
    {
        error = null;
        if (this.rolls.Count == 0)
        {
            error = "Nothing to undo on this lane.";
            return false;
        }

        this.rolls.RemoveAt(this.rolls.Count - 1);
        this.IsComplete = this.HasFinishedTenthFrame();
        return true;
    }

    /// <summary>
    /// Gutters any remaining balls in the open frame so the next bowler can go.
    /// </summary>
    public bool TryPassFrame(out int gutters, out string? error)
    {
        gutters = 0;
        error = null;
        if (this.IsComplete)
        {
            error = "That game is already finished.";
            return false;
        }

        var safety = 0;
        while (!this.IsComplete && safety++ < 4)
        {
            var framesBefore = this.CompletedFrames;
            var inProgress = this.IsFrameInProgress;
            if (!this.TryAddRoll(0, out _, out _, out error))
                return false;

            gutters++;
            if (this.IsComplete || this.CompletedFrames > framesBefore || (!inProgress && !this.IsFrameInProgress))
                break;
        }

        return gutters > 0;
    }

    public IReadOnlyList<FrameView> GetFrames()
    {
        var frames = new FrameView[MaxFrames];
        var rollIndex = 0;
        var running = 0;
        var open = this.GetOpenFrameIndex();

        for (var frame = 0; frame < MaxFrames - 1; frame++)
        {
            var isCurrent = !this.IsComplete && frame == open;
            frames[frame] = this.BuildStandardFrame(frame, ref rollIndex, ref running, isCurrent);
        }

        frames[MaxFrames - 1] = this.BuildTenthFrame(
            rollIndex,
            running,
            !this.IsComplete && open == MaxFrames - 1);

        return frames;
    }

    public string DescribeLastMark(int countedPins)
    {
        if (countedPins == PinsPerFrame)
            return this.IsComplete ? "Strike! That's the game." : "Strike!";

        var frames = this.GetFrames();
        var index = this.CurrentFrameNumber - 1;
        if (!this.IsComplete && this.RollsInCurrentFrame == 0 && this.CurrentFrameNumber > 1)
            index = this.CurrentFrameNumber - 2;

        var frame = frames[Math.Clamp(index, 0, MaxFrames - 1)];
        if (frame.IsSpare && (this.RollsInCurrentFrame == 0 || this.IsComplete || frame.Number == MaxFrames))
            return this.IsComplete ? "Spare! That's the game." : "Spare!";

        if (this.IsFrameInProgress && this.RollsInCurrentFrame == 1 && this.RemainingPins == 2)
            return SplitLine();

        if (this.IsComplete)
            return countedPins == 0 ? "The pins won. Tragic. That's the game." : $"{countedPins} pin(s) down. That's the game.";

        return countedPins == 0 ? "The pins won. Tragic." : $"{countedPins} pin(s) down.";
    }

    private static readonly string[] SplitLines =
    {
        "Split. Are you in lane 2? That lane is haunted, and the ghost is keeping your pins hostage.",
        "Split. Two pins left, holding hands in the corner like they pay rent.",
        "Split. The 7-10 just filed a restraining order against your ball.",
        "Split. Cute. The pins formed a support group and you're not invited.",
    };

    private string SplitLine() => SplitLines[this.rolls.Count % SplitLines.Length];

    private FrameView BuildStandardFrame(int frame, ref int rollIndex, ref int running, bool isCurrent)
    {
        if (rollIndex >= this.rolls.Count)
            return FrameView.Empty(frame + 1, isCurrent);

        var first = this.rolls[rollIndex];
        if (first == PinsPerFrame)
        {
            var resolved = this.TryBonus(rollIndex + 1, 2, out var bonus);
            if (resolved)
                running += PinsPerFrame + bonus;

            rollIndex += 1;
            return new FrameView(
                frame + 1,
                "X",
                string.Empty,
                string.Empty,
                resolved ? running : null,
                isCurrent,
                IsStrike: true,
                IsSpare: false);
        }

        var firstLabel = FormatPins(first);
        if (rollIndex + 1 >= this.rolls.Count)
        {
            rollIndex = this.rolls.Count;
            return new FrameView(frame + 1, firstLabel, string.Empty, string.Empty, null, isCurrent, false, false);
        }

        var second = this.rolls[rollIndex + 1];
        var isSpare = first + second == PinsPerFrame;
        var secondLabel = isSpare ? "/" : FormatPins(second);
        int? score = null;

        if (isSpare)
        {
            if (this.TryBonus(rollIndex + 2, 1, out var bonus))
            {
                running += PinsPerFrame + bonus;
                score = running;
            }
        }
        else
        {
            running += first + second;
            score = running;
        }

        rollIndex += 2;
        return new FrameView(frame + 1, firstLabel, secondLabel, string.Empty, score, isCurrent, false, isSpare);
    }

    private FrameView BuildTenthFrame(int rollIndex, int running, bool isCurrent)
    {
        if (rollIndex >= this.rolls.Count)
            return FrameView.Empty(MaxFrames, isCurrent);

        var labels = new[] { string.Empty, string.Empty, string.Empty };
        var consumed = 0;
        var needed = 2;
        var frameScore = 0;

        if (this.rolls[rollIndex] == PinsPerFrame)
        {
            labels[0] = "X";
            frameScore += PinsPerFrame;
            consumed = 1;
            needed = 3;
        }
        else
        {
            labels[0] = FormatPins(this.rolls[rollIndex]);
            frameScore += this.rolls[rollIndex];
            consumed = 1;
        }

        if (rollIndex + consumed < this.rolls.Count)
        {
            var second = this.rolls[rollIndex + consumed];
            if (labels[0] == "X")
            {
                labels[1] = second == PinsPerFrame ? "X" : FormatPins(second);
                frameScore += second;
            }
            else if (this.rolls[rollIndex] + second == PinsPerFrame)
            {
                labels[1] = "/";
                frameScore = PinsPerFrame;
                needed = 3;
            }
            else
            {
                labels[1] = FormatPins(second);
                frameScore += second;
            }

            consumed++;
        }

        if (needed == 3 && rollIndex + consumed < this.rolls.Count)
        {
            var third = this.rolls[rollIndex + consumed];
            if (labels[0] == "X" && labels[1] != "X" && this.rolls[rollIndex + 1] + third >= PinsPerFrame)
                labels[2] = "/";
            else
                labels[2] = third == PinsPerFrame ? "X" : FormatPins(third);

            frameScore += third;
            consumed++;
        }

        int? cumulative = null;
        var complete = consumed >= needed;
        if (complete)
            cumulative = running + frameScore;

        return new FrameView(
            MaxFrames,
            labels[0],
            labels[1],
            labels[2],
            cumulative,
            isCurrent,
            labels[0] == "X",
            labels[1] == "/");
    }

    private int GetOpenFrameIndex()
    {
        var rollIndex = 0;
        for (var frame = 0; frame < MaxFrames; frame++)
        {
            if (frame == MaxFrames - 1)
                return frame;

            if (rollIndex >= this.rolls.Count)
                return frame;

            if (this.rolls[rollIndex] == PinsPerFrame)
            {
                rollIndex += 1;
                continue;
            }

            if (rollIndex + 1 >= this.rolls.Count)
                return frame;

            rollIndex += 2;
        }

        return MaxFrames - 1;
    }

    private int GetRollsInOpenFrame()
    {
        if (this.IsComplete)
            return 0;

        var start = this.GetRollIndexForFrame(this.GetOpenFrameIndex());
        return this.rolls.Count - start;
    }

    private int GetRollIndexForFrame(int frame)
    {
        var rollIndex = 0;
        for (var i = 0; i < frame && i < MaxFrames - 1; i++)
        {
            if (rollIndex >= this.rolls.Count)
                return this.rolls.Count;

            rollIndex += this.rolls[rollIndex] == PinsPerFrame ? 1 : 2;
        }

        return Math.Min(rollIndex, this.rolls.Count);
    }

    private int GetRemainingPins()
    {
        if (this.IsComplete)
            return 0;

        var frame = this.GetOpenFrameIndex();
        var start = this.GetRollIndexForFrame(frame);
        var inFrame = this.rolls.Count - start;

        if (frame < MaxFrames - 1)
        {
            if (inFrame == 0)
                return PinsPerFrame;

            return PinsPerFrame - this.rolls[start];
        }

        if (inFrame == 0)
            return PinsPerFrame;

        var first = this.rolls[start];
        if (inFrame == 1)
            return first == PinsPerFrame ? PinsPerFrame : PinsPerFrame - first;

        var second = this.rolls[start + 1];
        if (first == PinsPerFrame)
            return second == PinsPerFrame ? PinsPerFrame : PinsPerFrame - second;

        return PinsPerFrame;
    }

    private bool HasFinishedTenthFrame()
    {
        var start = this.GetRollIndexForFrame(MaxFrames - 1);
        var count = this.rolls.Count - start;
        if (count < 2)
            return false;

        var first = this.rolls[start];
        var second = this.rolls[start + 1];
        var extra = first == PinsPerFrame || first + second == PinsPerFrame;
        return extra ? count >= 3 : count >= 2;
    }

    private int ComputeResolvedScore()
    {
        var frames = this.GetFrames();
        for (var i = frames.Count - 1; i >= 0; i--)
        {
            if (frames[i].CumulativeScore.HasValue)
                return frames[i].CumulativeScore.Value;
        }

        return 0;
    }

    private bool TryBonus(int fromIndex, int count, out int bonus)
    {
        bonus = 0;
        if (fromIndex + count > this.rolls.Count)
            return false;

        for (var i = 0; i < count; i++)
            bonus += this.rolls[fromIndex + i];

        return true;
    }

    private static string FormatPins(int pins) => pins == 0 ? "-" : pins.ToString();
}

public readonly record struct FrameView(
    int Number,
    string Roll1,
    string Roll2,
    string Roll3,
    int? CumulativeScore,
    bool IsCurrent,
    bool IsStrike,
    bool IsSpare)
{
    public static FrameView Empty(int number, bool isCurrent) =>
        new(number, string.Empty, string.Empty, string.Empty, null, isCurrent, false, false);
}
