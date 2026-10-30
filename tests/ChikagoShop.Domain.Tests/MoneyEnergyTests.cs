using ChikagoShop.Domain.Economy;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class MoneyEnergyTests
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

    [Test]
    public void EnergyNeverDropsBelowZeroAndRestoresToCapacity()
    {
        var energy = new EnergyPool(1);

        Assert.That(energy.TrySpend(), Is.True);
        Assert.That(energy.TrySpend(), Is.False);
        Assert.That(energy.Available, Is.Zero);
        energy.SetCapacity(2, restore: true);
        Assert.That(energy.Available, Is.EqualTo(2));
        energy.TrySpend(2);
        energy.Restore();
        Assert.That(energy.Available, Is.EqualTo(2));
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
