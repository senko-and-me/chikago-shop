using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayCardPurchaseTests
{
    [TestCase(DayUpgrade.Cash, 2, 20)]
    [TestCase(DayUpgrade.Energy, 2, 40)]
    [TestCase(DayUpgrade.Time, 2, 40)]
    [TestCase(DayUpgrade.Atelier, 4, 80)]
    [TestCase(DayUpgrade.Music, 4, 80)]
    [TestCase(DayUpgrade.AtelierPrice, 6, 100)]
    [TestCase(DayUpgrade.MusicPrice, 6, 100)]
    public void PurchaseRequiresFundsAndChargesOnlyOnce(DayUpgrade choice, int day, int cost)
    {
        var session = new DayUpgradeSession();
        session.Offer(day);
        Assert.That(DayUpgradeSession.Cost(choice), Is.EqualTo(cost));
        Assert.That(session.TryPurchase(choice, cost - 1, out int balance), Is.False);
        Assert.That(balance, Is.EqualTo(cost - 1));
        Assert.That(session.Pending, Is.True);
        Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(session.AtelierLevel + session.MusicLevel + session.AtelierPriceLevel + session.MusicPriceLevel, Is.Zero);
        Assert.That(session.TryPurchase(choice, cost, out balance), Is.True);
        Assert.That(balance, Is.Zero);
        Assert.That(session.TryPurchase(choice, 1000, out balance), Is.False);
        Assert.That(balance, Is.EqualTo(1000));
        Assert.That(session.TrySkip(), Is.False);
    }

    [Test]
    public void SkipIsFreePreservesEarlierUpgradesAndSurvivesReload()
    {
        var session = new DayUpgradeSession();
        session.Restore(true, DayUpgrade.None, 6, atelierLevel: 2, musicPriceLevel: 1);
        Assert.That(session.TrySkip(), Is.True);
        Assert.That(session.Pending, Is.False);
        Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(session.AtelierLevel, Is.EqualTo(2));
        Assert.That(session.MusicPriceLevel, Is.EqualTo(1));
        Assert.That(session.TrySkip(), Is.False);
        var restored = new DayUpgradeSession();
        restored.Restore(session.Pending, session.Selected, 6, session.AtelierLevel, session.MusicLevel,
            session.AtelierPriceLevel, session.MusicPriceLevel);
        Assert.That(restored.TryPurchase(DayUpgrade.MusicPrice, 1000, out _), Is.False);
        Assert.That(restored.Pending, Is.False);
        Assert.That(restored.MusicPriceLevel, Is.EqualTo(1));
    }

    [Test]
    public void InvalidOrUnaffordablePurchaseCannotConsumeTheOffer()
    {
        var session = new DayUpgradeSession();
        session.Offer();
        foreach (var choice in new[] { DayUpgrade.None, (DayUpgrade)99, DayUpgrade.MusicPrice })
            Assert.That(session.TryPurchase(choice, 1000, out _), Is.False);
        Assert.That(session.TryPurchase(DayUpgrade.Cash, -1, out _), Is.False);
        Assert.That(session.Pending, Is.True);
        Assert.That(session.TrySkip(), Is.True);
    }

    [TestCase(3, 4)]
    [TestCase(6, 7)]
    public void PaidEnergyExtendsTheCurrentRoomCapacity(int baseline, int expected)
    {
        var session = new DayUpgradeSession();
        session.Offer();
        Assert.That(session.TryPurchase(DayUpgrade.Energy, 40, out int balance), Is.True);
        Assert.That(balance, Is.Zero);
        Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(expected));
        session.Restore(false, DayUpgrade.Energy);
        Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(expected));
        session.Reset();
        Assert.That(session.EnergyCapacityFor(baseline), Is.EqualTo(baseline));
    }
}
