namespace ChikagoShop.Domain.Crafting;

// A rarity keeps its configured probability regardless of how many items it contains.
public sealed class RarityItemTable
{
    private readonly WeightedItemTable _table;
    public int TotalWeight => _table.TotalWeight;

    public RarityItemTable(IEnumerable<(int ItemIndex, string RarityId)> items,
        IReadOnlyDictionary<string, int> rarityWeights)
    {
        var groups = items.GroupBy(item => item.RarityId).ToArray();
        int multiplier = 1;
        foreach (var group in groups)
        {
            int count = group.Count(), a = multiplier, b = count;
            while (b != 0) (a, b) = (b, a % b);
            multiplier = checked(multiplier / a * count);
        }
        _table = new WeightedItemTable(groups.SelectMany(group => group.Select(item =>
            (item.ItemIndex, checked(rarityWeights[group.Key] * multiplier / group.Count())))));
    }

    public int Select(int ticket) => _table.Select(ticket);
}
