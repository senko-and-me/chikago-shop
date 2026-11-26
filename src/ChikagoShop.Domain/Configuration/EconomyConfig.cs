namespace ChikagoShop.Domain.Configuration;

public sealed class EconomyConfig
{
    public int SchemaVersion { get; init; }
    public bool IsProvisional { get; init; }
    public int StartingMoney { get; init; }
    public DayConfig Days { get; init; } = new();
    public List<WorkshopConfig> Workshops { get; init; } = [];
    public List<ItemConfig> Items { get; init; } = [];
    public List<OfferRangeConfig> OfferRanges { get; init; } = [];
    public List<QuotaConfig> Quotas { get; init; } = [];
}

public sealed class DayConfig
{
    public int DurationSeconds { get; init; } = (int)ChikagoShop.Domain.Days.DaySession.DurationSeconds;
    public int TotalDays { get; init; }
    public int TutorialDay { get; init; }
}

public sealed class WorkshopConfig
{
    public string Id { get; init; } = string.Empty;
    public int SpinCost { get; init; }
}

public sealed class ItemConfig
{
    public string Id { get; init; } = string.Empty;
    public string Workshop { get; init; } = string.Empty;
    public int BasePrice { get; init; }
    public int Rerolls { get; init; }
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
