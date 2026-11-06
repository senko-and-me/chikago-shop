using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DaySessionTests
{
    [Test]
    public void TimeoutClosesExactlyOnceAndDiscardsEnergy()
    {
        var day = new DaySession();
        Assert.That(day.RemainingSeconds, Is.EqualTo(60));
        Assert.That(day.AvailableEnergy(2), Is.EqualTo(2));
        Assert.That(day.Tick(59.75), Is.False);
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
        Assert.That(day.RemainingSeconds, Is.EqualTo(60));
        Assert.That(day.RequestEnd(tradeActive: false), Is.EqualTo(EndDayAction.Closed));
    }

    [TestCase(null, false, 60, false)]
    [TestCase(12.5, false, 12.5, false)]
    [TestCase(12.5, true, 0, true)]
    [TestCase(0, false, 0, true)]
    [TestCase(-5, false, 0, true)]
    [TestCase(200, false, 60, false)]
    [TestCase(double.NaN, false, 60, false)]
    [TestCase(double.PositiveInfinity, false, 60, false)]
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
        Assert.That(day.RemainingSeconds, Is.EqualTo(60));
    }
}
