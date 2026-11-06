namespace ChikagoShop.Domain.Crafting;

// The caller supplies a random ticket, keeping selection deterministic and engine-independent.
public sealed class WeightedItemTable
{
    private readonly (int ItemIndex, int End)[] _entries;
    public int TotalWeight { get; }

    public WeightedItemTable(IEnumerable<(int ItemIndex, int Weight)> entries)
    {
        var result = new List<(int, int)>();
        var indices = new HashSet<int>();
        int total = 0;
        foreach (var entry in entries)
        {
            if (entry.ItemIndex < 0 || entry.Weight <= 0 || !indices.Add(entry.ItemIndex))
                throw new ArgumentException("Each item needs a distinct nonnegative index and positive weight.", nameof(entries));
            total = checked(total + entry.Weight);
            result.Add((entry.ItemIndex, total));
        }
        if (total == 0) throw new ArgumentException("A loot table cannot be empty.", nameof(entries));
        _entries = result.ToArray();
        TotalWeight = total;
    }

    public int Select(int ticket)
    {
        if (ticket < 0 || ticket >= TotalWeight) throw new ArgumentOutOfRangeException(nameof(ticket));
        foreach (var entry in _entries) if (ticket < entry.End) return entry.ItemIndex;
        throw new InvalidOperationException("Invalid loot table.");
    }
}
