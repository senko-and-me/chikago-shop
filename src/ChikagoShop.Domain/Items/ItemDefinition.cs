namespace ChikagoShop.Domain.Items;

public sealed record ItemDefinition
{
    public ItemDefinition(string id, string workshopId, string rarityId)
    {
        Id = RequireId(id, nameof(id));
        WorkshopId = RequireId(workshopId, nameof(workshopId));
        RarityId = RequireId(rarityId, nameof(rarityId));
    }

    public string Id { get; }
    public string WorkshopId { get; }
    public string RarityId { get; }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A stable ID cannot be empty.", parameterName);
        return value;
    }
}
