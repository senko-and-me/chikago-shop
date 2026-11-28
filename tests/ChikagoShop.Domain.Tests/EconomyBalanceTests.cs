using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Simulation;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EconomyBalanceTests
{
    private static EconomyConfig Config() => EconomyConfigLoader.Load(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json")));

    [Test]
    public void SingleItemCraftingUsesTheUniformBasePrice()
    {
        var config = Config();
        Assert.That(config.Days.DurationSeconds, Is.EqualTo(60));
        Assert.That(config.Days.ItemsPerCraft, Is.EqualTo(1));
        Assert.That(config.Items.Select(item => item.BasePrice), Is.EqualTo(Enumerable.Repeat(50, 10)));
        Assert.That(config.Quotas.Single(quota => quota.Id == "first").DeadlineDay, Is.EqualTo(7));
    }

    [Test]
    public void AZeroBalanceStillAllowsProductionInTheSimulator()
    {
        var original = Config();
        var config = new EconomyConfig
        {
            SchemaVersion = original.SchemaVersion, StartingMoney = 0, Days = original.Days,
            Workshops = original.Workshops, Items = original.Items, Rarities = original.Rarities,
            OfferRanges = original.OfferRanges, Quotas = original.Quotas
        };
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 10).MeanItems, Is.GreaterThan(0));
    }

    [Test]
    public void ReproducibleSelectedItemPolicyCanFundTheFirstQuotaAndPassivePlayCannot()
    {
        var config = Config();
        var ordinary = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000);
        var careful = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[1], 1000);
        var passive = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[5], 1000);
        Assert.That(ordinary.FirstQuotaPassRate, Is.GreaterThan(0));
        Assert.That(ordinary.WinRate, Is.LessThanOrEqualTo(ordinary.FirstQuotaPassRate));
        Assert.That(careful.WinRate, Is.LessThanOrEqualTo(careful.FirstQuotaPassRate));
        Assert.That(passive.FirstQuotaPassRate + passive.WinRate, Is.Zero);
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000), Is.EqualTo(ordinary));
    }
}
