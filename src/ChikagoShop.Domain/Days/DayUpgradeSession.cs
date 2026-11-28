namespace ChikagoShop.Domain.Days;

// Keep retired upgrades in the serialized enum so older saves keep their original IDs.
public enum DayUpgrade { None, Cash, Energy, Time, Atelier, Music, AtelierPrice, MusicPrice, Materials, Patient, Bargaining, QualityMark, Loyal, QuickService }

// Every purchased upgrade is effective only during the upcoming day.
// Legacy level fields remain readable for save compatibility but no longer affect gameplay.
public sealed class DayUpgradeSession
{
    private bool _training;
    public bool Pending { get; private set; }
    public DayUpgrade Selected { get; private set; }
    public int Day { get; private set; } = 2;
    public bool MusicUnlocked { get; private set; }
    public int AtelierLevel => 0;
    public int MusicLevel => 0;
    public int AtelierPriceLevel => 0;
    public int MusicPriceLevel => 0;
    public DayUpgradeRules Rules { get; set; } = new();
    public IReadOnlyList<DayUpgrade> Options => Rules.Options(Day, MusicUnlocked, _training);
    public int CostFor(DayUpgrade upgrade) => Rules.Cost(upgrade);
    public int EnergyCapacity => EnergyCapacityFor(2);
    // Legacy callers and saves retain this API, but energy upgrades no longer have an effect.
    public int EnergyCapacityFor(int baseline) => baseline;
    public double Duration => DaySession.DurationSeconds;
    public int CashReward => Rules.For(DayUpgrade.Cash).Amount;
    public bool TrainingOffer => _training;
    public int CraftPrice(int baseline) => Selected == DayUpgrade.Materials
        ? Math.Max(0, baseline - Rules.For(Selected).Amount) : baseline;

    public int WaitExtension => Selected == DayUpgrade.Patient ? Rules.For(Selected).Amount : 0;
    public int ExpiredOffer(int percentage) => Selected == DayUpgrade.Loyal
        ? Math.Min(percentage, -Rules.For(Selected).Amount) : -50;

    public int SaleBonus(string workshop, string grade, double patience)
    {
        bool eligible = Selected switch
        {
            DayUpgrade.AtelierPrice => workshop == "atelier",
            DayUpgrade.MusicPrice => workshop == "music",
            DayUpgrade.QualityMark => grade is "B" or "A" or "S",
            DayUpgrade.QuickService => patience > 0,
            _ => false
        };
        if (!eligible) return 0;
        return Rules.For(Selected).Amount;
    }

    public void RefreshWorkshopAvailability(bool musicUnlocked) => MusicUnlocked = musicUnlocked;

    public void Offer(int day = 2, bool musicUnlocked = false, bool training = false)
    {
        _training = training;
        Day = Math.Max(2, day);
        MusicUnlocked = musicUnlocked;
        Pending = true;
        Selected = DayUpgrade.None;
    }

    public void Reset(bool preserveCategories = false)
    {
        _training = false;
        Pending = false;
        Selected = DayUpgrade.None;
        // preserveCategories is intentionally ignored: category bonuses no longer stack.
    }

    public static int Cost(DayUpgrade upgrade) => new DayUpgradeRules().Cost(upgrade);

    public bool TryPurchase(DayUpgrade upgrade, int balance, out int remainingBalance)
    {
        remainingBalance = balance;
        if (!Pending || !Options.Contains(upgrade)) return false;
        int cost = CostFor(upgrade);
        if (balance < cost) return false;
        remainingBalance = balance - cost;
        Selected = upgrade;
        Pending = false;
        return true;
    }

    public bool TrySkip()
    {
        if (!Pending) return false;
        Pending = false;
        Selected = DayUpgrade.None;
        return true;
    }

    public void Restore(bool pending, DayUpgrade selected, int day = 2, int atelierLevel = 0, int musicLevel = 0,
        int atelierPriceLevel = 0, int musicPriceLevel = 0, bool musicUnlocked = false, bool training = false)
    {
        Day = Math.Max(2, day);
        MusicUnlocked = musicUnlocked;
        _training = training;
        // Permanent bonuses in legacy saves do not carry over to the new day-only system.
        Selected = DayUpgradeRules.Available.Contains(selected) ? selected : DayUpgrade.None;
        if (!MusicUnlocked && Selected == DayUpgrade.MusicPrice) Selected = DayUpgrade.None;
        Pending = pending && Selected == DayUpgrade.None;
    }

    public int Price(string workshop, int baseline)
    {
        if (baseline < 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        return baseline;
    }

    public double CraftDuration(string workshop, double baseline)
    {
        if (!double.IsFinite(baseline) || baseline <= 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        return baseline;
    }

    // Kept for save/runtime compatibility; selected-item crafting no longer rolls rarity.
    public int Weight(string workshop, string rarity, int baseline, int commonBaseline) => baseline;
}
