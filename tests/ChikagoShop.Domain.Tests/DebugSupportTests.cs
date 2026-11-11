using ChikagoShop.Domain.Crafting;
using ChikagoShop.Domain.Days;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DebugSupportTests
{
    [Test]
    public void DebugTimeStacksAndReopensWithoutChangingNormalResetLimits()
    {
        var day = new DaySession();
        day.AddTimeForTesting(30);
        day.AddTimeForTesting(30);
        Assert.That(day.RemainingSeconds, Is.EqualTo(150));
        day.Tick(150);
        Assert.That(day.IsClosed, Is.True);
        day.AddTimeForTesting(30);
        Assert.That(day.IsClosed, Is.False);
        Assert.That(day.RemainingSeconds, Is.EqualTo(30));
        day.AddTimeForTesting(double.NaN);
        day.AddTimeForTesting(-30);
        Assert.That(day.RemainingSeconds, Is.EqualTo(30));
        day.Reset();
        Assert.That(day.RemainingSeconds, Is.EqualTo(90));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void DebugCraftFinishProducesStableOutcomeOnlyAfterStarting(bool polishing, bool good)
    {
        ICraftSession session = polishing
            ? new PolishingSession(Enumerable.Repeat((byte)255, 49).ToArray(), 7, 7)
            : new EmbroiderySession();
        session.FinishForTesting(good);
        Assert.That(session.State, Is.EqualTo(EmbroideryState.Ready));
        session.Start();
        session.FinishForTesting(good);
        Assert.That(session.State, Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Complete, Is.EqualTo(good));
        Assert.That(session.Quality, Is.EqualTo(good ? 100 : 0));
        session.Tick(100);
        session.FinishForTesting(!good);
        Assert.That(session.Quality, Is.EqualTo(good ? 100 : 0));
    }
}
