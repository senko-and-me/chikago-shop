using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Simulation;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EconomyBalanceTests
{
    private static EconomyConfig Config() => EconomyConfigLoader.Load(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json")));

    [Test]
    public void SingleItemCraftingAndBaseSaleUseTheWorkshopPrice()
    {
        var config = Config();
        Assert.That(config.Days.DurationSeconds, Is.EqualTo(45));
        Assert.That(config.Days.ItemsPerCraft, Is.EqualTo(1));
        foreach (var item in config.Items)
        {
            Assert.That(config.CraftCosts[item.Id], Is.EqualTo(20));
            Assert.That(config.BasePrice, Is.EqualTo(50));
        }
        Assert.That(config.Quotas.Single(quota => quota.Id == "first").DeadlineDay, Is.EqualTo(7));
    }

    [Test]
    public void SimulatorRequiresEnoughMoneyToCraft()
    {
        var original = Config();
        var config = new EconomyConfig
        {
            SchemaVersion = original.SchemaVersion, StartingMoney = 0, Days = original.Days,
            Workshops = original.Workshops, Items = original.Items, Rarities = original.Rarities,
            OfferRanges = original.OfferRanges, Quotas = original.Quotas
        };
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 10).MeanItems, Is.Zero);
    }

    [Test]
    public void BuyerOfferPoliciesAreReproducibleAndPassivePlayCannotFundTheCampaign()
    {
        var config = Config();
        var ordinary = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000);
        var careful = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[1], 1000);
        var passive = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[5], 1000);
        Assert.That(ordinary.FirstQuotaPassRate, Is.LessThan(1), "Ordinary sales at the fixed base no longer guarantee the first quota");
        Assert.That(ordinary.WinRate, Is.LessThanOrEqualTo(ordinary.FirstQuotaPassRate));
        Assert.That(careful.WinRate, Is.LessThanOrEqualTo(careful.FirstQuotaPassRate));
        Assert.That(passive.FirstQuotaPassRate + passive.WinRate, Is.Zero);
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000), Is.EqualTo(ordinary));
    }
}
