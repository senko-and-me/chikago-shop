using System.Text.Json.Nodes;
using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Economy;
using ChikagoShop.Domain.Trading;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

// A deterministic budget feasibility check, not a simulation of customer arrivals,
// input skill, offer randomness or the full campaign runtime.
public sealed class SelectiveCraftBalanceTests
{
    private static readonly (string Kind, string Room, string Rarity, int Cost)[] Items =
    [
        ("hat", "atelier", "common", 20), ("vest", "atelier", "ordinary", 20),
        ("gloves", "atelier", "first_class", 20), ("matteo_hat", "atelier", "exclusive", 20),
        ("lorgnette", "atelier", "unique", 20), ("guitar", "music", "common", 20),
        ("drum", "music", "ordinary", 20), ("banjo", "music", "first_class", 20),
        ("violin", "music", "exclusive", 20), ("tuba", "music", "unique", 20)
    ];

    private static string ConfigJson() => File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "config", "economy.v1.json"));
    private static EconomyConfig Config() => EconomyConfigLoader.Load(ConfigJson());
    private static int SalePrice(EconomyConfig config, string room, string rarity) =>
        config.Rarities.Single(value => value.Id == rarity).BasePrices[room];

    [Test]
    public void AllTenItemsShareCraftCostAndBaseSalePrice()
    {
        var config = Config();
        Assert.That(config.Items.Select(item => item.Id), Is.EquivalentTo(Items.Select(item => item.Kind)));
        foreach (var item in Items)
        {
            Assert.That(config.BasePrice, Is.EqualTo(50), item.Kind);
            Assert.That(config.CraftCost, Is.EqualTo(item.Cost), item.Kind);
            Assert.That(SalePrice(config, item.Room, item.Rarity), Is.EqualTo(50), item.Kind);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FastBonusedSalesCanFundBothQuotasWithOptionalMusicCrafting(bool openMusic)
    {
        var config = Config();
        Assert.That(config.StartingMoney, Is.EqualTo(100));
        Assert.That(config.Days.TotalDays, Is.EqualTo(14));
        Assert.That(config.Quotas.Select(q => (q.DeadlineDay, q.Amount)),
            Is.EquivalentTo(new[] { (7, 800), (14, 1350) }));

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
                Assert.That(unlockCost, Is.Zero, "The first-week story grants the room without a purchase.");
                money -= unlockCost;
                musicUnlocked = true;
                musicUnlockDay = day;
            }

            // Check a feasible high-offer route with ten seconds of craft and one second to sell.
            const int secondsPerCraftAndSale = 11;
            int duration = config.Days.DurationFor(day, 7);
            int attempts = duration / secondsPerCraftAndSale;
            int secondsUsed = 0;
            for (int i = 0; i < attempts; i++)
            {
                var affordable = Items.Where(item => (item.Room == "atelier" || musicUnlocked)
                        && config.CraftCost <= money)
                    .OrderByDescending(item => SalePrice(config, item.Room, item.Rarity))
                    .ToArray();
                Assert.That(affordable, Is.Not.Empty, $"Day {day}: must afford an actual item before sale.");
                var selected = affordable[0];
                money -= config.CraftCosts[selected.Kind];
                Assert.That(money, Is.GreaterThanOrEqualTo(0));
                int basePrice = SalePrice(config, selected.Room, selected.Rarity);
                money += TradeOfferRules.Price(basePrice, TradeOfferRules.MaxPercent)
                    + RunRewardRules.SpeedBonus(config.BuyerWaitMaxSeconds - secondsPerCraftAndSale, config.BuyerWaitMaxSeconds)
                    + RunRewardRules.QualityBonus("S");
                secondsUsed += secondsPerCraftAndSale;
            }
            Assert.That(secondsUsed, Is.LessThanOrEqualTo(duration));

            foreach (var quota in config.Quotas.Where(q => q.DeadlineDay == day))
            {
                Assert.That(money, Is.GreaterThanOrEqualTo(quota.Amount));
                money -= quota.Amount;
                paidQuotas++;
                if (quota.Id == "first") firstQuotaPaid = true;
            }
        }
        Assert.That(paidQuotas, Is.EqualTo(2));
        Assert.That(musicUnlocked, Is.EqualTo(openMusic));
        if (openMusic) Assert.That(musicUnlockDay, Is.EqualTo(8));
        Assert.That(money, Is.GreaterThanOrEqualTo(0), "The route remains affordable without a story reward.");
        TestContext.WriteLine($"Budget feasibility: music={openMusic}, unlock day={musicUnlockDay}, final cash={money}; fast sales with high offers and bonuses.");
    }

    [TestCase(0, 20)]
    [TestCase(21, 20)]
    [TestCase(15, 601)]
    public void InvalidBuyerWaitRangesAreRejected(int min, int max)
    {
        var json = JsonNode.Parse(ConfigJson())!.AsObject();
        json["buyer_wait_min_seconds"] = min;
        json["buyer_wait_max_seconds"] = max;
        Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void InvalidCommonCraftCostIsRejected(int cost)
    {
        var json = JsonNode.Parse(ConfigJson())!.AsObject();
        json["craft_cost"] = cost;
        Assert.Throws<EconomyConfigException>(() => EconomyConfigLoader.Load(json.ToJsonString()));
    }

    [Test]
    public void LegacyItemPricesCannotOverrideTheSharedSalePrice()
    {
        var json = JsonNode.Parse(ConfigJson())!.AsObject();
        json["items"]![0]!["base_price"] = 900;
        json["items"]![1]!["base_price"] = 5;
        var config = EconomyConfigLoader.Load(json.ToJsonString());
        Assert.That(config.BasePrice, Is.EqualTo(50));
        Assert.That(config.Items, Has.Count.EqualTo(10));
    }
}
