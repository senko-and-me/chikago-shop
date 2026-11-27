using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayUpgradeSessionTests
{
    [TestCase(DayUpgrade.Cash, 2, false)]
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
    public void RetiredCardSavesAreDiscardedWithoutRenumberingValues()
    {
        var upgrades = new DayUpgradeSession();
        Assert.That((int)DayUpgrade.Energy, Is.EqualTo(2));
        Assert.That((int)DayUpgrade.Time, Is.EqualTo(3));
        Assert.That((int)DayUpgrade.MusicPrice, Is.EqualTo(7));
        foreach (var retired in new[] { DayUpgrade.Energy, DayUpgrade.Time })
        {
            upgrades.Restore(false, retired);
            Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
            Assert.That(upgrades.EnergyCapacityFor(6), Is.EqualTo(6));
            upgrades.Restore(true, retired);
            Assert.That(upgrades.Pending, Is.True);
        }
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
    public void CraftTimeBoostIsScopedRestoredAndExpiresWithoutChangingRarity(DayUpgrade card, string workshop, string other)
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, card, 5, musicUnlocked: true);
        string[] rarities = ["common", "ordinary", "first_class", "exclusive", "unique"];
        int[] baseline = [60, 25, 10, 4, 1];
        int[] weights = rarities.Select((rarity, i) => upgrades.Weight(workshop, rarity, baseline[i], 60)).ToArray();
        Assert.That(weights, Is.EqualTo(baseline));
        Assert.That(weights.Sum(), Is.EqualTo(100));
        Assert.That(upgrades.CraftDuration(workshop, 10), Is.EqualTo(12));
        Assert.That(upgrades.CraftDuration(workshop, 5), Is.EqualTo(6));
        Assert.That(upgrades.CraftDuration(other, 10), Is.EqualTo(10));
        Assert.That(upgrades.CraftDuration("unknown", 10), Is.EqualTo(10));
        for (int i = 0; i < baseline.Length; i++)
            Assert.That(upgrades.Weight(other, rarities[i], baseline[i], 60), Is.EqualTo(baseline[i]));
        upgrades.Reset(preserveCategories: true);
        Assert.That(upgrades.CraftDuration(workshop, 10), Is.EqualTo(10));
        for (int i = 0; i < baseline.Length; i++)
            Assert.That(upgrades.Weight(workshop, rarities[i], baseline[i], 60), Is.EqualTo(baseline[i]));
        Assert.That(upgrades.AtelierLevel + upgrades.MusicLevel, Is.Zero);
    }

    [Test]
    public void CraftTimeBoostRequiresAnEligibleSelectionAndPositiveFiniteDuration()
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, DayUpgrade.Music, musicUnlocked: false);
        Assert.That(upgrades.CraftDuration("music", 10), Is.EqualTo(10));
        upgrades.Restore(false, DayUpgrade.Atelier, musicUnlocked: true);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(12));
        upgrades.Offer(4, musicUnlocked: true);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(10));
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.CraftDuration("atelier", invalid));
        upgrades.Restore(false, DayUpgrade.AtelierPrice);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(10));
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
            Assert.That(options, Does.Not.Contain(DayUpgrade.Energy));
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

    [TestCase(false, false, 3)]
    [TestCase(false, true, 2)]
    [TestCase(true, false, 3)]
    [TestCase(true, true, 3)]
    public void EveryDayOffersUniqueEligibleCardsIncludingTraining(bool music, bool training, int count)
    {
        foreach (bool weighted in new[] { false, true })
        for (int day = 1; day <= 30; day++)
        {
            var rules = new DayUpgradeRules
            {
                WeightedSelection = weighted,
                Weights = new Dictionary<DayUpgrade, int>
                {
                    [DayUpgrade.Energy] = int.MaxValue,
                    [DayUpgrade.Time] = int.MaxValue
                }
            };
            var options = rules.Options(day, music, training);
            Assert.That(options.Count, Is.EqualTo(count));
            Assert.That(options.Distinct().Count(), Is.EqualTo(count));
            Assert.That(options, Does.Not.Contain(DayUpgrade.Energy));
            Assert.That(options, Does.Not.Contain(DayUpgrade.Time));
            if (training) Assert.That(options, Does.Not.Contain(DayUpgrade.Cash));
            if (!music)
            {
                Assert.That(options, Does.Not.Contain(DayUpgrade.Music));
                Assert.That(options, Does.Not.Contain(DayUpgrade.MusicPrice));
            }
            Assert.That(rules.Options(day, music, training), Is.EqualTo(options));
        }
    }

    [Test]
    public void TrainingWithZeroWeightsFallsBackToTwoDistinctAtelierCards()
    {
        var rules = new DayUpgradeRules
        {
            WeightedSelection = true,
            Weights = new Dictionary<DayUpgrade, int>
            {
                [DayUpgrade.Atelier] = 0,
                [DayUpgrade.AtelierPrice] = 0,
                [DayUpgrade.Energy] = int.MaxValue
            }
        };
        for (int day = 2; day <= 14; day++)
            Assert.That(rules.Options(day, musicUnlocked: false, training: true),
                Is.EquivalentTo(new[] { DayUpgrade.Atelier, DayUpgrade.AtelierPrice }));
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
