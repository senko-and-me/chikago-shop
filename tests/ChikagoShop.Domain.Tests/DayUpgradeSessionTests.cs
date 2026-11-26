using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DayUpgradeSessionTests
{
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(14)]
    public void EligibleHandsStayDeterministicWithoutDuplicates(int day)
    {
        var rules = new DayUpgradeRules();
        foreach (bool music in new[] { false, true })
        {
            var expected = music ? new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice }
                : new[] { DayUpgrade.Cash, DayUpgrade.AtelierPrice };
            Assert.That(rules.Options(day, music), Is.EquivalentTo(expected));
            Assert.That(rules.Options(day, music), Is.EqualTo(rules.Options(day, music)));
        }
        Assert.That(rules.Options(day, training: true), Is.EqualTo(new[] { DayUpgrade.AtelierPrice }));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void RetiredCardSlotsRestoreAsNoEffect(int wireValue)
    {
        var session = new DayUpgradeSession();
        session.Restore(false, (DayUpgrade)wireValue, musicUnlocked: true);
        Assert.That(session.Selected, Is.EqualTo(DayUpgrade.None));
        Assert.That(session.Price("atelier", 100), Is.EqualTo(100));
    }

    [TestCase(DayUpgrade.AtelierPrice, "atelier", "music")]
    [TestCase(DayUpgrade.MusicPrice, "music", "atelier")]
    public void PriceCardBoostsOnlyTheMatchingCategoryAndExpires(DayUpgrade card, string workshop, string other)
    {
        var session = new DayUpgradeSession();
        session.Offer(8, musicUnlocked: true);
        Assert.That(session.TryPurchase(card, 100, out int after), Is.True);
        Assert.That(after, Is.EqualTo(100 - DayUpgradeSession.Cost(card)));
        Assert.That(session.Price(workshop, 25), Is.EqualTo(30));
        Assert.That(session.Price(other, 25), Is.EqualTo(25));
        session.Reset();
        Assert.That(session.Price(workshop, 25), Is.EqualTo(25));
    }
}
