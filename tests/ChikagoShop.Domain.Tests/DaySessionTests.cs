using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DaySessionTests
{
    [TestCase(60, 12, false, false, 90)]
    [TestCase(30, 12, false, false, 45)]
    [TestCase(45, 12, true, false, 45)]
    [TestCase(60, 10, false, false, 45)]
    [TestCase(75, 10, true, true, 60)]
    [TestCase(45, 12, false, true, 60)]
    [TestCase(30, 13, false, false, 30)]
    [TestCase(0, 12, false, false, 0)]
    public void LegacyClockMigrationPreservesTheElapsedFraction(double remaining, int version, bool music, bool bonus, double expected)
    {
        Assert.That(DaySession.MigrateRemaining(remaining, version, music, bonus), Is.EqualTo(expected));
    }

    [Test]
    public void TimeoutClosesExactlyOnceAndDiscardsEnergy()
    {
        var day = new DaySession();
        Assert.That(day.RemainingSeconds, Is.EqualTo(90));
        Assert.That(day.AvailableEnergy(2), Is.EqualTo(2));
        Assert.That(day.Tick(89.75), Is.False);
        Assert.That(day.Tick(0.25), Is.True);
        Assert.That(day.IsClosed, Is.True);
        Assert.That(day.CanReceiveBuyers, Is.False);
        Assert.That(day.AvailableEnergy(2), Is.Zero);
        Assert.That(day.Tick(100), Is.False);
        Assert.That(day.RemainingSeconds, Is.Zero);
    }

    [Test]
    public void ClosingAllowsLastTradeBeforeExplicitAdvance()
    {
        var day = new DaySession();
        Assert.That(day.RequestEnd(tradeActive: true), Is.EqualTo(EndDayAction.Closed));
        Assert.That(day.RequestEnd(tradeActive: true), Is.EqualTo(EndDayAction.WaitForTrade));
        Assert.That(day.RequestEnd(tradeActive: false), Is.EqualTo(EndDayAction.Advance));
        Assert.That(day.IsClosed, Is.True);
        day.Reset();
        Assert.That(day.IsClosed, Is.False);
        Assert.That(day.RemainingSeconds, Is.EqualTo(90));
        Assert.That(day.RequestEnd(tradeActive: false), Is.EqualTo(EndDayAction.Closed));
    }

    [TestCase(null, false, 90, false)]
    [TestCase(12.5, false, 12.5, false)]
    [TestCase(12.5, true, 0, true)]
    [TestCase(0, false, 0, true)]
    [TestCase(-5, false, 0, true)]
    [TestCase(200, false, 90, false)]
    [TestCase(double.NaN, false, 90, false)]
    [TestCase(double.PositiveInfinity, false, 90, false)]
    public void RestoreHandlesOldAndInvalidSaves(double? remaining, bool closed, double expectedTime, bool expectedClosed)
    {
        var day = new DaySession();
        day.Restore(remaining, closed);
        Assert.That(day.RemainingSeconds, Is.EqualTo(expectedTime));
        Assert.That(day.IsClosed, Is.EqualTo(expectedClosed));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidTicksCannotChangeTime(double delta)
    {
        var day = new DaySession();
        Assert.That(day.Tick(delta), Is.False);
        Assert.That(day.RemainingSeconds, Is.EqualTo(90));
    }
}
