namespace ChikagoShop.Domain.Crafting;

public static class CraftQualityRank
{
    public const int BandSize = 20;
    internal const int MaxEarlyQuality = 3 * BandSize - 1;

    private static readonly string[] Ranks = ["D", "C", "B", "A", "S"];

    public static string FromQuality(int quality)
    {
        int band = Math.Clamp(quality / BandSize, 0, Ranks.Length - 1);
        return Ranks[band];
    }
}
