namespace ChikagoShop.Domain.Trading;

public static class BuyerAvailabilityRules
{
    public static bool CanWait(bool dayClosed) => !dayClosed;

    // Legacy callers do not provide day state; energy and stock no longer gate buyers.
    public static bool CanWait(int remainingEnergy, bool hasStoredGoods, bool hasDisplayedGoods) => true;
}
