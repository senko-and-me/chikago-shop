namespace ChikagoShop.Domain.Configuration;

public static class EconomyConfigValidator
{
    public static void Validate(EconomyConfig config)
    {
        if (config.SchemaVersion != 1)
            throw new EconomyConfigException($"Unsupported economy schema: {config.SchemaVersion}.");
        if (config.StartingMoney < 0)
            throw new EconomyConfigException("Starting money cannot be negative.");
        if (config.Days.TotalDays <= 0 || config.Days.TutorialDay < 1 || config.Days.TutorialDay > config.Days.TotalDays)
            throw new EconomyConfigException("Day configuration is invalid.");

        RequireUniqueIds(config.Workshops.Select(value => value.Id), "workshop");
        RequireUniqueIds(config.Rarities.Select(value => value.Id), "rarity");
        RequireUniqueIds(config.Quotas.Select(value => value.Id), "quota");

        if (config.Workshops.Count == 0 || config.Workshops.Any(value => value.SpinCost < 0))
            throw new EconomyConfigException("Workshops require non-negative spin costs.");
        if (config.Rarities.Count == 0 || config.Rarities.Any(value => value.Rerolls <= 0))
            throw new EconomyConfigException("Rarities require positive rerolls.");
        ValidateRelativeWeights(config.Rarities.Select(value => value.Weight));

        string[] workshopIds = config.Workshops.Select(value => value.Id).ToArray();
        foreach (RarityConfig rarity in config.Rarities)
        {
            if (string.IsNullOrWhiteSpace(rarity.Name) || rarity.BasePrices.Count != workshopIds.Length ||
                workshopIds.Any(id => !rarity.BasePrices.TryGetValue(id, out int price) || price <= 0))
                throw new EconomyConfigException($"Rarity '{rarity.Id}' must define a positive price for every workshop.");
        }

        if (config.OfferRanges.Count == 0 || config.OfferRanges.Any(value =>
                value.MinPercent > value.MaxPercent || value.ChancePercent <= 0))
            throw new EconomyConfigException("Offer ranges are invalid.");
        ValidatePercentageDistribution(config.OfferRanges.Select(value => value.ChancePercent), "offer chances");

        if (config.Quotas.Count == 0 || config.Quotas.Any(value =>
                value.DeadlineDay < 1 || value.DeadlineDay > config.Days.TotalDays || value.Amount <= 0))
            throw new EconomyConfigException("Quotas require valid deadline days and positive amounts.");
        if (config.Quotas.Select(value => value.DeadlineDay).Distinct().Count() != config.Quotas.Count)
            throw new EconomyConfigException("Quota deadline days must be unique.");
    }

    public static void ValidateRelativeWeights(IEnumerable<int> weights)
    {
        int[] values = weights.ToArray();
        if (values.Length == 0 || values.Any(value => value <= 0))
            throw new EconomyConfigException("Relative weights must be non-empty and positive.");
    }

    private static void ValidatePercentageDistribution(IEnumerable<int> values, string name)
    {
        int[] distribution = values.ToArray();
        if (distribution.Length == 0 || distribution.Any(value => value <= 0) || distribution.Sum() != 100)
            throw new EconomyConfigException($"{name} must contain positive values summing to 100.");
    }

    private static void RequireUniqueIds(IEnumerable<string> ids, string kind)
    {
        string[] values = ids.ToArray();
        if (values.Length == 0 || values.Any(string.IsNullOrWhiteSpace) ||
            values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new EconomyConfigException($"Every {kind} requires a unique non-empty ID.");
    }
}
