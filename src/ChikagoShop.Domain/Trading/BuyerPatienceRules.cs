namespace ChikagoShop.Domain.Trading;

public static class BuyerPatienceRules
{
    public const int ExpiredPercent = -50;

    public static bool IsExpired(double remaining) => remaining <= 0;

    public static double RemainingAfterDayClosure(double remaining, bool dayClosed) =>
        dayClosed ? 0 : remaining;

    public static int OfferPercent(int normalPercent, double remaining) =>
        IsExpired(remaining) ? ExpiredPercent : normalPercent;

    public static bool CanReroll(bool active, bool dayClosed, int attempts, double remaining) =>
        !IsExpired(remaining) && TradeOfferRules.CanReroll(active, dayClosed, attempts);

    public static int TotalPayout(int offeredPrice, double remaining, int speedBonus, int qualityBonus) =>
        offeredPrice + qualityBonus + (IsExpired(remaining) ? 0 : speedBonus);
}
