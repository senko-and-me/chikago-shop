using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DaySessionTests
{
    [TestCase(0.25, 0.1, 0.15, false)]
    [TestCase(0.25, 0.25, 180, true)]
    [TestCase(0.25, 1, 180, true)]
    [TestCase(180, 180, 180, true)]
    [TestCase(180, 360, 180, true)]
    public void PracticeClockRefillsWithoutClosingOrRemovingEnergy(double remaining, double delta, double expected, bool refill)
    {
        var day = new DaySession(5, 3630);
        day.Restore(remaining, false, 180);
        Assert.That(day.TickRepeating(delta), Is.EqualTo(refill));
        Assert.That(day.RemainingSeconds, Is.EqualTo(expected).Within(0.000001));
        Assert.That(day.Duration, Is.EqualTo(180));
        Assert.That(day.IsClosed, Is.False);
        Assert.That(day.CanReceiveBuyers, Is.True);
        Assert.That(day.AvailableEnergy(3), Is.EqualTo(3));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void PracticeClockIgnoresInvalidTicks(double delta)
    {
        var day = new DaySession(5, 3630);
        day.Reset(180);
        Assert.That(day.TickRepeating(delta), Is.False);
        Assert.That(day.RemainingSeconds, Is.EqualTo(180));
        Assert.That(day.IsClosed, Is.False);
    }

    [Test]
    public void PracticeClockCanRefillRepeatedlyWhileOrdinaryClockStillExpires()
    {
        var day = new DaySession(5, 3630);
        day.Reset(180);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(day.TickRepeating(180), Is.True);
            Assert.That(day.IsClosed, Is.False);
        }
        day.Reset(60);
        Assert.That(day.Tick(60), Is.True);
        Assert.That(day.IsClosed, Is.True);
    }

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
