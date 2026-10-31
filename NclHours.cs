using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FFXIVBowling;

/// <summary>
/// Live open/close timer from the NCL listing on FFXIV Venues.
/// https://ffxivvenues.com/venue/Mt5pdZd2GbD1
/// </summary>
internal sealed class NclHours : IDisposable
{
    public const string PageUrl = "https://ffxivvenues.com/venue/Mt5pdZd2GbD1";
    private const string ApiUrl = "https://api.ffxivvenues.com/venue/Mt5pdZd2GbD1";
    private const long RefreshMs = 10 * 60 * 1000;
    private const long MinGapMs = 20_000;

    private static readonly HttpClient Http = CreateClient();

    private DateTimeOffset? start;
    private DateTimeOffset? end;
    private bool isNow;
    private bool loaded;
    private bool fetching;
    private long lastAttempt;

    public void Pulse()
    {
        var nowTicks = Environment.TickCount64;
        var overdue = this.end is DateTimeOffset close && DateTimeOffset.Now >= close;
        if (this.fetching)
            return;
        if (this.loaded && !overdue && nowTicks - this.lastAttempt < RefreshMs)
            return;
        if (nowTicks - this.lastAttempt < MinGapMs)
            return;

        this.lastAttempt = nowTicks;
        this.fetching = true;
        _ = this.FetchAsync();
    }

    public string Line()
    {
        this.Pulse();
        if (!this.loaded)
            return "Hours…";

        var now = DateTimeOffset.Now;
        if ((this.isNow || (this.start is DateTimeOffset opened && now >= opened)) &&
            this.end is DateTimeOffset until && now < until)
            return $"OPEN NOW  ·  closes in {Format(until - now)}";

        if (this.start is DateTimeOffset next && now < next)
            return $"OPENS IN {Format(next - now)}";

        return "Checking hours…";
    }

    private async Task FetchAsync()
    {
        try
        {
            using var response = await Http.GetAsync(ApiUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (!TryReadResolution(doc.RootElement, out var startAt, out var endAt, out var now))
                return;

            this.start = startAt;
            this.end = endAt;
            this.isNow = now;
            this.loaded = true;
        }
        catch
        {
            // Keep the last good times if the API is down or rate-limited.
        }
        finally
        {
            this.fetching = false;
        }
    }

    private static bool TryReadResolution(JsonElement root, out DateTimeOffset startAt, out DateTimeOffset endAt, out bool now)
    {
        startAt = default;
        endAt = default;
        now = false;
        if (root.TryGetProperty("resolution", out var res) && ReadWindow(res, out startAt, out endAt, out now))
            return true;

        if (!root.TryGetProperty("schedule", out var schedule) || schedule.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var entry in schedule.EnumerateArray())
        {
            if (entry.TryGetProperty("resolution", out var inner) && ReadWindow(inner, out startAt, out endAt, out now))
                return true;
        }

        return false;
    }

    private static bool ReadWindow(JsonElement res, out DateTimeOffset startAt, out DateTimeOffset endAt, out bool now)
    {
        startAt = default;
        endAt = default;
        now = res.TryGetProperty("isNow", out var isNowEl) && isNowEl.ValueKind is JsonValueKind.True;
        if (!res.TryGetProperty("start", out var startEl) || !res.TryGetProperty("end", out var endEl))
            return false;

        var startText = startEl.GetString();
        var endText = endEl.GetString();
        if (string.IsNullOrWhiteSpace(startText) || string.IsNullOrWhiteSpace(endText))
            return false;

        return DateTimeOffset.TryParse(startText, out startAt) && DateTimeOffset.TryParse(endText, out endAt);
    }

    private static string Format(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;

        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes:D2}m";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m {span.Seconds:D2}s";
        return $"{span.Minutes}m {span.Seconds:D2}s";
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "NCLBowlingScore (Neon Celestial Lanes; https://github.com/NeonCelestialLanes/NCL-Bowling-Score-Keeper)");
        return client;
    }

    public void Dispose()
    {
    }
}
