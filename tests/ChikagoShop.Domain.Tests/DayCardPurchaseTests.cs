using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayCardPurchaseTests
{
    [TestCase(DayUpgrade.Cash, 2, false, 0)]
    [TestCase(DayUpgrade.Atelier, 3, false, 15)]
    [TestCase(DayUpgrade.Music, 4, true, 25)]
    [TestCase(DayUpgrade.AtelierPrice, 4, false, 10)]
    [TestCase(DayUpgrade.MusicPrice, 5, true, 20)]
    public void PurchaseRequiresFundsAndChargesOnlyOnce(DayUpgrade choice, int day, bool musicUnlocked, int cost)
    {
        var session = new DayUpgradeSession();
        session.Offer(day, musicUnlocked);
        Assert.That(DayUpgradeSession.Cost(choice), Is.EqualTo(cost));
        if (!session.Options.Contains(choice))
        {
            // The user may only buy one of the offered three cards.
            Assert.That(session.TryPurchase(choice, cost, out _), Is.False);
            return;
        }
        Assert.That(session.TryPurchase(choice, cost - 1, out int balance), Is.False);
        Assert.That(balance, Is.EqualTo(cost - 1));
        Assert.That(session.Pending, Is.True);
        Assert.That(session.TryPurchase(choice, cost, out balance), Is.True);
        Assert.That(balance, Is.Zero);
        Assert.That(session.TryPurchase(choice, 1000, out balance), Is.False);
        Assert.That(balance, Is.EqualTo(1000));
        Assert.That(session.TrySkip(), Is.False);
    }

    [TestCase(DayUpgrade.Energy)]
    [TestCase(DayUpgrade.Time)]
    public void RetiredCardsCannotBePurchasedOrOffered(DayUpgrade retired)
    {
        var session = new DayUpgradeSession();
        for (int day = 2; day <= 14; day++)
        {
            session.Offer(day, musicUnlocked: true);
            Assert.That(session.Options, Does.Not.Contain(retired));
            Assert.That(session.TryPurchase(retired, 1000, out _), Is.False);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => DayUpgradeSession.Cost(retired));
    }

    [Test]
    public void SkipIsFreeAndLegacyPermanentLevelsAreDiscardedOnRestore()
    {
        var session = new DayUpgradeSession();
        session.Restore(true, DayUpgrade.None, 6, atelierLevel: 2, musicPriceLevel: 1);
        Assert.That(session.TrySkip(), Is.True);
        Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(session.AtelierLevel + session.MusicPriceLevel, Is.Zero);
        var restored = new DayUpgradeSession();
        restored.Restore(session.Pending, session.Selected, 6, atelierLevel: 6, musicPriceLevel: 6);
        Assert.That(restored.Price("atelier", 100), Is.EqualTo(100));
        Assert.That(restored.Price("music", 100), Is.EqualTo(100));
    }

    [Test]
    public void LegacyEnergySelectionHasNoBonusAndCannotBePurchased()
    {
        foreach (int baseline in new[] { 3, 6 })
        {
            var session = new DayUpgradeSession();
            session.Offer(2);
            Assert.That(session.TryPurchase(DayUpgrade.Energy, 10, out int balance), Is.False);
            Assert.That(balance, Is.EqualTo(10));
            Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(baseline));
            session.Restore(false, DayUpgrade.Energy);
            Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
            Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(baseline));
            session.Reset(preserveCategories: true);
            Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(baseline));
        }
    }
}
