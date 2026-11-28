using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class RunRewardRulesTests
{
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

    [TestCase("S", 20)]
    [TestCase("A", 10)]
    [TestCase("B", 0)]
    [TestCase("C", -10)]
    [TestCase("D", -20)]
    [TestCase("unknown", 0)]
    public void QualityRewardFollowsTheCraftedCopy(string rank, int expected) =>
        Assert.That(RunRewardRules.QualityBonus(rank), Is.EqualTo(expected));
}
