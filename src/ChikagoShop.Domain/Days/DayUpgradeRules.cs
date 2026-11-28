namespace ChikagoShop.Domain.Days;

public sealed record CardTerms(int Cost, int Amount);

public sealed class DayUpgradeRules
{
    public static IReadOnlyList<DayUpgrade> Available { get; } = Array.AsReadOnly(new[]
    {
        DayUpgrade.Cash, DayUpgrade.Materials, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice,
        DayUpgrade.Patient, DayUpgrade.Bargaining, DayUpgrade.QualityMark, DayUpgrade.Loyal, DayUpgrade.QuickService
    });

    public static Dictionary<DayUpgrade, CardTerms> DefaultTerms() => new()
    {
        [DayUpgrade.Cash] = new(6, 15),
        [DayUpgrade.Materials] = new(5, 5),
        [DayUpgrade.AtelierPrice] = new(6, 6),
        [DayUpgrade.MusicPrice] = new(6, 6),
        [DayUpgrade.Patient] = new(9, 2),
        [DayUpgrade.Bargaining] = new(4, 30),
        [DayUpgrade.QualityMark] = new(6, 6),
        [DayUpgrade.Loyal] = new(5, 30),
        [DayUpgrade.QuickService] = new(4, 5),
    };

    public IReadOnlyDictionary<DayUpgrade, CardTerms> Terms { get; init; } = DefaultTerms();
    public int Seed { get; init; } = 1;
    public CardTerms For(DayUpgrade upgrade) => Available.Contains(upgrade)
        ? Terms[upgrade] : throw new ArgumentOutOfRangeException(nameof(upgrade));
    public int Cost(DayUpgrade upgrade) => For(upgrade).Cost;

    public IReadOnlyList<DayUpgrade> Options(int day, bool musicUnlocked = false, bool training = false)
    {
        var pool = Available.Where(card => (!training || card != DayUpgrade.Cash) &&
            (musicUnlocked || card != DayUpgrade.MusicPrice)).ToList();
        var random = new Random(unchecked(Seed * 397 ^ day));
        var result = new List<DayUpgrade>();
        while (result.Count < 3)
        {
            int index = random.Next(pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }
}
