namespace ChikagoShop.Domain.Days;

public sealed class DayUpgradeRules
{
    // Keep the legacy Time enum value for old saves, but never offer that card.
    private static readonly DayUpgrade[] Available =
    [
        DayUpgrade.Cash, DayUpgrade.Energy, DayUpgrade.Atelier,
        DayUpgrade.Music, DayUpgrade.AtelierPrice, DayUpgrade.MusicPrice
    ];

    public IReadOnlyDictionary<DayUpgrade, int> Costs { get; init; } = new Dictionary<DayUpgrade, int>();
    public IReadOnlyDictionary<DayUpgrade, int> Weights { get; init; } = new Dictionary<DayUpgrade, int>();
    public bool WeightedSelection { get; init; }
    public int Seed { get; init; } = 1;
    public int Cost(DayUpgrade upgrade) => upgrade == DayUpgrade.Cash ? 0
        : Costs.TryGetValue(upgrade, out int cost) ? cost : DayUpgradeSession.Cost(upgrade);

    // Fixed day/seed retains a pending hand on Continue; locked-room cards cannot appear.
    public IReadOnlyList<DayUpgrade> Options(int day, bool musicUnlocked = false, bool training = false)
    {
        var eligible = Available.Where(card => (!training || card != DayUpgrade.Cash) &&
            (musicUnlocked || card is not (DayUpgrade.Music or DayUpgrade.MusicPrice))).ToArray();
        var pool = eligible.Where(card => Weight(card) > 0).ToList();
        if (!WeightedSelection || training && pool.Count == 0)
            return Enumerable.Range(0, 3).Select(offset => eligible[(day - 2 + offset) % eligible.Length]).ToArray();

        var random = new Random(unchecked(Seed * 397 ^ day));
        var result = new List<DayUpgrade>();
        while (result.Count < 3 && pool.Count > 0)
        {
            int ticket = random.Next(pool.Sum(Weight));
            var chosen = pool[0];
            foreach (var candidate in pool)
            {
                ticket -= Weight(candidate);
                if (ticket < 0) { chosen = candidate; break; }
            }
            result.Add(chosen);
            pool.Remove(chosen);
        }
        return result;
    }

    private int Weight(DayUpgrade upgrade) => Weights.GetValueOrDefault(upgrade, 1);
}
