namespace Candidate.API.Infrastructure;

public static class TimeProviderExtensions
{
    /// <summary>
    /// "Today" for license rules. UTC, to match date('now') in the SQLite views; see README assumptions.
    /// </summary>
    public static DateOnly GetTodayUtc(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
