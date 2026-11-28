using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayUpgradeSessionTests
{
    [TestCase(DayUpgrade.Cash, 2, false)]
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
        Assert.That(upgrades.CashReward, Is.EqualTo(15));
        upgrades.Reset(preserveCategories: true);
        Assert.That(upgrades.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(upgrades.EnergyCapacity, Is.EqualTo(2));
    }

    [Test]
    public void RotationRespectsLockedRooms()
    {
        var rules = new DayUpgradeRules { Seed = 81 };
        var beforeMusic = rules.Options(4, musicUnlocked: false);
        Assert.That(beforeMusic, Has.Count.EqualTo(3));
        Assert.That(beforeMusic, Is.EqualTo(new DayUpgradeRules { Seed = 81 }.Options(4)));
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
        foreach (var retired in new[] { DayUpgrade.Energy, DayUpgrade.Time, DayUpgrade.Atelier, DayUpgrade.Music })
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
    public void RetiredCraftTimeCardsLeaveDurationAndLegacyWeightsUnchanged(DayUpgrade card, string workshop, string other)
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, card, 5, musicUnlocked: true);
        string[] rarities = ["common", "ordinary", "first_class", "exclusive", "unique"];
        int[] baseline = [60, 25, 10, 4, 1];
        int[] weights = rarities.Select((rarity, i) => upgrades.Weight(workshop, rarity, baseline[i], 60)).ToArray();
        Assert.That(weights, Is.EqualTo(baseline));
        Assert.That(weights.Sum(), Is.EqualTo(100));
        Assert.That(upgrades.CraftDuration(workshop, 10), Is.EqualTo(10));
        Assert.That(upgrades.CraftDuration(workshop, 5), Is.EqualTo(5));
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
    public void RetiredCraftTimeCardsStillValidatePositiveFiniteBaseline()
    {
        var upgrades = new DayUpgradeSession();
        upgrades.Restore(false, DayUpgrade.Music, musicUnlocked: false);
        Assert.That(upgrades.CraftDuration("music", 10), Is.EqualTo(10));
        upgrades.Restore(false, DayUpgrade.Atelier, musicUnlocked: true);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(10));
        upgrades.Offer(4, musicUnlocked: true);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(10));
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => upgrades.CraftDuration("atelier", invalid));
        upgrades.Restore(false, DayUpgrade.AtelierPrice);
        Assert.That(upgrades.CraftDuration("atelier", 10), Is.EqualTo(10));
    }

    [Test]
    public void AllOffersHaveUniqueEligibleCardsAndAreStableAcrossReload()
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
    [TestCase(false, true, 3)]
    [TestCase(true, false, 3)]
    [TestCase(true, true, 3)]
    public void EveryDayOffersUniqueEligibleCardsIncludingTraining(bool music, bool training, int count)
    {
        for (int day = 1; day <= 30; day++)
        {
            var rules = new DayUpgradeRules();
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
    public void TrainingWithoutMusicOrCashStillDealsThreeCards()
    {
        var rules = new DayUpgradeRules();
        for (int day = 2; day <= 14; day++)
        {
            var options = rules.Options(day, musicUnlocked: false, training: true);
            Assert.That(options.Count, Is.EqualTo(3));
            Assert.That(options, Does.Not.Contain(DayUpgrade.Cash));
            Assert.That(options, Does.Not.Contain(DayUpgrade.MusicPrice));
        }
    }

    [TestCase(DayUpgrade.AtelierPrice, "atelier", "music")]
    [TestCase(DayUpgrade.MusicPrice, "music", "atelier")]
    public void WorkshopBonusAppliesToEveryEligibleSaleAndExpires(DayUpgrade card, string workshop, string other)
    {
        var session = new DayUpgradeSession();
        session.Restore(false, card, musicUnlocked: true);
        foreach (int baseline in new[] { 30, 45, 50, 75, 80, 120, 200, 300 })
        {
            Assert.That(session.Price(workshop, baseline), Is.EqualTo(baseline));
            Assert.That(session.Price(other, baseline), Is.EqualTo(baseline));
        }
        for (int i = 0; i < 8; i++)
        {
            Assert.That(session.SaleBonus(workshop, "D", 0), Is.EqualTo(session.Rules.For(card).Amount));
            Assert.That(session.SaleBonus(other, "S", 10), Is.Zero);
        }
        session.Reset(preserveCategories: true);
        Assert.That(session.SaleBonus(workshop, "S", 10), Is.Zero);
        Assert.That(session.Price(workshop, 100), Is.EqualTo(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Price("atelier", -1));
    }

    [Test]
    public void LegacyLevelFieldsAreIgnoredButRestoredSelectionWorks()
    {
        var session = new DayUpgradeSession();
        session.Restore(false, DayUpgrade.AtelierPrice, atelierLevel: 6, musicLevel: 6,
            atelierPriceLevel: 6, musicPriceLevel: 6);
        Assert.That(session.Price("atelier", 100), Is.EqualTo(100));
        Assert.That(session.Price("music", 100), Is.EqualTo(100));
        Assert.That(session.AtelierLevel + session.MusicLevel + session.AtelierPriceLevel + session.MusicPriceLevel, Is.Zero);
    }

    [Test]
    public void AllDayRulesRespectEligibilityAndRestore()
    {
        var session = new DayUpgradeSession();
        session.Offer(3, training: true);
        var options = session.Options.ToArray();
        session.Restore(true, DayUpgrade.None, 3, training: true);
        Assert.That(session.Options, Is.EqualTo(options));
        session.Restore(false, DayUpgrade.Materials);
        for (int i = 0; i < 10; i++)
            Assert.That(session.CraftPrice(20), Is.EqualTo(20 - session.Rules.For(DayUpgrade.Materials).Amount));
        session.Restore(false, DayUpgrade.QualityMark);
        foreach (var rank in new[] { "B", "A", "S" })
            Assert.That(session.SaleBonus("atelier", rank, 0), Is.EqualTo(session.Rules.For(DayUpgrade.QualityMark).Amount));
        Assert.That(session.SaleBonus("music", "C", 10), Is.Zero);
        session.Restore(false, DayUpgrade.QuickService);
        Assert.That(session.SaleBonus("music", "D", 0), Is.Zero);
        Assert.That(session.SaleBonus("music", "D", 1), Is.EqualTo(session.Rules.For(DayUpgrade.QuickService).Amount));
        session.Restore(false, DayUpgrade.Patient);
        Assert.That(session.WaitExtension, Is.EqualTo(session.Rules.For(DayUpgrade.Patient).Amount));
        session.Restore(false, DayUpgrade.Bargaining);
        Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(DayUpgradeRules.Available, Does.Not.Contain(DayUpgrade.Bargaining));
        session.Restore(false, DayUpgrade.Loyal);
        for (int offer = -30; offer <= 30; offer++)
            Assert.That(session.ExpiredOffer(offer), Is.EqualTo(Math.Min(offer, -session.Rules.For(DayUpgrade.Loyal).Amount)));
        session.Offer(4);
        Assert.That(session.ExpiredOffer(30), Is.EqualTo(-50));
        Assert.That(session.WaitExtension, Is.Zero);
        Assert.That(session.CraftPrice(20), Is.EqualTo(20));
    }
}
