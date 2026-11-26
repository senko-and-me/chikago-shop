using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class MoneyTests
{
    [Test]
    public void FailedSpendDoesNotChangeWallet()
    {
        var wallet = new Wallet(89);

        Assert.That(wallet.TrySpend(90), Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(89));
        wallet.Credit(1);
        Assert.That(wallet.TrySpend(90), Is.True);
        Assert.That(wallet.Balance, Is.Zero);
    }


    [TestCase(5, 10, 6)]
    [TestCase(15, 10, 17)]
    [TestCase(30, -20, 24)]
    [TestCase(36, 30, 47)]
    public void PriceUsesRoundHalfUp(int basePrice, int percent, int expected)
    {
        Assert.That(MoneyMath.ApplyPercentRoundHalfUp(basePrice, percent), Is.EqualTo(expected));
    }
}
