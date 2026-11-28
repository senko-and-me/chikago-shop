namespace ChikagoShop.Domain.Configuration;

public sealed class EconomyConfig
{
    public int SchemaVersion { get; init; }
    public bool IsProvisional { get; init; }
    public int StartingMoney { get; init; }
    public DayConfig Days { get; init; } = new();
    public List<WorkshopConfig> Workshops { get; init; } = [];
    public List<ItemConfig> Items { get; init; } = [];
    public const int MaximumRerollsPerItem = 3;
    public int RerollsPerItem { get; init; } = MaximumRerollsPerItem;
    public int BuyerWaitMinSeconds { get; init; } = 15;
    public int BuyerWaitMaxSeconds { get; init; } = 20;
    public int CraftCost { get; init; } = 20;
    public int BasePrice { get; init; } = 50;
    public Dictionary<string, int> CraftCosts => Items.ToDictionary(item => item.Id, _ => CraftCost);
    public Dictionary<Days.DayUpgrade, Days.CardTerms> Cards { get; init; } = ChikagoShop.Domain.Days.DayUpgradeRules.DefaultTerms();
    public List<RarityConfig> Rarities { get; init; } = [];
    public List<OfferRangeConfig> OfferRanges { get; init; } = [];
    public List<QuotaConfig> Quotas { get; init; } = [];
}

public sealed class DayConfig
{
    public int DurationSeconds { get; init; } = (int)ChikagoShop.Domain.Days.DaySession.DurationSeconds;
    public int SecondWeekDurationSeconds { get; init; } = 60;
    public int DurationFor(int day, int firstQuotaDay) => day <= firstQuotaDay ? DurationSeconds : SecondWeekDurationSeconds;

    // Retained solely for old configuration/save readers; production has no energy budget.
    public int EnergyBeforeMusicUnlock { get; init; } = 3;
    public int EnergyAfterMusicUnlock { get; init; } = 6;
    public int ItemsPerCraft { get; init; } = 1;
    public int TotalDays { get; init; }
    public int TutorialDay { get; init; }
}

public sealed class WorkshopConfig
{
    public string Id { get; init; } = string.Empty;
    public int UnlockCost { get; init; }
}

public sealed class ItemConfig
{
    public string Id { get; init; } = string.Empty;
    public string Workshop { get; init; } = string.Empty;

    // Older economy files included an item price. Only the shared base price is used now.
    [System.Text.Json.Serialization.JsonPropertyName("base_price")]
    public int? LegacyBasePrice { init { } }
}

public sealed class RarityConfig
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Weight { get; init; }
    public Dictionary<string, int> BasePrices { get; init; } = [];
}

public sealed class OfferRangeConfig
{
    public int MinPercent { get; init; }
    public int MaxPercent { get; init; }
    public int ChancePercent { get; init; }
}

public sealed class QuotaConfig
{
    public string Id { get; init; } = string.Empty;
    public int DeadlineDay { get; init; }
    public int Amount { get; init; }
}
