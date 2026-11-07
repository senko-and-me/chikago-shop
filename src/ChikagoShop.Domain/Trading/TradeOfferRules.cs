using ChikagoShop.Domain.Economy;

namespace ChikagoShop.Domain.Trading;

// Shared by the Godot adapter and the balance simulator.
public static class TradeOfferRules
{
    public const int MinPercent = -30;
    public const int MaxPercent = 30;
    public const double HigherRerollChance = 0.7;
    public static int InitialPercent(int ticket) => ticket is >= 0 and <= 60
        ? MinPercent + ticket : throw new ArgumentOutOfRangeException(nameof(ticket));
    public static int Price(int basePrice, int percent) => MoneyMath.ApplyPercentRoundHalfUp(basePrice, percent);
    public static int[] RerollOptions(int basePrice, int currentPrice, bool higher)
    {
        int[] Select(bool direction) => Enumerable.Range(MinPercent, MaxPercent - MinPercent + 1)
            .Where(percent => direction ? Price(basePrice, percent) > currentPrice : Price(basePrice, percent) < currentPrice).ToArray();
        var result = Select(higher);
        return result.Length == 0 ? Select(!higher) : result;
    }
}
