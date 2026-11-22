using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class InvisibleHandDirectorTests
{
    [Test]
    public void TrailingAndLeadingRunsMoveInOppositeDirectionsWithinSafeCaps()
    {
        var hand = new InvisibleHandDirector();
        for (int i = 0; i < 20; i++)
            hand.Observe(100, 0, 220, 6, 1, 7, 0.5, 400, true);
        Assert.That(hand.Pressure, Is.EqualTo(1).Within(0.001));
        Assert.That(hand.RarityShift(100, 5), Is.EqualTo(5));
        Assert.That(hand.AdjustInitialOffer(10, 3), Is.EqualTo(13));
        for (int i = 0; i < 20; i++)
            hand.Observe(3000, 0, 220, 6, 1, 7, 0.5, 400, true);
        Assert.That(hand.Pressure, Is.EqualTo(-1).Within(0.001));
        Assert.That(hand.RarityShift(100, 5), Is.EqualTo(-5));
        Assert.That(hand.AdjustInitialOffer(-30, 3), Is.EqualTo(-30));
    }

    [Test]
    public void DayProgressAndExistingStockLimitUnnecessaryHelp()
    {
        var hand = new InvisibleHandDirector();
        hand.Observe(220, 0, 220, 1, 1, 7, 0, 400, true);
        Assert.That(hand.Pressure, Is.Zero);
        hand.Observe(100, 600, 220, 5, 1, 7, 0.5, 400, true);
        Assert.That(hand.Pressure, Is.Negative);
        hand.Observe(0, 0, 220, 5, 1, 7, 0.5, 400, false);
        Assert.That(hand.Pressure, Is.Zero);
    }

    [Test]
    public void SaveRestorePreservesPressureAndClampsInvalidState()
    {
        var hand = new InvisibleHandDirector();
        hand.Restore(0.65);
        Assert.That(hand.Pressure, Is.EqualTo(0.65));
        hand.Restore(double.NaN);
        Assert.That(hand.Pressure, Is.Zero);
        hand.Restore(20);
        Assert.That(hand.Pressure, Is.EqualTo(1));
    }
    [TestCase(5)]
    [TestCase(-5)]
    public void RarityTransferPreservesTotalAndNeverEnablesDisabledTiers(int shift)
    {
        var weights = new Dictionary<string, int> { ["common"] = 60, ["ordinary"] = 25,
            ["first_class"] = 10, ["exclusive"] = 4, ["unique"] = 0 };
        InvisibleHandDirector.ApplyRarityShift(weights, shift);
        Assert.That(weights.Values.Sum(), Is.EqualTo(99));
        Assert.That(weights["unique"], Is.Zero);
        Assert.That(weights.Where(entry => entry.Key != "unique").All(entry => entry.Value > 0), Is.True);
        Assert.That(weights["common"], Is.EqualTo(60 - shift));
    }
}
