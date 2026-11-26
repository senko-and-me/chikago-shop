using ChikagoShop.Domain.Configuration;
using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class BalanceProfileTests
{
    private static readonly BalanceField[] Fields = [new("hand.offer", "Run", "Offer", 3, 0, 3)];

    [Test]
    public void ProfilesIgnoreOmittedLegacyRarityEditorsAndCloneIndependently()
    {
        var profile = BalanceProfile.FromJson("{\"Numbers\":{\"hand.offer\":2},\"ItemRarities\":{\"hat\":\"unique\"}}");
        profile.Validate(Fields);
        var clone = profile.Clone();
        clone.Numbers["hand.offer"] = 1;
        Assert.That(profile.Numbers["hand.offer"], Is.EqualTo(2));
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(1.5)]
    public void NumericOverridesMustFitFieldLimits(double value)
    {
        var profile = new BalanceProfile();
        profile.Numbers["hand.offer"] = value;
        Assert.Throws<ArgumentException>(() => profile.Validate(Fields));
    }

    [Test]
    public void WeightedSelectionPreservesEligiblePriceCards()
    {
        var rules = new DayUpgradeRules
        {
            WeightedSelection = true,
            Weights = new Dictionary<DayUpgrade, int> { [DayUpgrade.Cash] = 0, [DayUpgrade.MusicPrice] = 30 },
            Costs = new Dictionary<DayUpgrade, int> { [DayUpgrade.AtelierPrice] = 15 }
        };
        Assert.That(rules.Options(8), Is.EqualTo(new[] { DayUpgrade.AtelierPrice }));
        Assert.That(rules.Options(8, true), Is.EquivalentTo(new[] { DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice }));
        Assert.That(rules.Cost(DayUpgrade.AtelierPrice), Is.EqualTo(15));
    }
}
