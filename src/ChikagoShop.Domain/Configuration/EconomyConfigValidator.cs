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
        if (config.Days.DurationSeconds <= 0 || config.Days.DurationSeconds > 3600)
            throw new EconomyConfigException("Day duration is invalid.");

        RequireUniqueIds(config.Workshops.Select(value => value.Id), "workshop");
        RequireUniqueIds(config.Items.Select(value => value.Id), "item");
        RequireUniqueIds(config.Quotas.Select(value => value.Id), "quota");

        if (config.Workshops.Count == 0 || config.Workshops.Any(value =>
                value.SpinCost < 0))
            throw new EconomyConfigException("Workshops require non-negative crafting costs.");
        string[] workshopIds = config.Workshops.Select(value => value.Id).ToArray();
        if (config.Items.Any(item => !workshopIds.Contains(item.Workshop) || item.BasePrice <= 0 || item.Rerolls < 0) ||
            workshopIds.Any(id => config.Items.Count(item => item.Workshop == id) != 5))
            throw new EconomyConfigException("Every workshop must have five items with positive prices and valid reroll counts.");

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
