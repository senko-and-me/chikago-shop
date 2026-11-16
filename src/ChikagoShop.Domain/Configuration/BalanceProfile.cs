using System.Text.Json;

namespace ChikagoShop.Domain.Configuration;

public sealed record BalanceField(string Key, string Section, string Label, double Default,
    double Min = 0, double Max = 100000, double Step = 1);

// A separate debug profile never changes the shipped economy file or the game's save format.
public sealed class BalanceProfile
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, double> Numbers { get; set; } = new();
    public Dictionary<string, string> ItemRarities { get; set; } = new();
    public double Number(string key, double fallback) => Numbers.GetValueOrDefault(key, fallback);
    public BalanceProfile Clone() => new()
    {
        Numbers = new(Numbers), ItemRarities = new(ItemRarities)
    };
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public static BalanceProfile FromJson(string json) => JsonSerializer.Deserialize<BalanceProfile>(json)
        ?? throw new ArgumentException("Профиль пуст.");

    public void Validate(IReadOnlyList<BalanceField> fields, IReadOnlySet<string> itemIds, IReadOnlySet<string> rarityIds)
    {
        if (SchemaVersion != 1 || Numbers == null || ItemRarities == null)
            throw new ArgumentException("Неизвестная версия или повреждённая структура профиля.");
        var definitions = fields.ToDictionary(field => field.Key);
        foreach (var (key, value) in Numbers)
        {
            if (!definitions.TryGetValue(key, out var field)) throw new ArgumentException($"Неизвестный параметр: {key}");
            if (!double.IsFinite(value) || value < field.Min || value > field.Max ||
                Math.Abs(value / field.Step - Math.Round(value / field.Step)) > 0.00001)
                throw new ArgumentException($"{field.Label}: значение должно быть от {field.Min} до {field.Max}, шаг {field.Step}.");
        }
        foreach (var (item, rarity) in ItemRarities)
            if (!itemIds.Contains(item) || !rarityIds.Contains(rarity))
                throw new ArgumentException($"Неизвестный предмет или редкость: {item} / {rarity}");
    }
}
