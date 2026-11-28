namespace ChikagoShop.Domain.Economy;

// Rewards shared by the runtime and the balance model.
public static class RunRewardRules
{
    public const double BuyerWaitDuration = 20;

    public static int SpeedBonus(double seconds, double initialSeconds = BuyerWaitDuration)
    {
        if (!double.IsFinite(seconds) || !double.IsFinite(initialSeconds) || seconds <= 0 || initialSeconds <= 0)
            return 0;

        int bonus = 1;
        while (bonus < 5 && seconds > initialSeconds * (bonus / 5.0)) bonus++;
        return bonus;
    }

    public static int QualityBonus(string rank) => rank switch
    {
        "S" => 20, "A" => 10, "C" => -10, "D" => -20, _ => 0
    };
}
