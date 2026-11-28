using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class EmbroiderySessionTests
{
    private static readonly string[] ItemKinds = ["hat", "vest", "gloves", "matteo_hat", "lorgnette"];

    [TestCaseSource(nameof(ItemKinds))]
    public void SelectedItemHasDeterministicBoundedAndCompletableContour(string kind)
    {
        var session = new EmbroiderySession(kind);
        Assert.That(session.Points, Is.EqualTo(new EmbroiderySession(kind).Points));
        Assert.That(session.Points.Count, Is.EqualTo(kind == "lorgnette" ? 15 : 14));
        Assert.That(session.Points.All(p => p.X >= 25 && p.X <= 815 && p.Y >= 25 && p.Y <= 455), Is.True);
        Assert.That(session.Points.Max(p => p.X) - session.Points.Min(p => p.X), Is.GreaterThan(200));
        Assert.That(session.Points.Max(p => p.Y) - session.Points.Min(p => p.Y), Is.GreaterThan(100));
        session.Start();
        foreach (var point in session.Points)
        {
            session.Tick(0.25);
            Assert.That(session.Hit(point.X, point.Y), Is.True);
        }
        Assert.That(session.Complete, Is.True);
        Assert.That(session.State, Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Rank, Is.EqualTo("S"));
    }

    [Test]
    public void SelectedContoursHaveSimilarTravelAndWellSeparatedConsecutiveTargets()
    {
        var lengths = ItemKinds.Select(kind =>
        {
            var points = new EmbroiderySession(kind).Points;
            var segments = points.Zip(points.Skip(1), (a, b) =>
                Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2))).ToArray();
            Assert.That(segments.Min(), Is.GreaterThanOrEqualTo(30), kind);
            return segments.Sum();
        }).ToArray();
        Assert.That(lengths.Max() / lengths.Min(), Is.LessThanOrEqualTo(1.12));
    }

    [Test]
    public void GloveTargetsAreMirroredAcrossTheFabricCenter()
    {
        var points = new EmbroiderySession("gloves").Points;
        for (int i = 0; i < points.Count / 2; i++)
        {
            var left = points[i];
            var right = points[points.Count - 1 - i];
            Assert.That(left.X + right.X, Is.EqualTo(840));
            Assert.That(left.Y, Is.EqualTo(right.Y));
        }
    }

    [Test]
    public void EveryItemHasItsOwnGeometryAndUnknownKindsAreRejected()
    {
        for (int i = 0; i < ItemKinds.Length; i++)
        for (int j = i + 1; j < ItemKinds.Length; j++)
            Assert.That(new EmbroiderySession(ItemKinds[i]).Points,
                Is.Not.EqualTo(new EmbroiderySession(ItemKinds[j]).Points));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmbroiderySession("unknown"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmbroiderySession((string)null!));
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => new EmbroiderySession("hat", invalid));
        var boosted = new EmbroiderySession("hat", 12);
        Assert.That(boosted.Duration, Is.EqualTo(12));
        boosted.Start();
        boosted.Tick(10);
        Assert.That(boosted.State, Is.EqualTo(EmbroideryState.Playing));
        boosted.Tick(2);
        Assert.That(boosted.State, Is.EqualTo(EmbroideryState.Finished));
    }

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
