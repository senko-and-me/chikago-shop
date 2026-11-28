namespace ChikagoShop.Domain.Configuration;

public static class EconomyConfigValidator
{
    public static void Validate(EconomyConfig config)
    {
        if (config.SchemaVersion != 1)
            throw new EconomyConfigException($"Unsupported economy schema: {config.SchemaVersion}.");
        if (config.StartingMoney < 0)
            throw new EconomyConfigException("Starting money cannot be negative.");
        if (config.RerollsPerItem != EconomyConfig.MaximumRerollsPerItem)
            throw new EconomyConfigException("The common reroll limit is out of range.");
        string[] craftKinds = ["hat", "vest", "gloves", "matteo_hat", "lorgnette", "guitar", "drum", "banjo", "violin", "tuba"];
        if (config.BasePrice is < 1 or > 100000)
            throw new EconomyConfigException("The shared base sale price is out of range.");
        if (config.CraftCost < 1 || config.CraftCost > 1000000)
            throw new EconomyConfigException("The shared crafting cost is out of range.");
        if (config.Cards == null || !config.Cards.Keys.ToHashSet().SetEquals(Days.DayUpgradeRules.Available) ||
            config.Cards.Values.Any(card => card == null || card.Cost is < 1 or > Days.DayUpgradeRules.MaximumCost ||
                card.Amount is < 0 or > 1000000) ||
            config.Cards[Days.DayUpgrade.Cash].Amount <= config.Cards[Days.DayUpgrade.Cash].Cost ||
            config.Cards[Days.DayUpgrade.Patient].Amount > 600 || config.Cards[Days.DayUpgrade.Loyal].Amount > 50)
            throw new EconomyConfigException("Invalid day card settings.");
        if (config.BuyerWaitMinSeconds < 1 || config.BuyerWaitMinSeconds > config.BuyerWaitMaxSeconds || config.BuyerWaitMaxSeconds > 600)
            throw new EconomyConfigException("Buyer wait must be an ordered range from 1 to 600 seconds.");
        if (config.Items == null || config.Items.Count != craftKinds.Length ||
            config.Items.Select(item => item.Id).Distinct().Count() != craftKinds.Length ||
            config.Items.Any(item => !craftKinds.Contains(item.Id) || item.Workshop is not ("atelier" or "music")))
            throw new EconomyConfigException("The ten selected items require valid identifiers and workshops.");
        if (config.Days.TotalDays <= 0 || config.Days.TutorialDay < 1 || config.Days.TutorialDay > config.Days.TotalDays)
            throw new EconomyConfigException("Day configuration is invalid.");
        if (config.Days.DurationSeconds is < 5 or > 3600 || config.Days.SecondWeekDurationSeconds is < 5 or > 3600 ||
            config.Days.ItemsPerCraft != 1)
            throw new EconomyConfigException("Day duration and single-item crafting configuration are invalid.");

        RequireUniqueIds(config.Workshops.Select(value => value.Id), "workshop");
        RequireUniqueIds(config.Rarities.Select(value => value.Id), "rarity");
        RequireUniqueIds(config.Quotas.Select(value => value.Id), "quota");

        if (config.Workshops.Count == 0 || config.Workshops.Any(value =>
                value.UnlockCost < 0))
            throw new EconomyConfigException("Workshops require non-negative unlock costs.");
        if (config.Rarities.Count == 0)
            throw new EconomyConfigException("At least one rarity is required.");
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
