namespace ChikagoShop.Domain.Economy;

// Rewards shared by the runtime and the balance model.
public static class RunRewardRules
{
    public const double BuyerWaitDuration = 20;
    public const int FirstQuotaGift = 1000;

    public static int SpeedBonus(double seconds) => !double.IsFinite(seconds) || seconds <= 0 ? 0 :
        (int)Math.Ceiling(Math.Min(BuyerWaitDuration, seconds) / 4);

    public static int QualityBonus(string rank) => rank switch
    {
        "S" => 5, "A" => 4, "B" => 3, "C" => 2, _ => 0
    };
}
