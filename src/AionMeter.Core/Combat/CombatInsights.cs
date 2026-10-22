namespace AionMeter.Core.Combat;

/// <summary>
/// Read-only insights calculated from the existing damage-per-second buckets.
/// These do not require additional packet capture or game memory access.
/// </summary>
public readonly record struct DamageInsights(
    int BurstWindowSeconds,
    double PeakBurstDps,
    int DamageSeconds,
    int ObservedSeconds)
{
    /// <summary>
    /// Fraction of observed fight seconds that contain at least one damage event.
    /// DoT and pet ticks count. This is not skill uptime or an APM estimate.
    /// </summary>
    public double DamageSecondShare => ObservedSeconds > 0 ? (double)DamageSeconds / ObservedSeconds : 0;
}

public static class CombatInsights
{
    /// <summary>
    /// Calculates peak damage averaged over a sliding, whole-second window and the
    /// share of seconds with damage. Uses existing per-second buckets (not casts).
    /// Missing trailing buckets are treated as zero; extra buckets outside the
    /// supplied fight duration are ignored. Partial fights are marked by the caller.
    /// </summary>
    public static DamageInsights Analyze(IReadOnlyList<long> damagePerSecond, long fightDurationMs, int burstWindowSeconds = 5)
    {
        ArgumentNullException.ThrowIfNull(damagePerSecond);
        if (burstWindowSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(burstWindowSeconds));
        if (fightDurationMs <= 0) return default;

        // Avoid overflow on extremely large durations when reading a corrupt history file.
        var observed = (int)Math.Min(int.MaxValue,
            fightDurationMs / 1000 + (fightDurationMs % 1000 > 0 ? 1 : 0));
        var window = Math.Min(burstWindowSeconds, observed);
        var available = Math.Min(observed, damagePerSecond.Count);
        var active = 0;
        double rolling = 0;
        double peakDamage = 0;

        for (var i = 0; i < available; i++)
        {
            var damage = Math.Max(0L, damagePerSecond[i]);
            if (damage > 0) active++;
            rolling += damage;
            if (i >= window)
                rolling -= Math.Max(0L, damagePerSecond[i - window]);
            peakDamage = Math.Max(peakDamage, rolling);
        }

        // Divide by the complete window length, including zero-damage seconds.
        // For a fight shorter than five seconds, use its available duration.
        return new DamageInsights(window, window > 0 ? peakDamage / window : 0, active, observed);
    }
}
