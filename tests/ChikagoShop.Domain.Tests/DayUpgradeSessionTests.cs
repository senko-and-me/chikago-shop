using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayUpgradeSessionTests
{
    [TestCase(DayUpgrade.Cash, 2, false)]
    [TestCase(DayUpgrade.Energy, 3, false)]
    [TestCase(DayUpgrade.Atelier, 2, false)]
    [TestCase(DayUpgrade.Music, 2, true)]
    [TestCase(DayUpgrade.AtelierPrice, 2, false)]
    [TestCase(DayUpgrade.MusicPrice, 2, true)]
    public void OneDayChoiceSurvivesRestore(DayUpgrade choice, int energy, bool unlocked)
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Offer(3, unlocked);
        // Restore the exact choice as if it was selected on a previous launch.
        upgrades.Restore(false, choice, 3, musicUnlocked: unlocked);
        Assert.That(upgrades.Selected, Is.EqualTo(choice));
        Assert.That(upgrades.EnergyCapacity, Is.EqualTo(energy));
        Assert.That(upgrades.CashReward, Is.EqualTo(50));
        upgrades.Reset(preserveCategories: true);
        Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(upgrades.EnergyCapacity, Is.EqualTo(2));
    }

    [Test]
    public void WeightedDealsRespectZeroWeightsAndLockedRooms()
    {
        var rules = new DayUpgradeRules
        {
            WeightedSelection = true,
            Seed = 81,
            Weights = new Dictionary<DayUpgrade, int> { [DayUpgrade.Cash] = 10,
                [DayUpgrade.Atelier] = 0, [DayUpgrade.Energy] = 0,
                [DayUpgrade.Music] = 100, [DayUpgrade.MusicPrice] = 100,
                [DayUpgrade.AtelierPrice] = 0 }
        };
        var beforeMusic = rules.Options(4, musicUnlocked: false);
        Assert.That(beforeMusic, Has.Count.EqualTo(1));
        Assert.That(beforeMusic, Does.Contain(DayUpgrade.Cash));
        Assert.That(beforeMusic, Does.Not.Contain(DayUpgrade.Music));
        Assert.That(beforeMusic, Does.Not.Contain(DayUpgrade.MusicPrice));
        Assert.That(rules.Options(4, musicUnlocked: false), Is.EqualTo(beforeMusic));
    }

    [Test]
    public void ExistingTimeCardSaveIsRetiredWithoutRenumberingValues()
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, DayUpgrade.Time);
        Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
        upgrades.Restore(true, (DayUpgrade)99);
        Assert.That(upgrades.Pending, Is.True);
        Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
    }

    [Test]
    public void DaySessionStillRestoresFractionalTimeAndExpires()
    {
        var day = new DaySession();
        day.Restore(119.75, false, 120);
        Assert.That(day.RemainingSeconds, Is.EqualTo(119.75));
        Assert.That(day.Tick(119.75), Is.True);
        day.Reset(60);
        // The no-argument DaySession instance has an old minimum of 90 seconds;
        // the live DayCycleController explicitly uses a 5..3630-second session.
        Assert.That(day.RemainingSeconds, Is.EqualTo(90));
    }

    [TestCase(DayUpgrade.Atelier, "atelier", "music")]
    [TestCase(DayUpgrade.Music, "music", "atelier")]
    public void RarityBoostAppliesOnceAndDisappearsOnDayEnd(DayUpgrade card, string workshop, string other)
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, card, 5, musicUnlocked: true);
        string[] rarities = ["common", "ordinary", "first_class", "exclusive", "unique"];
        int[] baseline = [60, 25, 10, 4, 1];
        int[] expected = [48, 25, 16, 8, 3];
        int[] weights = rarities.Select((rarity, i) => upgrades.Weight(workshop, rarity, baseline[i], 60)).ToArray();
        Assert.That(weights, Is.EqualTo(expected));
        Assert.That(weights.Sum(), Is.EqualTo(100));
        for (int i = 0; i < baseline.Length; i++)
            Assert.That(upgrades.Weight(other, rarities[i], baseline[i], 60), Is.EqualTo(baseline[i]));
        upgrades.Reset(preserveCategories: true);
        for (int i = 0; i < baseline.Length; i++)
            Assert.That(upgrades.Weight(workshop, rarities[i], baseline[i], 60), Is.EqualTo(baseline[i]));
        Assert.That(upgrades.AtelierLevel + upgrades.MusicLevel, Is.Zero);
    }

    [Test]
    public void AllOffersHaveThreeEligibleCardsAndAreStableAcrossReload()
    {
        var session = new DayUpgradeSession();
        for (int day = 2; day <= 14; day++)
        foreach (bool unlocked in new[] { false, true })
        {
            session.Offer(day, unlocked);
            var options = session.Options;
            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options.Distinct().Count(), Is.EqualTo(3));
            Assert.That(options, Does.Not.Contain(DayUpgrade.Time));
            if (!unlocked)
            {
                Assert.That(options, Does.Not.Contain(DayUpgrade.Music));
                Assert.That(options, Does.Not.Contain(DayUpgrade.MusicPrice));
            }
            var restored = new DayUpgradeSession();
            restored.Restore(true, DayUpgrade.None, day, musicUnlocked: unlocked);
            Assert.That(restored.Options, Is.EqualTo(options));
        }
    }

    [TestCase(DayUpgrade.AtelierPrice, "atelier", "music")]
    [TestCase(DayUpgrade.MusicPrice, "music", "atelier")]
    public void TwentyPercentPriceBoostExpiresAfterTheNextDay(DayUpgrade card, string workshop, string other)
    {
        var session = new DayUpgradeSession();
        session.Restore(false, card, musicUnlocked: true);
        foreach (int baseline in new[] { 30, 45, 50, 75, 80, 120, 200, 300 })
        {
            int expected = (int)decimal.Round(baseline * 1.2m, 0, MidpointRounding.AwayFromZero);
            Assert.That(session.Price(workshop, baseline), Is.EqualTo(expected));
            Assert.That(session.Price(other, baseline), Is.EqualTo(baseline));
        }
        session.Reset(preserveCategories: true);
        Assert.That(session.Price(workshop, 100), Is.EqualTo(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Price("atelier", -1));
    }

    [Test]
    public void LegacyLevelFieldsAreIgnoredButRestoredSelectionWorks()
    {
        var session = new DayUpgradeSession();
        session.Restore(false, DayUpgrade.AtelierPrice, atelierLevel: 6, musicLevel: 6,
            atelierPriceLevel: 6, musicPriceLevel: 6);
        Assert.That(session.Price("atelier", 100), Is.EqualTo(120));
        Assert.That(session.Price("music", 100), Is.EqualTo(100));
        Assert.That(session.AtelierLevel + session.MusicLevel + session.AtelierPriceLevel + session.MusicPriceLevel, Is.Zero);
    }
}
