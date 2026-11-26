using ChikagoShop.Domain.Trading;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class BuyerAvailabilityRulesTests
{
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void WaitingOnlyDependsOnDayBeingOpen(bool dayClosed, bool expected) =>
        Assert.That(BuyerAvailabilityRules.CanWait(dayClosed), Is.EqualTo(expected));

    [Test]
    public void LegacyEnergyAndStockValuesNeverGateWaiting()
    {
        foreach (int energy in new[] { -1, 0, 1, 3 })
        foreach (bool stored in new[] { false, true })
        foreach (bool displayed in new[] { false, true })
            Assert.That(BuyerAvailabilityRules.CanWait(energy, stored, displayed), Is.True);
    }
}
