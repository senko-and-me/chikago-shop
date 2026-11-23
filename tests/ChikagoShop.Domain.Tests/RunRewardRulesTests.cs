using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class RunRewardRulesTests
{
    [Test]
    public void BuyerHasTenSecondsAndKeepsTheFiveDollarMaximum() =>
        Assert.Multiple(() =>
        {
            Assert.That(RunRewardRules.BuyerWaitDuration, Is.EqualTo(10));
            Assert.That(RunRewardRules.SpeedBonus(10), Is.EqualTo(5));
        });

    [TestCase(0, 0)]
    [TestCase(0.01, 1)]
    [TestCase(2, 1)]
    [TestCase(2.01, 2)]
    [TestCase(8, 4)]
    [TestCase(8.01, 5)]
    [TestCase(10, 5)]
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
