using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class RunRewardRulesTests
{
    [TestCase(10)]
    [TestCase(12)]
    [TestCase(15)]
    [TestCase(20)]
    [TestCase(25)]
    public void SpeedRewardScalesWithOriginalPatience(double initial)
    {
        Assert.That(RunRewardRules.SpeedBonus(initial, initial), Is.EqualTo(5));
        Assert.That(RunRewardRules.SpeedBonus(initial * 0.8, initial), Is.EqualTo(4));
        Assert.That(RunRewardRules.SpeedBonus(initial * 0.4, initial), Is.EqualTo(2));
        Assert.That(RunRewardRules.SpeedBonus(0, initial), Is.Zero);
    }

    [Test]
    public void BuyerCategoriesHaveDistinctInclusivePatienceRanges()
    {
        Assert.That(RunRewardRules.BuyerWaitRange(false), Is.EqualTo((10, 20)));
        Assert.That(RunRewardRules.BuyerWaitRange(true), Is.EqualTo((15, 25)));
        Assert.That(RunRewardRules.SpeedBonus(double.NaN, 20), Is.Zero);
        Assert.That(RunRewardRules.SpeedBonus(10, double.PositiveInfinity), Is.Zero);
        Assert.That(RunRewardRules.SpeedBonus(10, 0), Is.Zero);
    }

    [TestCase(0, 0)]
    [TestCase(0.01, 1)]
    [TestCase(4, 1)]
    [TestCase(4.01, 2)]
    [TestCase(16, 4)]
    [TestCase(16.01, 5)]
    [TestCase(20, 5)]
    [TestCase(21, 5)]
    public void SpeedRewardUsesFiveBoundedBands(double seconds, int expected) =>
        Assert.That(RunRewardRules.SpeedBonus(seconds), Is.EqualTo(expected));

    [TestCase("S", 5)]
    [TestCase("A", 4)]
    [TestCase("B", 3)]
    [TestCase("C", 2)]
    [TestCase("D", 0)]
    [TestCase("unknown", 0)]
    public void QualityRewardFollowsTheCraftedCopy(string rank, int expected) =>
        Assert.That(RunRewardRules.QualityBonus(rank), Is.EqualTo(expected));
}
