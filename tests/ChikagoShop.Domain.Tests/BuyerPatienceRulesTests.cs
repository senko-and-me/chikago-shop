using ChikagoShop.Domain.Trading;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class BuyerPatienceRulesTests
{
    [TestCase(0.01, 20)]
    [TestCase(0, -50)]
    [TestCase(-1, -50)]
    public void OfferChangesOnlyWhenPatienceRunsOut(double remaining, int expected) =>
        Assert.That(BuyerPatienceRules.OfferPercent(20, remaining), Is.EqualTo(expected));

    [TestCase(true, false, 3, 0.01, true)]
    [TestCase(true, false, 3, 0, false)]
    [TestCase(true, false, 3, -1, false)]
    [TestCase(false, false, 3, 20, false)]
    [TestCase(true, true, 3, 20, false)]
    [TestCase(true, false, 0, 20, false)]
    public void RerollRequiresPatienceAndNormalTradePermissions(bool active, bool closed, int attempts, double remaining, bool expected) =>
        Assert.That(BuyerPatienceRules.CanReroll(active, closed, attempts, remaining), Is.EqualTo(expected));

    [TestCase(100, 50)]
    [TestCase(101, 51)]
    public void ExpiredOfferIsHalfUsingExistingMoneyRounding(int normalPrice, int expected) =>
        Assert.That(TradeOfferRules.Price(normalPrice, BuyerPatienceRules.OfferPercent(30, 0)), Is.EqualTo(expected));

    [TestCase(0.01, 56)]
    [TestCase(0, 50)]
    [TestCase(-1, 50)]
    public void ExpiredPayoutCannotExceedHalfThroughTips(double remaining, int expected) =>
        Assert.That(BuyerPatienceRules.TotalPayout(50, remaining, 1, 5), Is.EqualTo(expected));
}
