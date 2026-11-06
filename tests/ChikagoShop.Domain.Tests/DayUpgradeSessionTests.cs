using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayUpgradeSessionTests
{
    [TestCase(DayUpgrade.Cash, 2, 120)]
    [TestCase(DayUpgrade.Energy, 3, 120)]
    [TestCase(DayUpgrade.Time, 2, 150)]
    public void ChoiceIsExclusiveAndSurvivesRestore(DayUpgrade choice, int energy, double duration)
    {
        var upgrades = new DayUpgradeSession();
        Assert.That(upgrades.TryChoose(choice), Is.False);
        upgrades.Offer();
        Assert.That(upgrades.TryChoose(DayUpgrade.None), Is.False);
        Assert.That(upgrades.TryChoose((DayUpgrade)99), Is.False);
        Assert.That(upgrades.Pending, Is.True);
        Assert.That(upgrades.TryChoose(choice), Is.True);
        foreach (DayUpgrade other in new[] { DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Time })
            Assert.That(upgrades.TryChoose(other), Is.False);
        var restored = new DayUpgradeSession();
        restored.Restore(upgrades.Pending, upgrades.Selected);
        Assert.That(restored.TryChoose(choice), Is.False);
        Assert.That(restored.EnergyCapacity, Is.EqualTo(energy));
        Assert.That(restored.Duration, Is.EqualTo(duration));
        Assert.That(restored.CashReward, Is.EqualTo(50));
        restored.Reset();
        Assert.That(restored.EnergyCapacity, Is.EqualTo(2));
        Assert.That(restored.Duration, Is.EqualTo(120));
        Assert.That(restored.Pending, Is.False);
    }

    [Test]
    public void PendingRestoreRequiresAChoiceAndDoesNotInventAReward()
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(true, (DayUpgrade)99);
        Assert.That(upgrades.Pending, Is.True);
        Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
        upgrades.Restore(true, DayUpgrade.Cash);
        Assert.That(upgrades.Pending, Is.False);
        Assert.That(upgrades.TryChoose(DayUpgrade.Cash), Is.False);
    }

    [Test]
    public void ExtraTimeReloadRetainsFractionAndStillExpires()
    {
        var day = new DaySession();
        day.Restore(149.75, false, 150);
        Assert.That(day.RemainingSeconds, Is.EqualTo(149.75));
        Assert.That(day.Tick(149.75), Is.True);
        Assert.That(day.CanReceiveBuyers, Is.False);
        day.Reset();
        Assert.That(day.RemainingSeconds, Is.EqualTo(120));
        day.Restore(1000, false, 150);
        Assert.That(day.RemainingSeconds, Is.EqualTo(150));
    }

    [TestCase(DayUpgrade.Atelier, "atelier", "music")]
    [TestCase(DayUpgrade.Music, "music", "atelier")]
    public void CategoryBoostStacksPersistsAndPreservesTheDistribution(DayUpgrade choice, string workshop, string other)
    {
        var upgrades = new DayUpgradeSession();
        string[] rarities = ["common", "ordinary", "first_class", "exclusive", "unique"];
        int[] baseline = [60, 25, 10, 4, 1];
        for (int level = 1; level <= 6; level++)
        {
            upgrades.Offer(4);
            Assert.That(upgrades.TryChoose(choice), Is.True);
            Assert.That(upgrades.TryChoose(choice), Is.False);
            int[] weights = rarities.Select((rarity, i) => upgrades.Weight(workshop, rarity, baseline[i], 60)).ToArray();
            Assert.That(weights.Sum(), Is.EqualTo(100));
            Assert.That(weights.All(value => value > 0), Is.True);
            Assert.That(weights[0], Is.LessThan(60));
            Assert.That(weights[1], Is.EqualTo(25));
            for (int i = 2; i < 5; i++) Assert.That(weights[i], Is.GreaterThan(baseline[i]));
            for (int i = 0; i < 5; i++) Assert.That(upgrades.Weight(other, rarities[i], baseline[i], 60), Is.EqualTo(baseline[i]));
            upgrades.Reset(preserveCategories: true);
            Assert.That(workshop == "atelier" ? upgrades.AtelierLevel : upgrades.MusicLevel, Is.EqualTo(level));
        }
        upgrades.Reset();
        Assert.That(upgrades.AtelierLevel + upgrades.MusicLevel, Is.Zero);
    }

    [Test]
    public void OffersContainExactlyThreeAndRejectAnUnshownUpgrade()
    {
        var upgrades = new DayUpgradeSession();
        for (int day = 2; day <= 7; day++)
        {
            upgrades.Offer(day);
            Assert.That(upgrades.Options.Distinct().Count(), Is.EqualTo(3));
            var unshown = Enum.GetValues<DayUpgrade>().First(choice => choice != DayUpgrade.None && !upgrades.Options.Contains(choice));
            Assert.That(upgrades.TryChoose(unshown), Is.False);
            var restored = new DayUpgradeSession();
            restored.Restore(upgrades.Pending, upgrades.Selected, day);
            Assert.That(restored.Options, Is.EqualTo(upgrades.Options));
            Assert.That(restored.Pending, Is.True);
        }
    }

    [TestCase(DayUpgrade.AtelierPrice, "atelier", "music", 5)]
    [TestCase(DayUpgrade.MusicPrice, "music", "atelier", 6)]
    public void PriceCardsStackAdditivelyWithoutChangingOdds(DayUpgrade choice, string workshop, string other, int day)
    {
        var upgrades = new DayUpgradeSession();
        for (int level = 1; level <= 6; level++)
        {
            upgrades.Offer(day);
            Assert.That(upgrades.TryChoose(choice), Is.True);
            Assert.That(upgrades.TryChoose(choice), Is.False);
            foreach (int price in new[] { 30, 45, 50, 75, 80, 120, 180, 200, 300 })
            {
                Assert.That(upgrades.Price(workshop, price), Is.EqualTo((int)decimal.Round(price * (100m + level * 20) / 100, 0, MidpointRounding.AwayFromZero)));
                Assert.That(upgrades.Price(other, price), Is.EqualTo(price));
            }
            Assert.That(upgrades.Weight(workshop, "common", 60, 60), Is.EqualTo(60));
            var restored = new DayUpgradeSession();
            restored.Restore(false, upgrades.Selected, day, atelierPriceLevel: upgrades.AtelierPriceLevel, musicPriceLevel: upgrades.MusicPriceLevel);
            Assert.That(restored.Price(workshop, 45), Is.EqualTo(upgrades.Price(workshop, 45)));
            Assert.That(restored.TryChoose(choice), Is.False);
            upgrades.Reset(preserveCategories: true);
        }
        upgrades.Reset();
        Assert.That(upgrades.Price(workshop, 45), Is.EqualTo(45));
    }

    [Test]
    public void PriceRoundingAndRestoreBoundsAreSafe()
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, DayUpgrade.None, atelierPriceLevel: 99, musicPriceLevel: -1);
        Assert.That(upgrades.AtelierPriceLevel, Is.EqualTo(6));
        Assert.That(upgrades.MusicPriceLevel, Is.Zero);
        Assert.That(upgrades.Price("atelier", 3), Is.EqualTo(7));
        Assert.That(upgrades.Price("music", 3), Is.EqualTo(3));
        Assert.That(upgrades.Price("unknown", 3), Is.EqualTo(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.Price("atelier", -1));
        Assert.Throws<OverflowException>(() => upgrades.Price("atelier", int.MaxValue));
    }
}
