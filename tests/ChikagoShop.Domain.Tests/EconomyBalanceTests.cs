using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Simulation;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EconomyBalanceTests
{
    private static EconomyConfig Config() => EconomyConfigLoader.Load(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json")));

    [Test]
    public void SingleItemCraftingHasPositiveExpectedMarginWithoutChangingBasePrices()
    {
        var config = Config();
        Assert.That(config.Days.DurationSeconds, Is.EqualTo(90));
        Assert.That(config.Days.ItemsPerCraft, Is.EqualTo(1));
        foreach (var workshop in config.Workshops)
        {
            double value = config.Rarities.Sum(rarity => (double)rarity.Weight * rarity.BasePrices[workshop.Id]) / config.Rarities.Sum(rarity => rarity.Weight);
            Assert.That(workshop.SpinCost / value, Is.InRange(0.5, 0.8), "Crafting must cost a meaningful but sustainable fraction of expected sale value");
        }
        Assert.That(config.Quotas.Single(quota => quota.Id == "first").DeadlineDay, Is.EqualTo(4));
    }

    [Test]
    public void ReproducibleWeekRewardsTradingDecisionsWithoutGuaranteeingVictory()
    {
        var config = Config();
        var ordinary = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000);
        var careful = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[1], 1000);
        var passive = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[5], 1000);
        Assert.That(ordinary.FirstQuotaPassRate, Is.GreaterThan(0.75));
        Assert.That(ordinary.WinRate, Is.InRange(0.3, 0.7));
        Assert.That(careful.WinRate, Is.InRange(0.7, 0.95));
        Assert.That(careful.WinRate, Is.GreaterThan(ordinary.WinRate + 0.15));
        Assert.That(passive.FirstQuotaPassRate + passive.WinRate, Is.Zero);
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000), Is.EqualTo(ordinary));
    }
}
