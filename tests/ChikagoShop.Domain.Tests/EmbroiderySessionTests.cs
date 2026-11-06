using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EmbroiderySessionTests
{
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void OnlyOrderedHitsCompletePatternAndFinishIsTerminal(int pattern)
    {
        var session = new EmbroiderySession(pattern);
        Assert.That(session.Hit(150, 180), Is.False);
        session.Start();
        session.Tick(1);
        var later = session.Points[1];
        Assert.That(session.Hit(later.X, later.Y), Is.False);
        Assert.That(session.Misses, Is.EqualTo(1));
        foreach (var point in session.Points) Assert.That(session.Hit(point.X, point.Y), Is.True);
        Assert.That(session.Complete, Is.True);
        Assert.That(session.State, Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Quality, Is.EqualTo(87));
        session.Start(); session.Tick(99); session.Hit(0, 0);
        Assert.That(session.RemainingSeconds, Is.EqualTo(9));
        Assert.That(session.Misses, Is.EqualTo(1));
    }

    [Test]
    public void TimeoutPenalizesMissingStitchesAndIgnoresInvalidInput()
    {
        var session = new EmbroiderySession();
        session.Tick(5);
        Assert.That(session.RemainingSeconds, Is.EqualTo(10));
        session.Start();
        session.Tick(double.NaN); session.Tick(double.PositiveInfinity); session.Tick(-1);
        session.Hit(double.NaN, 0);
        Assert.That(session.Misses, Is.Zero);
        Assert.That(session.RemainingSeconds, Is.EqualTo(10));
        session.Tick(10);
        Assert.That(session.State, Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Quality, Is.EqualTo(19));
        Assert.That(session.Rank, Is.EqualTo("D"));
        Assert.That(session.Hit(150,180), Is.False);
    }

    [Test]
    public void HitRadiusAndQualityAreBounded()
    {
        var session = new EmbroiderySession(); session.Start();
        Assert.That(session.Hit(175,180), Is.False);
        Assert.That(session.Hit(174.9,180), Is.True);
        for (int i=0;i<100;i++) session.Hit(0,0);
        Assert.That(session.Quality, Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmbroiderySession(4));
    }
}
