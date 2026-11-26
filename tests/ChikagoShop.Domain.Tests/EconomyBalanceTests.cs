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
        Assert.That(config.Days.DurationSeconds, Is.EqualTo(60));
        foreach (var workshop in config.Workshops)
        {
            double value = config.Items.Where(item => item.Workshop == workshop.Id).Average(item => item.BasePrice);
            Assert.That(workshop.SpinCost / value, Is.InRange(0.25, 0.4), "Crafting must cost a meaningful but sustainable fraction of expected sale value");
        }
        Assert.That(config.Quotas.Single(quota => quota.Id == "first").DeadlineDay, Is.EqualTo(7));
    }

    [Test]
    public void ReproducibleWeekRewardsTradingDecisionsWithoutGuaranteeingVictory()
    {
        var config = Config();
        var ordinary = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000);
        var careful = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[1], 1000);
        var passive = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[5], 1000);
        Assert.That(ordinary.FirstQuotaPassRate, Is.GreaterThan(0.8), "The current time model must permit the first quota");
        Assert.That(ordinary.WinRate, Is.GreaterThan(0));
        Assert.That(ordinary.WinRate, Is.LessThanOrEqualTo(ordinary.FirstQuotaPassRate));
        Assert.That(careful.WinRate, Is.GreaterThanOrEqualTo(ordinary.WinRate));
        Assert.That(passive.FirstQuotaPassRate + passive.WinRate, Is.Zero);
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000), Is.EqualTo(ordinary));
    }
}
