using ChikagoShop.Domain.Trading;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class BuyerAvailabilityRulesTests
{
    [TestCase(0, false, false, false)]
    [TestCase(-1, false, false, false)]
    [TestCase(1, false, false, true)]
    [TestCase(3, false, false, true)]
    [TestCase(0, true, false, true)]
    [TestCase(0, false, true, true)]
    [TestCase(0, true, true, true)]
    [TestCase(1, true, false, true)]
    [TestCase(1, false, true, true)]
    public void WaitingRequiresRemainingProductionEnergyOrUnsoldStock(int energy, bool stored, bool displayed, bool expected) =>
        Assert.That(BuyerAvailabilityRules.CanWait(energy, stored, displayed), Is.EqualTo(expected));
}
