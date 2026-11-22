namespace ChikagoShop.Domain.Trading;

public static class BuyerAvailabilityRules
{
    // Stock remains sellable without energy; an empty shop can await crafting only while energy remains.
    public static bool CanWait(int remainingEnergy, bool hasStoredGoods, bool hasDisplayedGoods) =>
        remainingEnergy > 0 || hasStoredGoods || hasDisplayedGoods;
}
