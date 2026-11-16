namespace ChikagoShop.Domain.Days;

public sealed class DayUpgradeRules
{
    public IReadOnlyDictionary<DayUpgrade, int> Costs { get; init; } = new Dictionary<DayUpgrade, int>();
    public IReadOnlyDictionary<DayUpgrade, int> Weights { get; init; } = new Dictionary<DayUpgrade, int>();
    public bool WeightedSelection { get; init; }
    public int Seed { get; init; } = 1;
    public int Cost(DayUpgrade upgrade) => Costs.TryGetValue(upgrade, out int cost) ? cost : DayUpgradeSession.Cost(upgrade);

    // A fixed seed makes edited weights reproducible and keeps pending choices stable across Continue.
    public IReadOnlyList<DayUpgrade> Options(int day)
    {
        if (!WeightedSelection) return Enumerable.Range(0, 3)
            .Select(offset => (DayUpgrade)(1 + (day - 2 + offset) % 7)).ToArray();
        var pool = Enum.GetValues<DayUpgrade>().Where(value => value != DayUpgrade.None && Weight(value) > 0).ToList();
        if (pool.Count == 0) throw new InvalidOperationException("Хотя бы одна карта должна иметь положительный вес.");
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
