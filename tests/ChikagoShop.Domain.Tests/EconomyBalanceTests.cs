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
        Assert.That(config.Days.ItemsPerCraft, Is.EqualTo(1));
        foreach (var item in config.Items)
        {
            Assert.That((double)config.CraftCosts[item.Id] / item.BasePrice, Is.InRange(0.5, 0.8),
                "Each selected item must have a sustainable crafting margin");
        }
        Assert.That(config.Quotas.Single(quota => quota.Id == "first").DeadlineDay, Is.EqualTo(7));
    }

    [Test]
    public void SelectedItemSimulationDoesNotUseRetiredRoomSpinCosts()
    {
        var config = Config();
        var before = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[2], 10);
        for (int i = 0; i < config.Workshops.Count; i++)
        {
            var room = config.Workshops[i];
            config.Workshops[i] = new WorkshopConfig { Id = room.Id, SpinCost = 100000, UnlockCost = room.UnlockCost };
        }
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[2], 10), Is.EqualTo(before));
    }

    [Test]
    public void ReproducibleSelectedItemPolicyCanFundCampaignAndPassivePlayCannot()
    {
        var config = Config();
        var ordinary = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000);
        var careful = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[1], 1000);
        var passive = WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[5], 1000);
        Assert.That(ordinary.FirstQuotaPassRate, Is.EqualTo(1), "The model deliberately selects affordable high-margin items at fixed timing; it is a feasibility policy, not a human difficulty estimate");
        Assert.That(ordinary.WinRate, Is.GreaterThan(0));
        Assert.That(ordinary.WinRate, Is.LessThanOrEqualTo(ordinary.FirstQuotaPassRate));
        Assert.That(careful.WinRate, Is.GreaterThanOrEqualTo(ordinary.WinRate));
        Assert.That(passive.FirstQuotaPassRate + passive.WinRate, Is.Zero);
        Assert.That(WeekBalanceSimulator.Run(config, WeekBalanceSimulator.Profiles[0], 1000), Is.EqualTo(ordinary));
    }
}
