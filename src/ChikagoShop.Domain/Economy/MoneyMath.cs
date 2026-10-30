namespace ChikagoShop.Domain.Economy;

public static class MoneyMath
{
    public static int ApplyPercentRoundHalfUp(int basePrice, int totalPercent)
    {
        if (basePrice <= 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (totalPercent <= -100) throw new ArgumentOutOfRangeException(nameof(totalPercent));

        decimal value = basePrice * (100m + totalPercent) / 100m;
        return checked((int)decimal.Round(value, 0, MidpointRounding.AwayFromZero));
    }
}
