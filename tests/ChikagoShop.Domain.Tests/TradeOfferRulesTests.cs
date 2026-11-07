using ChikagoShop.Domain.Trading;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class TradeOfferRulesTests
{
    [Test]
    public void InitialOffersCoverEveryPercentAndRejectInvalidTickets()
    {
        Assert.That(Enumerable.Range(0, 61).Select(TradeOfferRules.InitialPercent), Is.EqualTo(Enumerable.Range(-30, 61)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TradeOfferRules.InitialPercent(61));
    }

    [Test]
    public void RerollsCompareRoundedMoneyAndFallBackAtTheExtremes()
    {
        foreach (int price in new[] { 30, 45, 50, 120, 200, 300 })
        foreach (int percent in new[] { -30, -5, 0, 5, 30 })
        {
            int offer = TradeOfferRules.Price(price, percent);
            foreach (bool higher in new[] { false, true })
            {
                var options = TradeOfferRules.RerollOptions(price, offer, higher);
                Assert.That(options.Length, Is.GreaterThan(0));
                Assert.That(options.All(option => TradeOfferRules.Price(price, option) != offer), Is.True);
                bool higherExists = Enumerable.Range(-30, 61).Any(value => TradeOfferRules.Price(price, value) > offer);
                bool lowerExists = Enumerable.Range(-30, 61).Any(value => TradeOfferRules.Price(price, value) < offer);
                if (higher && higherExists) Assert.That(options.All(value => TradeOfferRules.Price(price, value) > offer), Is.True);
                if (!higher && lowerExists) Assert.That(options.All(value => TradeOfferRules.Price(price, value) < offer), Is.True);
            }
        }
        Assert.That(TradeOfferRules.Price(45, 10), Is.EqualTo(50));
        Assert.That(TradeOfferRules.Price(30, 15), Is.EqualTo(35));
    }
}
