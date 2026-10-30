using System;
using System.Collections.Generic;

namespace FFXIVBowling;

/// <summary>
/// Around the Clock (Carrd easy mode): collect 1–20 in order, then bullseye.
/// Each turn is 3 darts. /random 10: 2/4/6/8 hit numbers, 3/5/7/9 miss,
/// 1 and 10 are bullseye only (a miss on 1–20). Progress never resets.
/// </summary>
internal sealed class ClockGame
{
    public const int LastNumber = 20;
    public const int DartsPerTurn = 3;

    private readonly List<string> lastTurn = new();

    public int AimingAt { get; private set; } = 1;
    public int DartsThisTurn { get; private set; }
    public int CompletedTurns { get; private set; }
    public bool IsComplete => this.AimingAt > LastNumber + 1;
    public bool IsBullseye => this.AimingAt == LastNumber + 1;
    public int PointsEarned => Math.Clamp(this.AimingAt - 1, 0, LastNumber);
    public bool IsTurnInProgress => !this.IsComplete && this.DartsThisTurn is > 0 and < DartsPerTurn;
    public IReadOnlyList<string> LastTurnMarks => this.lastTurn;

    public string AimLabel =>
        this.IsComplete ? "DONE" : this.IsBullseye ? "BULL" : this.AimingAt.ToString();

    public static bool IsHit(int roll, bool bullseye)
    {
        if (roll is < 1 or > 10)
            return false;

        return bullseye ? roll is 1 or 10 : roll is 2 or 4 or 6 or 8;
    }

    public bool TryThrow(int roll, out bool hit, out bool turnOver, out string? error)
    {
        hit = false;
        turnOver = false;
        error = null;

        if (this.IsComplete)
        {
            error = "That clock is finished.";
            return false;
        }

        if (roll is < 1 or > 10)
        {
            error = "Darts use /random 10.";
            return false;
        }

        if (this.DartsThisTurn == 0)
            this.lastTurn.Clear();

        var bull = this.IsBullseye;
        hit = IsHit(roll, bull);
        this.DartsThisTurn++;
        this.lastTurn.Add(hit ? (bull ? "BULL" : this.AimingAt.ToString()) : "·");

        if (hit)
        {
            this.AimingAt++;
            if (this.IsComplete)
            {
                this.DartsThisTurn = 0;
                this.CompletedTurns++;
                turnOver = true;
                return true;
            }
        }

        if (this.DartsThisTurn >= DartsPerTurn)
        {
            this.DartsThisTurn = 0;
            this.CompletedTurns++;
            turnOver = true;
        }

        return true;
    }

    public void SkipRestOfTurn()
    {
        if (this.IsComplete)
            return;

        this.DartsThisTurn = 0;
        this.CompletedTurns++;
    }

    public void Reset()
    {
        this.AimingAt = 1;
        this.DartsThisTurn = 0;
        this.CompletedTurns = 0;
        this.lastTurn.Clear();
    }
}
