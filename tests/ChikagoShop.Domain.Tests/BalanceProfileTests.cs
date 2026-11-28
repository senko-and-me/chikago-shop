using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class BalanceProfileTests
{
    private static readonly BalanceField[] Fields = [new("count", "Craft", "Count", 1, 1, 50)];
    private static void Validate(BalanceProfile profile) => profile.Validate(Fields, new HashSet<string> { "cap" }, new HashSet<string> { "unique" });

    [Test]
    public void ProfileRoundTripsAndClonesWithoutSharingEditableDictionaries()
    {
        var profile = new BalanceProfile { FirstQuotaBaselineRevision = 1, EconomyBaselineRevision = 1, Numbers = new() { ["count"] = 3 }, ItemRarities = new() { ["cap"] = "unique" } };
        var loaded = BalanceProfile.FromJson(profile.ToJson());
        Validate(loaded);
        Assert.AreEqual(3, loaded.Number("count", 1));
        Assert.AreEqual("unique", loaded.ItemRarities["cap"]);
        var clone = loaded.Clone(); clone.Numbers["count"] = 5;
        Assert.AreEqual(3, loaded.Numbers["count"]);
        Assert.AreEqual(1, clone.FirstQuotaBaselineRevision);
        Assert.AreEqual(1, loaded.EconomyBaselineRevision);
        Assert.AreEqual(1, clone.EconomyBaselineRevision);
    }

    [TestCase(-1)] [TestCase(0)] [TestCase(1.5)] [TestCase(51)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
    public void RejectsInvalidNumericValues(double value) => Assert.Throws<ArgumentException>(() =>
        Validate(new BalanceProfile { Numbers = new() { ["count"] = value } }));

    [Test]
    public void RejectsUnknownParametersRaritiesAndSchema()
    {
        Assert.Throws<ArgumentException>(() => Validate(new BalanceProfile { SchemaVersion = 2 }));
        Assert.Throws<ArgumentException>(() => Validate(new BalanceProfile { Numbers = new() { ["typo"] = 2 } }));
        Assert.Throws<ArgumentException>(() => Validate(new BalanceProfile { ItemRarities = new() { ["cap"] = "wrong" } }));
        Assert.Throws<ArgumentException>(() => Validate(new BalanceProfile { ItemRarities = new() { ["missing"] = "unique" } }));
        Assert.Catch<Exception>(() => BalanceProfile.FromJson("{broken json"));
    }

    [Test]
    public void ZeroRarityWeightExcludesItemsAndOtherRaritiesNormalize()
    {
        var table = new RarityItemTable(new[] { (0, "common"), (1, "unique"), (2, "unique") },
            new Dictionary<string, int> { ["common"] = 0, ["unique"] = 10 });
        var results = Enumerable.Range(0, table.TotalWeight).Select(table.Select).ToArray();
        CollectionAssert.DoesNotContain(results, 0);
        Assert.AreEqual(results.Count(value => value == 1), results.Count(value => value == 2));
    }

    [Test]
    public void UniformCardsAreStableWithoutDuplicatesAndPurchaseUsesEditedCost()
    {
        var terms = DayUpgradeRules.DefaultTerms();
        terms[DayUpgrade.AtelierPrice] = terms[DayUpgrade.AtelierPrice] with { Cost = 17 };
        var rules = new DayUpgradeRules { Terms = terms };
        var session = new DayUpgradeSession { Rules = rules };
        session.Offer(Enumerable.Range(2, 100).First(day => rules.Options(day).Contains(DayUpgrade.AtelierPrice)));
        Assert.That(session.Options, Has.Count.EqualTo(3));
        Assert.IsFalse(session.TryPurchase(DayUpgrade.AtelierPrice, 16, out _));
        Assert.IsTrue(session.TryPurchase(DayUpgrade.AtelierPrice, 100, out int cash));
        Assert.AreEqual(83, cash);
        var many = new DayUpgradeRules { Seed = 71 };
        Assert.AreEqual(many.Options(3), new DayUpgradeRules { Seed = 71 }.Options(3));
        Assert.AreEqual(3, many.Options(3).Distinct().Count());
        CollectionAssert.DoesNotContain(many.Options(3), DayUpgrade.Energy);
    }

    [Test]
    public void EveryAvailableCardHasSimilarFrequencyAcrossSeeds()
    {
        var counts = Enumerable.Range(0, 1800).SelectMany(seed => new DayUpgradeRules { Seed = seed }
            .Options(2, musicUnlocked: true)).GroupBy(card => card).Select(cards => cards.Count()).ToArray();
        Assert.That(counts, Has.Length.EqualTo(8));
        Assert.That(counts, Is.All.InRange(595, 755));
    }

    [Test]
    public void ConfiguredCraftAndDayDurationsAreUsedWithoutChangingDefaultLimits()
    {
        var craft = new EmbroiderySession(duration: 25); craft.Start(); craft.Tick(11);
        Assert.AreEqual(14, craft.RemainingSeconds);
        craft.FinishForTesting(true); Assert.AreEqual(25, craft.RemainingSeconds);
        var polish = new PolishingSession(Enumerable.Repeat((byte)255, 49).ToArray(), 7, 7, duration: 7);
        polish.Start(); polish.Tick(7); Assert.AreEqual(EmbroideryState.Finished, polish.State);
        var day = new DaySession(5, 3630); day.Reset(300); day.Tick(10); day.ConfigureDuration(60);
        Assert.AreEqual(50, day.RemainingSeconds);
        var baseline = new DaySession(); baseline.Reset(300); Assert.AreEqual(120, baseline.Duration);
    }
}
