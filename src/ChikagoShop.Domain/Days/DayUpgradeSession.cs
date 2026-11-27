namespace ChikagoShop.Domain.Days;

// Serialized enum values must remain stable; Energy and Time are retired, not renumbered.
public enum DayUpgrade { None, Cash, Energy, Time, Atelier, Music, AtelierPrice, MusicPrice }

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
    public int CashReward => 50;

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

    public static int Cost(DayUpgrade upgrade) => upgrade switch
    {
        DayUpgrade.Cash => 0,
        DayUpgrade.Atelier => 15,
        DayUpgrade.Music => 25,
        DayUpgrade.AtelierPrice => 10,
        DayUpgrade.MusicPrice => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade))
    };

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
        int atelierPriceLevel = 0, int musicPriceLevel = 0, bool musicUnlocked = false)
    {
        Day = Math.Max(2, day);
        MusicUnlocked = musicUnlocked;
        _training = false;
        // Permanent bonuses in legacy saves do not carry over to the new day-only system.
        Selected = Enum.IsDefined(selected) && selected is not (DayUpgrade.Time or DayUpgrade.Energy) ? selected : DayUpgrade.None;
        if (!MusicUnlocked && (Selected is DayUpgrade.Music or DayUpgrade.MusicPrice)) Selected = DayUpgrade.None;
        Pending = pending && Selected == DayUpgrade.None;
    }

    public int Price(string workshop, int baseline)
    {
        if (baseline < 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        bool boosted = (workshop == "atelier" && Selected == DayUpgrade.AtelierPrice) ||
                       (workshop == "music" && Selected == DayUpgrade.MusicPrice);
        return boosted
            ? checked((int)decimal.Round((decimal)baseline * 120 / 100, 0, MidpointRounding.AwayFromZero))
            : baseline;
    }

    public double CraftDuration(string workshop, double baseline)
    {
        if (!double.IsFinite(baseline) || baseline <= 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        bool boosted = (workshop == "atelier" && Selected == DayUpgrade.Atelier) ||
                       (workshop == "music" && Selected == DayUpgrade.Music);
        return boosted ? baseline * 1.2 : baseline;
    }

    // Kept for save/runtime compatibility; selected-item crafting no longer rolls rarity.
    public int Weight(string workshop, string rarity, int baseline, int commonBaseline) => baseline;
}
