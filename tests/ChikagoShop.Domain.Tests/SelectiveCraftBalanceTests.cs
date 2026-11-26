using System.Text.Json.Nodes;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

// A deterministic budget feasibility check, not a simulation of customer arrivals,
// input skill, offer randomness or the full campaign runtime.
public sealed class SelectiveCraftBalanceTests
{
    private static readonly (string Kind, string Room, string Rarity, int Cost)[] Items =
    [
        ("hat", "atelier", "common", 20), ("vest", "atelier", "ordinary", 30),
        ("gloves", "atelier", "first_class", 55), ("matteo_hat", "atelier", "exclusive", 80),
        ("lorgnette", "atelier", "unique", 135), ("guitar", "music", "common", 30),
        ("drum", "music", "ordinary", 50), ("banjo", "music", "first_class", 80),
        ("violin", "music", "exclusive", 125), ("tuba", "music", "unique", 205)
    ];

    private static string ConfigJson() => File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json"));
    private static EconomyConfig Config() => EconomyConfigLoader.Load(ConfigJson());
    private static int SalePrice(EconomyConfig config, string room, string rarity) =>
        config.Rarities.Single(value => value.Id == rarity).BasePrices[room];

    [Test]
    public void AllTenExplicitCostsMatchApprovedPricesAndRoundedSeventyPercentRule()
    {
        var json = JsonNode.Parse(ConfigJson())!.AsObject();
        var explicitCosts = json["craft_costs"]!.AsObject();
        var config = Config();
        Assert.That(explicitCosts.Select(pair => pair.Key), Is.EquivalentTo(Items.Select(item => item.Kind)));
        Assert.That(config.CraftCosts.Keys, Is.EquivalentTo(Items.Select(item => item.Kind)));
        foreach (var item in Items)
        {
            int sale = SalePrice(config, item.Room, item.Rarity);
            int roundedCost = (int)(decimal.Round(sale * 0.7m / 5m, 0,
                MidpointRounding.AwayFromZero) * 5m);
            Assert.That(explicitCosts[item.Kind]!.GetValue<int>(), Is.EqualTo(item.Cost), item.Kind);
            Assert.That(config.CraftCosts[item.Kind], Is.EqualTo(roundedCost), item.Kind);
            Assert.That(sale - config.CraftCosts[item.Kind], Is.GreaterThan(0), item.Kind);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NeutralSalesCanFundBothQuotasWithOptionalMusicUnlock(bool openMusic)
    {
        var config = Config();
        Assert.That(config.StartingMoney, Is.EqualTo(220));
        Assert.That(config.Days.TotalDays, Is.EqualTo(14));
        Assert.That(config.Quotas.Select(q => (q.DeadlineDay, q.Amount)),
            Is.EquivalentTo(new[] { (7, 750), (14, 1500) }));

        int money = config.StartingMoney;
        bool musicUnlocked = false;
        bool firstQuotaPaid = false;
        int musicUnlockDay = 0;
        int paidQuotas = 0;
        for (int day = 1; day <= config.Days.TotalDays; day++)
        {
            if (openMusic && firstQuotaPaid && !musicUnlocked)
            {
                int unlockCost = config.Workshops.Single(w => w.Id == "music").UnlockCost;
                Assert.That(money, Is.GreaterThanOrEqualTo(unlockCost), "Unlock must use earned money and the real quota gift.");
                money -= unlockCost;
                musicUnlocked = true;
                musicUnlockDay = day;
            }

            // Ten-second attempts plus ten seconds per sale leave a deliberately
            // slower three-item daily policy, not a game-imposed attempt limit.
            const int secondsPerCraftAndSale = 20;
            int attempts = Math.Min(3, config.Days.DurationSeconds / secondsPerCraftAndSale);
            Assert.That(attempts, Is.GreaterThanOrEqualTo(3));
            int secondsUsed = 0;
            for (int i = 0; i < attempts; i++)
            {
                var affordable = Items.Where(item => (item.Room == "atelier" || musicUnlocked)
                        && config.CraftCosts[item.Kind] <= money)
                    .OrderByDescending(item => SalePrice(config, item.Room, item.Rarity) - config.CraftCosts[item.Kind])
                    .ToArray();
                Assert.That(affordable, Is.Not.Empty, $"Day {day}: must afford an actual item before sale.");
                var selected = affordable[0];
                money -= config.CraftCosts[selected.Kind];
                Assert.That(money, Is.GreaterThanOrEqualTo(0));
                money += SalePrice(config, selected.Room, selected.Rarity);
                secondsUsed += secondsPerCraftAndSale;
            }
            Assert.That(secondsUsed, Is.LessThanOrEqualTo(config.Days.DurationSeconds));

            foreach (var quota in config.Quotas.Where(q => q.DeadlineDay == day))
            {
                Assert.That(money, Is.GreaterThanOrEqualTo(quota.Amount), $"Day {day}: quota must be payable without borrowing.");
                money -= quota.Amount;
                paidQuotas++;
                if (quota.Id == "first")
                {
                    firstQuotaPaid = true;
                    money += RunRewardRules.FirstQuotaGift;
                }
            }
        }
        Assert.That(paidQuotas, Is.EqualTo(2));
        Assert.That(musicUnlocked, Is.EqualTo(openMusic));
        if (openMusic) Assert.That(musicUnlockDay, Is.EqualTo(8));
        else Assert.That(money - RunRewardRules.FirstQuotaGift, Is.GreaterThanOrEqualTo(0),
            "Atelier-only route remains affordable even without spending the first-quota gift.");
        TestContext.WriteLine($"Budget feasibility: music={openMusic}, unlock day={musicUnlockDay}, final cash={money}; neutral base sales, no extra bonuses.");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonPositivePerItemCraftCostsAreRejected(int invalidCost)
    {
        foreach (var item in Items)
        {
            var json = JsonNode.Parse(ConfigJson())!.AsObject();
            json["craft_costs"]![item.Kind] = invalidCost;
            Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()), item.Kind);
        }
    }

    [Test]
    public void MissingPerItemCraftCostsAreRejected()
    {
        foreach (var item in Items)
        {
            var json = JsonNode.Parse(ConfigJson())!.AsObject();
            json["craft_costs"]!.AsObject().Remove(item.Kind);
            Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()), item.Kind);
        }
    }

    [Test]
    public void NullCostTableAndUnknownItemIdsAreRejected()
    {
        var json = JsonNode.Parse(ConfigJson())!.AsObject();
        json["craft_costs"] = null;
        Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()));
        json = JsonNode.Parse(ConfigJson())!.AsObject();
        json["craft_costs"]!["unknown"] = 20;
        Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()));
    }
}
