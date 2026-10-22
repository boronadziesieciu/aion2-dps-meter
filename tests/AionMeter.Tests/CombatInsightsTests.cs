using AionMeter.Core.Combat;

namespace AionMeter.Tests;

public class CombatInsightsTests
{
    [Fact]
    public void Peak_is_the_best_rolling_five_second_average()
    {
        long[] seconds = [10, 20, 0, 0, 0, 50, 50, 50, 50, 50, 0];
        var insights = CombatInsights.Analyze(seconds, 11_000);

        Assert.Equal(5, insights.BurstWindowSeconds);
        Assert.Equal(50, insights.PeakBurstDps);
        Assert.Equal(7, insights.DamageSeconds);
        Assert.Equal(11, insights.ObservedSeconds);
        Assert.Equal(7.0 / 11, insights.DamageSecondShare, 9);
    }

    [Fact]
    public void Short_encounter_uses_shorter_burst_window()
    {
        var insights = CombatInsights.Analyze([100, 0, 50], 3_000);

        Assert.Equal(3, insights.BurstWindowSeconds);
        Assert.Equal(50, insights.PeakBurstDps);
        Assert.Equal(2.0 / 3, insights.DamageSecondShare, 9);
    }

    [Fact]
    public void Missing_buckets_count_as_no_damage_and_extra_buckets_are_ignored()
    {
        var missing = CombatInsights.Analyze([100, 100], 5_000);
        Assert.Equal(40, missing.PeakBurstDps);
        Assert.Equal(2, missing.DamageSeconds);
        Assert.Equal(5, missing.ObservedSeconds);

        var extra = CombatInsights.Analyze([100, 0, 1000], 1_000);
        Assert.Equal(100, extra.PeakBurstDps);
        Assert.Equal(1, extra.ObservedSeconds);
    }

    [Fact]
    public void Empty_and_invalid_inputs_have_safe_results()
    {
        Assert.Equal(default, CombatInsights.Analyze([], 0));
        Assert.Equal(0, CombatInsights.Analyze([], 10_000).DamageSecondShare);
        Assert.Throws<ArgumentOutOfRangeException>(() => CombatInsights.Analyze([], 1000, 0));
        Assert.Throws<ArgumentNullException>(() => CombatInsights.Analyze(null!, 1000));
    }

    [Fact]
    public void Negative_corrupt_buckets_are_not_counted_as_damage()
    {
        var insights = CombatInsights.Analyze([-5, 15, 0], 3_000);
        Assert.Equal(5, insights.PeakBurstDps);
        Assert.Equal(1, insights.DamageSeconds);
    }
}
